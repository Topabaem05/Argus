"""Economic system — income, expenses, order generation, contracts."""

from __future__ import annotations

import random
from dataclasses import dataclass

from korean_social_simulator.errors import SimulationError
from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.game.task_system import TaskSystem
from korean_social_simulator.models import Order

_SALARY_PER_EMPLOYEE = 500
_RENT_PER_ROUND = 1000
_SATISFACTION_BONUS_THRESHOLD = 70
_SATISFACTION_BONUS = 200


@dataclass
class EconomicSystem:
    manager: GameStateManager
    task_system: TaskSystem
    salary_per_employee: int = 300
    rent_per_round: int = 500

    def generate_round_orders(self, round_number: int, count: int = 3) -> dict[str, list[Order]]:
        orders_by_player: dict[str, list[Order]] = {}
        for player_id in self.manager.game_state.companies:
            orders = self.task_system.generate_orders(round_number, count)
            for order in orders:
                self.manager.company(player_id).pending_orders.append(order)
            orders_by_player[player_id] = orders
        return orders_by_player

    def settle_round(self, round_number: int) -> dict[str, dict[str, int]]:
        results: dict[str, dict[str, int]] = {}
        for player_id, company in self.manager.game_state.companies.items():
            income = 0
            expenses = 0
            active = company.active_employees()
            expenses += len(active) * self.salary_per_employee
            expenses += self.rent_per_round
            if company.customer_satisfaction >= _SATISFACTION_BONUS_THRESHOLD:
                income += _SATISFACTION_BONUS
            net = income - expenses
            self.manager.adjust_funds(player_id, net)
            results[player_id] = {
                "income": income,
                "expenses": expenses,
                "net": net,
                "funds_after": company.funds,
            }
            self.manager.log_event(
                "metric_hook",
                player_id,
                {
                    "economy_settle": round_number,
                    "income": income,
                    "expenses": expenses,
                    "net": net,
                },
            )
        return results

    def bid_for_contract(self, player_id: str, order: Order, bid_amount: int) -> bool:
        company = self.manager.company(player_id)
        if bid_amount > company.funds:
            raise SimulationError(f"Player {player_id} cannot afford bid {bid_amount}.")
        if order in company.pending_orders:
            return True
        rng = random.Random(hash(order.order_id) % 2**32)
        if rng.random() < 0.5:
            company.pending_orders.append(order)
            return True
        return False
