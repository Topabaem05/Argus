from __future__ import annotations

from korean_social_simulator.models import AgentProfile, IndividualEvaluation
from korean_social_simulator.simulation.behavior_planner import build_behavior_intents


def _profile(agent_id: str) -> AgentProfile:
    return AgentProfile(
        agent_id=agent_id,
        persona_uuid=f"{agent_id}-uuid",
        display_name=f"Agent {agent_id}",
        language="ko",
        background="Synthetic public background.",
        memory_seeds=["private seed"],
        goals=["Observe the scenario."],
        behavior_rules=["Answer safely."],
        safety_notes=["Synthetic persona only."],
    )


def _evaluation(
    agent_id: str,
    stance: str,
    confidence: float,
) -> IndividualEvaluation:
    return IndividualEvaluation(
        agent_id=agent_id,
        stance=stance,  # tests use strings to cover planner mapping through Pydantic.
        confidence=confidence,
        rationale="Public dry-run rationale.",
        uncertainty="Dry-run output.",
    )


def test_behavior_planner_emits_one_intent_per_selected_agent() -> None:
    profiles = [_profile("agent-001"), _profile("agent-002")]
    evaluations = [
        _evaluation("agent-001", "supports", 0.72),
        _evaluation("agent-002", "opposes", 0.81),
    ]

    intents = build_behavior_intents(profiles, evaluations)

    assert len(intents) == len(profiles)
    assert {intent.agent_id for intent in intents} == {profile.agent_id for profile in profiles}
    assert all(intent.public_reason for intent in intents)
    assert all("synthetic_persona" in intent.safety_tags for intent in intents)


def test_opposed_high_confidence_maps_to_non_graphic_argument() -> None:
    intents = build_behavior_intents(
        [_profile("agent-001")],
        [_evaluation("agent-001", "opposes", 0.9)],
    )

    intent = intents[0]
    assert intent.intent == "argue"
    assert intent.locomotion == "run"
    assert intent.emotion is not None
    assert intent.emotion.label == "angry"
    assert "non_graphic" in intent.safety_tags


def test_missing_evaluation_uses_observe_fallback() -> None:
    intents = build_behavior_intents([_profile("agent-001")], [])

    intent = intents[0]
    assert intent.intent == "observe"
    assert intent.locomotion == "idle"
    assert "fallback" in intent.safety_tags
