"""Session manager — lobby, matchmaking, 4-player game sessions."""

from __future__ import annotations

import uuid
from dataclasses import dataclass, field

from korean_social_simulator.errors import SimulationError
from korean_social_simulator.game.state import GameStateManager

MAX_PLAYERS = 4
MIN_PLAYERS = 1


@dataclass
class PlayerConnection:
    player_id: str
    display_name: str
    company_name: str
    is_ready: bool = False


@dataclass
class SessionManager:
    _lobby: dict[str, PlayerConnection] = field(default_factory=dict)
    _active_game: GameStateManager | None = None
    session_id: str = ""

    def create_session(self) -> str:
        self.session_id = uuid.uuid4().hex[:12]
        self._lobby.clear()
        self._active_game = None
        return self.session_id

    def join(self, player_id: str, display_name: str, company_name: str) -> None:
        if self._active_game is not None:
            raise SimulationError("Game already in progress, cannot join.")
        if len(self._lobby) >= MAX_PLAYERS:
            raise SimulationError(f"Lobby full (max {MAX_PLAYERS}).")
        if player_id in self._lobby:
            raise SimulationError(f"Player {player_id} already in lobby.")
        self._lobby[player_id] = PlayerConnection(
            player_id=player_id, display_name=display_name, company_name=company_name
        )

    def leave(self, player_id: str) -> None:
        self._lobby.pop(player_id, None)

    def set_ready(self, player_id: str, ready: bool = True) -> None:
        if player_id not in self._lobby:
            raise SimulationError(f"Player {player_id} not in lobby.")
        self._lobby[player_id].is_ready = ready

    def all_ready(self) -> bool:
        return bool(self._lobby) and all(p.is_ready for p in self._lobby.values())

    def start_game(self, max_rounds: int = 5) -> GameStateManager:
        if self._active_game is not None:
            raise SimulationError("Game already started.")
        if len(self._lobby) < MIN_PLAYERS:
            raise SimulationError(f"Need at least {MIN_PLAYERS} player(s).")
        if not self.all_ready():
            raise SimulationError("Not all players are ready.")
        specs = [(p.player_id, p.company_name) for p in self._lobby.values()]
        self._active_game = GameStateManager.new_game(specs, max_rounds=max_rounds)
        return self._active_game

    @property
    def active_game(self) -> GameStateManager:
        if self._active_game is None:
            raise SimulationError("No active game.")
        return self._active_game

    def end_game(self) -> None:
        self._active_game = None
        self._lobby.clear()

    def lobby_state(self) -> dict[str, object]:
        return {
            "session_id": self.session_id,
            "player_count": len(self._lobby),
            "max_players": MAX_PLAYERS,
            "players": [
                {
                    "id": p.player_id,
                    "name": p.display_name,
                    "company": p.company_name,
                    "ready": p.is_ready,
                }
                for p in self._lobby.values()
            ],
            "all_ready": self.all_ready(),
            "game_active": self._active_game is not None,
        }
