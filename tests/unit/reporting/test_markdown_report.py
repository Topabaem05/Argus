from __future__ import annotations

from korean_social_simulator.models import SimulationEvent
from korean_social_simulator.reporting.markdown import render_report


def _make_event(
    turn: int,
    event_type: str = "system",
    payload: dict[str, object] | None = None,
) -> SimulationEvent:
    return SimulationEvent(
        run_id="run-001",
        turn=turn,
        event_type=event_type,
        actor_id="agent-001" if event_type != "system" else None,
        timestamp=f"2026-04-27T00:00:0{turn}+00:00",
        payload=payload or {"label": f"event-{turn}"},
    )


def test_markdown_report_includes_all_sections() -> None:
    """GIVEN a populated run WHEN the report is rendered THEN all expected markdown sections are included."""
    report = render_report(
        run_id="run-001",
        status="success",
        metrics={"trust_score": 0.8},
        events=[_make_event(1), _make_event(2, event_type="observation")],
        scenario_title="Notice comprehension",
        scenario_hypothesis="Shorter copy increases trust.",
        safety_notes=["Synthetic participants only."],
    )

    assert "## Summary" in report
    assert "## Metrics" in report
    assert "## Event Examples" in report
    assert "## Safety Notes" in report
    assert "## Limitations" in report
    assert "## Follow-up Validation" in report


def test_markdown_report_metrics_table() -> None:
    """GIVEN metric values WHEN the report is rendered THEN the metrics table lists each metric and value."""
    report = render_report(
        run_id="run-001",
        status="success",
        metrics={"trust_score": 0.8, "confusion_rate": 0.1},
        events=[_make_event(1)],
    )

    assert "| trust_score | 0.8 |" in report
    assert "| confusion_rate | 0.1 |" in report


def test_markdown_report_partial_identifies_missing() -> None:
    """GIVEN a partial run WHEN the report is rendered THEN it highlights incomplete status and surfaced errors."""
    report = render_report(
        run_id="run-001",
        status="partial",
        metrics={"trust_score": 0.8},
        events=[_make_event(1)],
        warnings=["Metrics may be incomplete."],
        errors=["LLM timeout"],
    )

    assert "partial" in report
    assert "LLM timeout" in report
    assert "incomplete simulation run" in report


def test_markdown_report_no_unsupported_claims() -> None:
    """GIVEN a rendered report WHEN limitations are reviewed THEN it explicitly denies real-world predictive authority."""
    report = render_report(
        run_id="run-001",
        status="success",
        metrics={},
        events=[_make_event(1)],
    )

    assert "Results are NOT real-world predictions." in report
    assert "not as evidence of real-world behavior" in report


def test_markdown_report_event_examples() -> None:
    """GIVEN more than three events WHEN the report is rendered THEN only the first three examples are included."""
    events = [_make_event(turn, payload={"label": f"example-{turn}"}) for turn in range(1, 6)]

    report = render_report(
        run_id="run-001",
        status="success",
        metrics={},
        events=events,
    )

    assert "example-1" in report
    assert "example-2" in report
    assert "example-3" in report
    assert "example-4" not in report
    assert "example-5" not in report


def test_markdown_report_handles_empty_data() -> None:
    """GIVEN empty metrics and events WHEN the report is rendered THEN it still returns all sections with empty-state values."""
    report = render_report(
        run_id="run-001",
        status="success",
        metrics={},
        events=[],
    )

    assert "## Summary" in report
    assert "## Metrics" in report
    assert "## Event Examples" in report
    assert "## Safety Notes" in report
    assert "## Warnings" in report
    assert "## Errors" in report
    assert "- event_count: 0" in report
    assert "- turn_count: 0" in report
    assert "| _none_ | _no metrics recorded_ |" in report
    assert "No event examples available." in report
