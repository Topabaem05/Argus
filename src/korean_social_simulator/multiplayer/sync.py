"""State synchronizer — per-player filtered views and broadcast diffs."""

from __future__ import annotations

from dataclasses import dataclass

from korean_social_simulator.game.state import GameStateManager


@dataclass
class StateSynchronizer:
    manager: GameStateManager

    def player_view(self, player_id: str) -> dict[str, object]:
        state = self.manager.game_state
        if player_id not in state.companies:
            raise KeyError(f"Unknown player: {player_id}")
        own_company = state.companies[player_id]
        visible_rivals = {}
        for pid, company in state.companies.items():
            if pid == player_id:
                continue
            visible_rivals[pid] = {
                "company_name": company.company_name,
                "funds": company.funds,
                "employee_count": len(company.active_employees()),
                "completed_tasks": company.completed_tasks,
                "customer_satisfaction": company.customer_satisfaction,
                "is_bankrupt": company.is_bankrupt,
            }
        return {
            "game_id": state.game_id,
            "round_number": state.round_number,
            "phase": state.phase,
            "is_finished": state.is_finished,
            "own_company": {
                "player_id": own_company.player_id,
                "company_name": own_company.company_name,
                "funds": own_company.funds,
                "employees": [
                    {
                        "employee_id": e.employee_id,
                        "display_name": e.display_name,
                        "mood": e.mood,
                        "mood_level": e.mood_level(),
                        "loyalty_to_me": e.loyalty_to(player_id),
                        "stats": {
                            "stamina": e.stats.stamina,
                            "intelligence": e.stats.intelligence,
                            "speed": e.stats.speed,
                            "communication": e.stats.communication,
                        },
                        "is_active": e.is_active,
                    }
                    for e in own_company.employees
                ],
                "pending_orders": [
                    {
                        "order_id": o.order_id,
                        "customer_name": o.customer_name,
                        "category": o.task_category,
                        "difficulty": o.difficulty,
                        "reward": o.reward,
                        "deadline": o.deadline_round,
                    }
                    for o in own_company.pending_orders
                ],
                "completed_tasks": own_company.completed_tasks,
                "failed_tasks": own_company.failed_tasks,
                "customer_satisfaction": own_company.customer_satisfaction,
                "is_bankrupt": own_company.is_bankrupt,
            },
            "rivals": visible_rivals,
            "pending_tasks": [
                {
                    "task_id": t.task_id,
                    "category": t.category,
                    "status": t.status,
                    "assigned_to": t.assigned_to,
                    "progress": t.progress,
                }
                for t in state.task_queue
                if t.assigned_to
                and any(e.employee_id == t.assigned_to for e in own_company.employees)
            ],
        }

    def broadcast_snapshot(self) -> dict[str, dict[str, object]]:
        return {pid: self.player_view(pid) for pid in self.manager.game_state.companies}

    def public_scoreboard(self) -> list[dict[str, object]]:
        ranked = self.manager.rankings()
        return [
            {
                "rank": idx + 1,
                "player_id": pid,
                "company_name": self.manager.company(pid).company_name,
                "value": val,
                "funds": self.manager.company(pid).funds,
            }
            for idx, (pid, val) in enumerate(ranked)
        ]
