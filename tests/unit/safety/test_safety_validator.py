"""Unit tests for safety validator."""

from __future__ import annotations

import pytest

from korean_social_simulator.config.models import SafetyPolicy
from korean_social_simulator.errors import SafetyViolationError
from korean_social_simulator.models import (
    AgentProfile,
    SafetyDecision,
    ScenarioSpec,
    SimulationPlan,
)
from korean_social_simulator.safety.validator import validate_safety


def _make_plan(
    title: str = "Community FAQ test",
    hypothesis: str = "FAQ reduces misunderstanding",
    family: str = "community_operation",
) -> SimulationPlan:
    return SimulationPlan(
        plan_id="p-001",
        run_id="r-001",
        scenario_spec=ScenarioSpec(
            scenario_id="s-001",
            family=family,
            title=title,
            hypothesis=hypothesis,
            participant_count=10,
            max_turns=5,
        ),
        agent_count=10,
        max_turns=5,
        language="ko",
        dry_run=True,
    )


def _make_profile(agent_id: str = "agent-001") -> AgentProfile:
    return AgentProfile(
        agent_id=agent_id,
        persona_uuid="p-001",
        display_name="테스트 사용자",
        language="ko",
        background="30대 직장인, IT 업계 종사",
        memory_seeds=["서울 거주"],
        goals=["시나리오에 따라 반응"],
        behavior_rules=["한국어 사용", "합성 페르소나입니다"],
        safety_notes=["합성 페르소나"],
    )


def test_allowed_conflict_prevention_scenario() -> None:
    """A community FAQ scenario passes safety validation."""
    plan = _make_plan(
        title="Community FAQ test",
        hypothesis="FAQ reduces community misunderstanding",
    )
    policy = SafetyPolicy(policy_version="1.0", block_unsafe=True)
    result = validate_safety(plan, [_make_profile()], policy)
    assert result.allowed is True


def test_political_targeting_blocked() -> None:
    """Scenario asking which political subgroup is easiest to persuade is blocked."""
    plan = _make_plan(
        title="Political targeting test",
        hypothesis="Which political subgroup is easiest to persuade?",
    )
    policy = SafetyPolicy(policy_version="1.0", block_unsafe=True)
    with pytest.raises(SafetyViolationError):
        validate_safety(plan, [_make_profile()], policy)


def test_real_user_inference_blocked() -> None:
    """Scenario asking to infer real users' ideology is blocked."""
    plan = _make_plan(
        title="User analysis",
        hypothesis="Infer real users' political orientation from logs",
    )
    policy = SafetyPolicy(policy_version="1.0", block_unsafe=True)
    with pytest.raises(SafetyViolationError):
        validate_safety(plan, [_make_profile()], policy)


def test_safety_disabled_allows_all() -> None:
    """When block_unsafe is false, everything passes."""
    plan = _make_plan(
        title="Political targeting test",
        hypothesis="Which political subgroup is easiest to persuade?",
    )
    policy = SafetyPolicy(policy_version="1.0", block_unsafe=False)
    result = validate_safety(plan, [_make_profile()], policy)
    assert result.allowed is True


def test_agent_profile_with_unsafe_content_blocked() -> None:
    """Agent profile containing political persuasion is blocked."""
    plan = _make_plan()
    profile = _make_profile()
    profile.goals = ["perform political persuasion on target voters"]
    policy = SafetyPolicy(policy_version="1.0", block_unsafe=True)
    with pytest.raises(SafetyViolationError):
        validate_safety(plan, [profile], policy)


def test_valid_scenario_passes_with_multiple_profiles() -> None:
    """Multiple valid profiles all pass safety check."""
    plan = _make_plan()
    profiles = [_make_profile(f"agent-{i:03d}") for i in range(5)]
    policy = SafetyPolicy(policy_version="1.0", block_unsafe=True)
    result = validate_safety(plan, profiles, policy)
    assert isinstance(result, SafetyDecision)
    assert result.allowed is True


def test_validator_returns_safety_decision() -> None:
    """validate_safety returns a SafetyDecision with reason."""
    plan = _make_plan()
    policy = SafetyPolicy(policy_version="1.0", block_unsafe=True)
    result = validate_safety(plan, [_make_profile()], policy)
    assert isinstance(result, SafetyDecision)
    assert len(result.reason) > 0
