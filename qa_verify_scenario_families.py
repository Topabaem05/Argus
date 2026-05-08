from __future__ import annotations

import json
import tempfile
from pathlib import Path

import yaml

from korean_social_simulator.config.models import ScenarioConfig
from korean_social_simulator.errors import ScenarioValidationError
from korean_social_simulator.models import SimulationPlan
from korean_social_simulator.pipeline import run_command
from korean_social_simulator.scenarios.compiler import compile_scenario
from korean_social_simulator.scenarios.registry import (
    FAMILY_DEFAULT_METRICS,
    list_supported_families,
)

REPO_ROOT = Path(__file__).resolve().parent
REQUIRED_ARTIFACTS = [
    "run_metadata.json",
    "sample.json",
    "profiles.json",
    "plan.json",
    "events.jsonl",
    "metrics.json",
    "metrics.csv",
    "report.md",
]

EXPECTED_FAMILIES = [
    "ai_agent_evaluation",
    "community_conflict",
    "content_culture",
    "crisis_risk_communication",
    "customer_support_service",
    "education_learning",
    "finance_consumer_protection",
    "game_npc_social_world",
    "healthcare_wellbeing",
    "marketing_viral",
    "media_information_ecosystem",
    "organization_workplace",
    "policy_public_opinion",
    "product_market",
    "social_norms_behavior",
    "urban_local_life",
]


def _make_config(family: str) -> ScenarioConfig:
    return ScenarioConfig(
        id=f"qa_{family}",
        family=family,
        title=f"QA validation for {family}",
        hypothesis=f"{family} compiles into a valid simulation plan.",
        participant_count=3,
        max_turns=2,
        metrics=[],
    )


def _family_report(family: str) -> dict[str, object]:
    expected_metrics = list(FAMILY_DEFAULT_METRICS.get(family, []))
    result: dict[str, object] = {
        "family": family,
        "compile_valid_simulation_plan": False,
        "default_metrics_registered": False,
        "default_metrics_applied": False,
        "plan_type": None,
        "applied_metrics": [],
        "errors": [],
    }

    result["default_metrics_registered"] = (
        family in FAMILY_DEFAULT_METRICS and len(expected_metrics) > 0
    )

    try:
        plan = compile_scenario(
            _make_config(family),
            run_id=f"run-{family}",
            plan_id=f"plan-{family}",
        )
    except Exception as exc:
        result["errors"] = [f"compile_scenario raised {type(exc).__name__}: {exc}"]
        return result

    result["compile_valid_simulation_plan"] = isinstance(plan, SimulationPlan) and (
        plan.scenario_spec.family == family
    )
    result["plan_type"] = type(plan).__name__
    result["applied_metrics"] = list(plan.scenario_spec.metrics)
    result["default_metrics_applied"] = plan.scenario_spec.metrics == expected_metrics

    return result


def _unknown_family_report() -> dict[str, object]:
    unknown_family = "unknown_family_for_qa"
    config = _make_config(unknown_family)
    try:
        compile_scenario(config)
    except ScenarioValidationError as exc:
        message = str(exc)
        supported_families_listed = all(family in message for family in EXPECTED_FAMILIES)
        return {
            "raises_scenario_validation_error": True,
            "supported_families_listed": supported_families_listed,
            "error_type": type(exc).__name__,
            "message": message,
        }
    except Exception as exc:
        return {
            "raises_scenario_validation_error": False,
            "supported_families_listed": False,
            "error_type": type(exc).__name__,
            "message": str(exc),
        }

    return {
        "raises_scenario_validation_error": False,
        "supported_families_listed": False,
        "error_type": None,
        "message": "compile_scenario unexpectedly accepted an unknown family.",
    }


