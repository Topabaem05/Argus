from __future__ import annotations

from pathlib import Path

from korean_social_simulator.bridge_schema import BridgeEnvelope
from korean_social_simulator.errors import StorageError


class ReplayStore:
    """JSONL store for validated bridge replay envelopes."""

    def __init__(self, replay_path: Path) -> None:
        self.replay_path = Path(replay_path)
        try:
            self.replay_path.parent.mkdir(parents=True, exist_ok=True)
        except OSError as exc:
            raise StorageError(
                f"Failed to create replay directory {self.replay_path.parent}: {exc}"
            ) from exc

    def write_event(self, envelope: BridgeEnvelope) -> None:
        """Append one bridge envelope as a UTF-8 JSONL line."""
        try:
            with self.replay_path.open("a", encoding="utf-8") as file_handle:
                file_handle.write(f"{envelope.model_dump_json()}\n")
        except OSError as exc:
            raise StorageError(f"Failed to write replay log {self.replay_path}: {exc}") from exc

    def write_events_batch(self, envelopes: list[BridgeEnvelope]) -> None:
        """Append bridge envelopes in the provided order."""
        try:
            with self.replay_path.open("a", encoding="utf-8") as file_handle:
                file_handle.writelines(f"{envelope.model_dump_json()}\n" for envelope in envelopes)
        except OSError as exc:
            raise StorageError(f"Failed to write replay batch {self.replay_path}: {exc}") from exc

    def load_events(self) -> list[BridgeEnvelope]:
        """Load all replay envelopes in file order.

        Raises:
            StorageError: If the replay file is missing, unreadable, or contains
                an invalid JSONL line.
        """
        if not self.replay_path.exists():
            raise StorageError(f"Replay log not found: {self.replay_path}")

        try:
            lines = self.replay_path.read_text(encoding="utf-8").splitlines()
        except OSError as exc:
            raise StorageError(f"Failed to read replay log {self.replay_path}: {exc}") from exc

        envelopes: list[BridgeEnvelope] = []
        for line_number, line in enumerate(lines, start=1):
            if not line.strip():
                continue
            try:
                envelopes.append(BridgeEnvelope.model_validate_json(line))
            except ValueError as exc:
                raise StorageError(
                    f"Invalid bridge replay JSON at line {line_number} in {self.replay_path}: {exc}"
                ) from exc

        return envelopes
