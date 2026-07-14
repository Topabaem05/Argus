"""AI brain public API with cycle-safe lazy exports."""

from __future__ import annotations

__all__ = [
    "MODEL_PROFILES",
    "GamePromptBuilder",
    "SLMModelProfile",
    "SLMResponse",
    "SLMRuntimeAdapter",
    "get_model_profile",
]


def __getattr__(name: str) -> object:
    """Resolve public symbols without importing the game/social graph at package import time."""

    if name == "GamePromptBuilder":
        from korean_social_simulator.ai.prompt_builder import GamePromptBuilder

        return GamePromptBuilder
    if name in {"MODEL_PROFILES", "SLMModelProfile", "get_model_profile"}:
        from korean_social_simulator.ai import model_profiles

        return getattr(model_profiles, name)
    if name in {"SLMResponse", "SLMRuntimeAdapter"}:
        from korean_social_simulator.ai import slm_adapter

        return getattr(slm_adapter, name)
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")
