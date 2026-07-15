"""Curated local SLM profiles for minibot employee control.

The game-facing profile keys stay stable even when a runtime exposes a different alias.
Small profiles use text-only GGUF deployments; the Qwen3.5 vision projector is not loaded
because the employee decision protocol only consumes compact text state.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class SLMModelProfile:
    key: str
    model_id: str
    runtime_model: str
    usage: str
    parameter_billions: float
    recommended_vram_gb: float
    max_parallel_decisions: int
    gguf_repo: str | None = None
    quantization: str = "Q4_K_M"
    context_tokens: int = 2048
    temperature: float = 0.3
    max_tokens: int = 160
    finetune_base_model: str | None = None


MODEL_PROFILES: dict[str, SLMModelProfile] = {
    "micro": SLMModelProfile(
        key="micro",
        model_id="Qwen/Qwen3.5-0.8B",
        runtime_model="argus-minibot-0.8b",
        usage="2 GB VRAM or CPU fallback; strict JSON decisions and short Korean dialogue.",
        parameter_billions=0.873,
        recommended_vram_gb=2.0,
        max_parallel_decisions=1,
        gguf_repo="unsloth/Qwen3.5-0.8B-GGUF",
        quantization="Q4_K_M",
        context_tokens=2048,
        temperature=0.2,
        max_tokens=128,
        finetune_base_model="Qwen/Qwen3-0.6B",
    ),
    "edge": SLMModelProfile(
        key="edge",
        model_id="Qwen/Qwen3.5-2B",
        runtime_model="argus-minibot-2b",
        usage="Default 3-4 GB VRAM profile with one inference slot and compact context.",
        parameter_billions=2.274,
        recommended_vram_gb=3.5,
        max_parallel_decisions=1,
        gguf_repo="unsloth/Qwen3.5-2B-GGUF",
        quantization="Q4_K_M",
        context_tokens=2048,
        temperature=0.25,
        max_tokens=160,
        finetune_base_model="Qwen/Qwen3-1.7B",
    ),
    "balanced": SLMModelProfile(
        key="balanced",
        model_id="Qwen/Qwen3.5-4B",
        runtime_model="argus-minibot-4b",
        usage="6 GB recommended; 4 GB is possible only with llama.cpp auto-fit and CPU offload.",
        parameter_billions=4.660,
        recommended_vram_gb=6.0,
        max_parallel_decisions=1,
        gguf_repo="unsloth/Qwen3.5-4B-GGUF",
        quantization="Q4_K_M",
        context_tokens=3072,
        temperature=0.3,
        max_tokens=192,
        finetune_base_model="Qwen/Qwen3-4B",
    ),
    "quality": SLMModelProfile(
        key="quality",
        model_id="Qwen/Qwen3.5-35B-A3B",
        runtime_model="argus-minibot-35b-a3b",
        usage="Dedicated high-memory local inference host; never selected for low-VRAM auto mode.",
        parameter_billions=35.952,
        recommended_vram_gb=24.0,
        max_parallel_decisions=1,
        temperature=0.25,
        max_tokens=192,
    ),
}


PROFILE_ALIASES: dict[str, str] = {
    "vram_2gb": "micro",
    "vram_3gb": "edge",
    "vram_4gb": "edge",
    "low": "micro",
    "default": "edge",
}


def get_model_profile(key: str) -> SLMModelProfile:
    """Return a profile or raise a descriptive error for configuration mistakes."""

    canonical = PROFILE_ALIASES.get(key, key)
    try:
        return MODEL_PROFILES[canonical]
    except KeyError as exc:
        choices = ", ".join(sorted((*MODEL_PROFILES, *PROFILE_ALIASES)))
        raise ValueError(f"Unknown SLM model profile {key!r}; choose one of: {choices}") from exc
