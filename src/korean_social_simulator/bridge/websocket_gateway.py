from __future__ import annotations

import json
from collections.abc import Mapping
from typing import Any

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from pydantic import ValidationError

from korean_social_simulator.bridge.client_registry import ClientRegistry
from korean_social_simulator.bridge.replay_controller import ReplayControlError, ReplayController
from korean_social_simulator.bridge_schema import BridgeEnvelope, StructuredError
from korean_social_simulator.config.models import BridgeConfig

_UNITY_HANDSHAKE_TYPE = "unity.ready"
_UNITY_INBOUND_TYPES = frozenset(
    {
        _UNITY_HANDSHAKE_TYPE,
        "unity.ack",
        "unity.error",
        "observer.pause",
        "observer.resume",
        "observer.step",
        "observer.select_agent",
        "observer.camera_state",
    }
)


class _BridgeWebSocketClosed(Exception):
    """Internal signal for a server-initiated WebSocket close."""


def register_unity_websocket(
    app: FastAPI,
    config: BridgeConfig,
    registry: ClientRegistry,
    replay_controller: ReplayController,
) -> None:
    """Register the Unity WebSocket endpoint on the bridge app."""

    @app.websocket("/ws/unity")
    async def unity_websocket(websocket: WebSocket) -> None:
        await websocket.accept()
        await registry.connect(websocket)
        try:
            while True:
                raw_message = await websocket.receive_text()
                envelope = await _parse_inbound_message(
                    websocket=websocket,
                    raw_message=raw_message,
                    config=config,
                    registry=registry,
                )
                if envelope is None:
                    continue
                if envelope.type == _UNITY_HANDSHAKE_TYPE:
                    registry.mark_ready(envelope.session_id)
                    await _send_bridge_ready(websocket, config, registry, envelope)
                    continue
                if envelope.type not in _UNITY_INBOUND_TYPES:
                    await _send_error(
                        websocket=websocket,
                        config=config,
                        registry=registry,
                        message="Unsupported Unity bridge message type.",
                        correlation_id=envelope.message_id,
                        session_id=envelope.session_id,
                        details={"message_type": envelope.type},
                    )
                    continue

                dispatched = await _dispatch_unity_session_message(
                    websocket=websocket,
                    config=config,
                    registry=registry,
                    replay_controller=replay_controller,
                    envelope=envelope,
                )
                if not dispatched:
                    await _send_error(
                        websocket=websocket,
                        config=config,
                        registry=registry,
                        message="Unhandled Unity bridge message routing.",
                        correlation_id=envelope.message_id,
                        session_id=envelope.session_id,
                        details={"message_type": envelope.type},
                    )
        except WebSocketDisconnect:
            registry.disconnect("client_disconnect")
        except _BridgeWebSocketClosed:
            registry.disconnect("server_disconnect")
        except Exception:
            registry.disconnect("server_error")
            raise


async def _dispatch_unity_session_message(
    *,
    websocket: WebSocket,
    config: BridgeConfig,
    registry: ClientRegistry,
    replay_controller: ReplayController,
    envelope: BridgeEnvelope,
) -> bool:
    """Return True when fully handled (including ignored ack telemetry)."""
    if envelope.type in ("unity.ack", "unity.error"):
        return True

    if envelope.type == "observer.select_agent":
        raw = envelope.payload.get("agent_id")
        if isinstance(raw, str) and raw.strip():
            registry.set_observer_selection(raw.strip())
        else:
            registry.set_observer_selection(None)
        return True

    if envelope.type == "observer.camera_state":
        snapshot = dict(envelope.payload)
        registry.record_observer_camera(snapshot)
        return True

    extra: dict[str, object] = {"observer_action": envelope.type}

    if envelope.type == "observer.pause":
        replay_controller.pause()
        await _send_replay_observer_status(
            websocket=websocket,
            config=config,
            registry=registry,
            inbound=envelope,
            replay_controller=replay_controller,
            extra=extra,
        )
        return True

    if envelope.type == "observer.resume":
        try:
            replay_controller.resume()
        except ReplayControlError as exc:
            await _send_error(
                websocket=websocket,
                config=config,
                registry=registry,
                message=str(exc),
                correlation_id=envelope.message_id,
                session_id=envelope.session_id,
                details={"observer_action": envelope.type},
            )
            return True
        await _send_replay_observer_status(
            websocket=websocket,
            config=config,
            registry=registry,
            inbound=envelope,
            replay_controller=replay_controller,
            extra=extra,
        )
        return True

    if envelope.type == "observer.step":
        try:
            stepped = replay_controller.step()
        except ReplayControlError as exc:
            await _send_error(
                websocket=websocket,
                config=config,
                registry=registry,
                message=str(exc),
                correlation_id=envelope.message_id,
                session_id=envelope.session_id,
                details={"observer_action": envelope.type},
            )
            return True
        extra["stepped_event"] = stepped.model_dump(mode="json") if stepped is not None else None
        await _send_replay_observer_status(
            websocket=websocket,
            config=config,
            registry=registry,
            inbound=envelope,
            replay_controller=replay_controller,
            extra=extra,
        )
        return True

    return False


