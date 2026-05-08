from __future__ import annotations

import importlib.util
import json
import sys
from pathlib import Path


def _load_complex_module():
    script_path = (
        Path(__file__).resolve().parents[2] / "scripts" / "run_complex_scenario_analysis.py"
    )
    spec = importlib.util.spec_from_file_location("run_complex_scenario_analysis", script_path)
    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load run_complex_scenario_analysis.py")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def test_complex_analysis_runs_pipeline_and_writes_results(tmp_path: Path) -> None:
    module = _load_complex_module()

    result = module.run_complex_analysis(output_root=tmp_path)

    assert result["pipeline_status"] == {
        "cosmetics": "success",
        "worker_law": "success",
    }
    for run_id in ("complex_cosmetics_20s", "complex_worker_law"):
        run_dir = tmp_path / run_id
        for name in (
            "run_metadata.json",
            "sample.json",
            "profiles.json",
            "plan.json",
            "events.jsonl",
            "metrics.json",
            "metrics.csv",
            "report.md",
        ):
            assert (run_dir / name).is_file(), f"{run_id}/{name}"
        metadata = json.loads((run_dir / "run_metadata.json").read_text(encoding="utf-8"))
        assert metadata["sample_size"] == 3000
        assert metadata["agent_count"] == 3000

    analysis_json = tmp_path / "complex_scenario_analysis.json"
    analysis_report = tmp_path / "complex_scenario_analysis.md"
    assert analysis_json.is_file()
    assert analysis_report.is_file()

    payload = json.loads(analysis_json.read_text(encoding="utf-8"))
    cosmetics = payload["cosmetics"]
    worker_law = payload["worker_law"]

    assert cosmetics["persona_count"] == 3000
    assert worker_law["persona_count"] == 3000
    assert cosmetics["twenties_persona_count"] >= 500
    assert cosmetics["twenties_top_variant"]
    assert cosmetics["ranked_variants"][0]["average_score"] > 0
    assert "Song" in analysis_report.read_text(encoding="utf-8")

    stances = {row["stance"] for row in worker_law["persona_results"]}
    assert {"likes", "mixed", "dislikes"}.issubset(stances)
    assert worker_law["short_term_burden"]
    assert worker_law["stance_by_age"]
    assert len(payload["source_notes"]) >= 4
    assert "Minimum personas per scenario: 3000" in analysis_report.read_text(encoding="utf-8")
