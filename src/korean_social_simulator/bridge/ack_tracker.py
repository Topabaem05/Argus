from __future__ import annotations

from collections.abc import Callable
from dataclasses import dataclass, field

from korean_social_simulator.bridge_schema import BridgeEnvelope, UnityAck

ClockMs = Callable[[], int]


@dataclass(frozen=True)
class TrackedMessage:
    """Delivery metadata for one bridge message awaiting Unity ACK."""

    message_id: str
    sequence: int
    sent_at_ms: int
    acknowledged_at_ms: int | None = None
    applied: bool | None = None
    warnings: tuple[str, ...] = field(default_factory=tuple)

    @property
    def is_pending(self) -> bool:
        return self.acknowledged_at_ms is None


class AckTracker:
    """Track Unity ACKs and compute deterministic reconnect resume points."""

    def __init__(self, clock_ms: ClockMs) -> None:
        self._clock_ms = clock_ms
        self._messages_by_sequence: dict[int, TrackedMessage] = {}
        self._sequence_by_message_id: dict[str, int] = {}

    def track_sent(self, envelope: BridgeEnvelope) -> TrackedMessage:
        """Record a sent envelope by message ID and sequence."""
        if envelope.sequence in self._messages_by_sequence:
            raise ValueError(f"Message sequence already tracked: {envelope.sequence}")
        if envelope.message_id in self._sequence_by_message_id:
            raise ValueError(f"Message ID already tracked: {envelope.message_id}")

        tracked = TrackedMessage(
            message_id=envelope.message_id,
            sequence=envelope.sequence,
            sent_at_ms=self._clock_ms(),
        )
        self._messages_by_sequence[envelope.sequence] = tracked
        self._sequence_by_message_id[envelope.message_id] = envelope.sequence
        return tracked

    def mark_ack(self, ack: UnityAck) -> TrackedMessage:
        """Apply a Unity ACK to a tracked message."""
        tracked = self._messages_by_sequence.get(ack.acknowledged_sequence)
        if tracked is None:
            raise ValueError(f"ACK references unknown sequence: {ack.acknowledged_sequence}")
        if tracked.message_id != ack.acknowledged_message_id:
            raise ValueError("ACK message_id does not match the tracked sequence.")

        updated = TrackedMessage(
            message_id=tracked.message_id,
            sequence=tracked.sequence,
            sent_at_ms=tracked.sent_at_ms,
            acknowledged_at_ms=self._clock_ms(),
            applied=ack.applied,
            warnings=tuple(ack.warnings),
        )
        self._messages_by_sequence[tracked.sequence] = updated
        return updated

    def timed_out_sequences(self, timeout_ms: int) -> list[int]:
        """Return pending sequences whose ACK wait time has reached the timeout."""
        if timeout_ms < 0:
            raise ValueError("timeout_ms must be non-negative.")
        now_ms = self._clock_ms()
        return [
            tracked.sequence
            for tracked in sorted(
                self._messages_by_sequence.values(),
                key=lambda message: message.sequence,
            )
            if tracked.is_pending and now_ms - tracked.sent_at_ms >= timeout_ms
        ]

    def reconnect_resume_sequence(self) -> int:
        """Return the first sequence Unity should receive after reconnect."""
        if not self._messages_by_sequence:
            return 0

        resume_sequence = min(self._messages_by_sequence)
        while True:
            tracked = self._messages_by_sequence.get(resume_sequence)
            if tracked is None or tracked.applied is not True:
                return resume_sequence
            resume_sequence += 1

    def pending_sequences(self) -> list[int]:
        """Return tracked sequences that have not received an ACK."""
        return [
            tracked.sequence
            for tracked in sorted(
                self._messages_by_sequence.values(),
                key=lambda message: message.sequence,
            )
            if tracked.is_pending
        ]
