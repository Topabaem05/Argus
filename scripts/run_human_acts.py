from __future__ import annotations

import json
import os
from datetime import UTC, datetime
from pathlib import Path

from korean_social_simulator.agents.profile_builder import build_agent_profiles
from korean_social_simulator.config.loader import load_config
from korean_social_simulator.evaluation.metrics import evaluate_run
from korean_social_simulator.models import SimulationExecution, SimulationResult
from korean_social_simulator.reporting.markdown import render_report
from korean_social_simulator.safety.validator import validate_safety
from korean_social_simulator.scenarios.compiler import compile_scenario
from korean_social_simulator.simulation.concordia_adapter import run_simulation
from korean_social_simulator.simulation.dry_run import run_dry_run
from korean_social_simulator.storage.run_store import RunStore

CONFIG_PATH = Path("examples/run_human_acts.yaml")
FIXTURE_PATH = Path("data/samples/personas_fixture.jsonl")


def run_pipeline():
    print("=" * 60)
    print("  Han Kang - Human Acts  Simulation")
    print("  Nvidia NIM  LLM Backend")
    print("=" * 60)

    phase("1/7", "Loading configuration")
    has_live_key = bool(os.environ.get("NVIDIA_API_KEY") or os.environ.get("KSSIM_LLM_API_KEY"))
    config = load_config(CONFIG_PATH, dry_run_override=not has_live_key)
    if config.runtime.dry_run:
        print("  No live key found; using offline dry-run fallback.")
    print(f"  Scenario: {config.scenario.title}")
    print(f"  Hypothesis: {config.scenario.hypothesis[:80]}...")

    phase("2/7", "Loading personas from fixture")
    from korean_social_simulator.data.loader import load_personas_fixture

    personas = load_personas_fixture(FIXTURE_PATH)
    print(f"  Loaded {len(personas)} persona records")

    phase("3/7", "Sampling + building agent profiles")
    from korean_social_simulator.personas.sampler import sample_population

    sample = sample_population(personas, config.sampling)
    print(f"  Sampled {len(sample.records)} personas (seed={config.sampling.seed})")

    age_groups: dict[str, list[str]] = {}
    for r in sample.records:
        decade = f"{r.age // 10 * 10}s"
        age_groups.setdefault(decade, []).append(f"{r.occupation}({r.district})")
    for decade, occs in sorted(age_groups.items(), key=lambda x: x[0]):
        print(f"  {decade}: {len(occs)} personas - occupations: {', '.join(occs[:3])}")

    profiles = build_agent_profiles(sample, language="ko")
    print(f"  Built {len(profiles)} agent profiles")

    phase("4/7", "Compiling scenario plan")
    sconfig = config.scenario
    sconfig.participant_count = len(profiles)
    plan = compile_scenario(
        sconfig,
        run_id=config.runtime.run_id,
        plan_id=f"{config.runtime.run_id}-plan",
        dry_run=config.runtime.dry_run,
    )
    print(f"  Plan: {plan.plan_id}, turns={plan.max_turns}, agents={len(profiles)}")

    phase("5/7", "Running safety validation")
    result = validate_safety(plan, profiles, config.safety)
    print(f"  Safety: allowed={result.allowed}, reason={result.reason[:60]}...")

    phase("6/7", "Running simulation")
    if config.runtime.dry_run:
        sim_result = SimulationExecution(
            run_id=plan.run_id,
            status="success",
            events=run_dry_run(plan, profiles),
            warnings=["Offline dry-run fallback used because live credentials were unavailable."],
            errors=[],
        )
    else:
        sim_result = run_simulation(plan, profiles)
        if sim_result.status != "success" and not sim_result.events:
            errors_text = "; ".join(sim_result.errors)
            print(f"  Live adapter unavailable: {sim_result.status} - {errors_text}")
            sim_result = SimulationExecution(
                run_id=plan.run_id,
                status="partial",
                events=run_dry_run(plan, profiles),
                warnings=[
                    *sim_result.warnings,
                    "Offline dry-run fallback used after live adapter failure.",
                ],
                errors=sim_result.errors,
            )

    print(f"  Status: {sim_result.status}")
    print(f"  Events generated: {len(sim_result.events)}")

    phase("7/7", "Evaluating metrics + generating report")
    events = sim_result.events
    events_count = len(events)
    print(f"  Total persisted events: {events_count}")

    metrics_list = config.scenario.metrics or ["event_count", "turn_count", "agent_count"]
    all_metric_names = list(
        dict.fromkeys([*metrics_list, "event_count", "turn_count", "agent_count"])
    )
    metrics_result = evaluate_run(events, all_metric_names)
    print(f"  Metrics: {json.dumps(metrics_result.metrics, ensure_ascii=False)}")

    run_dir = Path(config.runtime.output_dir) / config.runtime.run_id
    store = RunStore(run_dir=run_dir, overwrite=True)
    (run_dir / "sample.json").write_text(
        json.dumps(sample.model_dump(mode="json"), ensure_ascii=False, indent=2, sort_keys=True)
        + "\n",
        encoding="utf-8",
    )
    (run_dir / "profiles.json").write_text(
        json.dumps(
            [profile.model_dump(mode="json") for profile in profiles],
            ensure_ascii=False,
            indent=2,
            sort_keys=True,
        )
        + "\n",
        encoding="utf-8",
    )
    (run_dir / "plan.json").write_text(
        json.dumps(plan.model_dump(mode="json"), ensure_ascii=False, indent=2, sort_keys=True)
        + "\n",
        encoding="utf-8",
    )
    store.write_events_batch(events)
    store.write_metrics(metrics_result.model_dump(mode="json"))
    store.write_metrics_csv(metrics_result.metrics)

    report = render_report(
        run_id=plan.run_id,
        status=sim_result.status,
        metrics=metrics_result.metrics,
        events=events,
        scenario_title=plan.scenario_spec.title,
        scenario_hypothesis=plan.scenario_spec.hypothesis,
        safety_notes=profiles[0].safety_notes if profiles else None,
        errors=sim_result.errors,
        warnings=sim_result.warnings,
    )

    store.write_metadata(
        {
            "run_id": plan.run_id,
            "plan_id": plan.plan_id,
            "scenario_id": plan.scenario_spec.scenario_id,
            "family": plan.scenario_spec.family,
            "dry_run": plan.dry_run,
            "backend": "dry_run" if plan.dry_run else "nvidia_nim",
            "model": "deepseek-ai/deepseek-v4-pro",
            "metric_names": list(metrics_result.metrics.keys()),
            "persona_count": len(profiles),
            "timestamp": datetime.now(UTC).isoformat(),
            "artifact_paths": {
                "run_metadata": str(run_dir / "run_metadata.json"),
                "sample": str(run_dir / "sample.json"),
                "profiles": str(run_dir / "profiles.json"),
                "plan": str(run_dir / "plan.json"),
                "events": str(run_dir / "events.jsonl"),
                "metrics_json": str(run_dir / "metrics.json"),
                "metrics_csv": str(run_dir / "metrics.csv"),
                "report": str(run_dir / "report.md"),
            },
        }
    )

    store.finalize(SimulationResult(run_id=plan.run_id, status=sim_result.status))

    report_path = run_dir / "report.md"
    report_path.write_text(report, encoding="utf-8")

    print()
    print("--- Report Preview (first 600 chars) ---")
    print(report[:600])
    print()

    report_full = report_path.read_text(encoding="utf-8")
    for keyword in (
        "## Simulation Report",
        "## Summary",
        "## Metrics",
        "## Limitations",
        "Han Kang",
        "Gwangju",
    ):
        line_num = report_full.find(keyword)
        if line_num == -1:
            print(f"  MISSING: {keyword} not found in report")

    print()
    print(f"Report: {report_path}")
    print(f"Metrics JSONL: {run_dir / 'metrics.json'}")
    print(f"Metrics CSV: {run_dir / 'metrics.csv'}")
    print(f"Events JSONL: {run_dir / 'events.jsonl'}")
    print(f"Metadata: {run_dir / 'run_metadata.json'}")
    print()
    print("Pipeline complete.")


def phase(index: str, label: str):
    print(f"\n  [{index}] {label}...")


if __name__ == "__main__":
    run_pipeline()
