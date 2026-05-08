from __future__ import annotations

import pytest
from pydantic import ValidationError

from korean_social_simulator.bridge_schema import BridgeEnvelope, PhysicsRequest, PhysicsResult


def _physics_request_payload(**overrides: object) -> dict[str, object]:
    payload: dict[str, object] = {
        "request_id": "phys-req-001",
        "event_id": "event-001",
        "actor_id": "agent-001",
        "target_id": "agent-002",
        "action": "push",
        "actor_position": {"x": 0.0, "y": 0.0, "z": 0.0},
        "target_position": {"x": 1.0, "y": 0.0, "z": 0.0},
        "intensity": 0.4,
        "duration_ms": 500,
        "seed": 1234,
        "constraints": {
            "max_force": 10.0,
            "allow_fall": True,
            "allow_contact": True,
            "non_graphic_mode": True,
        },
    }
    payload.update(overrides)
    return payload


def _physics_result_payload(**overrides: object) -> dict[str, object]:
    payload: dict[str, object] = {
        "request_id": "phys-req-001",
        "event_id": "event-001",
        "status": "fallback",
        "outcome": "stumble",
        "affected_agent_ids": ["agent-002"],
        "final_positions": {
            "agent-001": {"x": 0.0, "y": 0.0, "z": 0.0},
            "agent-002": {"x": 1.2, "y": 0.0, "z": 0.0},
        },
        "animation_hints": ["stumble"],
        "confidence": 0.75,
        "warnings": ["Physics simulated using deterministic local fallback."],
    }
    payload.update(overrides)
    return payload


def test_valid_physics_request_passes_validation() -> None:
    request = PhysicsRequest.model_validate(_physics_request_payload())

    assert request.action == "push"
    assert request.constraints.non_graphic_mode is True


def test_invalid_physics_intensity_fails_validation() -> None:
    with pytest.raises(ValidationError, match="intensity"):
        PhysicsRequest.model_validate(_physics_request_payload(intensity=1.5))


def test_unknown_physics_outcome_fails_validation() -> None:
    with pytest.raises(ValidationError, match="outcome"):
        PhysicsResult.model_validate(_physics_result_payload(outcome="teleport"))


def test_non_finite_position_fails_via_vec3_validation() -> None:
    with pytest.raises(ValidationError, match="finite"):
        PhysicsRequest.model_validate(
            _physics_request_payload(actor_position={"x": 0.0, "y": float("inf"), "z": 0.0})
        )


def test_envelope_validates_physics_result_payload() -> None:
    envelope = BridgeEnvelope.model_validate(
        {
            "schema_version": "1.0.0",
            "message_id": "msg-phys-001",
            "correlation_id": "phys-req-001",
            "session_id": "session-001",
            "sequence": 10,
            "sent_at_ms": 123456,
            "type": "physics.result",
            "payload": _physics_result_payload(),
        }
    )

    assert envelope.type == "physics.result"


def test_envelope_rejects_invalid_physics_result_payload() -> None:
    with pytest.raises(ValidationError, match="outcome"):
        BridgeEnvelope.model_validate(
            {
                "schema_version": "1.0.0",
                "message_id": "msg-phys-001",
                "correlation_id": "phys-req-001",
                "session_id": "session-001",
                "sequence": 10,
                "sent_at_ms": 123456,
                "type": "physics.result",
                "payload": _physics_result_payload(outcome="teleport"),
            }
        )
