"""Relationship graph — employee-to-employee ties and rumor routing."""

from __future__ import annotations

from dataclasses import dataclass, field

from korean_social_simulator.game.state import GameStateManager

RelKind = str  # "friend", "rival", "neutral"


@dataclass
class RelationshipGraph:
    manager: GameStateManager
    _edges: dict[tuple[str, str], int] = field(default_factory=dict)

    def _key(self, a: str, b: str) -> tuple[str, str]:
        return (a, b) if a <= b else (b, a)

    def bond(self, emp_a_id: str, emp_b_id: str, strength: int) -> None:
        if strength < -100 or strength > 100:
            raise ValueError("Strength must be -100..100")
        key = self._key(emp_a_id, emp_b_id)
        self._edges[key] = strength

    def relationship(self, emp_a_id: str, emp_b_id: str) -> int:
        return self._edges.get(self._key(emp_a_id, emp_b_id), 0)

    def kind(self, emp_a_id: str, emp_b_id: str) -> RelKind:
        s = self.relationship(emp_a_id, emp_b_id)
        if s >= 30:
            return "friend"
        if s <= -30:
            return "rival"
        return "neutral"

    def friends(self, employee_id: str) -> list[str]:
        result: list[str] = []
        for (a, b), strength in self._edges.items():
            if a == employee_id and strength >= 30:
                result.append(b)
            elif b == employee_id and strength >= 30:
                result.append(a)
        return result

    def rumor_recipients(self, employee_id: str) -> list[str]:
        return self.friends(employee_id)

    def build_initial_bonds(self) -> None:
        for company in self.manager.game_state.companies.values():
            active = company.active_employees()
            for i, emp_a in enumerate(active):
                for emp_b in active[i + 1 :]:
                    shared_tags = set(emp_a.personality_tags) & set(emp_b.personality_tags)
                    strength = 20 if shared_tags else 5
                    self.bond(emp_a.employee_id, emp_b.employee_id, strength)

    def adjust_from_interaction(self, emp_a_id: str, emp_b_id: str, delta: int) -> None:
        key = self._key(emp_a_id, emp_b_id)
        current = self._edges.get(key, 0)
        self._edges[key] = max(-100, min(100, current + delta))
