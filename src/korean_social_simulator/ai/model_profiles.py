"""Curated local SLM profiles for minibot employee control.

The profile keys are intentionally stable game-facing names. Runtime model names remain
configurable because Ollama, vLLM and llama.cpp deployments may expose different aliases.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class SLMModelProfile:
    key: str
    model_id: str
    runtime_model: str
    usage: str
    max_parallel_decisions: int
    temperature: float = 0.35
    max_tokens: int = 192


MODEL_PROFILES: dict[str, SLMModelProfile] = {
    "edge": SLMModelProfile(
        key="edge",
        model_id="Qwen/Qwen3.5-4B",
        runtime_model="qwen3.5:4b",
        usage="Default low-latency Korean employee dialogue and structured decisions.",
        max_parallel_decisions=4,
    ),
    "balanced": SLMModelProfile(
        key="balanced",
        model_id="Qwen/Qwen3.5-9B",
        runtime_model="qwen3.5:9b",
        usage="Higher dialogue quality for a single local GPU host.",
        max_parallel_decisions=2,
    ),
    "quality": SLMModelProfile(
        key="quality",
        model_id="Qwen/Qwen3.5-35B-A3B",
        runtime_model="qwen3.5:35b-a3b",
        usage="MoE quality profile for a dedicated local inference server.",
        max_parallel_decisions=1,
        temperature=0.3,
    ),
}


def get_model_profile(key: str) -> SLMModelProfile:
    """Return a profile or raise a descriptive error for configuration mistakes."""

    try:
        return MODEL_PROFILES[key]
    except KeyError as exc:
        choices = ", ".join(sorted(MODEL_PROFILES))
        raise ValueError(f"Unknown SLM model profile {key!r}; choose one of: {choices}") from exc
