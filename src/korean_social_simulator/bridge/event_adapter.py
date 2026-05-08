from __future__ import annotations

from datetime import datetime
from typing import cast

from pydantic import ValidationError

from korean_social_simulator.bridge_schema import BridgeEnvelope, StructuredError
from korean_social_simulator.models import SimulationEvent

_EXPLICIT_BRIDGE_TYPES = frozenset(
    {
        "agent.dialogue",
        "agent.emotion",
        "agent.move",
        "conflict.update",
        "group.update",
        "physics.result",
    }
)


class SimulationEventAdapter:
    """Convert Argus simulation events into versioned bridge envelopes."""

    def __init__(
        self,
        schema_version: str = "1.0.0",
        session_id: str | None = None,
        start_sequence: int = 0,
    ) -> None:
        self._schema_version = schema_version
        self._session_id = session_id
        self._next_sequence = start_sequence
        self._agent_position_indices: dict[str, int] = {}

    def adapt_events(self, events: list[SimulationEvent]) -> list[BridgeEnvelope]:
        """Adapt a sequence of events with monotonic bridge sequence numbers."""
        return [self.adapt_event(event) for event in events]

    def adapt_event(self, event: SimulationEvent) -> BridgeEnvelope:
        """Adapt one event, returning an adapter error envelope for unsupported shapes."""
        sequence = self._allocate_sequence()
        bridge_type = event.payload.get("bridge_type")
        if bridge_type is not None:
            return self._adapt_explicit_bridge_payload(event, sequence, bridge_type)

        if event.event_type == "observation":
            return self._adapt_observation(event, sequence)
        if event.event_type == "system":
            return self._adapt_generic_event(event, sequence, "simulation.event")
        if event.event_type == "metric_hook":
            target_type = (
                "replay.status"
                if event.payload.get("phase") == "turn_limit_reached"
                else "simulation.event"
            )
            return self._adapt_generic_event(event, sequence, target_type)

        return self._adapter_error(
            event,
            sequence,
            f"Unsupported simulation event type for bridge adapter: {event.event_type}",
        )

    def _adapt_explicit_bridge_payload(
        self,
        event: SimulationEvent,
        sequence: int,
        bridge_type: object,
    ) -> BridgeEnvelope:
        if not isinstance(bridge_type, str) or bridge_type not in _EXPLICIT_BRIDGE_TYPES:
            return self._adapter_error(event, sequence, "Unsupported explicit bridge_type.")
        bridge_payload = event.payload.get("bridge_payload")
        if not isinstance(bridge_payload, dict):
            return self._adapter_error(
                event, sequence, "Explicit bridge_payload must be a mapping."
            )
        return self._build_envelope(
            event=event,
            sequence=sequence,
            message_type=bridge_type,
            payload=cast(dict[str, object], bridge_payload),
        )

    def _adapt_observation(self, event: SimulationEvent, sequence: int) -> BridgeEnvelope:
        if not event.actor_id:
            return self._adapter_error(event, sequence, "Observation event is missing actor_id.")

        display_name = event.payload.get("display_name")
        if not isinstance(display_name, str) or not display_name:
            return self._adapter_error(
                event, sequence, "Observation event is missing display_name."
            )

        position = self._position_for_agent(event.actor_id)
        return self._build_envelope(
            event=event,
            sequence=sequence,
            message_type="agent.spawn",
            payload={
                "agent": {
                    "agent_id": event.actor_id,
                    "display_name": display_name,
                    "group_id": None,
                    "position": position,
                    "facing": 0.0,
                    "emotion": {"label": "neutral", "intensity": 0.0},
                    "current_action": "idle",
                    "visible": True,
                },
                "spawn_reason": "lazy_spawn",
            },
        )

    def _adapt_generic_event(
        self,
        event: SimulationEvent,
        sequence: int,
        message_type: str,
    ) -> BridgeEnvelope:
        return self._build_envelope(
            event=event,
            sequence=sequence,
            message_type=message_type,
            payload={
                "source_event": self._source_metadata(event),
                "payload": event.payload,
            },
        )

    def _build_envelope(
        self,
        event: SimulationEvent,
        sequence: int,
        message_type: str,
        payload: dict[str, object],
    ) -> BridgeEnvelope:
        envelope_payload = {
            "schema_version": self._schema_version,
            "message_id": self._message_id(event, sequence),
            "correlation_id": self._correlation_id(event),
            "session_id": self._session_id or event.run_id,
            "sequence": sequence,
            "sent_at_ms": _timestamp_to_ms(event.timestamp, fallback=sequence),
            "type": message_type,
            "payload": payload,
        }
        try:
            return BridgeEnvelope.model_validate(envelope_payload)
        except ValidationError as exc:
            return self._adapter_error(event, sequence, f"Bridge payload validation failed: {exc}")

    def _adapter_error(
        self,
        event: SimulationEvent,
        sequence: int,
        message: str,
    ) -> BridgeEnvelope:
        error = StructuredError(
            error_id=f"adapter-error-{sequence}",
            source="adapter",
            severity="error",
            message=message,
            recoverable=True,
            correlation_id=self._correlation_id(event),
            details={"source_event": self._source_metadata(event)},
        )
        return BridgeEnvelope.model_validate(
            {
                "schema_version": self._schema_version,
                "message_id": self._message_id(event, sequence, suffix="adapter-error"),
                "correlation_id": self._correlation_id(event),
                "session_id": self._session_id or event.run_id,
                "sequence": sequence,
                "sent_at_ms": _timestamp_to_ms(event.timestamp, fallback=sequence),
                "type": "adapter.error",
                "payload": error.model_dump(mode="json"),
            }
        )

    def _position_for_agent(self, agent_id: str) -> dict[str, float]:
        if agent_id not in self._agent_position_indices:
            self._agent_position_indices[agent_id] = len(self._agent_position_indices)
        index = self._agent_position_indices[agent_id]
        return {
            "x": float((index % 5) * 2),
            "y": 0.6,
            "z": float((index // 5) * 2),
        }

    def _source_metadata(self, event: SimulationEvent) -> dict[str, object]:
        return {
            "run_id": event.run_id,
            "turn": event.turn,
            "event_type": event.event_type,
            "actor_id": event.actor_id,
            "timestamp": event.timestamp,
        }

    def _message_id(
        self,
        event: SimulationEvent,
        sequence: int,
        suffix: str | None = None,
    ) -> str:
        parts = [
            event.run_id,
            str(event.turn),
            str(sequence),
            event.event_type,
            event.actor_id or "system",
        ]
        if suffix is not None:
            parts.append(suffix)
        return ":".join(parts)

    def _correlation_id(self, event: SimulationEvent) -> str:
        return ":".join(
            [
                event.run_id,
                str(event.turn),
                event.event_type,
                event.actor_id or "system",
            ]
        )

    def _allocate_sequence(self) -> int:
        sequence = self._next_sequence
        self._next_sequence += 1
        return sequence


def adapt_events(
    events: list[SimulationEvent],
    session_id: str | None = None,
    schema_version: str = "1.0.0",
) -> list[BridgeEnvelope]:
    """Adapt events using a fresh adapter instance."""
    return SimulationEventAdapter(
        schema_version=schema_version, session_id=session_id
    ).adapt_events(events)


def _timestamp_to_ms(timestamp: str, fallback: int) -> int:
    try:
        normalized_timestamp = timestamp.replace("Z", "+00:00")
        parsed = datetime.fromisoformat(normalized_timestamp)
    except ValueError:
        return fallback
    return int(parsed.timestamp() * 1000)
