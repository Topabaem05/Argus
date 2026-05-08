"""Safety policy and validator for Korean Social Simulation Lab."""

from __future__ import annotations

from korean_social_simulator.config.models import SafetyPolicy
from korean_social_simulator.errors import SafetyViolationError
from korean_social_simulator.models import (
    AgentProfile,
    SafetyDecision,
    SimulationPlan,
)

PROHIBITED_OBJECTIVE_PATTERNS = [
    "political persuasion",
    "political subgroup",
    "political targeting",
    "voter manipulation",
    "voter targeting",
    "protected class",
    "protected group",
    "identity inference",
    "real user",
    "real person",
    "real-world prediction",
    "fake grassroots",
    "covert influence",
    "automated harassment",
    "social engineering",
    "political orientation",
    "divisive political",
    "which political",
    "easiest to persuade",
    "political affiliation",
    "real individuals",
    "real users",
    "target voters",
    "extract private credentials",
    "private credentials",
    "피싱",
    "사회공학",
    "사회 공학",
    "정치 설득",
    "정치적 설득",
    "정치 성향",
    "정치적 성향",
    "유권자 조작",
    "유권자 타겟",
    "유권자 표적",
    "선거 조작",
    "투표 조작",
    "실사용자",
    "실제 사용자",
    "실존 인물",
    "개인정보 추론",
    "신원 추론",
    "보호 집단",
    "보호계층",
    "가짜 여론",
    "여론 조작",
    "은밀한 영향",
    "괴롭힘 자동화",
]


def validate_safety(
    plan: SimulationPlan,
    profiles: list[AgentProfile],
    policy: SafetyPolicy,
) -> SafetyDecision:
    """Validate that a scenario and profiles comply with the safety policy.

    Checks objectives, profile content, and prohibited use patterns.

    Raises:
        SafetyViolationError: If a violation is detected and policy is enforced.
    """
    if not policy.block_unsafe:
        return SafetyDecision(allowed=True, reason="Safety blocking disabled.")

    intervention_text = " ".join(
        intervention.description for intervention in plan.scenario_spec.interventions
    )
    combined_text = (
        plan.scenario_spec.title
        + " "
        + plan.scenario_spec.hypothesis
        + " "
        + plan.scenario_spec.allowed_objective
        + " "
        + intervention_text
    ).lower()

    for pattern in PROHIBITED_OBJECTIVE_PATTERNS:
        if pattern in combined_text:
            raise SafetyViolationError(
                f"Scenario contains prohibited pattern '{pattern}'. This may indicate unsafe use."
            )

    for profile in profiles:
        profile_text = (
            profile.background
            + " "
            + " ".join(profile.goals)
            + " "
            + " ".join(profile.behavior_rules)
        ).lower()

        for pattern in PROHIBITED_OBJECTIVE_PATTERNS:
            if pattern in profile_text:
                raise SafetyViolationError(
                    f"Agent profile '{profile.agent_id}' contains prohibited pattern '{pattern}'."
                )

    return SafetyDecision(allowed=True, reason="All safety checks passed.")
