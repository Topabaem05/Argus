from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

CommandOutcome = Literal["accept", "reluctant_accept", "refuse", "complain", "quit"]
RumorKind = Literal["salary", "personality", "ability", "positive", "false"]


class PlayerCommandPayload(BaseModel):
    """Payload for a player command issued to an AI employee."""

    model_config = ConfigDict(extra="forbid")

    command_id: str = Field(min_length=1)
    player_id: str = Field(min_length=1)
    action: str = Field(min_length=1)
    target_employee_id: str | None = None
    target_player_id: str | None = None
    round_number: int = Field(ge=1)


class TaskUpdatePayload(BaseModel):
    """Payload for a task status change visible to Unity."""

    model_config = ConfigDict(extra="forbid")

    task_id: str = Field(min_length=1)
    employee_id: str | None = None
    category: str = Field(min_length=1)
    status: str = Field(min_length=1)
    progress: float = Field(ge=0.0, le=1.0)
    reward: int = Field(ge=0)


class RumorEventPayload(BaseModel):
    """Payload for a rumor propagation event."""

    model_config = ConfigDict(extra="forbid")

    rumor_id: str = Field(min_length=1)
    kind: RumorKind
    source_player_id: str = Field(min_length=1)
    target_player_id: str = Field(min_length=1)
    content: str = Field(min_length=1)
    credibility: float = Field(ge=0.0, le=1.0)


class EconomyUpdatePayload(BaseModel):
    """Payload for a per-round economy settlement update."""

    model_config = ConfigDict(extra="forbid")

    player_id: str = Field(min_length=1)
    round_number: int = Field(ge=1)
    income: int = Field(ge=0)
    expenses: int = Field(ge=0)
    funds_after: int = Field(ge=0)


class GameStateSyncPayload(BaseModel):
    """Payload for synchronizing full game state to a client."""

    model_config = ConfigDict(extra="forbid")

    game_id: str = Field(min_length=1)
    round_number: int = Field(ge=1)
    phase: str = Field(min_length=1)
    player_ids: list[str] = Field(default_factory=list)
    is_finished: bool = False


__all__ = [
    "CommandOutcome",
    "EconomyUpdatePayload",
    "GameStateSyncPayload",
    "PlayerCommandPayload",
    "RumorEventPayload",
    "RumorKind",
    "TaskUpdatePayload",
]