async def _send_replay_observer_status(
    *,
    websocket: WebSocket,
    config: BridgeConfig,
    registry: ClientRegistry,
    inbound: BridgeEnvelope,
    replay_controller: ReplayController,
    extra: dict[str, object],
) -> None:
    sequence = registry.next_sequence()
    payload: dict[str, object] = {"replay": replay_controller.status()}
    payload.update(extra)
    envelope = BridgeEnvelope(
        schema_version=config.schema_config.version,
        message_id=f"replay-status-{sequence}",
        correlation_id=inbound.message_id,
        session_id=inbound.session_id,
        sequence=sequence,
        sent_at_ms=0,
        type="replay.status",
        payload=payload,
    )
    await websocket.send_json(envelope.model_dump(mode="json"))


async def _parse_inbound_message(
    *,
    websocket: WebSocket,
    raw_message: str,
    config: BridgeConfig,
    registry: ClientRegistry,
) -> BridgeEnvelope | None:
    if len(raw_message.encode("utf-8")) > config.server.max_payload_bytes:
        await _send_error(
            websocket=websocket,
            config=config,
            registry=registry,
            message="Unity bridge payload exceeds configured maximum size.",
            details={"max_payload_bytes": config.server.max_payload_bytes},
        )
        await websocket.close(code=1009)
        raise _BridgeWebSocketClosed

    try:
        raw_payload = json.loads(raw_message)
    except json.JSONDecodeError as exc:
        await _send_error(
            websocket=websocket,
            config=config,
            registry=registry,
            message="Unity bridge payload is not valid JSON.",
            details={"error": str(exc)},
        )
        return None

    if not isinstance(raw_payload, Mapping):
        await _send_error(
            websocket=websocket,
            config=config,
            registry=registry,
            message="Unity bridge payload must be a JSON object.",
            details={"payload_type": type(raw_payload).__name__},
        )
        return None

    try:
        envelope = BridgeEnvelope.model_validate(raw_payload)
    except ValidationError as exc:
        await _send_error(
            websocket=websocket,
            config=config,
            registry=registry,
            message="Unity bridge envelope validation failed.",
            correlation_id=_optional_string(raw_payload.get("message_id")),
            session_id=_optional_string(raw_payload.get("session_id")),
            details={"errors": exc.errors()},
        )
        return None

    if envelope.type not in _UNITY_INBOUND_TYPES:
        await _send_error(
            websocket=websocket,
            config=config,
            registry=registry,
            message="Unity bridge message type is not accepted from Unity.",
            correlation_id=envelope.message_id,
            session_id=envelope.session_id,
            details={"message_type": envelope.type},
        )
        return None

    return envelope


async def _send_bridge_ready(
    websocket: WebSocket,
    config: BridgeConfig,
    registry: ClientRegistry,
    inbound: BridgeEnvelope,
) -> None:
    sequence = registry.next_sequence()
    envelope = BridgeEnvelope(
        schema_version=config.schema_config.version,
        message_id=f"bridge-ready-{sequence}",
        correlation_id=inbound.message_id,
        session_id=inbound.session_id,
        sequence=sequence,
        sent_at_ms=0,
        type="bridge.ready",
        payload={
            "require_ack": config.unity.require_ack,
            "max_payload_bytes": config.server.max_payload_bytes,
        },
    )
    await websocket.send_json(envelope.model_dump(mode="json"))


async def _send_error(
    *,
    websocket: WebSocket,
    config: BridgeConfig,
    registry: ClientRegistry,
    message: str,
    correlation_id: str | None = None,
    session_id: str | None = None,
    details: dict[str, Any] | None = None,
) -> None:
    sequence = registry.next_sequence()
    error_id = f"bridge-error-{sequence}"
    error = StructuredError(
        error_id=error_id,
        source="bridge",
        severity="error",
        message=message,
        recoverable=True,
        correlation_id=correlation_id,
        details=details or {},
    )
    envelope = BridgeEnvelope(
        schema_version=config.schema_config.version,
        message_id=error_id,
        correlation_id=correlation_id,
        session_id=session_id or "bridge",
        sequence=sequence,
        sent_at_ms=0,
        type="bridge.error",
        payload=error.model_dump(mode="json"),
    )
    await websocket.send_json(envelope.model_dump(mode="json"))


def _optional_string(value: object) -> str | None:
    return value if isinstance(value, str) and value else None
