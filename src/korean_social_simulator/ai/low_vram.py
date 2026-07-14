"""Low-VRAM runtime planning for local minibot SLM inference.

The planner deliberately controls context, slots, batches, KV precision, multimodal
loading, and GPU offload together. Loading a small model with an unconstrained context or
multiple server slots can still exceed a 2-4 GB graphics card.
"""

from __future__ import annotations

import subprocess
from dataclasses import dataclass
from typing import Literal

from korean_social_simulator.ai.model_profiles import SLMModelProfile, get_model_profile

KVCacheType = Literal["q4_0", "q8_0", "f16"]


@dataclass(frozen=True)
class LowVramRuntimePlan:
    """One reproducible llama.cpp server configuration."""

    key: str
    profile_key: str
    vram_budget_gb: float
    context_tokens: int
    batch_size: int
    ubatch_size: int
    parallel_slots: int
    fit_target_mib: int
    cache_type_k: KVCacheType
    cache_type_v: KVCacheType
    gpu_layers: str = "auto"
    text_only: bool = True

    @property
    def profile(self) -> SLMModelProfile:
        return get_model_profile(self.profile_key)

    def llama_server_args(
        self,
        *,
        binary: str = "llama-server",
        host: str = "127.0.0.1",
        port: int = 8080,
        lora_path: str | None = None,
    ) -> list[str]:
        """Build shell-safe argv for a current llama.cpp server."""

        profile = self.profile
        if profile.gguf_repo is None:
            raise ValueError(f"Profile {profile.key!r} does not define a GGUF repository")

        args = [
            binary,
            "--hf-repo",
            f"{profile.gguf_repo}:{profile.quantization}",
            "--alias",
            profile.runtime_model,
            "--host",
            host,
            "--port",
            str(port),
            "--ctx-size",
            str(self.context_tokens),
            "--parallel",
            str(self.parallel_slots),
            "--batch-size",
            str(self.batch_size),
            "--ubatch-size",
            str(self.ubatch_size),
            "--gpu-layers",
            self.gpu_layers,
            "--fit",
            "on",
            "--fit-target",
            str(self.fit_target_mib),
            "--fit-ctx",
            str(self.context_tokens),
            "--cache-type-k",
            self.cache_type_k,
            "--cache-type-v",
            self.cache_type_v,
            "--flash-attn",
            "auto",
        ]
        if self.text_only:
            args.append("--no-mmproj")
        if lora_path:
            args.extend(("--lora", lora_path))
        return args


LOW_VRAM_PLANS: dict[str, LowVramRuntimePlan] = {
    "cpu": LowVramRuntimePlan(
        key="cpu",
        profile_key="micro",
        vram_budget_gb=0.0,
        context_tokens=1536,
        batch_size=128,
        ubatch_size=64,
        parallel_slots=1,
        fit_target_mib=64,
        cache_type_k="q4_0",
        cache_type_v="q4_0",
        gpu_layers="0",
    ),
    "vram_2gb": LowVramRuntimePlan(
        key="vram_2gb",
        profile_key="micro",
        vram_budget_gb=2.0,
        context_tokens=2048,
        batch_size=256,
        ubatch_size=128,
        parallel_slots=1,
        fit_target_mib=192,
        cache_type_k="q4_0",
        cache_type_v="q4_0",
    ),
    "vram_3gb": LowVramRuntimePlan(
        key="vram_3gb",
        profile_key="edge",
        vram_budget_gb=3.0,
        context_tokens=1536,
        batch_size=256,
        ubatch_size=128,
        parallel_slots=1,
        fit_target_mib=256,
        cache_type_k="q4_0",
        cache_type_v="q4_0",
    ),
    "vram_4gb": LowVramRuntimePlan(
        key="vram_4gb",
        profile_key="edge",
        vram_budget_gb=4.0,
        context_tokens=2048,
        batch_size=384,
        ubatch_size=192,
        parallel_slots=1,
        fit_target_mib=384,
        cache_type_k="q8_0",
        cache_type_v="q8_0",
    ),
    "vram_6gb": LowVramRuntimePlan(
        key="vram_6gb",
        profile_key="balanced",
        vram_budget_gb=6.0,
        context_tokens=3072,
        batch_size=512,
        ubatch_size=256,
        parallel_slots=1,
        fit_target_mib=512,
        cache_type_k="q8_0",
        cache_type_v="q8_0",
    ),
}


def select_low_vram_plan(vram_gb: float | None) -> LowVramRuntimePlan:
    """Select a conservative plan from detected or user-supplied VRAM."""

    if vram_gb is None or vram_gb <= 0:
        return LOW_VRAM_PLANS["cpu"]
    if vram_gb < 2.5:
        return LOW_VRAM_PLANS["vram_2gb"]
    if vram_gb < 3.5:
        return LOW_VRAM_PLANS["vram_3gb"]
    if vram_gb < 5.0:
        return LOW_VRAM_PLANS["vram_4gb"]
    return LOW_VRAM_PLANS["vram_6gb"]


def detect_nvidia_vram_gb() -> float | None:
    """Return the smallest visible NVIDIA GPU VRAM, or None without NVIDIA."""

    command = [
        "nvidia-smi",
        "--query-gpu=memory.total",
        "--format=csv,noheader,nounits",
    ]
    try:
        completed = subprocess.run(
            command,
            check=True,
            capture_output=True,
            text=True,
            timeout=5,
        )
    except (FileNotFoundError, subprocess.CalledProcessError, subprocess.TimeoutExpired):
        return None

    values: list[float] = []
    for line in completed.stdout.splitlines():
        try:
            values.append(float(line.strip()) / 1024.0)
        except ValueError:
            continue
    return min(values) if values else None
