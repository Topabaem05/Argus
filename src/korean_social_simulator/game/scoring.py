"""Scoring system — company value, rankings, bankruptcy, victory."""

from __future__ import annotations

from dataclasses import dataclass

from korean_social_simulator.game.state import GameStateManager


@dataclass
class ScoringSystem:
    manager: GameStateManager

    def company_value(self, player_id: str) -> int:
        return self.manager.company(player_id).value()

    def rankings(self) -> list[tuple[str, int]]:
        return self.manager.rankings()

    def bankrupt_players(self) -> list[str]:
        return self.manager.bankrupt_players()

    def is_game_over(self) -> bool:
        state = self.manager.game_state
        if state.round_number >= state.max_rounds and state.phase == "evening":
            return True
        active = [pid for pid, c in state.companies.items() if not c.is_bankrupt]
        if len(active) <= 1 and len(state.companies) > 1:
            return True
        return state.is_finished

    def winner(self) -> str | None:
        if not self.is_game_over():
            return None
        ranked = self.rankings()
        if not ranked:
            return None
        return ranked[0][0]

    def final_report(self) -> dict[str, object]:
        ranked = self.rankings()
        return {
            "winner": ranked[0][0] if ranked else None,
            "rankings": [
                {
                    "rank": idx + 1,
                    "player_id": pid,
                    "company_name": self.manager.company(pid).company_name,
                    "value": val,
                    "funds": self.manager.company(pid).funds,
                    "completed_tasks": self.manager.company(pid).completed_tasks,
                    "failed_tasks": self.manager.company(pid).failed_tasks,
                    "customer_satisfaction": self.manager.company(pid).customer_satisfaction,
                    "employees": len(self.manager.company(pid).active_employees()),
                    "bankrupt": self.manager.company(pid).is_bankrupt,
                }
                for idx, (pid, val) in enumerate(ranked)
            ],
            "total_events": len(self.manager.game_state.event_log),
            "rounds_played": self.manager.game_state.round_number,
        }
