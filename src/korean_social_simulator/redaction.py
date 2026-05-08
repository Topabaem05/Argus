"""Helpers for removing known secret values from persisted text."""

from __future__ import annotations

import os
import re

REDACTED = "***REDACTED***"
SECRET_ENV_KEYS = ("KSSIM_LLM_API_KEY", "KSSIM_PAGEINDEX_API_KEY", "NVIDIA_API_KEY")
SECRET_PATTERNS = (
    re.compile(r"\bsk-[A-Za-z0-9][A-Za-z0-9_-]{3,}\b"),
    re.compile(r"\bnvapi-[A-Za-z0-9][A-Za-z0-9_-]{3,}\b"),
)


def redact_known_secrets(value: object) -> str:
    """Return text with configured secret values and common token shapes redacted."""
    text = str(value)

    for key in SECRET_ENV_KEYS:
        secret = os.environ.get(key)
        if secret:
            text = text.replace(secret, REDACTED)

    for pattern in SECRET_PATTERNS:
        text = pattern.sub(REDACTED, text)

    return text
