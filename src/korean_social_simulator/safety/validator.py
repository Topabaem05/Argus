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

    combined_text = (
        plan.scenario_spec.title
        + " "
        + plan.scenario_spec.hypothesis
        + " "
        + plan.scenario_spec.allowed_objective
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
