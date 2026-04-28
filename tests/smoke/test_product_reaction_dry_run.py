from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.agents.profile_builder import build_agent_profiles
from korean_social_simulator.config.models import SamplingConfig, ScenarioConfig
from korean_social_simulator.data.loader import load_personas_fixture
from korean_social_simulator.evaluation.metrics import evaluate_run
from korean_social_simulator.models import SimulationResult
from korean_social_simulator.personas.sampler import sample_population
from korean_social_simulator.reporting.markdown import render_report
from korean_social_simulator.scenarios.compiler import compile_scenario
from korean_social_simulator.simulation.dry_run import run_dry_run
from korean_social_simulator.storage.run_store import RunStore

_FIXTURE_PATH = Path(__file__).resolve().parents[2] / "data" / "samples" / "personas_fixture.jsonl"


def test_full_dry_run_pipeline(tmp_path: Path) -> None:
    """GIVEN fixture personas WHEN the full dry-run pipeline executes THEN it stores events, evaluates metrics, and renders a markdown report."""
    personas = load_personas_fixture(_FIXTURE_PATH)
    assert len(personas) >= 5

    sampling_config = SamplingConfig(sample_size=5, seed=42)
    sample = sample_population(personas, sampling_config)
    repeated_sample = sample_population(personas, sampling_config)

    sampled_uuids = [record.uuid for record in sample.records]
    repeated_uuids = [record.uuid for record in repeated_sample.records]
    assert sampled_uuids == repeated_uuids
    assert len(sample.records) == 5

    profiles = build_agent_profiles(sample, language="ko")
    assert len(profiles) == 5
    assert all(profile.language == "ko" for profile in profiles)

    scenario_config = ScenarioConfig(
        id="smoke_product_reaction",
        family="product_market",
        title="제품 반응 드라이런 스모크 테스트",
        hypothesis="건식 실행에서도 전체 파이프라인 산출물이 일관되게 생성된다.",
        participant_count=5,
        max_turns=3,
    )
    plan = compile_scenario(
        scenario_config,
        run_id="run-smoke-product-reaction",
        plan_id="plan-smoke-product-reaction",
    )

    events = run_dry_run(plan, profiles)
    assert len(events) == (scenario_config.max_turns * (len(profiles) + 2)) + 1
    assert all(
        event.payload.get("dry_run") is True
        for event in events
        if event.payload.get("phase") != "turn_limit_reached"
    )
    assert events[-1].payload == {
        "phase": "turn_limit_reached",
        "max_turns": scenario_config.max_turns,
    }

    run_dir = tmp_path / plan.run_id
    store = RunStore(run_dir=run_dir, overwrite=False)
    store.write_events_batch(events)

    metrics_result = evaluate_run(events, ["event_count", "turn_count", "agent_count"])
    assert metrics_result.metrics == {
        "event_count": 22,
        "turn_count": 3,
        "agent_count": 5,
    }
    assert metrics_result.unavailable_metrics == []

    report = render_report(
        run_id=plan.run_id,
        status="success",
        metrics=metrics_result.metrics,
        events=events,
        scenario_title=plan.scenario_spec.title,
        scenario_hypothesis=plan.scenario_spec.hypothesis,
        safety_notes=profiles[0].safety_notes,
    )

    store.write_metadata(
        {
            "run_id": plan.run_id,
            "plan_id": plan.plan_id,
            "scenario_id": plan.scenario_spec.scenario_id,
            "family": plan.scenario_spec.family,
            "dry_run": plan.dry_run,
            "metric_names": list(metrics_result.metrics.keys()),
        }
    )
    finalized_result = store.finalize(SimulationResult(run_id=plan.run_id, status="success"))

    events_path = run_dir / "events.jsonl"
    metadata_path = run_dir / "run_metadata.json"
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))

    assert "## Simulation Report" in report
    assert "## Summary" in report
    assert "## Metrics" in report
    assert "## Limitations" in report
    assert events_path.exists()
    assert finalized_result.events_path == str(events_path)
    assert metadata_path.exists()
    assert metadata["events_path"] == str(events_path)
    assert metadata["status"] == "success"
