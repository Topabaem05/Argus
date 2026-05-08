from __future__ import annotations

import importlib

import pytest

from korean_social_simulator.bridge.fallback_physics import (
    FALLBACK_WARNING,
    simulate_fallback,
    simulate_physics,
)
from korean_social_simulator.bridge_schema import PhysicsRequest


def _request(**overrides: object) -> PhysicsRequest:
    payload: dict[str, object] = {
        "request_id": "phys-001",
        "event_id": "event-001",
        "actor_id": "agent-001",
        "target_id": "agent-002",
        "action": "push",
        "actor_position": {"x": 0.0, "y": 0.0, "z": 0.0},
        "target_position": {"x": 1.0, "y": 0.0, "z": 0.0},
        "intensity": 0.8,
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
    return PhysicsRequest.model_validate(payload)


def test_same_request_and_seed_are_identical() -> None:
    request = _request()

    first = simulate_fallback(request)
    second = simulate_fallback(request)

    assert first == second


def test_returns_fallback_result() -> None:
    result = simulate_physics(_request())

    assert result.status == "fallback"
    assert result.warnings == [FALLBACK_WARNING]


def test_push_with_fall_allowed_can_return_fall() -> None:
    result = simulate_fallback(_request(intensity=0.8))

    assert result.outcome == "fall"
    assert result.affected_agent_ids == ["agent-002"]
    assert result.final_positions["agent-002"].x > 1.0


def test_constraints_block_contact_and_fall() -> None:
    result = simulate_fallback(
        _request(
            intensity=0.9,
            constraints={
                "max_force": 10.0,
                "allow_fall": False,
                "allow_contact": False,
                "non_graphic_mode": True,
            },
        )
    )

    assert result.outcome == "no_contact"
    assert result.affected_agent_ids == []


def test_intensity_is_clamped_to_configured_maximum() -> None:
    result = simulate_fallback(_request(intensity=0.9), max_physical_intensity=0.5)

    assert result.outcome == "stumble"
    assert "clamped" in result.warnings[1]


def test_non_graphic_mode_is_required() -> None:
    request = _request(
        constraints={
            "max_force": 10.0,
            "allow_fall": True,
            "allow_contact": True,
            "non_graphic_mode": False,
        }
    )

    with pytest.raises(ValueError, match="non_graphic_mode"):
        simulate_fallback(request)


def test_module_imports_without_external_dependency() -> None:
    module = importlib.import_module("korean_social_simulator.bridge.fallback_physics")

    assert hasattr(module, "simulate_fallback")
    assert hasattr(module, "simulate_physics")
