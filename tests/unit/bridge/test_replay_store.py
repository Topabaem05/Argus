from __future__ import annotations

import pytest

from korean_social_simulator.bridge import ReplayStore
from korean_social_simulator.bridge_schema import BridgeEnvelope
from korean_social_simulator.errors import StorageError


def _envelope(sequence: int) -> BridgeEnvelope:
    return BridgeEnvelope.model_validate(
        {
            "schema_version": "1.0.0",
            "message_id": f"msg-{sequence}",
            "correlation_id": None,
            "session_id": "session-001",
            "sequence": sequence,
            "sent_at_ms": sequence * 1000,
            "type": "simulation.event",
            "payload": {"value": sequence},
        }
    )


def test_write_event_appends_one_jsonl_line(tmp_path) -> None:
    replay_path = tmp_path / "session.jsonl"
    store = ReplayStore(replay_path)

    store.write_event(_envelope(1))

    lines = replay_path.read_text(encoding="utf-8").splitlines()
    assert len(lines) == 1
    assert BridgeEnvelope.model_validate_json(lines[0]).sequence == 1


def test_write_events_batch_preserves_order(tmp_path) -> None:
    replay_path = tmp_path / "session.jsonl"
    store = ReplayStore(replay_path)

    store.write_events_batch([_envelope(1), _envelope(2), _envelope(3)])

    loaded = store.load_events()
    assert [envelope.sequence for envelope in loaded] == [1, 2, 3]


def test_load_events_skips_blank_lines(tmp_path) -> None:
    replay_path = tmp_path / "session.jsonl"
    store = ReplayStore(replay_path)
    store.write_event(_envelope(1))
    with replay_path.open("a", encoding="utf-8") as file_handle:
        file_handle.write("\n")
    store.write_event(_envelope(2))

    loaded = store.load_events()

    assert [envelope.sequence for envelope in loaded] == [1, 2]


def test_load_events_missing_file_raises(tmp_path) -> None:
    store = ReplayStore(tmp_path / "missing.jsonl")

    with pytest.raises(StorageError, match="Replay log not found"):
        store.load_events()


def test_corrupted_line_reports_line_number(tmp_path) -> None:
    replay_path = tmp_path / "session.jsonl"
    store = ReplayStore(replay_path)
    store.write_event(_envelope(1))
    with replay_path.open("a", encoding="utf-8") as file_handle:
        file_handle.write("{not-json}\n")

    with pytest.raises(StorageError, match="line 2"):
        store.load_events()
