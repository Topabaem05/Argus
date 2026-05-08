from __future__ import annotations

from korean_social_simulator.bridge.physics import FallbackPhysicsBackend
from korean_social_simulator.bridge.physics_coordinator import (
    PhysicsCoordinator,
    build_physics_coordinator,
)
from korean_social_simulator.bridge_schema import PhysicsRequest
from korean_social_simulator.config.loader import load_bridge_config


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


def test_fallback_backend_is_deterministic() -> None:
    backend = FallbackPhysicsBackend(max_physical_intensity=0.7)

    first = backend.evaluate(_request())
    second = backend.evaluate(_request())

    assert first == second
    assert first.status == "fallback"

    backend.close()


def test_fallback_backend_health() -> None:
    backend = FallbackPhysicsBackend(max_physical_intensity=0.7)

    health = backend.health()

    assert health["backend"] == "fallback"
    assert health["available"] is True
    assert health["model_path"] is None

    backend.close()


def test_coordinator_health_includes_fallback() -> None:
    coordinator = PhysicsCoordinator(
        backend=FallbackPhysicsBackend(max_physical_intensity=0.7),
        max_physical_intensity=0.7,
    )

    health = coordinator.health()

    assert health["backend"] == "fallback"
    assert health["available"] is True
    assert health["fallback_on_error"] is True

    coordinator.close()


def test_build_physics_coordinator_from_config() -> None:
    config = load_bridge_config("configs/bridge.example.yaml")

    coordinator = build_physics_coordinator(config)

    health = coordinator.health()
    assert health["backend"] == "fallback"
    assert health["available"] is True

    coordinator.close()
