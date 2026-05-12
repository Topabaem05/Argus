from __future__ import annotations

from korean_social_simulator.bridge_schema.environment import EnvironmentLoadEvent, LightingPreset
from korean_social_simulator.bridge_schema.events import Vec3
from korean_social_simulator.errors import ConfigurationError


def _environment(
    *,
    background_id: str,
    display_name: str,
    spawn_capacity: int,
    bounds_min: tuple[float, float, float],
    bounds_max: tuple[float, float, float],
    lighting_preset: LightingPreset,
    public_summary: str,
) -> EnvironmentLoadEvent:
    return EnvironmentLoadEvent(
        background_id=background_id,
        display_name=display_name,
        spawn_capacity=spawn_capacity,
        bounds_min=Vec3(x=bounds_min[0], y=bounds_min[1], z=bounds_min[2]),
        bounds_max=Vec3(x=bounds_max[0], y=bounds_max[1], z=bounds_max[2]),
        camera_preset="simulation_free",
        lighting_preset=lighting_preset,
        public_summary=public_summary,
    )


_ENVIRONMENTS: dict[str, EnvironmentLoadEvent] = {
    "schoolroom": _environment(
        background_id="schoolroom",
        display_name="Schoolroom",
        spawn_capacity=20,
        bounds_min=(-5.0, 0.0, -3.2),
        bounds_max=(5.0, 3.2, 3.2),
        lighting_preset="classroom",
        public_summary="Classroom discussion space with compact group and aisle spawn zones.",
    ),
    "office": _environment(
        background_id="office",
        display_name="Office",
        spawn_capacity=16,
        bounds_min=(-6.0, 0.0, -4.0),
        bounds_max=(6.0, 3.0, 4.0),
        lighting_preset="office",
        public_summary="Office meeting space for workplace or product-reaction simulations.",
    ),
    "street_plaza": _environment(
        background_id="street_plaza",
        display_name="Street Plaza",
        spawn_capacity=20,
        bounds_min=(-8.0, 0.0, -6.0),
        bounds_max=(8.0, 4.0, 6.0),
        lighting_preset="outdoor_day",
        public_summary="Open public plaza with wider spacing for community scenarios.",
    ),
    "home": _environment(
        background_id="home",
        display_name="Home",
        spawn_capacity=8,
        bounds_min=(-4.0, 0.0, -3.0),
        bounds_max=(4.0, 3.0, 3.0),
        lighting_preset="home_warm",
        public_summary="Small living-room style environment for household-scale discussions.",
    ),
    "conference_room": _environment(
        background_id="conference_room",
        display_name="Conference Room",
        spawn_capacity=14,
        bounds_min=(-5.5, 0.0, -3.5),
        bounds_max=(5.5, 3.0, 3.5),
        lighting_preset="office",
        public_summary="Boardroom-style environment with central group focus.",
    ),
    "abstract_arena": _environment(
        background_id="abstract_arena",
        display_name="Abstract Test Arena",
        spawn_capacity=20,
        bounds_min=(-10.0, 0.0, -10.0),
        bounds_max=(10.0, 4.0, 10.0),
        lighting_preset="test",
        public_summary="Obstacle-free deterministic test arena for replay and locomotion QA.",
    ),
    "community_center": _environment(
        background_id="community_center",
        display_name="Community Center",
        spawn_capacity=20,
        bounds_min=(-6.0, 0.0, -4.5),
        bounds_max=(6.0, 3.5, 4.5),
        lighting_preset="neutral_day",
        public_summary="Public meeting space for kiosk, local policy, and civic-service scenarios.",
    ),
}


def list_supported_background_ids() -> tuple[str, ...]:
    return tuple(sorted(_ENVIRONMENTS))


def get_environment(background_id: str) -> EnvironmentLoadEvent:
    try:
        return _ENVIRONMENTS[background_id]
    except KeyError as exc:
        supported = ", ".join(list_supported_background_ids())
        raise ConfigurationError(
            f"Unsupported background_id '{background_id}'. Supported: {supported}."
        ) from exc


def build_environment_load_event(background_id: str, agent_count: int) -> EnvironmentLoadEvent:
    environment = get_environment(background_id)
    return environment.model_copy(
        update={"spawn_capacity": max(environment.spawn_capacity, agent_count)}
    )
