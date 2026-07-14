from __future__ import annotations

import pytest

from korean_social_simulator.errors import SimulationError
from korean_social_simulator.game.commands import PlayerCommandSystem
from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.models import PlayerCommand


def _system() -> PlayerCommandSystem:
    manager = GameStateManager.new_game(
        [("p1", "알파상사"), ("p2", "베타테크")],
        max_rounds=1,
    )
    return PlayerCommandSystem(manager)


def _command(
    action: str,
    target_employee_id: str | None,
    target_player_id: str | None = None,
) -> PlayerCommand:
    return PlayerCommand(
        command_id=f"cmd-{action}",
        player_id="p1",
        action=action,  # type: ignore[arg-type]
        target_employee_id=target_employee_id,
        target_player_id=target_player_id,
        round_number=1,
    )


def test_management_command_rejects_rival_employee() -> None:
    system = _system()
    rival = system.manager.company("p2").employees[0]

    with pytest.raises(SimulationError, match="requires an employee owned"):
        system.execute(_command("praise", rival.employee_id))

    assert rival.mood == 50


def test_gossip_rejects_own_employee() -> None:
    system = _system()
    own_employee = system.manager.company("p1").employees[0]

    with pytest.raises(SimulationError, match="requires an active rival employee"):
        system.execute(_command("gossip", own_employee.employee_id, "p2"))

    assert own_employee.mood == 50


def test_gossip_accepts_rival_employee_and_mutates_only_target() -> None:
    system = _system()
    rival = system.manager.company("p2").employees[0]

    result = system.execute(_command("gossip", rival.employee_id, "p2"))

    assert result.success is True
    assert rival.mood == 35
    assert rival.loyalty_to("p2") == -20


def test_scout_rejects_own_employee() -> None:
    system = _system()
    own_employee = system.manager.company("p1").employees[0]

    with pytest.raises(SimulationError, match="requires an active rival employee"):
        system.execute(_command("scout", own_employee.employee_id))
