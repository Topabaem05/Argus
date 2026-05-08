"""Persona data loaders for local fixtures and Hugging Face datasets."""

from __future__ import annotations

import json
from pathlib import Path

from pydantic import ValidationError

from korean_social_simulator.config.models import DatasetConfig
from korean_social_simulator.data.huggingface_loader import load_personas_hf
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
        except ValidationError as e:
            raise PersonaSchemaError(
                f"Invalid persona row at line {idx} in {fixture_path}: {e}"
            ) from e

        records.append(record)

    return records


def load_personas(config: DatasetConfig) -> list[PersonaRecord]:
    """Load personas from the configured source.

    Fixture mode is fully local. Hugging Face mode delegates to the optional
    loader and keeps missing dependency failures actionable.

    Raises:
        DatasetLoadError: If the configured source cannot be loaded.
        PersonaSchemaError: If fixture rows fail schema validation.
    """
    if config.mode == "fixture":
        return _expand_personas(
            load_personas_fixture(config.fixture_path),
            config.expand_to_size,
        )

    if config.mode == "huggingface":
        return _expand_personas(
            load_personas_hf(
                dataset_name=config.name,
                split=config.split,
                cache_dir=config.cache_dir,
            ),
            config.expand_to_size,
        )

    raise DatasetLoadError(f"Unsupported dataset mode: {config.mode}")


def _expand_personas(
    records: list[PersonaRecord],
    target_size: int | None,
) -> list[PersonaRecord]:
    if target_size is None or len(records) >= target_size:
        return records
    if not records:
        raise DatasetLoadError("Cannot expand an empty persona collection.")

    provinces = [
        ("서울특별시", "마포구"),
        ("서울특별시", "관악구"),
        ("부산광역시", "해운대구"),
        ("인천광역시", "연수구"),
        ("대구광역시", "달서구"),
        ("광주광역시", "북구"),
        ("대전광역시", "유성구"),
        ("경기도", "성남시"),
        ("경기도", "수원시"),
        ("제주특별자치도", "서귀포시"),
    ]
    occupations = [
        "대학생",
        "취업준비생",
        "백엔드 개발자",
        "소프트웨어 엔지니어",
        "UI/UX 디자이너",
        "마케팅 과장",
        "간호사",
        "자영업자",
        "공무원",
        "교사",
        "의사",
        "연구원",
        "변호사",
        "주부",
        "은퇴",
    ]
    expanded: list[PersonaRecord] = []

    for index in range(target_size):
        base = records[index % len(records)]
        cycle = index // len(records)
        province, district = provinces[(index + cycle) % len(provinces)]
        age = min(74, max(20, base.age + ((cycle % 11) - 5)))
        sex = "여성" if (index + cycle) % 2 else "남성"
        occupation = occupations[(index + len(base.occupation) + cycle) % len(occupations)]
        base_metadata = dict(base.metadata)
        base_metadata.update(
            {
                "synthetic_expansion": True,
                "base_uuid": base.uuid,
                "expansion_index": index,
            }
        )
        expanded.append(
            base.model_copy(
                update={
                    "uuid": f"{base.uuid}-x{index:04d}",
                    "persona": (
                        f"{age}세 {sex}, {province} {district} 거주, {occupation}. "
                        f"기반 페르소나: {base.persona}"
                    ),
                    "age": age,
                    "sex": sex,
                    "occupation": occupation,
                    "province": province,
                    "district": district,
                    "metadata": base_metadata,
                }
            )
        )

    return expanded
