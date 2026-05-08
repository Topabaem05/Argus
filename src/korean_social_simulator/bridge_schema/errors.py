from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

ErrorSource = Literal["adapter", "bridge", "unity", "physics", "replay"]
ErrorSeverity = Literal["debug", "info", "warning", "error", "fatal"]


class StructuredError(BaseModel):
    """Structured cross-boundary error payload."""

    model_config = ConfigDict(extra="forbid")

    error_id: str = Field(min_length=1)
    source: ErrorSource
    severity: ErrorSeverity
    message: str = Field(min_length=1)
    recoverable: bool
    correlation_id: str | None = None
    details: dict[str, object] = Field(default_factory=dict)