def _pipeline_report() -> dict[str, object]:
    config_path = REPO_ROOT / "examples" / "run_product_reaction.yaml"
    raw_config = yaml.safe_load(config_path.read_text(encoding="utf-8"))
    run_id = f"{raw_config['runtime']['run_id']}_qa_verification"
    raw_config["runtime"]["run_id"] = run_id
    raw_config["runtime"]["dry_run"] = True
    raw_config["runtime"]["overwrite"] = True

    with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".yaml", delete=False) as handle:
        yaml.safe_dump(raw_config, handle, allow_unicode=True, sort_keys=False)
        temp_config_path = Path(handle.name)

    try:
        finalized_result = run_command(temp_config_path, dry_run=True)
    finally:
        temp_config_path.unlink(missing_ok=True)

    run_dir = REPO_ROOT / raw_config["runtime"]["output_dir"] / run_id
    metadata = json.loads((run_dir / "run_metadata.json").read_text(encoding="utf-8"))
    metrics_result = json.loads((run_dir / "metrics.json").read_text(encoding="utf-8"))
    report_text = (run_dir / "report.md").read_text(encoding="utf-8")
    event_count = sum(
        1 for line in (run_dir / "events.jsonl").read_text(encoding="utf-8").splitlines() if line
    )

    report_sections = [
        "## Simulation Report",
        "## Summary",
        "## Metrics",
        "## Event Examples",
        "## Limitations",
        "## Follow-up Validation",
    ]
    artifact_exists = {artifact: (run_dir / artifact).exists() for artifact in REQUIRED_ARTIFACTS}

    return {
        "family": metadata["family"],
        "fixture_loading": metadata["sample_size"] > 0,
        "sampling": metadata["sample_size"] == raw_config["sampling"]["sample_size"],
        "profile_building": metadata["agent_count"] == raw_config["scenario"]["participant_count"],
        "safety_validation": finalized_result.status == "success",
        "dry_run": metadata["dry_run"] is True and event_count == metadata["event_count"],
        "artifacts_complete": all(artifact_exists.values()),
        "storage": Path(finalized_result.events_path or "").exists(),
        "metrics": metrics_result["unavailable_metrics"] == []
        and len(metrics_result["metrics"]) > 0,
        "report": all(section in report_text for section in report_sections),
        "artifact_paths": {
            "run_dir": str(run_dir),
            "events_path": finalized_result.events_path,
            "metrics_path": finalized_result.metrics_path,
            "report_path": finalized_result.report_path,
        },
        "artifact_exists": artifact_exists,
        "event_count": event_count,
        "metrics_result": metrics_result,
    }


def main() -> int:
    registered_families = list_supported_families()
    family_reports = [_family_report(family) for family in EXPECTED_FAMILIES]
    unknown_family = _unknown_family_report()
    pipeline = _pipeline_report()

    registry_matches_expected = registered_families == sorted(EXPECTED_FAMILIES)
    all_family_checks_pass = all(
        report["compile_valid_simulation_plan"]
        and report["default_metrics_registered"]
        and report["default_metrics_applied"]
        for report in family_reports
    )
    pipeline_pass = all(
        pipeline[step]
        for step in [
            "fixture_loading",
            "sampling",
            "profile_building",
            "safety_validation",
            "dry_run",
            "artifacts_complete",
            "storage",
            "metrics",
            "report",
        ]
    )
    unknown_family_pass = (
        unknown_family["raises_scenario_validation_error"]
        and unknown_family["supported_families_listed"]
    )

    qa_report = {
        "requirement": "Repository Stabilization RS-002/RS-007/RS-011",
        "requirements_spec": str(
            REPO_ROOT / "specs" / "repository-stabilization" / "requirements.md"
        ),
        "registered_families": registered_families,
        "expected_families": EXPECTED_FAMILIES,
        "registry_matches_expected": registry_matches_expected,
        "family_results": family_reports,
        "unknown_family_result": unknown_family,
        "end_to_end_pipeline": pipeline,
        "overall_pass": registry_matches_expected
        and all_family_checks_pass
        and unknown_family_pass
        and pipeline_pass,
    }

    qa_report_path = REPO_ROOT / "outputs" / "qa_scenario_family_report.json"
    qa_report_path.parent.mkdir(parents=True, exist_ok=True)
    qa_report_path.write_text(
        json.dumps(qa_report, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )

    print(json.dumps(qa_report, ensure_ascii=False, indent=2, sort_keys=True))
    print(f"QA report written to: {qa_report_path}")

    return 0 if qa_report["overall_pass"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
