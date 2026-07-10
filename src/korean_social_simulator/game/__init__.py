"""Game engine for the AI company operation game."""

from __future__ import annotations

from korean_social_simulator.game.commands import PlayerCommandSystem
from korean_social_simulator.game.economy import EconomicSystem
from korean_social_simulator.game.events import RandomEventSystem
from korean_social_simulator.game.loop import GameLoopController
from korean_social_simulator.game.runner import GameRunner, run_demo_game
from korean_social_simulator.game.scoring import ScoringSystem
from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.game.task_system import TaskSystem

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
