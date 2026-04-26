from __future__ import annotations

from korean_social_simulator.models import MetricsResult, SimulationEvent


def evaluate_run(
    events: list[SimulationEvent],
    metric_names: list[str],
) -> MetricsResult:
    """Evaluate requested placeholder metrics for a simulation run.

    Computes deterministic, counting-based metrics from the provided event
    list without calling any LLM, network service, or external runtime.

    Raises:
        None: This evaluator records unavailable metrics instead of raising.
    """
    run_id = events[0].run_id if events else "unknown"
    event_count = len(events)
    turn_count = max((event.turn for event in events), default=0)
    agent_count = len({event.actor_id for event in events if event.actor_id is not None})

    metric_values: dict[str, str | int | float | None] = {}
    unavailable_metrics: list[str] = []

    for metric_name in metric_names:
        if metric_name == "event_count":
            metric_values[metric_name] = event_count
            continue

        if metric_name == "turn_count":
            metric_values[metric_name] = turn_count
            continue

        if metric_name == "agent_count":
            metric_values[metric_name] = agent_count
            continue

        if metric_name == "trust_score":
            metric_values[metric_name] = 0.0 if event_count == 0 else min(1.0, event_count / 100.0)
            continue

        if metric_name in {"confusion_rate", "backlash_rate", "conversion_intent"}:
            metric_values[metric_name] = 0.0
            continue

        unavailable_metrics.append(metric_name)

    return MetricsResult(
        run_id=run_id,
        metrics=metric_values,
        unavailable_metrics=unavailable_metrics,
        errors=[],
    )
