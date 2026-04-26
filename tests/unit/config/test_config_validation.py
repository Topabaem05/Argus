"""Unit tests for configuration models and loader."""

from __future__ import annotations

import copy
import importlib
import json
from pathlib import Path
from tempfile import NamedTemporaryFile

import pytest
import yaml

from korean_social_simulator.config.loader import (
    ConfigurationError,
    load_config,
    redact_config,
)
from korean_social_simulator.config.models import RuntimeConfig


def _write_temp_yaml(data: dict[str, object]) -> Path:
    tmp = NamedTemporaryFile(mode="w", suffix=".yaml", delete=False)
    yaml.safe_dump(data, tmp)
    tmp.close()
    return Path(tmp.name)


_MINIMAL_CONFIG: dict[str, object] = {
    "runtime": {
        "run_id": "test_run_001",
        "seed": 42,
        "output_dir": "outputs",
        "dry_run": True,
        "max_turns": 5,
        "max_participants": 20,
        "overwrite": False,
    },
    "dataset": {
        "mode": "fixture",
        "name": "nvidia/Nemotron-Personas-Korea",
        "split": "train",
        "fixture_path": "data/samples/personas_fixture.jsonl",
    },
    "sampling": {
        "sample_size": 10,
        "seed": 42,
        "allow_smaller_sample": False,
    },
    "scenario": {
        "id": "test_scenario_v1",
        "family": "product_reaction",
        "title": "Test scenario",
        "hypothesis": "Test hypothesis",
        "language": "ko",
        "participant_count": 10,
        "max_turns": 5,
        "metrics": ["trust_score"],
        "safety_notes": ["Synthetic simulation only."],
    },
}


def test_valid_config_loads() -> None:
    """A valid YAML config should parse into a RuntimeConfig object."""
    data = copy.deepcopy(_MINIMAL_CONFIG)
    path = _write_temp_yaml(data)
    try:
        config = load_config(path)
        assert isinstance(config, RuntimeConfig)
        assert config.runtime.run_id == "test_run_001"
        assert config.scenario.family == "product_reaction"
    finally:
        path.unlink(missing_ok=True)


def test_missing_file_raises() -> None:
    """A non-existent config path raises ConfigurationError."""
    with pytest.raises(ConfigurationError, match="not found"):
        load_config("/nonexistent/config.yaml")


def test_empty_file_raises() -> None:
    """An empty YAML file raises ConfigurationError."""
    path = _write_temp_yaml({})
    try:
        with pytest.raises(ConfigurationError, match="missing"):
            load_config(path)
    finally:
        path.unlink(missing_ok=True)


def test_missing_runtime_section_raises() -> None:
    """Missing required top-level keys fail fast."""
    data = copy.deepcopy(_MINIMAL_CONFIG)
    del data["runtime"]
    path = _write_temp_yaml(data)
    try:
        with pytest.raises(ConfigurationError):
            load_config(path)
    finally:
        path.unlink(missing_ok=True)


def test_invalid_age_range_raises() -> None:
    """min_age > max_age fails before data loading."""
    data = copy.deepcopy(_MINIMAL_CONFIG)
    data["sampling"]["filters"] = {"age_range": {"min": 60, "max": 20}}
    path = _write_temp_yaml(data)
    try:
        with pytest.raises(ConfigurationError, match=r"age(?:_| )range"):
            load_config(path)
    finally:
        path.unlink(missing_ok=True)


def test_sample_size_exceeds_max_participants_raises() -> None:
    """Sample larger than max_participants fails fast."""
    data = copy.deepcopy(_MINIMAL_CONFIG)
    data["sampling"]["sample_size"] = 200
    path = _write_temp_yaml(data)
    try:
        with pytest.raises(ConfigurationError, match="Sample size"):
            load_config(path)
    finally:
        path.unlink(missing_ok=True)


def test_environment_override_output_dir(monkeypatch: pytest.MonkeyPatch) -> None:
    """KSSIM_OUTPUT_DIR env var overrides runtime.output_dir."""
    monkeypatch.setenv("KSSIM_OUTPUT_DIR", "custom_outputs")
    data = copy.deepcopy(_MINIMAL_CONFIG)
    path = _write_temp_yaml(data)
    try:
        config = load_config(path)
        assert config.runtime.output_dir == "custom_outputs"
    finally:
        path.unlink(missing_ok=True)


def test_environment_override_llm_api_key(monkeypatch: pytest.MonkeyPatch) -> None:
    """KSSIM_LLM_API_KEY env var injects into llm config."""
    monkeypatch.setenv("KSSIM_LLM_API_KEY", "sk-test-secret-123")
    data = copy.deepcopy(_MINIMAL_CONFIG)
    path = _write_temp_yaml(data)
    try:
        config = load_config(path)
        assert config.llm.api_key == "sk-test-secret-123"
        redacted = redact_config(config)
        llm_dict = redacted["llm"]
        assert llm_dict.get("api_key") == "***REDACTED***"
    finally:
        path.unlink(missing_ok=True)


def test_rag_enabled_without_dependency_raises(monkeypatch: pytest.MonkeyPatch) -> None:
    data = copy.deepcopy(_MINIMAL_CONFIG)
    data["rag"] = {"enabled": True}
    path = _write_temp_yaml(data)
    original_import_module = importlib.import_module

    def _mock_import_module(name: str, package: str | None = None) -> object:
        if name == "pageindex":
            raise ImportError("No module named 'pageindex'")
        return original_import_module(name, package)

    monkeypatch.setattr(importlib, "import_module", _mock_import_module)

    try:
        with pytest.raises(ConfigurationError, match="pageindex is not installed"):
            load_config(path)
    finally:
        path.unlink(missing_ok=True)


def test_live_mode_without_api_key_raises(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("KSSIM_LLM_API_KEY", raising=False)
    data = copy.deepcopy(_MINIMAL_CONFIG)
    data["runtime"]["dry_run"] = False
    data["llm"] = {"api_key": None}
    path = _write_temp_yaml(data)
    try:
        with pytest.raises(
            ConfigurationError,
            match=r"Live mode requires KSSIM_LLM_API_KEY environment variable\.",
        ):
            load_config(path)
    finally:
        path.unlink(missing_ok=True)


def test_live_mode_with_api_key_passes(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("KSSIM_LLM_API_KEY", raising=False)
    data = copy.deepcopy(_MINIMAL_CONFIG)
    data["runtime"]["dry_run"] = False
    data["llm"] = {"api_key": "sk-test"}
    path = _write_temp_yaml(data)
    try:
        config = load_config(path)
        assert config.llm.api_key == "sk-test"
        assert config.runtime.dry_run is False
    finally:
        path.unlink(missing_ok=True)


def test_redact_config_strips_secrets() -> None:
    """redact_config replaces secret values in serialized output."""
    data = copy.deepcopy(_MINIMAL_CONFIG)
    path = _write_temp_yaml(data)
    try:
        config = load_config(path)
        redacted = redact_config(config)
        serialized = json.dumps(redacted)
        assert "sk-" not in serialized
    finally:
        path.unlink(missing_ok=True)


def test_non_dict_yaml_raises() -> None:
    """A YAML list instead of mapping raises ConfigurationError."""
    path = _write_temp_yaml([1, 2, 3])  # type: ignore[arg-type]
    try:
        with pytest.raises(ConfigurationError, match="mapping"):
            load_config(path)
    finally:
        path.unlink(missing_ok=True)
