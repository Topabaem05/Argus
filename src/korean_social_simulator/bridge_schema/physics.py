from __future__ import annotations

import math
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator

from korean_social_simulator.bridge_schema.events import Vec3

PhysicsAction = Literal["push", "block", "stumble", "fall", "recover", "separate"]
PhysicsStatus = Literal["success", "fallback", "failed"]
PhysicsOutcome = Literal[
    "no_contact",
    "blocked",
    "stumble",
    "fall",
    "recover",
    "separate",
    "unknown",
]


class PhysicsConstraints(BaseModel):
    """Non-graphic physical-event constraints."""

    model_config = ConfigDict(extra="forbid")

    max_force: float = Field(ge=0.0)
    allow_fall: bool
    allow_contact: bool
    non_graphic_mode: bool

    @field_validator("max_force")
    @classmethod
    def _validate_max_force(cls, value: float) -> float:
        if not math.isfinite(value):
            raise ValueError("max_force must be finite.")
        return value


class PhysicsRequest(BaseModel):
    """Payload requesting deterministic evaluation of an abstract physical event."""

    model_config = ConfigDict(extra="forbid")

    request_id: str = Field(min_length=1)
    event_id: str = Field(min_length=1)
    actor_id: str = Field(min_length=1)
    target_id: str | None = None
    action: PhysicsAction
    actor_position: Vec3
    target_position: Vec3 | None = None
    intensity: float = Field(ge=0.0, le=1.0)
    duration_ms: int = Field(ge=0)
    seed: int
    constraints: PhysicsConstraints


class PhysicsResult(BaseModel):
    """Payload containing a high-level physical-event outcome."""

    model_config = ConfigDict(extra="forbid")

    request_id: str = Field(min_length=1)
    event_id: str = Field(min_length=1)
    status: PhysicsStatus
    outcome: PhysicsOutcome
    affected_agent_ids: list[str] = Field(default_factory=list)
    final_positions: dict[str, Vec3] = Field(default_factory=dict)
    animation_hints: list[str] = Field(default_factory=list)
    confidence: float = Field(ge=0.0, le=1.0)
    warnings: list[str] = Field(default_factory=list)
