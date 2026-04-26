"""Persona data loaders for local fixtures and Hugging Face datasets."""

from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.errors import DatasetLoadError, PersonaSchemaError
from korean_social_simulator.models import PersonaRecord

REQUIRED_PERSONA_FIELDS = frozenset(
    {"uuid", "persona", "age", "occupation", "district", "province"}
)


def load_personas_fixture(path: str | Path) -> list[PersonaRecord]:
    """Load persona records from a local JSONL fixture file.

    Each line must be a JSON object with all required persona fields.
    No network access is used.

    Raises:
        DatasetLoadError: If the file is missing or unreadable.
        PersonaSchemaError: If a row is missing required fields.
    """
    fixture_path = Path(path)
    if not fixture_path.exists():
        raise DatasetLoadError(f"Fixture file not found: {fixture_path}")

    try:
        with fixture_path.open("r", encoding="utf-8") as f:
            raw_lines = [line.strip() for line in f if line.strip()]
    except OSError as e:
        raise DatasetLoadError(f"Failed to read fixture file {fixture_path}: {e}") from e

    if not raw_lines:
        raise DatasetLoadError(f"Fixture file is empty: {fixture_path}")

    records: list[PersonaRecord] = []

    for idx, line in enumerate(raw_lines, start=1):
        try:
            raw = json.loads(line)
        except json.JSONDecodeError as e:
            raise DatasetLoadError(f"Invalid JSON at line {idx} in {fixture_path}: {e}") from e

        if not isinstance(raw, dict):
            raise PersonaSchemaError(
                f"Expected JSON object at line {idx} in {fixture_path}, got {type(raw).__name__}"
            )

        missing = REQUIRED_PERSONA_FIELDS - set(raw.keys())
        if missing:
            raise PersonaSchemaError(
                f"Line {idx} in {fixture_path} is missing required fields: {sorted(missing)}"
            )

        try:
            record = PersonaRecord.model_validate(raw)
        except Exception as e:
            raise PersonaSchemaError(
                f"Invalid persona row at line {idx} in {fixture_path}: {e}"
            ) from e

        records.append(record)

    return records
