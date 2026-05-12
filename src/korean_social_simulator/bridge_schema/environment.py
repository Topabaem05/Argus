from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

from korean_social_simulator.bridge_schema.events import Vec3

CameraPreset = Literal["simulation_free", "top_down", "follow_group", "replay_cinematic"]
LightingPreset = Literal["neutral_day", "classroom", "office", "outdoor_day", "home_warm", "test"]


class EnvironmentLoadEvent(BaseModel):
    """Unity-visible environment selection and spawn metadata."""

    model_config = ConfigDict(extra="forbid")

    background_id: str = Field(min_length=1)
    display_name: str = Field(min_length=1)
    spawn_capacity: int = Field(ge=1, le=50)
    bounds_min: Vec3
    bounds_max: Vec3
    camera_preset: CameraPreset = "simulation_free"
    lighting_preset: LightingPreset = "neutral_day"
    public_summary: str = Field(min_length=1)


class UiStatusEvent(BaseModel):
    """Public status update for Unity run panels."""

    model_config = ConfigDict(extra="forbid")

    status: Literal["configuring", "preparing", "running", "completed", "blocked", "failed"]
    message: str = Field(min_length=1)
    progress: float | None = Field(default=None, ge=0.0, le=1.0)


class SimulationSummaryEvent(BaseModel):
    """Final public simulation summary suitable for Unity display."""

    model_config = ConfigDict(extra="forbid")

    run_id: str = Field(min_length=1)
    status: Literal["success", "partial", "failed", "blocked"]
    agent_count: int = Field(ge=0)
    event_count: int = Field(ge=0)
    public_summary: str = Field(min_length=1)
    report_path: str | None = None
    replay_path: str | None = None
