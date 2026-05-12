from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.bridge import SimulationEventAdapter, adapt_events
from korean_social_simulator.models import SimulationEvent

_FIXTURE_DIR = Path("tests/golden/bridge")


def _event(
    event_type: str,
    payload: dict[str, object],
    actor_id: str | None = None,
    turn: int = 1,
) -> SimulationEvent:
    return SimulationEvent(
        run_id="run-001",
        turn=turn,
        event_type=event_type,
        actor_id=actor_id,
        timestamp="1970-01-01T00:00:01+00:00",
        payload=payload,
    )


def test_observation_maps_to_agent_spawn() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event(
            "observation",
            {"phase": "observation", "display_name": "Agent One", "language": "ko"},
            actor_id="agent-001",
        )
    )

    assert envelope.type == "agent.spawn"
    assert envelope.sequence == 0
    assert envelope.sent_at_ms == 1000
    assert envelope.payload["spawn_reason"] == "lazy_spawn"
    agent = envelope.payload["agent"]
    assert isinstance(agent, dict)
    assert agent["agent_id"] == "agent-001"
    assert agent["position"] == {"x": 0.0, "y": 0.0, "z": 0.0}


def test_observation_position_is_reused_for_same_agent() -> None:
    adapter = SimulationEventAdapter()
    first = adapter.adapt_event(
        _event(
            "observation",
            {"phase": "observation", "display_name": "Agent One", "language": "ko"},
            actor_id="agent-001",
        )
    )
    second = adapter.adapt_event(
        _event(
            "observation",
            {"phase": "observation", "display_name": "Agent One", "language": "ko"},
            actor_id="agent-001",
            turn=2,
        )
    )

    first_agent = first.payload["agent"]
    second_agent = second.payload["agent"]
    assert isinstance(first_agent, dict)
    assert isinstance(second_agent, dict)
    assert first_agent["position"] == second_agent["position"]
    assert second.sequence == 1


def test_system_event_maps_to_generic_simulation_event() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event("system", {"phase": "turn_start", "dry_run": True, "plan_id": "plan-001"})
    )

    assert envelope.type == "simulation.event"
    assert envelope.payload["payload"] == {
        "phase": "turn_start",
        "dry_run": True,
        "plan_id": "plan-001",
    }
    source_event = envelope.payload["source_event"]
    assert isinstance(source_event, dict)
    assert source_event["event_type"] == "system"


def test_generic_simulation_event_redacts_private_persona_fields() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event(
            "system",
            {
                "phase": "persona_selection",
                "selected_personas": [
                    {
                        "agent_id": "agent-001",
                        "persona_uuid": "private-uuid",
                        "display_name": "Agent One",
                        "memory_seeds": ["private memory"],
                        "reason": "Public reason.",
                    }
                ],
            },
        )
    )

    payload_text = json.dumps(envelope.payload, ensure_ascii=False)
    assert "private-uuid" not in payload_text
    assert "memory_seeds" not in payload_text
    assert "Agent One" in payload_text


def test_metric_hook_turn_limit_maps_to_replay_status() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event("metric_hook", {"phase": "turn_limit_reached", "max_turns": 5})
    )

    assert envelope.type == "replay.status"


def test_explicit_dialogue_payload_maps_to_declared_bridge_type() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event(
            "agent_action",
            {
                "bridge_type": "agent.dialogue",
                "bridge_payload": {
                    "speaker_id": "agent-001",
                    "target_ids": ["agent-002"],
                    "text": "Hello",
                    "emotion": {"label": "happy", "intensity": 0.5},
                    "speech_act": "say",
                    "duration_ms": 1500,
                },
            },
            actor_id="agent-001",
        )
    )

    assert envelope.type == "agent.dialogue"
    assert envelope.payload["speaker_id"] == "agent-001"


