"""Configuration loader with YAML parsing and environment variable overrides."""

from __future__ import annotations

import os
from pathlib import Path

import yaml

from korean_social_simulator.config.models import RuntimeConfig
from korean_social_simulator.errors import ConfigurationError

_SECRET_KEYS = frozenset(
    {"api_key", "secret", "password", "token", "KSSIM_LLM_API_KEY", "KSSIM_PAGEINDEX_API_KEY"}
)

_REDACTED_VALUE = "***REDACTED***"


def _redact_dict(data: dict[str, object]) -> dict[str, object]:
    result: dict[str, object] = {}
    for key, value in data.items():
        key_lower = key.lower()
        if key_lower in _SECRET_KEYS or any(s in key_lower for s in ("api_key", "secret")):
            result[key] = _REDACTED_VALUE
        elif isinstance(value, dict):
            result[key] = _redact_dict(value)
        else:
            result[key] = value
    return result


def load_config(path: str | Path) -> RuntimeConfig:
    """Load and validate a YAML configuration file.

    Expected config keys are documented in the RuntimeConfig model.

    Raises:
        ConfigurationError: If the file is missing, malformed, or fails validation.
    """
    config_path = Path(path)
    if not config_path.exists():
        raise ConfigurationError(f"Configuration file not found: {config_path}")

    with config_path.open("r", encoding="utf-8") as f:
        try:
            raw = yaml.safe_load(f)
        except yaml.YAMLError as e:
            raise ConfigurationError(f"Invalid YAML in {config_path}: {e}") from e

    if raw is None:
        raise ConfigurationError(f"Empty configuration file: {config_path}")
    if not isinstance(raw, dict):
        raise ConfigurationError(f"Configuration must be a mapping, got {type(raw).__name__}")

    raw = _apply_environment_overrides(raw)

    try:
        config = RuntimeConfig.model_validate(raw)
    except Exception as e:
        error_message = str(e).replace("age_range", "age range")
        raise ConfigurationError(f"Configuration validation failed: {error_message}") from e

    _validate_live_mode_secrets(config)
    _validate_config_business_rules(config)
    _validate_rag_dependencies(config)

    return config


def redact_config(config: RuntimeConfig) -> dict[str, object]:
    """Return a redacted dict representation safe for logging.

    All secret-like keys are replaced with redacted markers.
    """
    return _redact_dict(config.model_dump())


def _apply_environment_overrides(raw: dict[str, object]) -> dict[str, object]:
    llm_api_key = os.environ.get("KSSIM_LLM_API_KEY")
    if llm_api_key:
        raw.setdefault("llm", {})
        if isinstance(raw["llm"], dict):
            raw["llm"]["api_key"] = llm_api_key

    pageindex_api_key = os.environ.get("KSSIM_PAGEINDEX_API_KEY")
    if pageindex_api_key:
        raw.setdefault("rag", {})
        if isinstance(raw["rag"], dict):
            raw["rag"]["api_key"] = pageindex_api_key

    output_dir = os.environ.get("KSSIM_OUTPUT_DIR")
    if output_dir:
        raw.setdefault("runtime", {})
        if isinstance(raw["runtime"], dict):
            raw["runtime"]["output_dir"] = output_dir

    hf_cache_dir = os.environ.get("KSSIM_HF_CACHE_DIR")
    if hf_cache_dir:
        raw.setdefault("dataset", {})
        if isinstance(raw["dataset"], dict):
            raw["dataset"]["cache_dir"] = hf_cache_dir

    return raw


def _validate_config_business_rules(config: RuntimeConfig) -> None:
    sampling = config.sampling
    scenario = config.scenario

    if sampling.sample_size > config.runtime.max_participants:
        raise ConfigurationError(
            f"Sample size ({sampling.sample_size}) exceeds max participants "
            f"({config.runtime.max_participants})"
        )
    if scenario.participant_count > config.runtime.max_participants:
        raise ConfigurationError(
            f"Scenario participant count ({scenario.participant_count}) exceeds "
            f"max participants ({config.runtime.max_participants})"
        )
    if scenario.max_turns > config.runtime.max_turns:
        raise ConfigurationError(
            f"Scenario max_turns ({scenario.max_turns}) exceeds runtime max_turns "
            f"({config.runtime.max_turns})"
        )

    if sampling.filters.age_range is not None:
        age_range = sampling.filters.age_range
        if age_range.min > age_range.max:
            raise ConfigurationError(
                f"Invalid age range: min ({age_range.min}) > max ({age_range.max})"
            )


def _validate_live_mode_secrets(config: RuntimeConfig) -> None:
    if config.runtime.dry_run:
        return
    if config.llm.api_key is not None:
        return
    if os.environ.get("KSSIM_LLM_API_KEY"):
        return
    raise ConfigurationError("Live mode requires KSSIM_LLM_API_KEY environment variable.")


def _validate_rag_dependencies(config: RuntimeConfig) -> None:
    if not config.rag.enabled:
        return

    try:
        import importlib

        importlib.import_module("pageindex")
    except ImportError as e:
        raise ConfigurationError(
            "RAG is enabled but pageindex is not installed. Install with: uv sync --extra rag"
        ) from e
