"""Typed configuration models for Korean Social Simulation Lab."""

from __future__ import annotations

import re
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

from korean_social_simulator.models import AttachmentInput

_RUN_ID_PATTERN = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")
_BRIDGE_SCHEMA_VERSION_PATTERN = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
_LOCAL_BRIDGE_HOSTS = frozenset({"127.0.0.1", "localhost", "::1"})


class AgeRangeFilter(BaseModel):
    """Age range filter for persona sampling."""

    model_config = ConfigDict(extra="forbid")

    min: int = Field(ge=0, le=150)
    max: int = Field(ge=0, le=150)

    @model_validator(mode="after")
    def _check_range(self) -> AgeRangeFilter:
        if self.min > self.max:
            raise ValueError(f"min ({self.min}) must not exceed max ({self.max}).")
        return self


class SamplingFilters(BaseModel):
    """Filters applied to persona sampling."""

    model_config = ConfigDict(extra="forbid")

    age_range: AgeRangeFilter | None = None
    country: str | None = None
    province: str | None = None
    district: str | None = None
    occupation: str | None = None


class DatasetConfig(BaseModel):
    """Configuration for the persona dataset source."""

    model_config = ConfigDict(extra="forbid")

    mode: Literal["fixture", "huggingface"] = "fixture"
    name: str = "nvidia/Nemotron-Personas-Korea"
    split: str = "train"
    fixture_path: str = "data/samples/personas_fixture.jsonl"
    cache_dir: str | None = None
    expand_to_size: int | None = Field(default=None, ge=1)


class SamplingConfig(BaseModel):
    """Configuration for deterministic persona sampling."""

    model_config = ConfigDict(extra="forbid")

    sample_size: int = Field(ge=1)
    seed: int = 42
    allow_smaller_sample: bool = False
    filters: SamplingFilters = Field(default_factory=SamplingFilters)


class LLMConfig(BaseModel):
    """Configuration for the LLM provider."""

    model_config = ConfigDict(extra="forbid")

    provider: str = "openai"
    model: str = "gpt-4"
    api_key: str | None = None
    temperature: float = Field(ge=0.0, le=2.0, default=0.7)
    max_tokens: int = Field(ge=1, default=1024)
    timeout: int = Field(ge=1, default=60)
    retry_count: int = Field(ge=0, default=3)


class EmbedderConfig(BaseModel):
    """Configuration for the embedding provider."""

    model_config = ConfigDict(extra="forbid")

    provider: str = "openai"
    model: str = "text-embedding-3-small"


class RAGConfig(BaseModel):
    """Configuration for optional RAG grounding."""

    model_config = ConfigDict(extra="forbid")

    enabled: bool = False
    required: bool = False
    api_key: str | None = None


class AttachmentPolicyConfig(BaseModel):
    """Attachment limits for local bridge and CLI simulation input."""

    model_config = ConfigDict(extra="forbid")

    allowed_extensions: list[str] = Field(
        default_factory=lambda: [
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".mp4",
            ".mov",
            ".txt",
            ".md",
            ".csv",
            ".json",
            ".pdf",
        ],
        min_length=1,
    )
    max_attachments: int = Field(default=5, ge=0, le=20)
    max_attachment_bytes: int = Field(default=10_485_760, ge=1)

    @field_validator("allowed_extensions")
    @classmethod
    def _normalize_extensions(cls, value: list[str]) -> list[str]:
        normalized: list[str] = []
        for extension in value:
            cleaned = extension.strip().lower()
            if not cleaned:
                raise ValueError("allowed_extensions cannot contain blank values.")
            if not cleaned.startswith("."):
                cleaned = f".{cleaned}"
            if cleaned not in normalized:
                normalized.append(cleaned)
        return normalized


class SimulationInputConfig(BaseModel):
    """Optional user input used by persona selection and bridge simulations."""

    model_config = ConfigDict(extra="forbid")

    chat_text: str = ""
    attachments: list[AttachmentInput] = Field(default_factory=list)
    attachment_policy: AttachmentPolicyConfig = Field(default_factory=AttachmentPolicyConfig)


