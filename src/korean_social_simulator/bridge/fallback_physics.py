from __future__ import annotations

import math

from korean_social_simulator.bridge_schema import PhysicsRequest, PhysicsResult, Vec3
from korean_social_simulator.bridge_schema.physics import PhysicsOutcome

FALLBACK_WARNING = "Physics simulated using deterministic local fallback."


def simulate_physics(
    request: PhysicsRequest,
    *,
    max_physical_intensity: float = 1.0,
) -> PhysicsResult:
    """Evaluate a physical request through the deterministic fallback path."""
    return simulate_fallback(
        request,
        warning=FALLBACK_WARNING,
        max_physical_intensity=max_physical_intensity,
    )


def simulate_fallback(
    request: PhysicsRequest,
    *,
    warning: str = FALLBACK_WARNING,
    max_physical_intensity: float = 1.0,
) -> PhysicsResult:
    """Return a deterministic, non-graphic fallback result for a physical event."""
    if not request.constraints.non_graphic_mode:
        raise ValueError("Fallback physics requires non_graphic_mode.")
    if not 0.0 <= max_physical_intensity <= 1.0:
        raise ValueError("max_physical_intensity must be between 0.0 and 1.0.")

    effective_intensity = min(request.intensity, max_physical_intensity)
    warnings_list = [warning]
    if request.intensity > max_physical_intensity:
        warnings_list.append("Physical intensity exceeded configured maximum and was clamped.")

    outcome = _choose_outcome(request, effective_intensity)
    final_positions = _final_positions(request, outcome, effective_intensity)
    affected_agent_ids = _affected_agent_ids(request, outcome)

    return PhysicsResult(
        request_id=request.request_id,
        event_id=request.event_id,
        status="fallback",
        outcome=outcome,
        affected_agent_ids=affected_agent_ids,
        final_positions=final_positions,
        animation_hints=[outcome],
        confidence=0.5 if outcome == "unknown" else 0.8,
        warnings=warnings_list,
    )


def _choose_outcome(request: PhysicsRequest, intensity: float) -> PhysicsOutcome:
    if request.action in {"push", "block"} and not request.constraints.allow_contact:
        return "no_contact"
    if request.action == "block":
        return "blocked"
    if request.action == "stumble":
        return "stumble"
    if request.action == "fall":
        return "fall" if request.constraints.allow_fall else "stumble"
    if request.action == "recover":
        return "recover"
    if request.action == "separate":
        return "separate"
    if request.action == "push":
        if request.constraints.allow_fall and intensity >= 0.75:
            return "fall"
        if intensity >= 0.25:
            return "stumble"
        return "blocked"


def _final_positions(
    request: PhysicsRequest,
    outcome: PhysicsOutcome,
    intensity: float,
) -> dict[str, Vec3]:
    positions = {request.actor_id: request.actor_position}
    if request.target_id is None or request.target_position is None:
        return positions

    if outcome in {"stumble", "fall", "separate"}:
        positions[request.target_id] = _offset_target_position(request, intensity)
    else:
        positions[request.target_id] = request.target_position
    return positions


def _offset_target_position(request: PhysicsRequest, intensity: float) -> Vec3:
    if request.target_position is None:
        return request.actor_position

    dx = request.target_position.x - request.actor_position.x
    dz = request.target_position.z - request.actor_position.z
    length = math.hypot(dx, dz)
    if length == 0.0:
        direction_x = 1.0
        direction_z = 0.0
    else:
        direction_x = dx / length
        direction_z = dz / length

    offset = max(0.1, intensity) * 0.5
    return Vec3(
        x=request.target_position.x + direction_x * offset,
        y=request.target_position.y,
        z=request.target_position.z + direction_z * offset,
    )


def _affected_agent_ids(request: PhysicsRequest, outcome: PhysicsOutcome) -> list[str]:
    if request.target_id is not None and outcome in {"blocked", "stumble", "fall", "separate"}:
        return [request.target_id]
    if request.target_id is None and outcome in {"stumble", "fall", "recover"}:
        return [request.actor_id]
    return []
