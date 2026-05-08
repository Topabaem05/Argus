from __future__ import annotations

import pytest
from pydantic import ValidationError

from korean_social_simulator.bridge_schema import BridgeEnvelope


def _spawn_payload() -> dict[str, object]:
    return {
        "agent": {
            "agent_id": "agent-001",
            "display_name": "Synthetic Agent",
            "group_id": None,
            "position": {"x": 0.0, "y": 0.0, "z": 1.0},
            "facing": 90.0,
            "emotion": {"label": "neutral", "intensity": 0.0},
            "current_action": "idle",
            "visible": True,
        },
        "spawn_reason": "scenario_start",
    }


def _envelope_payload(**overrides: object) -> dict[str, object]:
    payload: dict[str, object] = {
        "schema_version": "1.0.0",
        "message_id": "msg-001",
        "correlation_id": None,
        "session_id": "session-001",
        "sequence": 0,
        "sent_at_ms": 123456,
        "type": "agent.spawn",
        "payload": _spawn_payload(),
    }
    payload.update(overrides)
    return payload


def test_valid_bridge_envelope_passes_validation() -> None:
    envelope = BridgeEnvelope.model_validate(_envelope_payload())

    assert envelope.schema_version == "1.0.0"
    assert envelope.type == "agent.spawn"
    assert envelope.payload["spawn_reason"] == "scenario_start"


def test_missing_required_field_fails_validation() -> None:
    payload = _envelope_payload()
    del payload["message_id"]

    with pytest.raises(ValidationError, match="message_id"):
        BridgeEnvelope.model_validate(payload)


def test_unsupported_major_version_fails_validation() -> None:
    with pytest.raises(ValidationError, match="Unsupported bridge schema major version"):
        BridgeEnvelope.model_validate(_envelope_payload(schema_version="2.0.0"))


def test_unknown_field_is_rejected() -> None:
    with pytest.raises(ValidationError, match="extra"):
        BridgeEnvelope.model_validate(_envelope_payload(unexpected=True))


def test_payload_type_mismatch_fails_validation() -> None:
    with pytest.raises(ValidationError, match="speaker_id"):
        BridgeEnvelope.model_validate(
            _envelope_payload(
                type="agent.dialogue",
                payload=_spawn_payload(),
            )
        )


def test_valid_agent_emotion_envelope_payload() -> None:
    envelope = BridgeEnvelope.model_validate(
        _envelope_payload(
            type="agent.emotion",
            payload={
                "agent_id": "agent-001",
                "label": "happy",
                "intensity": 0.42,
            },
        )
    )
    assert envelope.type == "agent.emotion"
    assert envelope.payload["agent_id"] == "agent-001"
    assert envelope.payload["label"] == "happy"
    assert envelope.payload["intensity"] == pytest.approx(0.42)


def test_valid_group_update_payload() -> None:
    envelope = BridgeEnvelope.model_validate(
        _envelope_payload(
            type="group.update",
            payload={
                "group_id": "g-1",
                "member_agent_ids": ["agent-001", "agent-002"],
                "badge_label": "north",
            },
        )
    )
    assert envelope.type == "group.update"
    assert envelope.payload["badge_label"] == "north"


def test_valid_conflict_update_payload_round_trip_stage() -> None:
    envelope = BridgeEnvelope.model_validate(
        _envelope_payload(
            type="conflict.update",
            payload={
                "conflict_id": "c-1",
                "participant_ids": ["agent-a", "agent-b"],
                "intensity": 0.71,
                "stage": "tension",
                "public_summary": "Calm disagreement about sequencing.",
            },
        )
    )
    assert envelope.type == "conflict.update"
    assert envelope.payload["stage"] == "tension"
