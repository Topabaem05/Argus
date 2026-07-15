"""Game prompt builder — compact employee context for low-parameter SLMs."""

from __future__ import annotations

from dataclasses import dataclass

from korean_social_simulator.models import GameEmployee, PlayerCommand, TaskCategory
from korean_social_simulator.social.memory import AgentMemorySystem

_SYSTEM_PROMPT = """당신은 회사 운영 게임의 AI 직원입니다.
제공된 게임 상태만 사용해 직원의 행동과 짧은 한국어 대사를 결정하세요.
추론 과정, 설명, 마크다운, 코드펜스를 출력하지 마세요.
반드시 JSON 객체 하나만 출력하세요.
허용 action: accept, reluctant_accept, refuse, complain
허용 side_action: null, gossip, consider_quit
형식: {"action":"accept","dialogue":"한국어 대사","efficiency":0.0,"mood_change":0,"side_action":null}
범위: efficiency 0~1, mood_change -10~10
"""


@dataclass
class GamePromptBuilder:
    memory: AgentMemorySystem

    def build_command_prompt(
        self,
        employee: GameEmployee,
        player_id: str,
        command: PlayerCommand,
    ) -> str:
        stats = employee.stats
        tags = ",".join(employee.personality_tags) if employee.personality_tags else "없음"
        memories = self.memory.memories(employee.employee_id)[-3:]
        memory_ctx = " | ".join(memories) if memories else "없음"
        detail = self._command_detail(command) or "없음"
        return (
            f"직원={employee.display_name}\n"
            f"성격={tags}\n"
            f"능력=체력{stats.stamina},지능{stats.intelligence},속도{stats.speed},소통{stats.communication}\n"
            f"기분: {employee.mood}/100\n"
            f"호감도: {employee.loyalty_to(player_id)}/100\n"
            f"사장평판={employee.reputation_perception.get(player_id, 0)}/100\n"
            f"최근기억={memory_ctx}\n"
            f"명령={command.action}\n"
            f"명령상세={detail}\n"
            "판단=성격,기분,호감도,업무적합도를 반영해 JSON 하나만 출력"
        )

    def _command_detail(self, command: PlayerCommand) -> str:
        if command.action == "assign_task" and command.task_id:
            return f"task_id={command.task_id}"
        if command.action == "gossip":
            rumor = command.payload.get("rumor", "")
            return f"target_player={command.target_player_id},rumor={rumor}"
        if command.action in ("snack", "raise", "bonus", "party"):
            cost = command.payload.get("cost", 0)
            return f"cost={cost}"
        return ""

    @staticmethod
    def system_prompt() -> str:
        return _SYSTEM_PROMPT

    def task_category_korean(self, category: TaskCategory) -> str:
        return {
            "carry": "운반",
            "deliver": "배달",
            "document": "서류",
            "sales": "영업",
            "maintenance": "관리",
        }[category]
