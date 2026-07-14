"""Game engine public API with cycle-safe lazy exports."""

from __future__ import annotations

__all__ = [
    "EconomicSystem",
    "GameLoopController",
    "GameRunner",
    "GameStateManager",
    "PlayerCommandSystem",
    "RandomEventSystem",
    "ScoringSystem",
    "TaskSystem",
    "run_demo_game",
]


def __getattr__(name: str) -> object:
    """Load the requested game subsystem without eagerly importing the runner graph."""

    if name == "PlayerCommandSystem":
        from korean_social_simulator.game.commands import PlayerCommandSystem

        return PlayerCommandSystem
    if name == "EconomicSystem":
        from korean_social_simulator.game.economy import EconomicSystem

        return EconomicSystem
    if name == "RandomEventSystem":
        from korean_social_simulator.game.events import RandomEventSystem

        return RandomEventSystem
    if name == "GameLoopController":
        from korean_social_simulator.game.loop import GameLoopController

        return GameLoopController
    if name in {"GameRunner", "run_demo_game"}:
        from korean_social_simulator.game import runner

        return getattr(runner, name)
    if name == "ScoringSystem":
        from korean_social_simulator.game.scoring import ScoringSystem

        return ScoringSystem
    if name == "GameStateManager":
        from korean_social_simulator.game.state import GameStateManager

        return GameStateManager
    if name == "TaskSystem":
        from korean_social_simulator.game.task_system import TaskSystem

        return TaskSystem
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")
