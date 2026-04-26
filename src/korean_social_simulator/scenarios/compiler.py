"""Scenario compiler that validates and produces SimulationPlans."""

from __future__ import annotations

from korean_social_simulator.config.models import ScenarioConfig
from korean_social_simulator.errors import ScenarioValidationError
from korean_social_simulator.models import (
    RetrievedContext,
    ScenarioIntervention,
    ScenarioSpec,
    SimulationPlan,
)
from korean_social_simulator.scenarios.registry import (
    get_default_metrics,
    is_supported_family,
    list_supported_families,
)


def compile_scenario(
    config: ScenarioConfig,
    context: RetrievedContext | None = None,
    run_id: str = "run_default",
    plan_id: str = "plan_default",
) -> SimulationPlan:
    """Compile a scenario config into a typed SimulationPlan.

    Applies default metrics, validates family support, and packages
    configuration into an executable plan.

    Raises:
        ScenarioValidationError: If the family is unknown or config is incomplete.
    """
    if not is_supported_family(config.family):
        raise ScenarioValidationError(
            f"Unknown scenario family '{config.family}'. "
            f"Supported families: {', '.join(list_supported_families())}"
        )

    metrics = list(config.metrics) if config.metrics else get_default_metrics(config.family)

    interventions = [
        ScenarioIntervention(id=intervention.id, description=intervention.description)
        for intervention in config.interventions
    ]

    rag_queries: list[str] = []
    if context is not None and context.status == "available":
        rag_queries.append(context.query)

    spec = ScenarioSpec(
        scenario_id=config.id,
        family=config.family,
        title=config.title,
        hypothesis=config.hypothesis,
        participant_count=config.participant_count,
        max_turns=config.max_turns,
        interventions=interventions,
        metrics=metrics,
        rag_queries=rag_queries,
    )

    return SimulationPlan(
        plan_id=plan_id,
        run_id=run_id,
        scenario_spec=spec,
        agent_count=config.participant_count,
        max_turns=config.max_turns,
        language=config.language,
        dry_run=True,
    )
