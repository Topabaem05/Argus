"""Unit tests for scenario registry and compiler."""

from __future__ import annotations

import pytest

from korean_social_simulator.config.models import ScenarioConfig, ScenarioIntervention
from korean_social_simulator.errors import ScenarioValidationError
from korean_social_simulator.models import RetrievedContext, SimulationPlan
from korean_social_simulator.scenarios.compiler import compile_scenario
from korean_social_simulator.scenarios.registry import (
    get_default_metrics,
    is_supported_family,
    list_supported_families,
)


def _make_config(
    family: str = "product_reaction",
    metrics: list[str] | None = None,
    interventions: list[ScenarioIntervention] | None = None,
) -> ScenarioConfig:
    return ScenarioConfig(
        id="test_scenario",
        family=family,
        title="Test Scenario",
        hypothesis="Test hypothesis",
        participant_count=10,
        max_turns=5,
        metrics=metrics or [],
        interventions=interventions or [],
    )


def test_supported_families_compile() -> None:
    """All supported families compile without error."""
    for family in list_supported_families():
        config = _make_config(family=family)
        plan = compile_scenario(config)
        assert isinstance(plan, SimulationPlan)
        assert plan.scenario_spec.family == family


def test_unknown_family_raises() -> None:
    """Unknown family raises ScenarioValidationError."""
    config = _make_config(family="nonexistent_family")
    with pytest.raises(ScenarioValidationError, match="Unknown scenario family"):
        compile_scenario(config)


def test_default_metrics_applied() -> None:
    """When no metrics configured, family defaults are applied."""
    config = _make_config(family="product_reaction", metrics=[])
    plan = compile_scenario(config)
    assert "trust_score" in plan.scenario_spec.metrics
    assert len(plan.scenario_spec.metrics) == 3


def test_explicit_metrics_override_defaults() -> None:
    """Explicit metrics override family defaults."""
    config = _make_config(family="product_reaction", metrics=["custom_metric"])
    plan = compile_scenario(config)
    assert plan.scenario_spec.metrics == ["custom_metric"]


def test_scenario_plan_has_correct_structure() -> None:
    """Compiled plan includes all expected fields."""
    config = _make_config(
        family="rumor_crisis_response",
        metrics=["trust_score"],
        interventions=[ScenarioIntervention(id="msg_a", description="Test message")],
    )
    plan = compile_scenario(config, run_id="r1", plan_id="p1")
    assert plan.plan_id == "p1"
    assert plan.run_id == "r1"
    assert plan.agent_count == 10
    assert plan.max_turns == 5
    assert plan.language == "ko"
    assert plan.dry_run is True
    assert len(plan.scenario_spec.interventions) == 1
    assert plan.scenario_spec.interventions[0].id == "msg_a"


def test_rag_context_included() -> None:
    """RAG context is included in scenario spec when available."""
    ctx = RetrievedContext(
        provider="pageindex",
        status="available",
        query="product safety docs",
    )
    config = _make_config(family="product_reaction")
    plan = compile_scenario(config, context=ctx)
    assert "product safety docs" in plan.scenario_spec.rag_queries


def test_rag_context_skipped() -> None:
    """RAG context with skipped status is not included."""
    ctx = RetrievedContext(
        provider="pageindex",
        status="skipped",
        query="product safety docs",
    )
    config = _make_config(family="product_reaction")
    plan = compile_scenario(config, context=ctx)
    assert len(plan.scenario_spec.rag_queries) == 0


def test_registry_lists_all_nine_families() -> None:
    """Nine supported scenario families are registered."""
    families = list_supported_families()
    assert len(families) == 9
    assert "product_reaction" in families
    assert "game_npc_social_world" in families


def test_is_supported_returns_false_for_unknown() -> None:
    """is_supported_family returns False for unknown family."""
    assert not is_supported_family("nonexistent")


def test_default_metrics_empty_for_unknown() -> None:
    """get_default_metrics returns empty list for unknown family."""
    assert get_default_metrics("unknown_family") == []
