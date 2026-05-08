"""Core data models for Korean Social Simulation Lab.

Defines PersonaRecord, PopulationSample, AgentProfile, ScenarioSpec,
SimulationEvent, SimulationResult, and related types.
"""

from __future__ import annotations

from datetime import UTC, datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class PersonaRecord(BaseModel):
    """A single synthetic persona row from Nemotron-Personas-Korea."""

    model_config = ConfigDict(extra="forbid")

    uuid: str
    persona: str
    professional_persona: str | None = None
    family_persona: str | None = None
    cultural_background: str | None = None
    skills_and_expertise: str | None = None
    hobbies_and_interests: str | None = None
    age: int = Field(ge=0, le=150)
    sex: str | None = None
    occupation: str
    district: str
    province: str
    country: str = "South Korea"
    metadata: dict[str, str | int | float | bool | None] = Field(default_factory=dict)


class PopulationSample(BaseModel):
    """A deterministically sampled group of synthetic personas."""

    model_config = ConfigDict(extra="forbid")

    sample_id: str
    seed: int
    filters: dict[str, object] = Field(default_factory=dict)
    records: list[PersonaRecord] = Field(default_factory=list)
    source: str = ""
    created_at: str = Field(default_factory=lambda: datetime.now(UTC).isoformat())


class AgentProfile(BaseModel):
    """A Concordia-ready agent profile built from a PersonaRecord."""

    model_config = ConfigDict(extra="forbid")

    agent_id: str
    persona_uuid: str
    display_name: str
    language: str = "ko"
    background: str
    memory_seeds: list[str] = Field(default_factory=list)
    goals: list[str] = Field(default_factory=list)
    behavior_rules: list[str] = Field(default_factory=list)
    safety_notes: list[str] = Field(default_factory=list)


AttachmentKind = Literal["image", "video", "text", "document", "unknown"]


class AttachmentInput(BaseModel):
    """User-supplied attachment metadata for a simulation request."""

    model_config = ConfigDict(extra="forbid")

    path: str = Field(min_length=1)
    media_type: str | None = None
    size_bytes: int | None = Field(default=None, ge=0)


class AttachmentValidation(BaseModel):
    """Validation outcome for one attachment before simulation use."""

    model_config = ConfigDict(extra="forbid")

    path: str
    filename: str
    extension: str
    kind: AttachmentKind
    accepted: bool
    reason: str
    size_bytes: int | None = Field(default=None, ge=0)


class SimulationInputSummary(BaseModel):
    """Sanitized user input summary passed into persona selection."""

    model_config = ConfigDict(extra="forbid")

    chat_text: str = ""
    attachments: list[AttachmentValidation] = Field(default_factory=list)
    accepted_attachment_count: int = 0
    rejected_attachment_count: int = 0
    topic_summary: str = ""


class PersonaSelectionResult(BaseModel):
    """Why a synthetic persona was selected for a run."""

    model_config = ConfigDict(extra="forbid")

    agent_id: str
    persona_uuid: str
    display_name: str
    reason: str
    confidence: float = Field(ge=0.0, le=1.0)
    matched_terms: list[str] = Field(default_factory=list)
    safety_notes: list[str] = Field(default_factory=list)


class IndividualEvaluation(BaseModel):
    """Structured dry-run individual evaluation for one synthetic persona."""

    model_config = ConfigDict(extra="forbid")

    agent_id: str
    stance: Literal["supports", "opposes", "mixed", "uncertain"]
    confidence: float = Field(ge=0.0, le=1.0)
    rationale: str = Field(min_length=1)
    uncertainty: str = Field(min_length=1)


class DiscussionTurn(BaseModel):
    """Structured dry-run discussion turn."""

    model_config = ConfigDict(extra="forbid")

    round_index: int = Field(ge=1)
    speaker_id: str
    target_ids: list[str] = Field(default_factory=list)
    speech_act: Literal["say", "ask", "argue", "apologize", "warn", "shout"]
    text: str = Field(min_length=1)
    stance_after: Literal["supports", "opposes", "mixed", "uncertain"]
    confidence_after: float = Field(ge=0.0, le=1.0)


class PersonaMemoryProposal(BaseModel):
    """Opt-in proposal for persona memory update; never applied by default."""

    model_config = ConfigDict(extra="forbid")

    persona_uuid: str
    agent_id: str
    proposed_memory: str = Field(min_length=1)
    reason: str = Field(min_length=1)
    evidence_event_ids: list[str] = Field(default_factory=list)
    safe_to_apply: bool = False


class ScenarioIntervention(BaseModel):
    """A single intervention step within a scenario."""

    model_config = ConfigDict(extra="forbid")

    id: str
    description: str


class ScenarioSpec(BaseModel):
    """A compiled scenario specification."""

    model_config = ConfigDict(extra="forbid")

    scenario_id: str
    family: str
    title: str
    hypothesis: str
    allowed_objective: str = ""
    participant_count: int = Field(ge=1)
    max_turns: int = Field(ge=1)
    interventions: list[ScenarioIntervention] = Field(default_factory=list)
    metrics: list[str] = Field(default_factory=list)
    rag_queries: list[str] = Field(default_factory=list)
    rag_warnings: list[str] = Field(default_factory=list)


class SimulationPlan(BaseModel):
    """A compiled, executable plan for a simulation run."""

    model_config = ConfigDict(extra="forbid")

    plan_id: str
    run_id: str
    scenario_spec: ScenarioSpec
    agent_count: int
    max_turns: int
    language: str = "ko"
    dry_run: bool = True


class RetrievedSection(BaseModel):
    """A single retrieved section from RAG."""

    model_config = ConfigDict(extra="forbid")

    section_id: str
    content_preview: str
    source_path: str
    page: int | None = None


class RetrievedContext(BaseModel):
    """Context retrieved from PageIndex MCP or other RAG provider."""

    model_config = ConfigDict(extra="forbid")

    provider: str
    status: Literal["available", "unavailable", "skipped"]
    query: str
    sections: list[RetrievedSection] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)


EventType = Literal[
    "observation", "agent_action", "gm_decision", "metric_hook", "safety_block", "system"
]


class SimulationEvent(BaseModel):
    """A single event emitted during simulation."""

    model_config = ConfigDict(extra="forbid")

    run_id: str
    turn: int = Field(ge=0)
    event_type: EventType
    actor_id: str | None = None
    timestamp: str = Field(default_factory=lambda: datetime.now(UTC).isoformat())
    payload: dict[str, object] = Field(default_factory=dict)


RunStatus = Literal["success", "partial", "failed", "blocked"]


class SimulationResult(BaseModel):
    """Final result of a simulation run."""

    model_config = ConfigDict(extra="forbid")

    run_id: str
    status: RunStatus
    events_path: str | None = None
    metrics_path: str | None = None
    report_path: str | None = None
    errors: list[str] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)


class SimulationExecution(BaseModel):
    """In-memory simulation execution result before artifact persistence."""

    model_config = ConfigDict(extra="forbid")

    run_id: str
    status: RunStatus
    events: list[SimulationEvent] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    errors: list[str] = Field(default_factory=list)


class MetricsResult(BaseModel):
    """Evaluated metrics from a simulation run."""

    model_config = ConfigDict(extra="forbid")

    run_id: str
    metrics: dict[str, str | int | float | None] = Field(default_factory=dict)
    unavailable_metrics: list[str] = Field(default_factory=list)
    errors: list[str] = Field(default_factory=list)


class SafetyDecision(BaseModel):
    """Result of safety validation."""

    model_config = ConfigDict(extra="forbid")

    allowed: bool
    reason: str = ""
    blocked_rule: str | None = None