class PersonaSelectionConfig(BaseModel):
    """Deterministic selector guardrails."""

    model_config = ConfigDict(extra="forbid")

    enabled: bool = False
    max_personas: int = Field(default=20, ge=1, le=20)
    seed: int = 42


class PersonaMemoryUpdateConfig(BaseModel):
    """Opt-in persona memory update behavior."""

    model_config = ConfigDict(extra="forbid")

    enabled: bool = False
    apply_confirmed: bool = False
    output_dir: str = "persona_memory_updates"


class ScenarioIntervention(BaseModel):
    """A scenario intervention definition."""

    model_config = ConfigDict(extra="forbid")

    id: str
    description: str


class ScenarioConfig(BaseModel):
    """Configuration for the simulation scenario."""

    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1)
    family: str = Field(min_length=1)
    title: str = ""
    hypothesis: str = ""
    language: str = "ko"
    participant_count: int = Field(ge=1)
    max_turns: int = Field(ge=1)
    interventions: list[ScenarioIntervention] = Field(default_factory=list)
    metrics: list[str] = Field(default_factory=list)
    safety_notes: list[str] = Field(default_factory=list)


class SafetyPolicy(BaseModel):
    """Safety policy configuration."""

    model_config = ConfigDict(extra="forbid")

    policy_version: str = "1.0"
    block_unsafe: bool = True


class RuntimeConfig(BaseModel):
    """Root runtime configuration for the simulation pipeline."""

    model_config = ConfigDict(extra="forbid")

    runtime: RuntimeSection
    dataset: DatasetConfig
    sampling: SamplingConfig
    scenario: ScenarioConfig
    llm: LLMConfig = Field(default_factory=LLMConfig)
    embedder: EmbedderConfig | None = None
    rag: RAGConfig = Field(default_factory=RAGConfig)
    input: SimulationInputConfig = Field(default_factory=SimulationInputConfig)
    persona_selection: PersonaSelectionConfig = Field(default_factory=PersonaSelectionConfig)
    persona_memory: PersonaMemoryUpdateConfig = Field(default_factory=PersonaMemoryUpdateConfig)
    safety: SafetyPolicy = Field(default_factory=SafetyPolicy)


class RuntimeSection(BaseModel):
    """Runtime-specific configuration."""

    model_config = ConfigDict(extra="forbid")

    run_id: str = Field(min_length=1)
    seed: int = 42
    output_dir: str = "outputs"
    dry_run: bool = True
    max_turns: int = Field(ge=1)
    max_participants: int = Field(ge=1)
    overwrite: bool = False

    @field_validator("run_id")
    @classmethod
    def _validate_run_id(cls, value: str) -> str:
        if not _RUN_ID_PATTERN.fullmatch(value):
            raise ValueError(
                "run_id must be a single path-safe slug using letters, numbers, dots, "
                "underscores, or hyphens."
            )
        if value in {".", ".."}:
            raise ValueError("run_id must not be a relative path segment.")
        return value


class BridgeServerConfig(BaseModel):
    """Local bridge server configuration."""

    model_config = ConfigDict(extra="forbid")

    host: str = "127.0.0.1"
    port: int = Field(default=8765, ge=1, le=65535)
    allow_remote_clients: bool = False
    max_payload_bytes: int = Field(default=1_048_576, ge=1)

    @model_validator(mode="after")
    def _check_remote_binding(self) -> BridgeServerConfig:
        if self.host not in _LOCAL_BRIDGE_HOSTS and not self.allow_remote_clients:
            raise ValueError(
                "server.allow_remote_clients must be true when binding bridge host "
                "outside localhost."
            )
        return self


class BridgeSchemaConfig(BaseModel):
    """Bridge schema behavior."""

    model_config = ConfigDict(extra="forbid")

    version: str = "1.0.0"
    strict: bool = True

    @field_validator("version")
    @classmethod
    def _validate_version(cls, value: str) -> str:
        if _BRIDGE_SCHEMA_VERSION_PATTERN.fullmatch(value) is None:
            raise ValueError("schema.version must use MAJOR.MINOR.PATCH semantic versioning.")
        return value


