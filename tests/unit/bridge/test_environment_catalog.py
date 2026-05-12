from __future__ import annotations

import pytest

from korean_social_simulator.bridge.environment_catalog import (
    build_environment_load_event,
    get_environment,
    list_supported_background_ids,
)
from korean_social_simulator.errors import ConfigurationError


def test_environment_catalog_lists_required_sdd_backgrounds() -> None:
    ids = set(list_supported_background_ids())

    assert {
        "schoolroom",
        "office",
        "street_plaza",
        "home",
        "conference_room",
        "abstract_arena",
    }.issubset(ids)


def test_environment_catalog_builds_public_load_event_with_capacity_floor() -> None:
    event = build_environment_load_event("home", agent_count=12)

    assert event.background_id == "home"
    assert event.spawn_capacity == 12
    assert event.public_summary
    assert event.bounds_min.x < event.bounds_max.x
    assert event.bounds_min.z < event.bounds_max.z


def test_unknown_environment_fails_with_supported_ids() -> None:
    with pytest.raises(ConfigurationError, match="Unsupported background_id"):
        get_environment("fake_whitebox")
