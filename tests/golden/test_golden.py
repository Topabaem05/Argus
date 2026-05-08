from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.models import SimulationEvent
from korean_social_simulator.reporting.markdown import render_report

GOLDEN_DIR = Path(__file__).parent


def test_golden_fixtures_loadable() -> None:
    profile = json.loads(
        (GOLDEN_DIR / "agent_profiles" / "expected_profile.json").read_text(encoding="utf-8")
    )
    assert {
        "agent_id",
        "persona_uuid",
        "display_name",
        "language",
        "background",
        "memory_seeds",
        "goals",
        "behavior_rules",
        "safety_notes",
    }.issubset(profile)

    metrics = json.loads(
        (GOLDEN_DIR / "metrics" / "expected_metrics.json").read_text(encoding="utf-8")
    )
    assert {"run_id", "metrics", "unavailable_metrics", "errors"}.issubset(metrics)
    assert {"event_count", "turn_count", "agent_count"}.issubset(metrics["metrics"])

    report = (GOLDEN_DIR / "reports" / "expected_report.md").read_text(encoding="utf-8")
    assert report.startswith("## Simulation Report: golden-run-001")
    assert "## Summary" in report
    assert "## Limitations" in report

    plan = json.loads(
        (GOLDEN_DIR / "scenario_plans" / "expected_plan.json").read_text(encoding="utf-8")
    )
    assert {
        "plan_id",
        "run_id",
        "scenario_family",
        "agent_count",
        "max_turns",
        "language",
        "dry_run",
    }.issubset(plan)


def test_golden_report_matches_renderer() -> None:
    events = [
        SimulationEvent(
            run_id="golden-run-001",
            turn=1,
            event_type="system",
            timestamp="2026-04-27T00:00:01+00:00",
            payload={"phase": "turn_start", "dry_run": True},
        )
    ]

    rendered = render_report(
        run_id="golden-run-001",
        status="success",
        metrics={"event_count": 1, "turn_count": 1, "agent_count": 0},
        events=events,
        scenario_title="Golden scenario",
        scenario_hypothesis="Golden reports remain stable.",
        safety_notes=["Synthetic participants only."],
    )
    expected = (GOLDEN_DIR / "reports" / "expected_report.md").read_text(encoding="utf-8")

    assert rendered == expected.rstrip("\n")
