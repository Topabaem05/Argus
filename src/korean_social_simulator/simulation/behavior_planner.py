from __future__ import annotations

from korean_social_simulator.bridge_schema.behavior import (
    AgentBehaviorIntentEvent,
    BehaviorIntent,
    LocomotionMode,
)
from korean_social_simulator.bridge_schema.events import EmotionLabel, EmotionState, Vec3
from korean_social_simulator.models import AgentProfile, IndividualEvaluation

STANCE_TO_INTENT: dict[str, BehaviorIntent] = {
    "supports": "speak",
    "opposes": "argue",
    "mixed": "ask",
    "uncertain": "observe",
}
STANCE_TO_EMOTION: dict[str, EmotionLabel] = {
    "supports": "happy",
    "opposes": "angry",
    "mixed": "confused",
    "uncertain": "neutral",
}


def grid_position(index: int) -> Vec3:
    """Return the same deterministic Unity grid used by bridge spawn fallback."""
    return Vec3(x=float((index % 5) * 2.0), y=0.0, z=float((index // 5) * 2.0))


def build_behavior_intents(
    profiles: list[AgentProfile],
    evaluations: list[IndividualEvaluation],
) -> list[AgentBehaviorIntentEvent]:
    """Build one public, deterministic mini-bot intent per selected profile."""
    evaluations_by_agent = {evaluation.agent_id: evaluation for evaluation in evaluations}
    intents: list[AgentBehaviorIntentEvent] = []

    for index, profile in enumerate(profiles):
        evaluation = evaluations_by_agent.get(profile.agent_id)
        if evaluation is None:
            intents.append(_fallback_intent(profile, index))
            continue

        stance = evaluation.stance
        confidence = float(evaluation.confidence)
        base = grid_position(index)
        move_offset = _stance_offset(stance, confidence)
        locomotion = _locomotion_for(confidence, stance)

        intents.append(
            AgentBehaviorIntentEvent(
                agent_id=profile.agent_id,
                intent=STANCE_TO_INTENT[stance],
                target_agent_ids=[],
                target_position=Vec3(x=base.x, y=base.y, z=base.z + move_offset),
                locomotion=locomotion,
                emotion=EmotionState(
                    label=STANCE_TO_EMOTION[stance],
                    intensity=min(1.0, confidence),
                ),
                animation_hint=STANCE_TO_INTENT[stance],
                urgency=confidence,
                duration_ms=_duration_for(confidence, locomotion),
                public_reason=f"Persona stance={stance}, confidence={confidence:.2f}.",
                safety_tags=["non_graphic", "synthetic_persona"],
            )
        )

    return intents


def _fallback_intent(profile: AgentProfile, index: int) -> AgentBehaviorIntentEvent:
    return AgentBehaviorIntentEvent(
        agent_id=profile.agent_id,
        intent="observe",
        target_position=grid_position(index),
        locomotion="idle",
        emotion=EmotionState(label="neutral", intensity=0.0),
        animation_hint="observe",
        urgency=0.1,
        duration_ms=1200,
        public_reason="No individual evaluation was available; using safe observe fallback.",
        safety_tags=["fallback", "non_graphic", "synthetic_persona"],
    )


def _stance_offset(stance: str, confidence: float) -> float:
    if stance == "supports":
        return 0.45 + confidence * 0.25
    if stance == "opposes":
        return 1.35 + confidence * 0.35
    if stance == "mixed":
        return 0.9
    return 0.0


def _locomotion_for(confidence: float, stance: str) -> LocomotionMode:
    if stance == "uncertain":
        return "idle"
    if confidence >= 0.85:
        return "run"
    return "walk"


def _duration_for(confidence: float, locomotion: LocomotionMode) -> int:
    if locomotion == "idle":
        return 1600
    return 2200 if confidence < 0.6 else 1800
