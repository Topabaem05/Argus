from __future__ import annotations

import json
from pathlib import Path

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
    assert report.startswith("# Simulation Report: golden-run-001")
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
