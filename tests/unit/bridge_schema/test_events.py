from __future__ import annotations

import pytest
from pydantic import ValidationError

from korean_social_simulator.bridge_schema import (
    AgentEmotionEvent,
    AgentMoveEvent,
    ConflictUpdateEvent,
    EmotionState,
    GroupUpdateEvent,
    StructuredError,
    Vec3,
)


def test_agent_emotion_event_valid() -> None:
    model = AgentEmotionEvent.model_validate(
        {"agent_id": "agent-9", "label": "confused", "intensity": 0.12}
    )
    assert model.agent_id == "agent-9"
    assert model.intensity == pytest.approx(0.12)


def test_agent_emotion_requires_agent_id() -> None:
    with pytest.raises(ValidationError, match="agent_id"):
        AgentEmotionEvent.model_validate({"label": "happy", "intensity": 0.5})


def test_group_update_event_optional_badge() -> None:
    bare = GroupUpdateEvent.model_validate({"group_id": "g-x", "member_agent_ids": ["a1"]})
    assert bare.badge_label is None

    with_badge = GroupUpdateEvent.model_validate(
        {
            "group_id": "g-x",
            "member_agent_ids": ["a1", "a2"],
            "badge_label": "team",
        }
    )
    assert with_badge.badge_label == "team"


def test_conflict_update_event_valid() -> None:
    model = ConflictUpdateEvent.model_validate(
        {
            "conflict_id": "c-1",
            "participant_ids": ["p1", "p2"],
            "intensity": 0.5,
            "stage": "argument",
            "public_summary": "Policy-focused debate without threats.",
        }
    )
    assert model.stage == "argument"


def test_invalid_emotion_enum_fails_validation() -> None:
    with pytest.raises(ValidationError, match="label"):
        EmotionState.model_validate({"label": "super_angry_v999", "intensity": 0.5})
    with pytest.raises(ValidationError, match="intensity"):
        EmotionState.model_validate({"label": "angry", "intensity": 1.5})


def test_non_finite_vec3_fails_validation() -> None:
    with pytest.raises(ValidationError, match="finite"):
        Vec3.model_validate({"x": 0.0, "y": float("nan"), "z": 1.0})


def test_move_event_rejects_negative_expected_arrival() -> None:
    with pytest.raises(ValidationError, match="expected_arrival_ms"):
        AgentMoveEvent.model_validate(
            {
                "agent_id": "agent-001",
                "start_position": None,
                "target_position": {"x": 1.0, "y": 0.0, "z": 2.0},
                "speed_mps": 1.5,
                "movement_style": "walk",
                "expected_arrival_ms": -1,
            }
        )


def test_structured_error_allows_field_specific_details() -> None:
    error = StructuredError.model_validate(
        {
            "error_id": "err-001",
            "source": "bridge",
            "severity": "error",
            "message": "Invalid bridge payload.",
            "recoverable": True,
            "correlation_id": "msg-001",
            "details": {"field_path": "payload.emotion.label"},
        }
    )

    assert error.details["field_path"] == "payload.emotion.label"
