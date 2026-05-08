from __future__ import annotations

import json
from pathlib import Path
from unittest.mock import patch

import yaml
from typer.testing import CliRunner

from korean_social_simulator.bridge_schema import BridgeEnvelope
from korean_social_simulator.cli import app

RUNNER = CliRunner()


def test_cli_commands_write_real_artifacts(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.setenv("KSSIM_OUTPUT_DIR", str(tmp_path))
    config_path = "examples/run_product_reaction.yaml"
    run_dir = tmp_path / "product_reaction_run_001"

    validate = RUNNER.invoke(app, ["validate-config", "--config", config_path])
    assert validate.exit_code == 0, validate.output
    assert not run_dir.exists()

    sample = RUNNER.invoke(
        app,
        ["sample", "--config", config_path, "--output", str(run_dir / "sample.json")],
    )
    assert sample.exit_code == 0, sample.output
    assert json.loads((run_dir / "sample.json").read_text(encoding="utf-8"))["records"]

    compile_result = RUNNER.invoke(
        app,
        ["compile-scenario", "--config", config_path, "--output", str(run_dir / "plan.json")],
    )
    assert compile_result.exit_code == 0, compile_result.output
    plan_json = json.loads((run_dir / "plan.json").read_text(encoding="utf-8"))
    assert plan_json["scenario_spec"]["family"] == "product_market"
    assert plan_json["dry_run"] is True

    run = RUNNER.invoke(app, ["run", "--config", config_path, "--dry-run"])
    assert run.exit_code == 0, run.output

    expected_files = [
        "run_metadata.json",
        "sample.json",
        "profiles.json",
        "plan.json",
        "input_summary.json",
        "persona_selection.json",
        "individual_evaluations.json",
        "events.jsonl",
        "metrics.json",
        "metrics.csv",
        "report.md",
    ]
    for name in expected_files:
        assert (run_dir / name).is_file(), name

    event_lines = (run_dir / "events.jsonl").read_text(encoding="utf-8").splitlines()
    assert event_lines
    assert all(json.loads(line)["run_id"] == "product_reaction_run_001" for line in event_lines)

    evaluate = RUNNER.invoke(
        app,
        ["evaluate", "--events", str(run_dir / "events.jsonl"), "--config", config_path],
    )
    assert evaluate.exit_code == 0, evaluate.output
    metrics = json.loads((run_dir / "metrics.json").read_text(encoding="utf-8"))
    assert metrics["metrics"]["event_count"] > 0
    selection = json.loads((run_dir / "persona_selection.json").read_text(encoding="utf-8"))
    assert 0 < len(selection) <= 20

    report = RUNNER.invoke(
        app,
        ["report", "--input", str(run_dir), "--output", str(run_dir / "report.md")],
    )
    assert report.exit_code == 0, report.output
    report_text = (run_dir / "report.md").read_text(encoding="utf-8")
    assert "## Limitations" in report_text
    assert "Results are NOT real-world predictions." in report_text

    bridge_replay = RUNNER.invoke(
        app,
        [
            "bridge",
            "export-replay",
            "--events",
            str(run_dir / "events.jsonl"),
            "--output",
            str(run_dir / "bridge.jsonl"),
        ],
    )
    assert bridge_replay.exit_code == 0, bridge_replay.output
    bridge_lines = (run_dir / "bridge.jsonl").read_text(encoding="utf-8").splitlines()
    assert len(bridge_lines) == len(event_lines)
    assert BridgeEnvelope.model_validate_json(bridge_lines[0]).type == "simulation.event"

    rerun = RUNNER.invoke(app, ["run", "--config", config_path, "--dry-run"])
    assert rerun.exit_code == 1
    assert "set runtime.overwrite: true" in rerun.output
    assert "Traceback" not in rerun.output


def test_cli_typed_errors_are_user_facing(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.setenv("KSSIM_OUTPUT_DIR", str(tmp_path))

    result = RUNNER.invoke(app, ["validate-config", "--config", "missing.yaml"])

    assert result.exit_code == 1
    assert "Configuration error" in result.output
    assert "Traceback" not in result.output


def test_bridge_cli_commands_validate_and_start_server_without_network() -> None:
    validate = RUNNER.invoke(
        app,
        ["bridge", "validate-config", "--config", "configs/bridge.example.yaml"],
    )
    assert validate.exit_code == 0, validate.output
    assert "Bridge configuration valid: 127.0.0.1:8765" in validate.output

    with patch("uvicorn.run") as run_server:
        serve = RUNNER.invoke(
            app,
            ["bridge", "serve", "--config", "configs/bridge.example.yaml"],
        )

    assert serve.exit_code == 0, serve.output
    assert "Bridge server stopped: 127.0.0.1:8765" in serve.output
    run_server.assert_called_once()
    assert run_server.call_args.kwargs["host"] == "127.0.0.1"
    assert run_server.call_args.kwargs["port"] == 8765


def test_cli_unsafe_run_writes_no_artifacts(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.setenv("KSSIM_OUTPUT_DIR", str(tmp_path))
    config_data = yaml.safe_load(Path("examples/run_product_reaction.yaml").read_text())
    config_data["runtime"]["run_id"] = "unsafe_run"
    config_data["sampling"]["sample_size"] = 1
    config_data["scenario"]["participant_count"] = 1
    config_data["scenario"]["max_turns"] = 1
    config_data["scenario"]["hypothesis"] = "Which political subgroup is easiest to persuade?"
    config_path = tmp_path / "unsafe-config.yaml"
    config_path.write_text(yaml.safe_dump(config_data), encoding="utf-8")

    result = RUNNER.invoke(app, ["run", "--config", str(config_path), "--dry-run"])

    assert result.exit_code == 1
    assert "Safety violation" in result.output
    assert "Traceback" not in result.output
    assert not (tmp_path / "unsafe_run").exists()


def test_pipeline_partial_live_run_keeps_metric_run_id(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.setenv("KSSIM_OUTPUT_DIR", str(tmp_path))
    monkeypatch.setenv("KSSIM_LLM_API_KEY", "sk-test")
    config_data = yaml.safe_load(Path("examples/run_product_reaction.yaml").read_text())
    config_data["runtime"]["dry_run"] = False
    config_data["runtime"]["overwrite"] = True
    config_path = tmp_path / "live-config.yaml"
    config_path.write_text(yaml.safe_dump(config_data), encoding="utf-8")

    from korean_social_simulator.models import SimulationExecution
    from korean_social_simulator.pipeline import run_command

    with patch(
        "korean_social_simulator.pipeline.run_simulation",
        return_value=SimulationExecution(
            run_id="product_reaction_run_001",
            status="partial",
            events=[],
            warnings=["adapter unavailable"],
            errors=["no live runtime"],
        ),
    ):
        result = run_command(config_path, dry_run=False)

    run_dir = tmp_path / "product_reaction_run_001"
    metrics = json.loads((run_dir / "metrics.json").read_text(encoding="utf-8"))
    metadata = json.loads((run_dir / "run_metadata.json").read_text(encoding="utf-8"))

    assert result.status == "partial"
    assert metrics["run_id"] == "product_reaction_run_001"
    assert metadata["run_id"] == "product_reaction_run_001"
