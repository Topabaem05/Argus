"""Agent memory system — per-employee rolling event log for SLM context."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass, field

from korean_social_simulator.models import GameEmployee


@dataclass
class AgentMemorySystem:
    max_per_employee: int = 10
    _memories: dict[str, deque[str]] = field(default_factory=dict)

    def init_employee(self, employee_id: str) -> None:
        if employee_id not in self._memories:
            self._memories[employee_id] = deque(maxlen=self.max_per_employee)

    def remember(self, employee_id: str, memory: str) -> None:
        self.init_employee(employee_id)
        self._memories[employee_id].append(memory)

    def memories(self, employee_id: str) -> list[str]:
        self.init_employee(employee_id)
        return list(self._memories[employee_id])

    def context_for_prompt(self, employee: GameEmployee) -> str:
        self.init_employee(employee.employee_id)
        recent = list(self._memories[employee.employee_id])
        if not recent:
            return "최근 기억 없음"
        return "\n".join(f"- {m}" for m in recent[-5:])

    def forget_all(self, employee_id: str) -> None:
        self._memories.pop(employee_id, None)
