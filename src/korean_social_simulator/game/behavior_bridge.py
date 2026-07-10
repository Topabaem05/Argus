from __future__ import annotations

from korean_social_simulator.bridge_schema.behavior import (
    AgentBehaviorIntentEvent,
    BehaviorIntent,
    LocomotionMode,
)
from korean_social_simulator.bridge_schema.events import EmotionLabel, EmotionState, Vec3
from korean_social_simulator.models import CommandAction, GameEmployee, PlayerCommand

_ACTION_TO_INTENT: dict[CommandAction, BehaviorIntent] = {
    "assign_task": "approach",
    "praise": "speak",
    "scold": "argue",
    "snack": "approach",
    "raise": "speak",
    "bonus": "speak",
    "party": "celebrate",
    "fire": "leave",
    "hire": "approach",
    "gossip": "speak",
    "scout": "approach",
}

_MOOD_TO_EMOTION: list[tuple[int, EmotionLabel]] = [
    (80, "excited"),
    (60, "happy"),
    (40, "neutral"),
    (20, "sad"),
    (0, "angry"),
]


def _emotion_for_mood(mood: int) -> EmotionState:
    for threshold, label in _MOOD_TO_EMOTION:
        if mood >= threshold:
            return EmotionState(label=label, intensity=min(1.0, mood / 100.0))
    return EmotionState(label="neutral", intensity=0.0)


def _grid_position(index: int) -> Vec3:
    return Vec3(x=float((index % 5) * 2.0), y=0.0, z=float((index // 5) * 2.0))


def build_command_behavior(
    employee: GameEmployee,
    command: PlayerCommand,
    index: int = 0,
) -> AgentBehaviorIntentEvent:
    """Translate a player command into a Unity-visible behavior intent."""
    intent = _ACTION_TO_INTENT.get(command.action, "observe")
    mood = employee.mood
    loyalty = employee.loyalty_to(command.player_id)
    urgency = max(0.1, min(1.0, (mood + loyalty + 100) / 300.0))
    locomotion: LocomotionMode = "run" if urgency > 0.7 else "walk"
    if intent in ("observe", "idle"):
        locomotion = "idle"
    base = _grid_position(index)
    return AgentBehaviorIntentEvent(
        agent_id=employee.employee_id,
        intent=intent,
        target_agent_ids=[command.player_id] if command.player_id else [],
        target_position=base,
        locomotion=locomotion,
        emotion=_emotion_for_mood(mood),
        animation_hint=intent,
        urgency=urgency,
        duration_ms=1800 if locomotion != "idle" else 1200,
        public_reason=f"Command={command.action}, mood={mood}, loyalty={loyalty}.",
        safety_tags=["non_graphic", "game_command"],
    )


__all__ = ["build_command_behavior"]