def test_explicit_behavior_payload_maps_to_declared_bridge_type() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event(
            "agent_action",
            {
                "bridge_type": "agent.behavior",
                "bridge_payload": {
                    "agent_id": "agent-001",
                    "intent": "argue",
                    "target_position": {"x": 1.0, "y": 0.0, "z": 2.0},
                    "locomotion": "walk",
                    "emotion": {"label": "angry", "intensity": 0.8},
                    "animation_hint": "argue",
                    "urgency": 0.8,
                    "duration_ms": 1800,
                    "public_reason": "Persona stance=opposes, confidence=0.80.",
                    "safety_tags": ["non_graphic", "synthetic_persona"],
                },
            },
            actor_id="agent-001",
        )
    )

    assert envelope.type == "agent.behavior"
    assert envelope.payload["intent"] == "argue"


def test_explicit_environment_payload_maps_to_declared_bridge_type() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event(
            "system",
            {
                "bridge_type": "environment.load",
                "bridge_payload": {
                    "background_id": "schoolroom",
                    "display_name": "Schoolroom",
                    "spawn_capacity": 20,
                    "bounds_min": {"x": -10.0, "y": 0.0, "z": -10.0},
                    "bounds_max": {"x": 10.0, "y": 4.0, "z": 10.0},
                    "camera_preset": "simulation_free",
                    "lighting_preset": "classroom",
                    "public_summary": "Public environment metadata only.",
                },
            },
        )
    )

    assert envelope.type == "environment.load"
    assert envelope.payload["background_id"] == "schoolroom"


def test_unsupported_event_maps_to_adapter_error() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event("agent_action", {"phase": "unknown_action"}, actor_id="agent-001")
    )

    assert envelope.type == "adapter.error"
    assert "Unsupported simulation event type" in str(envelope.payload["message"])


def test_invalid_explicit_bridge_payload_maps_to_adapter_error() -> None:
    envelope = SimulationEventAdapter().adapt_event(
        _event(
            "agent_action",
            {
                "bridge_type": "agent.dialogue",
                "bridge_payload": {"speaker_id": "agent-001"},
            },
            actor_id="agent-001",
        )
    )

    assert envelope.type == "adapter.error"
    assert "Bridge payload validation failed" in str(envelope.payload["message"])


def test_adapt_events_allocates_deterministic_sequences() -> None:
    envelopes = adapt_events(
        [
            _event("system", {"phase": "turn_start"}),
            _event(
                "observation",
                {"phase": "observation", "display_name": "Agent One", "language": "ko"},
                actor_id="agent-001",
            ),
        ],
        session_id="session-001",
    )

    assert [envelope.sequence for envelope in envelopes] == [0, 1]
    assert [envelope.session_id for envelope in envelopes] == ["session-001", "session-001"]


def test_simple_dialogue_golden_fixture() -> None:
    events = [
        SimulationEvent.model_validate_json(line)
        for line in (_FIXTURE_DIR / "simple_dialogue.input.jsonl")
        .read_text(encoding="utf-8")
        .splitlines()
        if line.strip()
    ]
    expected = [
        json.loads(line)
        for line in (_FIXTURE_DIR / "simple_dialogue.expected.jsonl")
        .read_text(encoding="utf-8")
        .splitlines()
        if line.strip()
    ]

    actual = [envelope.model_dump(mode="json") for envelope in adapt_events(events)]

    assert actual == expected


def test_conflict_push_golden_fixture() -> None:
    events = [
        SimulationEvent.model_validate_json(line)
        for line in (_FIXTURE_DIR / "conflict_push.input.jsonl")
        .read_text(encoding="utf-8")
        .splitlines()
        if line.strip()
    ]
    expected = [
        json.loads(line)
        for line in (_FIXTURE_DIR / "conflict_push.expected.jsonl")
        .read_text(encoding="utf-8")
        .splitlines()
        if line.strip()
    ]

    actual = [envelope.model_dump(mode="json") for envelope in adapt_events(events)]

    assert actual == expected
