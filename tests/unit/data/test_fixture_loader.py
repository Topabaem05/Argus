"""Unit tests for local fixture persona loader."""

from __future__ import annotations

import json
from pathlib import Path
from tempfile import NamedTemporaryFile

import pytest

from korean_social_simulator.data.loader import load_personas_fixture
from korean_social_simulator.errors import DatasetLoadError, PersonaSchemaError
from korean_social_simulator.models import PersonaRecord

_VALID_ROW: dict[str, object] = {
    "uuid": "p-001",
    "persona": "30대 직장인",
    "age": 34,
    "occupation": "소프트웨어 엔지니어",
    "district": "강남구",
    "province": "서울특별시",
    "country": "South Korea",
}


def _write_fixture(lines: list[dict[str, object]]) -> Path:
    tmp = NamedTemporaryFile(mode="w", suffix=".jsonl", delete=False)
    for row in lines:
        tmp.write(json.dumps(row, ensure_ascii=False) + "\n")
    tmp.close()
    return Path(tmp.name)


def test_load_valid_fixture() -> None:
    """Fixture mode loads persona records without network access."""
    path = _write_fixture([_VALID_ROW])
    try:
        records = load_personas_fixture(path)
        assert len(records) == 1
        assert isinstance(records[0], PersonaRecord)
        assert records[0].uuid == "p-001"
        assert records[0].occupation == "소프트웨어 엔지니어"
    finally:
        path.unlink(missing_ok=True)


def test_fixture_file_not_found() -> None:
    """Missing fixture file raises DatasetLoadError."""
    with pytest.raises(DatasetLoadError, match="not found"):
        load_personas_fixture("/nonexistent/fixture.jsonl")


def test_fixture_missing_required_field() -> None:
    """A row missing uuid, persona, age, occupation, district, or province raises PersonaSchemaError."""
    row = dict(_VALID_ROW)
    del row["uuid"]
    path = _write_fixture([row])
    try:
        with pytest.raises(PersonaSchemaError, match="uuid"):
            load_personas_fixture(path)
    finally:
        path.unlink(missing_ok=True)


def test_fixture_missing_persona_field() -> None:
    """Missing 'persona' field raises PersonaSchemaError."""
    row = dict(_VALID_ROW)
    del row["persona"]
    path = _write_fixture([row])
    try:
        with pytest.raises(PersonaSchemaError, match="persona"):
            load_personas_fixture(path)
    finally:
        path.unlink(missing_ok=True)


def test_fixture_missing_age_field() -> None:
    """Missing 'age' field raises PersonaSchemaError."""
    row = dict(_VALID_ROW)
    del row["age"]
    path = _write_fixture([row])
    try:
        with pytest.raises(PersonaSchemaError, match="age"):
            load_personas_fixture(path)
    finally:
        path.unlink(missing_ok=True)


def test_fixture_empty_file() -> None:
    """Empty fixture file raises DatasetLoadError."""
    path = _write_fixture([])
    try:
        with pytest.raises(DatasetLoadError, match="empty"):
            load_personas_fixture(path)
    finally:
        path.unlink(missing_ok=True)


def test_fixture_invalid_json() -> None:
    """Invalid JSON line raises DatasetLoadError."""
    tmp = NamedTemporaryFile(mode="w", suffix=".jsonl", delete=False)
    tmp.write("not valid json\n")
    tmp.close()
    path = Path(tmp.name)
    try:
        with pytest.raises(DatasetLoadError, match="Invalid JSON"):
            load_personas_fixture(path)
    finally:
        path.unlink(missing_ok=True)


def test_fixture_loads_multiple_records() -> None:
    """Multiple valid rows all load successfully."""
    row2 = dict(_VALID_ROW)
    row2["uuid"] = "p-002"
    row2["occupation"] = "선생님"
    path = _write_fixture([_VALID_ROW, row2])
    try:
        records = load_personas_fixture(path)
        assert len(records) == 2
        assert records[0].uuid == "p-001"
        assert records[1].uuid == "p-002"
    finally:
        path.unlink(missing_ok=True)


def test_fixture_with_optional_fields() -> None:
    """Optional fields are preserved when present."""
    row = dict(_VALID_ROW)
    row["professional_persona"] = "경력 10년 차"
    row["hobbies_and_interests"] = "요리, 등산"
    row["sex"] = "남성"
    path = _write_fixture([row])
    try:
        records = load_personas_fixture(path)
        assert records[0].professional_persona == "경력 10년 차"
        assert records[0].hobbies_and_interests == "요리, 등산"
        assert records[0].sex == "남성"
    finally:
        path.unlink(missing_ok=True)


def test_fixture_no_network_used() -> None:
    """Loading from fixture should not require network access."""
    path = _write_fixture([_VALID_ROW])
    try:
        records = load_personas_fixture(path)
        assert len(records) > 0
    finally:
        path.unlink(missing_ok=True)
