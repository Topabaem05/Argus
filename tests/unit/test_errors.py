"""Unit tests for typed project errors."""

from __future__ import annotations

import pytest

from korean_social_simulator import errors

_ALL_ERROR_TYPES = [
    errors.KoreanSocialSimulationError,
    errors.ConfigurationError,
    errors.DatasetLoadError,
    errors.PersonaSchemaError,
    errors.SamplingError,
    errors.AgentProfileError,
    errors.ScenarioValidationError,
    errors.SafetyViolationError,
    errors.RetrievalError,
    errors.SimulationError,
    errors.StorageError,
    errors.EvaluationError,
]


@pytest.mark.parametrize("error_cls", _ALL_ERROR_TYPES)
def test_error_is_instantiable_with_message(error_cls: type[Exception]) -> None:
    """All project errors can be instantiated with a message."""
    err = error_cls("test message")
    assert isinstance(err, Exception)
    assert str(err) == "test message"


@pytest.mark.parametrize("error_cls", _ALL_ERROR_TYPES)
def test_error_is_subclass_of_base(error_cls: type[Exception]) -> None:
    """All project errors inherit from KoreanSocialSimulationError or are it."""
    assert (
        issubclass(error_cls, errors.KoreanSocialSimulationError)
        or error_cls is errors.KoreanSocialSimulationError
    )


def test_errors_convert_to_nonzero_exit() -> None:
    """A caught error can be mapped to a non-zero exit code."""
    try:
        raise errors.SafetyViolationError("blocked")
    except errors.KoreanSocialSimulationError:
        exit_code = 1
    assert exit_code == 1
