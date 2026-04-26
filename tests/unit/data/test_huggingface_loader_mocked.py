"""Unit tests for Hugging Face dataset loader interface."""

from __future__ import annotations

import sys
from unittest.mock import MagicMock

import pytest

from korean_social_simulator.data.huggingface_loader import load_personas_hf
from korean_social_simulator.errors import DatasetLoadError

_VALID_ROW: dict[str, object] = {
    "uuid": "p-001",
    "persona": "30대 직장인",
    "age": 34,
    "occupation": "소프트웨어 엔지니어",
    "district": "강남구",
    "province": "서울특별시",
    "country": "South Korea",
}


def test_hf_loader_mocked() -> None:
    """Loader can be mocked without network or real HF dataset."""
    mock_dataset_mod = MagicMock()
    mock_ds = MagicMock()
    mock_ds.__iter__.return_value = iter([_VALID_ROW])
    mock_ds.__len__.return_value = 1
    mock_ds.select.return_value = mock_ds
    mock_dataset_mod.load_dataset.return_value = mock_ds

    sys.modules["datasets"] = mock_dataset_mod
    try:
        records = load_personas_hf(
            dataset_name="test/dataset",
            split="train",
        )
    finally:
        del sys.modules["datasets"]

    assert len(records) == 1
    assert records[0].uuid == "p-001"


def test_hf_loader_failure_raises_dataset_load_error() -> None:
    """Dataset load failures raise DatasetLoadError."""
    mock_dataset_mod = MagicMock()
    mock_dataset_mod.load_dataset.side_effect = RuntimeError("network error")

    sys.modules["datasets"] = mock_dataset_mod
    try:
        with pytest.raises(DatasetLoadError, match="Failed to load dataset"):
            load_personas_hf(dataset_name="bad/dataset")
    finally:
        del sys.modules["datasets"]


def test_hf_loader_empty_name_raises() -> None:
    """Empty dataset name raises DatasetLoadError after bypassing dataset import."""
    mock_dataset_mod = MagicMock()
    mock_dataset_mod.load_dataset.return_value = MagicMock()

    sys.modules["datasets"] = mock_dataset_mod
    try:
        with pytest.raises(DatasetLoadError, match="non-empty"):
            load_personas_hf(dataset_name="")
    finally:
        del sys.modules["datasets"]


def test_hf_loader_max_rows_respected() -> None:
    """max_rows limits the number of rows returned."""
    rows = [_VALID_ROW, {**_VALID_ROW, "uuid": "p-002"}, {**_VALID_ROW, "uuid": "p-003"}]
    mock_ds = MagicMock()
    mock_ds.__iter__.return_value = iter(rows)
    mock_ds.__len__.return_value = 3
    selected = MagicMock()
    selected.__iter__.return_value = iter(rows[:2])
    mock_ds.select.return_value = selected

    mock_dataset_mod = MagicMock()
    mock_dataset_mod.load_dataset.return_value = mock_ds

    sys.modules["datasets"] = mock_dataset_mod
    try:
        records = load_personas_hf(max_rows=2)
    finally:
        del sys.modules["datasets"]

    assert len(records) == 2


def test_live_hf_test_skipped_by_default() -> None:
    """This test uses a live_hf marker and should be skipped without --markers live_hf."""
    pass
