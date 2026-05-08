from __future__ import annotations

import pytest

from korean_social_simulator.bridge.ack_tracker import AckTracker
from korean_social_simulator.bridge_schema import BridgeEnvelope, UnityAck


class ManualClock:
    def __init__(self, now_ms: int = 0) -> None:
        self.now_ms = now_ms

    def __call__(self) -> int:
        return self.now_ms


def _envelope(sequence: int, message_id: str | None = None) -> BridgeEnvelope:
    return BridgeEnvelope(
        schema_version="1.0.0",
        message_id=message_id or f"msg-{sequence}",
        session_id="session-001",
        sequence=sequence,
        sent_at_ms=0,
        type="simulation.event",
        payload={"sequence": sequence},
    )


def _ack(sequence: int, applied: bool = True, message_id: str | None = None) -> UnityAck:
    return UnityAck(
        acknowledged_message_id=message_id or f"msg-{sequence}",
        acknowledged_sequence=sequence,
        applied=applied,
        warnings=[],
    )


def test_track_sent_records_message_metadata_with_injected_clock() -> None:
    clock = ManualClock(now_ms=123)
    tracker = AckTracker(clock)

    tracked = tracker.track_sent(_envelope(7))

    assert tracked.message_id == "msg-7"
    assert tracked.sequence == 7
    assert tracked.sent_at_ms == 123
    assert tracker.pending_sequences() == [7]


def test_mark_ack_marks_message_applied() -> None:
    clock = ManualClock()
    tracker = AckTracker(clock)
    tracker.track_sent(_envelope(0))

    clock.now_ms = 50
    tracked = tracker.mark_ack(_ack(0, applied=True))

    assert tracked.applied is True
    assert tracked.acknowledged_at_ms == 50
    assert tracker.pending_sequences() == []


def test_ack_for_unknown_sequence_fails() -> None:
    tracker = AckTracker(ManualClock())

    with pytest.raises(ValueError, match="unknown sequence"):
        tracker.mark_ack(_ack(3))


def test_ack_message_id_mismatch_fails() -> None:
    tracker = AckTracker(ManualClock())
    tracker.track_sent(_envelope(0, message_id="msg-0"))

    with pytest.raises(ValueError, match="does not match"):
        tracker.mark_ack(_ack(0, message_id="wrong-message"))


def test_timeout_detection_uses_injected_clock_and_only_pending_messages() -> None:
    clock = ManualClock(now_ms=1000)
    tracker = AckTracker(clock)
    tracker.track_sent(_envelope(0))
    tracker.track_sent(_envelope(1))

    clock.now_ms = 1250
    tracker.mark_ack(_ack(1))

    clock.now_ms = 1500

    assert tracker.timed_out_sequences(timeout_ms=400) == [0]


def test_reconnect_resume_sequence_advances_across_contiguous_applied_acks() -> None:
    tracker = AckTracker(ManualClock())
    tracker.track_sent(_envelope(0))
    tracker.track_sent(_envelope(1))
    tracker.track_sent(_envelope(2))

    tracker.mark_ack(_ack(0))
    tracker.mark_ack(_ack(2))

    assert tracker.reconnect_resume_sequence() == 1

    tracker.mark_ack(_ack(1))

    assert tracker.reconnect_resume_sequence() == 3


def test_rejected_ack_blocks_reconnect_resume() -> None:
    tracker = AckTracker(ManualClock())
    tracker.track_sent(_envelope(0))
    tracker.track_sent(_envelope(1))

    tracker.mark_ack(_ack(0))
    tracker.mark_ack(_ack(1, applied=False))

    assert tracker.pending_sequences() == []
    assert tracker.reconnect_resume_sequence() == 1


def test_duplicate_message_tracking_fails() -> None:
    tracker = AckTracker(ManualClock())
    tracker.track_sent(_envelope(0, message_id="message"))

    with pytest.raises(ValueError, match="sequence already tracked"):
        tracker.track_sent(_envelope(0, message_id="other-message"))

    with pytest.raises(ValueError, match="ID already tracked"):
        tracker.track_sent(_envelope(1, message_id="message"))
