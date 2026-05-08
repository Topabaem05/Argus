from __future__ import annotations

from typing import Protocol

from korean_social_simulator.bridge.fallback_physics import simulate_fallback
from korean_social_simulator.bridge_schema import PhysicsRequest, PhysicsResult

FALLBACK_WARNING = "Physics simulated using deterministic local fallback."


class PhysicsBackend(Protocol):
    """Backend contract for physical-event evaluation."""

    def evaluate(self, request: PhysicsRequest) -> PhysicsResult: ...

    def health(self) -> dict[str, object]: ...

    def close(self) -> None: ...


class FallbackPhysicsBackend:
    """Deterministic backend for physical-event evaluation."""

    def __init__(
        self,
        *,
        max_physical_intensity: float,
        warning: str = FALLBACK_WARNING,
    ) -> None:
        self._max_physical_intensity = max_physical_intensity
        self._warning = warning

    def evaluate(self, request: PhysicsRequest) -> PhysicsResult:
        return simulate_fallback(
            request,
            warning=self._warning,
            max_physical_intensity=self._max_physical_intensity,
        )

    def health(self) -> dict[str, object]:
        return {
            "backend": "fallback",
            "available": True,
            "model_path": None,
        }

    def close(self) -> None:
        return None
