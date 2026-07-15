"""AI brain public API with cycle-safe lazy exports."""

from __future__ import annotations

__all__ = [
    "LOW_VRAM_PLANS",
    "MODEL_PROFILES",
    "GamePromptBuilder",
    "LowVramRuntimePlan",
    "SLMModelProfile",
    "SLMResponse",
    "SLMRuntimeAdapter",
    "detect_nvidia_vram_gb",
    "get_model_profile",
    "select_low_vram_plan",
]


def __getattr__(name: str) -> object:
    """Resolve public symbols without importing the game/social graph at package import time."""

    if name == "GamePromptBuilder":
        from korean_social_simulator.ai.prompt_builder import GamePromptBuilder

        return GamePromptBuilder
    if name in {"MODEL_PROFILES", "SLMModelProfile", "get_model_profile"}:
        from korean_social_simulator.ai import model_profiles

        return getattr(model_profiles, name)
    if name in {
        "LOW_VRAM_PLANS",
        "LowVramRuntimePlan",
        "detect_nvidia_vram_gb",
        "select_low_vram_plan",
    }:
        from korean_social_simulator.ai import low_vram

        return getattr(low_vram, name)
    if name in {"SLMResponse", "SLMRuntimeAdapter"}:
        from korean_social_simulator.ai import slm_adapter

        return getattr(slm_adapter, name)
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")
