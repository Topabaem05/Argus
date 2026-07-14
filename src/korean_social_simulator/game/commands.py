"""Player command system — resolves player actions into state mutations."""

from __future__ import annotations

import uuid
from collections.abc import Callable
from dataclasses import dataclass

from korean_social_simulator.errors import SimulationError
from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.models import (
    CommandAction,
    GameEmployee,
    PlayerCommand,
)

_OWN_EMPLOYEE_ACTIONS: frozenset[CommandAction] = frozenset(
    {"praise", "scold", "snack", "raise", "bonus", "fire", "assign_task"}
)
_RIVAL_EMPLOYEE_ACTIONS: frozenset[CommandAction] = frozenset({"gossip", "scout"})


@dataclass
class CommandResult:
    command_id: str
    action: CommandAction
    success: bool
    message: str
    side_effects: dict[str, object]


@dataclass
class PlayerCommandSystem:
    manager: GameStateManager

    def execute(self, command: PlayerCommand) -> CommandResult:
        state = self.manager.game_state
        if state.is_finished:
            raise SimulationError("Cannot execute commands on a finished game.")
        if command.round_number != state.round_number:
            raise SimulationError(
                f"Command round {command.round_number} != current round {state.round_number}."
            )
        if command.player_id not in state.companies:
            raise SimulationError(f"Unknown player: {command.player_id}")

        self._validate_target_authority(command)
        handler = _HANDLERS.get(command.action)
        if handler is None:
            raise SimulationError(f"Unhandled action: {command.action}")
        result: CommandResult = handler(self, command)
        self.manager.log_event(
            "agent_action",
            command.player_id,
            {
                "action": command.action,
                "command_id": command.command_id,
                "result": result.message,
            },
        )
        return result

    def _validate_target_authority(self, command: PlayerCommand) -> None:
        if command.action not in _OWN_EMPLOYEE_ACTIONS | _RIVAL_EMPLOYEE_ACTIONS:
            return

        target = self._target_employee(command)
        if command.action in _OWN_EMPLOYEE_ACTIONS and target.employed_by != command.player_id:
            raise SimulationError(
                f"Action {command.action!r} requires an employee owned by {command.player_id}."
            )
        if command.action in _RIVAL_EMPLOYEE_ACTIONS and target.employed_by in {
            None,
            command.player_id,
        }:
            raise SimulationError(
                f"Action {command.action!r} requires an active rival employee."
            )

    def _target_employee(self, command: PlayerCommand) -> GameEmployee:
        if not command.target_employee_id:
            raise SimulationError(f"Action {command.action!r} requires target_employee_id.")
        target = self.manager.employee(command.target_employee_id)
        if not target.is_active or target.employed_by is None:
            raise SimulationError(f"Employee is not active: {command.target_employee_id}")
        return target

    def _praise(self, cmd: PlayerCommand) -> CommandResult:
        self.manager.adjust_mood(cmd.target_employee_id or "", 10)
        self.manager.adjust_loyalty(cmd.target_employee_id or "", cmd.player_id, 5)
        return CommandResult(cmd.command_id, cmd.action, True, "기분 +10, 호감도 +5", {})

    def _scold(self, cmd: PlayerCommand) -> CommandResult:
        self.manager.adjust_mood(cmd.target_employee_id or "", -20)
        self.manager.adjust_loyalty(cmd.target_employee_id or "", cmd.player_id, -5)
        return CommandResult(cmd.command_id, cmd.action, True, "기분 -20 (단기 효율 ↑)", {})

    def _snack(self, cmd: PlayerCommand) -> CommandResult:
        cost = int(str(cmd.payload.get("cost", 200)))
        self.manager.adjust_funds(cmd.player_id, -cost)
        self.manager.adjust_mood(cmd.target_employee_id or "", 15)
        return CommandResult(cmd.command_id, cmd.action, True, f"기분 +15 (간식 {cost}원)", {})

    def _raise_pay(self, cmd: PlayerCommand) -> CommandResult:
        cost = int(str(cmd.payload.get("cost", 300)))
        self.manager.adjust_funds(cmd.player_id, -cost)
        self.manager.adjust_loyalty(cmd.target_employee_id or "", cmd.player_id, 20)
        return CommandResult(
            cmd.command_id, cmd.action, True, f"호감도 +20 (월급인상 {cost}원/라운드)", {}
        )

    def _bonus(self, cmd: PlayerCommand) -> CommandResult:
        cost = int(str(cmd.payload.get("cost", 500)))
        self.manager.adjust_funds(cmd.player_id, -cost)
        self.manager.adjust_mood(cmd.target_employee_id or "", 25)
        self.manager.adjust_loyalty(cmd.target_employee_id or "", cmd.player_id, 15)
        return CommandResult(
            cmd.command_id, cmd.action, True, f"기분 +25, 호감도 +15 (보너스 {cost}원)", {}
        )

    def _party(self, cmd: PlayerCommand) -> CommandResult:
        cost = int(str(cmd.payload.get("cost", 1000)))
        self.manager.adjust_funds(cmd.player_id, -cost)
        company = self.manager.company(cmd.player_id)
        for emp in company.active_employees():
            self.manager.adjust_mood(emp.employee_id, 20)
        return CommandResult(
            cmd.command_id, cmd.action, True, f"팀 전체 기분 +20 (회식 {cost}원)", {}
        )

    def _fire(self, cmd: PlayerCommand) -> CommandResult:
        company = self.manager.company(cmd.player_id)
        self.manager.remove_employee(cmd.target_employee_id or "")
        for emp in company.active_employees():
            self.manager.adjust_mood(emp.employee_id, -10)
        return CommandResult(cmd.command_id, cmd.action, True, "직원 해고 (남은 직원 기분 -10)", {})

    def _hire(self, cmd: PlayerCommand) -> CommandResult:
        cost = int(str(cmd.payload.get("cost", 1000)))
        self.manager.adjust_funds(cmd.player_id, -cost)
        from korean_social_simulator.game.state import (
            _EMPLOYEE_NAME_POOL,
            _PERSONALITY_TAGS,
            _roll_stats,
        )

        idx = len(self.manager.company(cmd.player_id).employees)
        player_ids = list(self.manager.game_state.companies.keys())
        new_emp = GameEmployee(
            employee_id=f"emp-{uuid.uuid4().hex[:6]}",
            display_name=_EMPLOYEE_NAME_POOL[idx % len(_EMPLOYEE_NAME_POOL)],
            personality_tags=_PERSONALITY_TAGS[idx % len(_PERSONALITY_TAGS)],
            stats=_roll_stats(idx),
            loyalty_map=dict.fromkeys(player_ids, 0),
            reputation_perception=dict.fromkeys(player_ids, 0),
            employed_by=cmd.player_id,
        )
        self.manager.company(cmd.player_id).employees.append(new_emp)
        return CommandResult(
            cmd.command_id, cmd.action, True, f"신규 채용: {new_emp.display_name}", {}
        )

    def _gossip(self, cmd: PlayerCommand) -> CommandResult:
        if not cmd.target_player_id:
            raise SimulationError("Gossip requires target_player_id.")
        if cmd.target_player_id not in self.manager.game_state.companies:
            raise SimulationError(f"Unknown rumor target player: {cmd.target_player_id}")
        rumor = str(cmd.payload.get("rumor", "월급 밀린대"))
        emp = self.manager.employee(cmd.target_employee_id or "")
        self.manager.adjust_loyalty(cmd.target_employee_id or "", cmd.target_player_id, -20)
        self.manager.adjust_mood(cmd.target_employee_id or "", -15)
        emp.memory.append(f"소문: {cmd.target_player_id} - {rumor}")
        return CommandResult(cmd.command_id, cmd.action, True, f"소문 전달: '{rumor}'", {})

    def _scout(self, cmd: PlayerCommand) -> CommandResult:
        emp = self.manager.employee(cmd.target_employee_id or "")
        loyalty = emp.loyalty_to(emp.employed_by or "")
        if loyalty < -20:
            self.manager.reassign_employee(cmd.target_employee_id or "", cmd.player_id)
            cost = int(str(cmd.payload.get("cost", 2000)))
            self.manager.adjust_funds(cmd.player_id, -cost)
            return CommandResult(cmd.command_id, cmd.action, True, "스카우트 성공!", {})
        return CommandResult(cmd.command_id, cmd.action, False, "호감도가 높아 스카우트 실패", {})

    def _assign_task(self, cmd: PlayerCommand) -> CommandResult:
        return CommandResult(cmd.command_id, cmd.action, True, "Task assigned via task system", {})


_HANDLERS: dict[CommandAction, Callable[..., CommandResult]] = {
    "praise": PlayerCommandSystem._praise,
    "scold": PlayerCommandSystem._scold,
    "snack": PlayerCommandSystem._snack,
    "raise": PlayerCommandSystem._raise_pay,
    "bonus": PlayerCommandSystem._bonus,
    "party": PlayerCommandSystem._party,
    "fire": PlayerCommandSystem._fire,
    "hire": PlayerCommandSystem._hire,
    "gossip": PlayerCommandSystem._gossip,
    "scout": PlayerCommandSystem._scout,
    "assign_task": PlayerCommandSystem._assign_task,
}
