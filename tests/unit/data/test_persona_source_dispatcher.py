from __future__ import annotations

from pathlib import Path

import pytest

from korean_social_simulator.config.models import DatasetConfig
from korean_social_simulator.data.loader import load_personas
from korean_social_simulator.errors import DatasetLoadError

_FIXTURE_PATH = Path(__file__).resolve().parents[3] / "data" / "samples" / "personas_fixture.jsonl"


def test_persona_dispatcher_loads_fixture() -> None:
    config = DatasetConfig(mode="fixture", fixture_path=str(_FIXTURE_PATH))

    records = load_personas(config)

    assert records
    assert records[0].uuid == "p-001"


def test_persona_dispatcher_can_expand_fixture_deterministically() -> None:
    config = DatasetConfig(
        mode="fixture",
        fixture_path=str(_FIXTURE_PATH),
        expand_to_size=3000,
    )

    records = load_personas(config)

    assert len(records) == 3000
    assert records[0].uuid == "p-001-x0000"
    assert records[-1].metadata["synthetic_expansion"] is True
    assert {record.sex for record in records} == {"남성", "여성"}


def test_persona_dispatcher_hf_missing_dependency_actionable(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    config = DatasetConfig(mode="huggingface", name="fixture/not-used", split="train")

    def _raise_missing(*args: object, **kwargs: object) -> object:
        raise DatasetLoadError("Hugging Face datasets library is not installed.")

    monkeypatch.setattr("korean_social_simulator.data.loader.load_personas_hf", _raise_missing)

    with pytest.raises(DatasetLoadError, match="Hugging Face"):
        load_personas(config)
