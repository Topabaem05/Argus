"""AI brain: local SLM adapter, model profiles, and game prompt builder."""

from __future__ import annotations

from korean_social_simulator.ai.model_profiles import (
    MODEL_PROFILES,
    SLMModelProfile,
    get_model_profile,
)
from korean_social_simulator.ai.prompt_builder import GamePromptBuilder
from korean_social_simulator.ai.slm_adapter import SLMResponse, SLMRuntimeAdapter

__all__ = [
    "MODEL_PROFILES",
    "GamePromptBuilder",
    "SLMModelProfile",
    "SLMResponse",
    "SLMRuntimeAdapter",
    "get_model_profile",
]
