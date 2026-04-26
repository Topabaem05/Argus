from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.errors import StorageError
from korean_social_simulator.models import SimulationEvent, SimulationResult

_EVENTS_FILE_NAME = "events.jsonl"
_METADATA_FILE_NAME = "run_metadata.json"
_METRICS_FILE_NAME = "metrics.json"
_REPORT_FILE_NAME = "report.md"


class RunStore:
    def __init__(self, run_dir: Path, overwrite: bool = False) -> None:
        """Initialize a storage backend for one simulation run.

        Creates ``run_dir`` when it does not exist and prevents accidental reuse
        of an existing ``events.jsonl`` file unless ``overwrite`` is enabled.

        Raises:
            StorageError: If the run directory cannot be created, managed files
                cannot be reset, or an existing event log is present while
                ``overwrite`` is false.
        """
        self.run_dir = Path(run_dir)
        self._overwrite = overwrite
        self._events_path = self.run_dir / _EVENTS_FILE_NAME
        self._metadata_path = self.run_dir / _METADATA_FILE_NAME
        self._metrics_path = self.run_dir / _METRICS_FILE_NAME
        self._report_path = self.run_dir / _REPORT_FILE_NAME

        try:
            self.run_dir.mkdir(parents=True, exist_ok=True)
        except OSError as exc:
            raise StorageError(f"Failed to create run directory {self.run_dir}: {exc}") from exc

        if self._events_path.exists() and not self._overwrite:
            raise StorageError(
                f"Run directory already contains {self._events_path.name}: {self.run_dir}"
            )

        if self._overwrite:
            self._reset_managed_files()

    def write_event(self, event: SimulationEvent) -> None:
        """Append a single event as one UTF-8 JSONL line.

        Raises:
            StorageError: If the event log cannot be written.
        """
        try:
            with self._events_path.open("a", encoding="utf-8") as file_handle:
                file_handle.write(f"{event.model_dump_json()}\n")
        except OSError as exc:
            raise StorageError(f"Failed to write event log {self._events_path}: {exc}") from exc

    def write_events_batch(self, events: list[SimulationEvent]) -> None:
        """Append multiple events to ``events.jsonl`` in order.

        Raises:
            StorageError: If the batch cannot be written.
        """
        try:
            with self._events_path.open("a", encoding="utf-8") as file_handle:
                file_handle.writelines(f"{event.model_dump_json()}\n" for event in events)
        except OSError as exc:
            raise StorageError(f"Failed to write event batch {self._events_path}: {exc}") from exc

    def write_metadata(self, metadata: dict[str, object]) -> None:
        """Write run metadata to ``run_metadata.json``.

        Raises:
            StorageError: If the metadata cannot be serialized or written.
        """
        try:
            payload = json.dumps(metadata, ensure_ascii=False, indent=2, sort_keys=True)
            self._metadata_path.write_text(f"{payload}\n", encoding="utf-8")
        except (OSError, TypeError) as exc:
            raise StorageError(
                f"Failed to write run metadata {self._metadata_path}: {exc}"
            ) from exc

    def write_metrics(self, metrics: dict[str, object]) -> None:
        try:
            payload = json.dumps(metrics, ensure_ascii=False, indent=2, sort_keys=True)
            self._metrics_path.write_text(f"{payload}\n", encoding="utf-8")
        except (OSError, TypeError) as exc:
            raise StorageError(f"Failed to write metrics {self._metrics_path}: {exc}") from exc

    def write_metrics_csv(self, metrics: dict[str, object]) -> None:
        metrics_csv_path = self.run_dir / "metrics.csv"
        rows = ["metric,value"]
        rows.extend(f"{metric},{value}" for metric, value in metrics.items())

        try:
            metrics_csv_path.write_text("\n".join(rows) + "\n", encoding="utf-8")
        except OSError as exc:
            raise StorageError(f"Failed to write metrics CSV {metrics_csv_path}: {exc}") from exc

    def finalize(self, result: SimulationResult) -> SimulationResult:
        """Finalize run metadata and return a result with artifact paths.

        The returned result always includes the stable ``events.jsonl`` path.
        Existing metadata, if present, is merged with the final result payload.

        Raises:
            StorageError: If existing metadata cannot be read or final metadata
                cannot be written.
        """
        finalized_result = result.model_copy(
            update={
                "events_path": str(self._events_path),
                "metrics_path": result.metrics_path
                or (str(self._metrics_path) if self._metrics_path.exists() else None),
                "report_path": result.report_path
                or (str(self._report_path) if self._report_path.exists() else None),
            }
        )
        metadata = self._read_metadata()
        metadata.update(finalized_result.model_dump(mode="json"))
        self.write_metadata(metadata)
        return finalized_result

    def _read_metadata(self) -> dict[str, object]:
        if not self._metadata_path.exists():
            return {}

        try:
            raw_metadata = self._metadata_path.read_text(encoding="utf-8")
        except OSError as exc:
            raise StorageError(f"Failed to read run metadata {self._metadata_path}: {exc}") from exc

        if not raw_metadata.strip():
            return {}

        try:
            metadata = json.loads(raw_metadata)
        except json.JSONDecodeError as exc:
            raise StorageError(
                f"Run metadata file is not valid JSON: {self._metadata_path}"
            ) from exc

        if not isinstance(metadata, dict):
            raise StorageError(f"Run metadata must be a JSON object: {self._metadata_path}")

        return metadata

    def _reset_managed_files(self) -> None:
        for path in (
            self._events_path,
            self._metadata_path,
            self._metrics_path,
            self.run_dir / "metrics.csv",
        ):
            if not path.exists():
                continue

            try:
                path.unlink()
            except OSError as exc:
                raise StorageError(f"Failed to reset managed file {path}: {exc}") from exc
