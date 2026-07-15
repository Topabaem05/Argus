from __future__ import annotations

from korean_social_simulator.ai.prompt_builder import GamePromptBuilder
from korean_social_simulator.models import EmployeeStats, GameEmployee, PlayerCommand
from korean_social_simulator.social.memory import AgentMemorySystem


def test_compact_prompt_keeps_runtime_metrics_and_three_memories() -> None:
    memory = AgentMemorySystem()
    employee = GameEmployee(
        employee_id="emp-001",
        display_name="김민수",
        personality_tags=["성실", "내성적"],
        stats=EmployeeStats(stamina=7, intelligence=8, speed=5, communication=4),
        mood=23,
        loyalty_map={"p1": -42},
        reputation_perception={"p1": -10},
        employed_by="p1",
    )
    for index in range(5):
        memory.remember(employee.employee_id, f"기억-{index}")
    command = PlayerCommand(
        command_id="cmd-1",
        player_id="p1",
        action="assign_task",
        target_employee_id=employee.employee_id,
        task_id="task-3",
        round_number=1,
    )

    prompt = GamePromptBuilder(memory).build_command_prompt(employee, "p1", command)

    assert "기분: 23/100" in prompt
    assert "호감도: -42/100" in prompt
    assert "기억-0" not in prompt
    assert "기억-1" not in prompt
    assert "기억-2 | 기억-3 | 기억-4" in prompt
    assert "task_id=task-3" in prompt
    assert len(prompt) < 500


def test_system_prompt_forbids_reasoning_and_requires_single_json() -> None:
    system_prompt = GamePromptBuilder.system_prompt()
    assert "추론 과정" in system_prompt
    assert "JSON 객체 하나" in system_prompt
    assert "accept, reluctant_accept, refuse, complain" in system_prompt
