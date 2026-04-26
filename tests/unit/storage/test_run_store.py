from __future__ import annotations

import json
from pathlib import Path

import pytest

from korean_social_simulator.errors import StorageError
from korean_social_simulator.models import EventType, SimulationEvent, SimulationResult
from korean_social_simulator.storage.run_store import RunStore


def _make_event(
    turn: int,
    event_type: EventType = "system",
    actor_id: str | None = None,
) -> SimulationEvent:
    return SimulationEvent(
        run_id="run-001",
        turn=turn,
        event_type=event_type,
        actor_id=actor_id,
        timestamp=f"2026-04-27T00:00:0{turn}+00:00",
        payload={"step": turn},
    )


def _make_result(status: str = "success") -> SimulationResult:
    return SimulationResult(run_id="run-001", status=status)


def test_run_store_write_event_appends_jsonl(tmp_path: Path) -> None:
    """GIVEN two events WHEN they are written THEN events.jsonl stores one JSON object per line."""
    run_dir = tmp_path / "run-001"
    store = RunStore(run_dir)

    store.write_event(_make_event(turn=1))
    store.write_event(_make_event(turn=2, event_type="observation", actor_id="agent-001"))

    raw_lines = (run_dir / "events.jsonl").read_text(encoding="utf-8").splitlines()
    parsed_lines = [json.loads(line) for line in raw_lines]

    assert len(raw_lines) == 2
    assert parsed_lines[0]["turn"] == 1
    assert parsed_lines[1]["event_type"] == "observation"
    assert parsed_lines[1]["actor_id"] == "agent-001"
    assert {"run_id", "turn", "event_type", "timestamp", "payload"} <= set(parsed_lines[0])


def test_run_store_write_metadata_creates_file(tmp_path: Path) -> None:
    """GIVEN metadata WHEN it is written THEN run_metadata.json exists with the expected JSON content."""
    run_dir = tmp_path / "run-001"
    store = RunStore(run_dir)
    metadata = {"scenario_id": "product_reaction", "dry_run": True, "turns": 3}

    store.write_metadata(metadata)

    metadata_path = run_dir / "run_metadata.json"
    assert metadata_path.exists()
    assert json.loads(metadata_path.read_text(encoding="utf-8")) == metadata


def test_run_store_write_metrics_creates_json(tmp_path: Path) -> None:
    run_dir = tmp_path / "run-001"
    store = RunStore(run_dir)
    metrics = {"event_count": 22, "turn_count": 3, "status": "success"}

    store.write_metrics(metrics)

    metrics_path = run_dir / "metrics.json"
    assert metrics_path.exists()
    assert json.loads(metrics_path.read_text(encoding="utf-8")) == metrics


def test_run_store_write_metrics_csv_creates_csv(tmp_path: Path) -> None:
    run_dir = tmp_path / "run-001"
    store = RunStore(run_dir)
    metrics = {"event_count": 22, "turn_count": 3}

    store.write_metrics_csv(metrics)

    csv_path = run_dir / "metrics.csv"
    assert csv_path.exists()
    assert csv_path.read_text(encoding="utf-8").splitlines() == [
        "metric,value",
        "event_count,22",
        "turn_count,3",
    ]


def test_run_store_overwrite_protection(tmp_path: Path) -> None:
    """GIVEN an existing event log WHEN a new store is opened without overwrite THEN StorageError is raised."""
    run_dir = tmp_path / "run-001"
    store = RunStore(run_dir)
    store.write_event(_make_event(turn=1))

    with pytest.raises(StorageError, match=r"already contains events\.jsonl"):
        RunStore(run_dir, overwrite=False)


def test_run_store_overwrite_clears_stale_metrics_artifacts(tmp_path: Path) -> None:
    run_dir = tmp_path / "run-001"
    store = RunStore(run_dir)
    store.write_metrics({"event_count": 22})
    store.write_metrics_csv({"event_count": 22})

    assert (run_dir / "metrics.json").exists()
    assert (run_dir / "metrics.csv").exists()

    RunStore(run_dir, overwrite=True)

    assert not (run_dir / "metrics.json").exists()
    assert not (run_dir / "metrics.csv").exists()


def test_run_store_finalize_updates_paths(tmp_path: Path) -> None:
    """GIVEN stored events WHEN finalize runs THEN the returned result includes the stable events path."""
    run_dir = tmp_path / "run-001"
    store = RunStore(run_dir)
    store.write_events_batch([_make_event(turn=1), _make_event(turn=2)])

    finalized_result = store.finalize(_make_result())
    metadata = json.loads((run_dir / "run_metadata.json").read_text(encoding="utf-8"))

    assert finalized_result.events_path == str(run_dir / "events.jsonl")
    assert metadata["events_path"] == str(run_dir / "events.jsonl")
    assert metadata["status"] == "success"


def test_run_store_write_events_batch(tmp_path: Path) -> None:
    """GIVEN a batch of three events WHEN they are written THEN events.jsonl contains three JSONL lines."""
    run_dir = tmp_path / "run-001"
    store = RunStore(run_dir)

    store.write_events_batch([_make_event(turn=1), _make_event(turn=2), _make_event(turn=3)])

    raw_lines = (run_dir / "events.jsonl").read_text(encoding="utf-8").splitlines()
    assert len(raw_lines) == 3
