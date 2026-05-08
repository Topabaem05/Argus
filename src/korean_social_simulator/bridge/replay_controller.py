from __future__ import annotations

from pathlib import Path

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, ConfigDict, Field

from korean_social_simulator.bridge.replay_store import ReplayStore
from korean_social_simulator.bridge_schema import BridgeEnvelope
from korean_social_simulator.errors import StorageError


class ReplayControlError(ValueError):
    """Replay control request cannot be applied to the current state."""


class ReplayLoadRequest(BaseModel):
    """HTTP request body for loading a bridge replay JSONL file."""

    model_config = ConfigDict(extra="forbid")

    path: str = Field(min_length=1)


class ReplayController:
    """In-memory replay controller for observer-driven bridge playback."""

    def __init__(self) -> None:
        self._events: tuple[BridgeEnvelope, ...] = ()
        self._loaded_path: Path | None = None
        self._paused = True
        self._next_index = 0
        self._last_emitted_sequence: int | None = None

    def load(self, replay_path: str | Path) -> dict[str, object]:
        """Load a bridge replay file and reset cursor state."""
        path = Path(replay_path)
        self._events = tuple(ReplayStore(path).load_events())
        self._loaded_path = path
        self._paused = True
        self._next_index = 0
        self._last_emitted_sequence = None
        return self.status()

    def pause(self) -> dict[str, object]:
        """Pause automatic replay emission."""
        self._paused = True
        return self.status()

    def resume(self) -> dict[str, object]:
        """Resume automatic replay emission."""
        self._ensure_loaded()
        self._paused = False
        return self.status()

    def step(self) -> BridgeEnvelope | None:
        """Return exactly one replay event and advance the cursor."""
        self._ensure_loaded()
        if self._next_index >= len(self._events):
            return None

        envelope = self._events[self._next_index].model_copy(deep=True)
        self._next_index += 1
        self._last_emitted_sequence = envelope.sequence
        return envelope

    def status(self) -> dict[str, object]:
        """Return replay controller state without exposing hidden simulation state."""
        event_count = len(self._events)
        return {
            "loaded": self._loaded_path is not None,
            "loaded_path": str(self._loaded_path) if self._loaded_path is not None else None,
            "paused": self._paused,
            "event_count": event_count,
            "next_index": self._next_index,
            "completed": self._loaded_path is not None and self._next_index >= event_count,
            "last_emitted_sequence": self._last_emitted_sequence,
        }

    def _ensure_loaded(self) -> None:
        if self._loaded_path is None:
            raise ReplayControlError("No bridge replay is loaded.")


def register_replay_routes(app: FastAPI, controller: ReplayController) -> None:
    """Register HTTP replay control endpoints on the bridge app."""

    @app.post("/replay/load")
    def load_replay(request: ReplayLoadRequest) -> dict[str, object]:
        try:
            return controller.load(request.path)
        except StorageError as exc:
            raise HTTPException(status_code=400, detail=str(exc)) from exc

    @app.get("/replay/status")
    def replay_status() -> dict[str, object]:
        return controller.status()

    @app.post("/replay/pause")
    def pause_replay() -> dict[str, object]:
        return controller.pause()

    @app.post("/replay/resume")
    def resume_replay() -> dict[str, object]:
        try:
            return controller.resume()
        except ReplayControlError as exc:
            raise HTTPException(status_code=400, detail=str(exc)) from exc

    @app.post("/replay/step")
    def step_replay() -> dict[str, object]:
        try:
            envelope = controller.step()
        except ReplayControlError as exc:
            raise HTTPException(status_code=400, detail=str(exc)) from exc
        return {
            "event": envelope.model_dump(mode="json") if envelope is not None else None,
            "status": controller.status(),
        }
