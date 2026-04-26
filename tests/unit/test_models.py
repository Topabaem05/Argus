"""Unit tests for core data models."""

from __future__ import annotations

import pytest
from pydantic import ValidationError

from korean_social_simulator.models import (
    AgentProfile,
    MetricsResult,
    PersonaRecord,
    PopulationSample,
    RetrievedContext,
    SafetyDecision,
    ScenarioIntervention,
    ScenarioSpec,
    SimulationEvent,
    SimulationPlan,
    SimulationResult,
)

_VALID_PERSONA_DATA: dict[str, object] = {
    "uuid": "p-001",
    "persona": "30대 직장인, 서울 거주",
    "age": 34,
    "occupation": "소프트웨어 엔지니어",
    "district": "강남구",
    "province": "서울특별시",
    "country": "South Korea",
}


def test_persona_record_required_fields() -> None:
    """Required fields are enforced on PersonaRecord."""
    record = PersonaRecord.model_validate(_VALID_PERSONA_DATA)
    assert record.uuid == "p-001"
    assert record.age == 34
    assert record.occupation == "소프트웨어 엔지니어"


def test_persona_record_missing_required_fails() -> None:
    """Missing uuid, persona, age, occupation, district, or province fails."""
    data = dict(_VALID_PERSONA_DATA)
    del data["uuid"]
    with pytest.raises(ValidationError):
        PersonaRecord.model_validate(data)


def test_persona_record_optional_fields_default_none() -> None:
    """Optional fields default to None."""
    record = PersonaRecord.model_validate(_VALID_PERSONA_DATA)
    assert record.sex is None
    assert record.professional_persona is None


def test_population_sample_creation() -> None:
    """PopulationSample holds records and sampling metadata."""
    persona = PersonaRecord.model_validate(_VALID_PERSONA_DATA)
    sample = PopulationSample(
        sample_id="s-001",
        seed=42,
        records=[persona],
        source="fixture",
    )
    assert sample.sample_id == "s-001"
    assert len(sample.records) == 1
    assert sample.seed == 42


def test_agent_profile_required_fields() -> None:
    """AgentProfile validates required fields."""
    profile = AgentProfile(
        agent_id="agent-001",
        persona_uuid="p-001",
        display_name="김민수",
        language="ko",
        background="30대 소프트웨어 엔지니어",
        memory_seeds=["서울 강남구 거주"],
        goals=["제품 평가"],
        behavior_rules=["한국어로 대화"],
        safety_notes=["합성 페르소나입니다."],
    )
    assert profile.agent_id == "agent-001"
    assert profile.language == "ko"
    assert "한국어로 대화" in profile.behavior_rules


def test_scenario_spec_creation() -> None:
    """ScenarioSpec validates participant_count and max_turns."""
    spec = ScenarioSpec(
        scenario_id="prod-r-001",
        family="product_reaction",
        title="Test Product",
        hypothesis="Privacy messaging wins",
        participant_count=10,
        max_turns=5,
        metrics=["trust_score", "backlash_rate"],
    )
    assert spec.family == "product_reaction"
    assert len(spec.metrics) == 2


def test_simulation_plan_creation() -> None:
    """SimulationPlan connects scenario spec to run parameters."""
    spec = ScenarioSpec(
        scenario_id="prod-r-001",
        family="product_reaction",
        title="Test",
        hypothesis="H",
        participant_count=5,
        max_turns=3,
    )
    plan = SimulationPlan(
        plan_id="plan-001",
        run_id="run-001",
        scenario_spec=spec,
        agent_count=5,
        max_turns=3,
        language="ko",
        dry_run=True,
    )
    assert plan.agent_count == 5
    assert plan.dry_run is True


def test_simulation_event_payload() -> None:
    """SimulationEvent includes run_id, turn, event_type, and payload."""
    event = SimulationEvent(
        run_id="run-001",
        turn=1,
        event_type="agent_action",
        actor_id="agent-001",
        payload={"action": "respond", "text": "이 제품은 좋아 보입니다."},
    )
    assert event.run_id == "run-001"
    assert event.event_type == "agent_action"
    assert event.payload["action"] == "respond"


def test_simulation_result_statuses() -> None:
    """SimulationResult accepts valid statuses and rejects invalid."""
    result = SimulationResult(
        run_id="run-001",
        status="success",
    )
    assert result.status == "success"

    result2 = SimulationResult(run_id="run-002", status="partial")
    assert result2.status == "partial"

    with pytest.raises(ValidationError):
        SimulationResult(run_id="run-003", status="invalid_status")


def test_metrics_result_records_unavailable() -> None:
    """MetricsResult separates available and unavailable metrics."""
    result = MetricsResult(
        run_id="run-001",
        metrics={"trust_score": 0.75},
        unavailable_metrics=["advanced_sentiment"],
    )
    assert result.metrics["trust_score"] == 0.75
    assert "advanced_sentiment" in result.unavailable_metrics


def test_safety_decision_allowed() -> None:
    """SafetyDecision indicates allowed/blocked with reason."""
    decision = SafetyDecision(allowed=True, reason="No safety issues detected.")
    assert decision.allowed is True

    blocked = SafetyDecision(allowed=False, reason="Political targeting", blocked_rule="R-001")
    assert blocked.allowed is False
    assert blocked.blocked_rule == "R-001"


def test_retrieved_context_status_literals() -> None:
    """RetrievedContext only accepts valid status values."""
    ctx = RetrievedContext(provider="pageindex", status="skipped", query="test")
    assert ctx.status == "skipped"

    with pytest.raises(ValidationError):
        RetrievedContext(provider="pageindex", status="error", query="test")


def test_scenario_intervention_id_required() -> None:
    """ScenarioIntervention requires id field."""
    intervention = ScenarioIntervention(id="msg_a", description="Productivity message")
    assert intervention.id == "msg_a"

    with pytest.raises(ValidationError):
        ScenarioIntervention(description="Missing id")
