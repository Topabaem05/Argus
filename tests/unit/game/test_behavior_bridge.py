from __future__ import annotations

from korean_social_simulator.bridge_schema.behavior import AgentBehaviorIntentEvent
from korean_social_simulator.game.behavior_bridge import build_command_behavior
from korean_social_simulator.models import (
    EmployeeStats,
    GameEmployee,
    PlayerCommand,
)


def _employee(mood: int = 50, loyalty: int = 0) -> GameEmployee:
    return GameEmployee(
        employee_id="emp-001",
        display_name="김민수",
        stats=EmployeeStats(),
        mood=mood,
        loyalty_map={"p1": loyalty},
        employed_by="p1",
    )


def _command(action: str = "praise") -> PlayerCommand:
    return PlayerCommand(
        command_id="cmd-1",
        player_id="p1",
        action=action,  # type: ignore[arg-type]
        target_employee_id="emp-001",
        round_number=1,
    )


def test_praise_command_produces_speak_intent() -> None:
    emp = _employee(mood=80, loyalty=50)
    intent = build_command_behavior(emp, _command("praise"))
    assert isinstance(intent, AgentBehaviorIntentEvent)
    assert intent.intent == "speak"
    assert intent.emotion.label == "excited"


def test_scold_command_produces_argue_intent() -> None:
    emp = _employee(mood=20, loyalty=-30)
    intent = build_command_behavior(emp, _command("scold"))
    assert intent.intent == "argue"
    assert intent.emotion.label == "sad"


def test_low_mood_maps_to_angry_emotion() -> None:
    emp = _employee(mood=10, loyalty=-50)
    intent = build_command_behavior(emp, _command("assign_task"))
    assert intent.emotion.label == "angry"
    assert intent.urgency < 0.5


def test_fire_command_produces_leave_locomotion() -> None:
    emp = _employee(mood=30, loyalty=-40)
    intent = build_command_behavior(emp, _command("fire"))
    assert intent.intent == "leave"


def test_high_urgency_uses_run_locomotion() -> None:
    emp = _employee(mood=90, loyalty=80)
    intent = build_command_behavior(emp, _command("bonus"))
    assert intent.locomotion == "run"
    assert intent.urgency > 0.7
