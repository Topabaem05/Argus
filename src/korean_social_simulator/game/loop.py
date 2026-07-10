"""Game loop controller — round/phase transitions and end conditions."""

from __future__ import annotations

from dataclasses import dataclass

from korean_social_simulator.errors import SimulationError
from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.models import RoundPhase

_PHASE_ORDER: list[RoundPhase] = ["morning", "work", "event", "evening"]


@dataclass
class GameLoopController:
    manager: GameStateManager

    @property
    def state(self) -> object:
        return self.manager.game_state

    def advance_phase(self) -> RoundPhase:
        state = self.manager.game_state
        if state.is_finished:
            raise SimulationError("Game is already finished.")
        current_idx = _PHASE_ORDER.index(state.phase)
        if current_idx < len(_PHASE_ORDER) - 1:
            state.phase = _PHASE_ORDER[current_idx + 1]
            self.manager.log_event(
                "system",
                None,
                {
                    "phase_change": state.phase,
                    "round": state.round_number,
                },
            )
            return state.phase
        return self._next_round()

    def _next_round(self) -> RoundPhase:
        state = self.manager.game_state
        if state.round_number >= state.max_rounds:
            state.is_finished = True
            self.manager.log_event("system", None, {"game_end": True})
            return state.phase
        state.round_number += 1
        state.phase = "morning"
        self._pay_salaries()
        self.manager.log_event(
            "system",
            None,
            {
                "new_round": state.round_number,
                "phase": state.phase,
            },
        )
        return state.phase

    def _pay_salaries(self) -> None:
        state = self.manager.game_state
        salary = 500
        for pid, company in state.companies.items():
            active = company.active_employees()
            total = len(active) * salary
            if total > 0:
                self.manager.adjust_funds(pid, -total)
                self.manager.log_event(
                    "system",
                    pid,
                    {
                        "salaries_paid": total,
                        "employee_count": len(active),
                    },
                )

    def check_end_conditions(self) -> bool:
        state = self.manager.game_state
        if state.round_number >= state.max_rounds and state.phase == "evening":
            state.is_finished = True
            return True
        if self.manager.bankrupt_players():
            if len(self.manager.bankrupt_players()) >= len(state.companies) - 1:
                state.is_finished = True
                return True
        return state.is_finished
