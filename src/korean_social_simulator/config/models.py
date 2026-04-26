"""Typed configuration models for Korean Social Simulation Lab."""

from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator


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
