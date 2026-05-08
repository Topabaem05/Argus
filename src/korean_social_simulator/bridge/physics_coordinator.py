from __future__ import annotations

from korean_social_simulator.bridge.fallback_physics import simulate_fallback
from korean_social_simulator.bridge.physics import FallbackPhysicsBackend, PhysicsBackend
from korean_social_simulator.bridge_schema import PhysicsRequest, PhysicsResult
from korean_social_simulator.config.models import BridgeConfig


class PhysicsCoordinator:
    """Timeout and fallback wrapper around a physical-event backend."""

    def __init__(
        self,
        *,
        backend: PhysicsBackend,
        max_physical_intensity: float,
    ) -> None:
        self._backend = backend
        self._max_physical_intensity = max_physical_intensity

    def evaluate(self, request: PhysicsRequest) -> PhysicsResult:
        return self._backend.evaluate(request)

    def health(self) -> dict[str, object]:
        health = self._backend.health()
        health.update(
            {
                "fallback_on_error": True,
            }
        )
        return health

    def close(self) -> None:
        self._backend.close()

    def _fallback(self, request: PhysicsRequest, warning: str) -> PhysicsResult:
        return simulate_fallback(
            request,
            warning=warning,
            max_physical_intensity=self._max_physical_intensity,
        )


def build_physics_coordinator(config: BridgeConfig) -> PhysicsCoordinator:
    """Build a physical-event coordinator from bridge configuration."""
    return PhysicsCoordinator(
        backend=FallbackPhysicsBackend(max_physical_intensity=config.safety.max_physical_intensity),
        max_physical_intensity=config.safety.max_physical_intensity,
    )
