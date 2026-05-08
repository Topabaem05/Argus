from __future__ import annotations

import math
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator

EmotionLabel = Literal["neutral", "happy", "sad", "angry", "afraid", "confused", "excited"]
SpawnReason = Literal["scenario_start", "lazy_spawn", "replay"]
MovementStyle = Literal["walk", "run", "approach", "avoid", "leave"]
SpeechAct = Literal["say", "ask", "argue", "apologize", "warn", "shout"]
ConflictStage = Literal["none", "tension", "argument", "physical_risk", "deescalating", "resolved"]


class Vec3(BaseModel):
    """A finite Unity-style 3D vector."""

    model_config = ConfigDict(extra="forbid")

    x: float
    y: float
    z: float

    @field_validator("x", "y", "z")
    @classmethod
    def _validate_finite(cls, value: float) -> float:
        if not math.isfinite(value):
            raise ValueError("Vec3 coordinates must be finite.")
        return value


class EmotionState(BaseModel):
    """Public emotion label and normalized intensity."""

    model_config = ConfigDict(extra="forbid")

    label: EmotionLabel
    intensity: float = Field(ge=0.0, le=1.0)


class AgentState(BaseModel):
    """Unity-visible public agent state."""

    model_config = ConfigDict(extra="forbid")

    agent_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    group_id: str | None = None
    position: Vec3
    facing: float
    emotion: EmotionState
    current_action: str = Field(min_length=1)
    visible: bool = True

    @field_validator("facing")
    @classmethod
    def _validate_facing(cls, value: float) -> float:
        if not math.isfinite(value):
            raise ValueError("facing must be finite.")
        return value


class AgentSpawnEvent(BaseModel):
    """Payload for spawning or updating an agent avatar."""

    model_config = ConfigDict(extra="forbid")

    agent: AgentState
    spawn_reason: SpawnReason


class AgentMoveEvent(BaseModel):
    """Payload for a Unity-visible movement command."""

    model_config = ConfigDict(extra="forbid")

    agent_id: str = Field(min_length=1)
    start_position: Vec3 | None = None
    target_position: Vec3
    speed_mps: float = Field(ge=0.0)
    movement_style: MovementStyle
    expected_arrival_ms: int | None = Field(default=None, ge=0)


class AgentDialogueEvent(BaseModel):
    """Payload for a Unity-visible dialogue bubble."""

    model_config = ConfigDict(extra="forbid")

    speaker_id: str = Field(min_length=1)
    target_ids: list[str] = Field(default_factory=list)
    text: str = Field(min_length=1)
    emotion: EmotionState | None = None
    speech_act: SpeechAct
    duration_ms: int = Field(ge=0)


class AgentEmotionEvent(BaseModel):
    """Standalone emotion update targeting one agent avatar."""

    model_config = ConfigDict(extra="forbid")

    agent_id: str = Field(min_length=1)
    label: EmotionLabel
    intensity: float = Field(ge=0.0, le=1.0)


class GroupUpdateEvent(BaseModel):
    """Payload for synchronizing group membership and optional presentation hints."""

    model_config = ConfigDict(extra="forbid")

    group_id: str = Field(min_length=1)
    member_agent_ids: list[str] = Field(min_length=1)
    badge_label: str | None = Field(default=None, min_length=1)


class ConflictUpdateEvent(BaseModel):
    """Payload for non-graphic group or conflict visualization."""

    model_config = ConfigDict(extra="forbid")

    conflict_id: str = Field(min_length=1)
    participant_ids: list[str] = Field(min_length=1)
    intensity: float = Field(ge=0.0, le=1.0)
    stage: ConflictStage
    public_summary: str = Field(min_length=1)


class UnityAck(BaseModel):
    """Acknowledgement sent by Unity after applying or rejecting an envelope."""

    model_config = ConfigDict(extra="forbid")

    acknowledged_message_id: str = Field(min_length=1)
    acknowledged_sequence: int = Field(ge=0)
    applied: bool
    warnings: list[str] = Field(default_factory=list)
