"""Game prompt builder — assembles employee context into SLM prompts."""

from __future__ import annotations

from dataclasses import dataclass

from korean_social_simulator.models import GameEmployee, PlayerCommand, TaskCategory
from korean_social_simulator.social.memory import AgentMemorySystem

_SYSTEM_PROMPT = """당신은 회사 직원입니다. 사장의 명령에 대해 성격, 기분, 호감도에 따라 자연스럽게 반응하세요.
응답은 반드시 다음 JSON 형식이어야 합니다:
{"action": "accept|reluctant_accept|refuse|complain", "dialogue": "한국어 대사", "efficiency": 0.0~1.0, "mood_change": -10~+10, "side_action": null|"gossip"|"consider_quit"}
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
        mood = employee.mood
        loyalty = employee.loyalty_to(player_id)
        reputation = employee.reputation_perception.get(player_id, 0)
        tags = ", ".join(employee.personality_tags) if employee.personality_tags else "없음"
        stats = employee.stats
        memory_ctx = self.memory.context_for_prompt(employee)

        return f"""[직원 정보]
이름: {employee.display_name}
성격: {tags}
능력: 체력 {stats.stamina}, 지능 {stats.intelligence}, 속도 {stats.speed}, 소통 {stats.communication}
현재 기분: {mood}/100 ({employee.mood_level()})
사장({player_id}) 호감도: {loyalty} (-100~+100)
사장({player_id}) 평판 인식: {reputation} (-100~+100)

최근 기억:
{memory_ctx}

[사장의 명령]
액션: {command.action}
{self._command_detail(command)}

[응답 요청]
이 명령에 대해 어떻게 반응하겠습니까?"""

    def _command_detail(self, command: PlayerCommand) -> str:
        if command.action == "assign_task" and command.task_id:
            return f"태스크 ID: {command.task_id}"
        if command.action == "gossip":
            rumor = command.payload.get("rumor", "")
            return f"소문 대상: {command.target_player_id}, 내용: {rumor}"
        if command.action in ("snack", "raise", "bonus", "party"):
            cost = command.payload.get("cost", 0)
            return f"비용: {cost}원"
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
