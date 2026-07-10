"""Reputation system — mood, loyalty, and reputation perception tracker."""

from __future__ import annotations

from dataclasses import dataclass

from korean_social_simulator.game.state import GameStateManager


@dataclass
class ReputationSystem:
    manager: GameStateManager

    def player_reputation(self, player_id: str) -> int:
        total = 0
        count = 0
        for company in self.manager.game_state.companies.values():
            for emp in company.employees:
                if emp.is_active:
                    total += emp.reputation_perception.get(player_id, 0)
                    count += 1
        return total // count if count else 0

    def apply_rumor_effect(self, employee_id: str, target_player_id: str, intensity: int) -> None:
        self.manager.adjust_loyalty(employee_id, target_player_id, -intensity)
        self.manager.adjust_mood(employee_id, -intensity // 2)
        emp = self.manager.employee(employee_id)
        current = emp.reputation_perception.get(target_player_id, 0)
        emp.reputation_perception[target_player_id] = max(-100, current - intensity)

    def command_acceptance_threshold(self, employee_id: str, player_id: str) -> float:
        emp = self.manager.employee(employee_id)
        loyalty = emp.loyalty_to(player_id)
        mood = emp.mood
        reputation = emp.reputation_perception.get(player_id, 0)
        score = (loyalty + reputation) / 200.0 + (mood - 50) / 100.0
        return max(0.1, min(0.95, 0.5 + score))

    def will_accept_command(self, employee_id: str, player_id: str) -> bool:
        import random

        threshold = self.command_acceptance_threshold(employee_id, player_id)
        return random.random() < threshold

    def mood_summary(self, player_id: str) -> dict[str, int]:
        counts: dict[str, int] = {}
        company = self.manager.company(player_id)
        for emp in company.active_employees():
            level = emp.mood_level()
            counts[level] = counts.get(level, 0) + 1
        return counts
