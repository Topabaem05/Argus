from __future__ import annotations

from korean_social_simulator.evaluation.metrics import evaluate_run
from korean_social_simulator.models import SimulationEvent


def _make_event(turn: int, actor_id: str | None = None) -> SimulationEvent:
    return SimulationEvent(
        run_id="run-001",
        turn=turn,
        event_type="observation",
        actor_id=actor_id,
        payload={"dry_run": True},
    )


def test_metrics_evaluates_known_metrics() -> None:
    """GIVEN known metric names WHEN evaluate_run executes THEN it returns computed values."""
    events = [_make_event(1, "agent-001"), _make_event(2, "agent-001"), _make_event(2, "agent-002")]

    result = evaluate_run(events, ["event_count", "turn_count"])

    assert result.run_id == "run-001"
    assert result.metrics["event_count"] == 3
    assert result.metrics["turn_count"] == 2
    assert result.unavailable_metrics == []


def test_metrics_marks_unrecognized_metrics_unavailable() -> None:
    """GIVEN an unknown metric WHEN evaluate_run executes THEN it records the metric as unavailable."""
    result = evaluate_run([_make_event(1, "agent-001")], ["unknown_metric"])

    assert result.metrics == {}
    assert result.unavailable_metrics == ["unknown_metric"]


def test_metrics_trust_score_is_deterministic() -> None:
    """GIVEN the same events WHEN trust_score is evaluated twice THEN both results match."""
    events = [_make_event(1, "agent-001"), _make_event(2, "agent-002"), _make_event(3, "agent-003")]

    first_result = evaluate_run(events, ["trust_score"])
    second_result = evaluate_run(events, ["trust_score"])

    assert first_result.metrics["trust_score"] == second_result.metrics["trust_score"]
    assert first_result.metrics["trust_score"] == 0.03


def test_metrics_empty_events_produce_zero_counts() -> None:
    """GIVEN no events WHEN evaluate_run executes THEN event and turn counts are zero."""
    result = evaluate_run([], ["event_count", "turn_count"])

    assert result.run_id == "unknown"
    assert result.metrics["event_count"] == 0
    assert result.metrics["turn_count"] == 0
    assert result.errors == []


def test_metrics_agent_count_is_correct() -> None:
    """GIVEN repeated and missing actor IDs WHEN evaluate_run executes THEN it counts distinct non-null agents."""
    events = [
        _make_event(1, "agent-001"),
        _make_event(1, "agent-002"),
        _make_event(2, "agent-002"),
        _make_event(2, "agent-003"),
        _make_event(3, None),
    ]

    result = evaluate_run(events, ["agent_count"])

    assert result.metrics["agent_count"] == 3


def test_metrics_product_market_metrics() -> None:
    result = evaluate_run([], ["interest_score", "purchase_intent", "trial_intent"])

    assert result.metrics == {
        "interest_score": 0.0,
        "purchase_intent": 0.0,
        "trial_intent": 0.0,
    }
    assert result.unavailable_metrics == []


def test_metrics_content_culture_metrics() -> None:
    result = evaluate_run([], ["emotional_resonance", "share_intent", "controversy_risk"])

    assert result.metrics == {
        "emotional_resonance": 0.0,
        "share_intent": 0.0,
        "controversy_risk": 0.0,
    }
    assert result.unavailable_metrics == []


def test_metrics_mixed_metrics() -> None:
    result = evaluate_run(
        [_make_event(1, "agent-001")], ["trust_score", "virality_score", "unknown_metric"]
    )

    assert result.metrics["trust_score"] == 0.01
    assert result.metrics["virality_score"] == 0.0
    assert result.unavailable_metrics == ["unknown_metric"]