class BridgeUnityConfig(BaseModel):
    """Unity client synchronization configuration."""

    model_config = ConfigDict(extra="forbid")

    require_ack: bool = True
    ack_timeout_ms: int = Field(default=2000, ge=1)
    reconnect_buffer_size: int = Field(default=1000, ge=0)
    out_of_order_policy: Literal["skip", "buffer"] = "skip"
    client_token: str | None = None


class BridgeReplayConfig(BaseModel):
    """Bridge replay log configuration."""

    model_config = ConfigDict(extra="forbid")

    enabled: bool = True
    output_dir: str = "reports/replays"
    write_policy: Literal["pause_on_failure", "allow_unlogged_runtime"] = "pause_on_failure"


class BridgeWorldBounds(BaseModel):
    """Axis-aligned Unity world bounds."""

    model_config = ConfigDict(extra="forbid")

    min: tuple[float, float, float]
    max: tuple[float, float, float]

    @field_validator("min", "max")
    @classmethod
    def _validate_finite_bounds(
        cls, value: tuple[float, float, float]
    ) -> tuple[float, float, float]:
        if any(not isinstance(component, int | float) for component in value):
            raise ValueError("world bounds must contain numeric values.")
        if any(not (-float("inf") < component < float("inf")) for component in value):
            raise ValueError("world bounds must contain finite values.")
        return value

    @model_validator(mode="after")
    def _check_min_not_greater_than_max(self) -> BridgeWorldBounds:
        for min_value, max_value in zip(self.min, self.max, strict=True):
            if min_value > max_value:
                raise ValueError("world.bounds.min values must not exceed world.bounds.max values.")
        return self


class BridgeWorldConfig(BaseModel):
    """Unity world configuration."""

    model_config = ConfigDict(extra="forbid")

    bounds: BridgeWorldBounds = Field(
        default_factory=lambda: BridgeWorldBounds(
            min=(-20.0, 0.0, -20.0),
            max=(20.0, 5.0, 20.0),
        )
    )


class BridgeSafetyConfig(BaseModel):
    """Bridge safety constraints for visualization and physical events."""

    model_config = ConfigDict(extra="forbid")

    non_graphic_mode: bool = True
    max_physical_intensity: float = Field(default=0.7, ge=0.0, le=1.0)


class BridgeConfig(BaseModel):
    """Root configuration for the local Unity bridge."""

    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    server: BridgeServerConfig = Field(default_factory=BridgeServerConfig)
    schema_config: BridgeSchemaConfig = Field(default_factory=BridgeSchemaConfig, alias="schema")
    unity: BridgeUnityConfig = Field(default_factory=BridgeUnityConfig)
    replay: BridgeReplayConfig = Field(default_factory=BridgeReplayConfig)
    world: BridgeWorldConfig = Field(default_factory=BridgeWorldConfig)
    safety: BridgeSafetyConfig = Field(default_factory=BridgeSafetyConfig)
    log_level: Literal["DEBUG", "INFO", "WARNING", "ERROR", "FATAL"] = "INFO"


class GamePlayerSpec(BaseModel):
    """A single player's initial company configuration."""

    model_config = ConfigDict(extra="forbid")

    player_id: str = Field(min_length=1)
    company_name: str = Field(min_length=1)
    initial_funds: int = Field(ge=0, default=10000)
    employee_count: int = Field(ge=1, le=10, default=3)


class GameConfig(BaseModel):
    """Root configuration for the AI company operation game."""

    model_config = ConfigDict(extra="forbid")

    max_rounds: int = Field(ge=1, le=20, default=5)
    players: list[GamePlayerSpec] = Field(min_length=1, max_length=4)
    salary_per_employee: int = Field(ge=0, default=500)
    orders_per_round: int = Field(ge=0, le=10, default=3)
    slm_provider: Literal["ollama", "vllm", "nim", "none"] = "none"
    slm_model: str = "qwen2.5:7b"
