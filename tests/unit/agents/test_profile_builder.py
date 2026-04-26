"""Unit tests for agent profile builder."""

from __future__ import annotations

import pytest

from korean_social_simulator.agents.profile_builder import build_agent_profiles
from korean_social_simulator.errors import AgentProfileError, SafetyViolationError
from korean_social_simulator.models import (
    AgentProfile,
    PersonaRecord,
    PopulationSample,
)


def _make_persona(uuid: str = "p-001") -> PersonaRecord:
    return PersonaRecord(
        uuid=uuid,
        persona="30대 직장인, IT 업계 종사",
        professional_persona="소프트웨어 엔지니어",
        family_persona="기혼, 자녀 1명",
        cultural_background="한국 전통문화",
        skills_and_expertise="Python, 클라우드",
        hobbies_and_interests="게임, 요리, 등산",
        age=34,
        sex="남성",
        occupation="소프트웨어 엔지니어",
        district="강남구",
        province="서울특별시",
        country="South Korea",
    )


def _make_sample(records: list[PersonaRecord] | None = None) -> PopulationSample:
    return PopulationSample(
        sample_id="s-001",
        seed=42,
        records=records if records is not None else [_make_persona()],
        source="test",
    )


def test_valid_profile_generation() -> None:
    """Valid profile includes all required fields."""
    profiles = build_agent_profiles(_make_sample(), language="ko")
    assert len(profiles) == 1
    p = profiles[0]
    assert isinstance(p, AgentProfile)
    assert p.agent_id == "agent-p-001"
    assert p.persona_uuid == "p-001"
    assert p.display_name == "30대 직장인"
    assert p.language == "ko"
    assert len(p.background) > 0
    assert len(p.memory_seeds) >= 2
    assert len(p.goals) >= 1
    assert len(p.behavior_rules) >= 3
    assert len(p.safety_notes) >= 1


def test_korean_language_behavior() -> None:
    """Korean scenarios include Korean language instruction in behavior rules."""
    profiles = build_agent_profiles(_make_sample(), language="ko")
    rules = profiles[0].behavior_rules
    korean_rule = [r for r in rules if "한국어" in r]
    assert len(korean_rule) == 1


def test_non_korean_language_no_korean_rule() -> None:
    """Non-Korean scenarios do not include the Korean language rule."""
    profiles = build_agent_profiles(_make_sample(), language="en")
    rules = profiles[0].behavior_rules
    korean_rule = [r for r in rules if "한국어" in r]
    assert len(korean_rule) == 0


def test_unsafe_persona_text_blocked() -> None:
    record = _make_persona(uuid="p-bad")
    record.persona = "30대 직장인, real user 분석 대상"

    with pytest.raises(SafetyViolationError, match="real user"):
        build_agent_profiles(
            _make_sample([record]),
            language="ko",
        )


def test_unsafe_background_content_blocked() -> None:
    record = _make_persona(uuid="p-bad-background")
    record.professional_persona = "real individuals 대상 행동 분석"

    with pytest.raises(SafetyViolationError, match="real individuals"):
        build_agent_profiles(
            _make_sample([record]),
            language="ko",
        )


def test_empty_sample_raises() -> None:
    """Empty population sample raises AgentProfileError."""
    sample = _make_sample([])
    with pytest.raises(AgentProfileError, match="has no persona records"):
        build_agent_profiles(sample, language="ko")


def test_multiple_profiles() -> None:
    """Multiple personas produce multiple profiles."""
    personas = [_make_persona(f"p-{i:03d}") for i in range(5)]
    profiles = build_agent_profiles(_make_sample(personas), language="ko")
    assert len(profiles) == 5
    ids = [p.agent_id for p in profiles]
    assert "agent-p-000" in ids


def test_profile_contains_background_fields() -> None:
    """Background includes occupation, province, district, age, and sex."""
    profiles = build_agent_profiles(_make_sample(), language="ko")
    bg = profiles[0].background
    assert "소프트웨어 엔지니어" in bg
    assert "서울특별시" in bg
    assert "강남구" in bg
    assert "34세" in bg
    assert "남성" in bg


def test_memory_seeds_include_skills() -> None:
    """Memory seeds include skills_and_expertise when present."""
    profiles = build_agent_profiles(_make_sample(), language="ko")
    seeds = profiles[0].memory_seeds
    assert any("Python" in s for s in seeds)


def test_memory_seeds_without_optional_skills() -> None:
    """Memory seeds still work without skills_and_expertise."""
    record = _make_persona(uuid="p-min")
    record.skills_and_expertise = None
    profiles = build_agent_profiles(_make_sample([record]), language="ko")
    assert len(profiles[0].memory_seeds) == 2
