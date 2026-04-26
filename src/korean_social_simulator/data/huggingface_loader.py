"""Hugging Face dataset loader for Nemotron-Personas-Korea.

This module is an optional interface. The `datasets` package is not required
for base installs; import errors are caught and reported clearly.
"""

from __future__ import annotations

from korean_social_simulator.errors import DatasetLoadError
from korean_social_simulator.models import PersonaRecord


def load_personas_hf(
    dataset_name: str = "nvidia/Nemotron-Personas-Korea",
    split: str = "train",
    cache_dir: str | None = None,
    max_rows: int | None = None,
) -> list[PersonaRecord]:
    """Load persona records from a Hugging Face dataset.

    Requires the `datasets` library (install via `pip install datasets` or
    `uv sync --extra hf`).

    Returns a list of validated PersonaRecord objects.

    Raises:
        DatasetLoadError: If `datasets` is not installed or loading fails.
    """
    try:
        from datasets import load_dataset  # type: ignore[import-not-found]
    except ImportError as err:
        raise DatasetLoadError(
            "Hugging Face datasets library is not installed. Install with: uv sync --extra hf"
        ) from err

    if not isinstance(dataset_name, str) or not dataset_name.strip():
        raise DatasetLoadError("Dataset name must be a non-empty string.")

    try:
        ds = load_dataset(dataset_name, split=split, cache_dir=cache_dir, trust_remote_code=False)
    except Exception as e:
        raise DatasetLoadError(
            f"Failed to load dataset '{dataset_name}' (split={split}): {e}"
        ) from e

    rows_iter = ds
    if max_rows is not None:
        rows_iter = ds.select(range(min(max_rows, len(ds))))

    records: list[PersonaRecord] = []
    for row in rows_iter:
        row_dict = dict(row)
        try:
            records.append(PersonaRecord.model_validate(row_dict))
        except Exception as e:
            raise DatasetLoadError(f"Invalid persona row in dataset '{dataset_name}': {e}") from e

    return records
