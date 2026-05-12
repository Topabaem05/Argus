from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

from korean_social_simulator.bridge_schema.events import EmotionState, Vec3

BehaviorIntent = Literal[
    "idle",
    "observe",
    "approach",
    "avoid",
    "speak",
    "ask",
    "argue",
    "apologize",
    "warn",
    "deescalate",
    "leave",
    "celebrate",
    "think",
]
LocomotionMode = Literal["idle", "walk", "run"]


class AgentBehaviorIntentEvent(BaseModel):
    """Public Unity-visible intent derived from persona evaluation."""

    model_config = ConfigDict(extra="forbid")

    agent_id: str = Field(min_length=1)
    intent: BehaviorIntent
    target_agent_ids: list[str] = Field(default_factory=list)
    target_position: Vec3 | None = None
    locomotion: LocomotionMode = "walk"
    emotion: EmotionState | None = None
    animation_hint: str | None = Field(default=None, min_length=1)
    urgency: float = Field(default=0.5, ge=0.0, le=1.0)
    duration_ms: int = Field(default=1500, ge=0)
    public_reason: str = Field(min_length=1)
    safety_tags: list[str] = Field(default_factory=list)


class AgentAnimationEvent(BaseModel):
    """Optional direct animation hint for Unity animation drivers."""

    model_config = ConfigDict(extra="forbid")

    agent_id: str = Field(min_length=1)
    animation_hint: str = Field(min_length=1)
    duration_ms: int = Field(default=1000, ge=0)
    crossfade_ms: int = Field(default=120, ge=0)
    public_reason: str = Field(min_length=1)
