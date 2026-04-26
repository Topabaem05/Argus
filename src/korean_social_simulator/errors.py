"""Typed project errors for Korean Social Simulation Lab."""

from __future__ import annotations


class KoreanSocialSimulationError(Exception):
    """Base exception for all project errors."""


class ConfigurationError(KoreanSocialSimulationError):
    """Configuration validation or loading failure."""


class DatasetLoadError(KoreanSocialSimulationError):
    """Failed to load persona dataset from source."""


class PersonaSchemaError(KoreanSocialSimulationError):
    """Persona record is missing required fields or has invalid types."""


class SamplingError(KoreanSocialSimulationError):
    """Persona sampling failed due to insufficient rows or invalid constraints."""


class AgentProfileError(KoreanSocialSimulationError):
    """Agent profile construction failed."""


class ScenarioValidationError(KoreanSocialSimulationError):
    """Scenario template is invalid or unsupported."""


class SafetyViolationError(KoreanSocialSimulationError):
    """Scenario or profile violates safety policy."""


class RetrievalError(KoreanSocialSimulationError):
    """Required document retrieval failed."""


class SimulationError(KoreanSocialSimulationError):
    """Simulation execution failed."""


class StorageError(KoreanSocialSimulationError):
    """Output persistence failed."""


class EvaluationError(KoreanSocialSimulationError):
    """Metric evaluation failed."""
