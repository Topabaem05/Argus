"""SLM runtime adapter — Ollama/vLLM with offline-first fallback."""

from __future__ import annotations

import json
import random
from collections.abc import Callable
from dataclasses import dataclass
from importlib import import_module
from typing import Any, Literal, cast

from korean_social_simulator.errors import SimulationError

SLMProvider = Literal["ollama", "vllm", "nim", "none"]


@dataclass
class SLMResponse:
    action: str
    dialogue: str
    efficiency: float
    mood_change: int
    side_action: str | None


_FALLBACK_DIALOGUES = {
    "accept": ["네, 사장님! 바로 하겠습니다!", "알겠습니다. 처리하겠습니다."],
    "reluctant_accept": ["...알겠습니다.", "네, 하죠."],
    "refuse": ["저는 그런 일 안 합니다.", "죄송하지만 못 하겠습니다."],
    "complain": ["또 제가 해야 해요?", "정말 힘드네..."],
}


@dataclass
class SLMRuntimeAdapter:
    provider: SLMProvider = "none"
    model: str = "qwen2.5:7b"
    base_url: str | None = None
    api_key: str | None = None
    temperature: float = 0.7
    max_tokens: int = 256
    _client: object | None = None

    def __post_init__(self) -> None:
        if self.provider != "none":
            self._client = self._init_client()

    def _init_client(self) -> object:
        if self.provider == "ollama":
            base = self.base_url or "http://127.0.0.1:11434/v1"
            try:
                openai_mod = import_module("openai")
                factory = cast(Callable[..., object], vars(openai_mod)["OpenAI"])
                return factory(base_url=base, api_key=self.api_key or "ollama")
            except ImportError as exc:
                raise SimulationError("Ollama SLM requires: uv sync --extra llm") from exc
        if self.provider == "vllm":
            base = self.base_url or "http://127.0.0.1:8000/v1"
            try:
                openai_mod = import_module("openai")
                factory = cast(Callable[..., object], vars(openai_mod)["OpenAI"])
                return factory(base_url=base, api_key=self.api_key or "EMPTY")
            except ImportError as exc:
                raise SimulationError("vLLM SLM requires: uv sync --extra llm") from exc
        if self.provider == "nim":
            base = self.base_url or "https://integrate.api.nvidia.com/v1"
            try:
                openai_mod = import_module("openai")
                factory = cast(Callable[..., object], vars(openai_mod)["OpenAI"])
                return factory(base_url=base, api_key=self.api_key or "")
            except ImportError as exc:
                raise SimulationError("NVIDIA NIM requires: uv sync --extra llm") from exc
        raise SimulationError(f"Unknown SLM provider: {self.provider}")

    def generate(self, prompt: str, system_prompt: str = "") -> SLMResponse:
        if self.provider == "none" or self._client is None:
            return self._fallback_response(prompt)
        return self._live_response(prompt, system_prompt)

    def _live_response(self, prompt: str, system_prompt: str) -> SLMResponse:
        client = self._client
        chat = cast(object, getattr(client, "chat", None))
        if chat is None:
            raise SimulationError("SLM client missing chat interface")
        completions = cast(Any, chat).completions
        create_fn = cast(Callable[..., object], completions.create)
        messages: list[dict[str, str]] = []
        if system_prompt:
            messages.append({"role": "system", "content": system_prompt})
        messages.append({"role": "user", "content": prompt})
        response = create_fn(
            model=self.model,
            messages=messages,
            temperature=self.temperature,
            max_tokens=self.max_tokens,
        )
        raw_text = response.choices[0].message.content or ""  # type: ignore[attr-defined]
        return self._parse_json_response(raw_text)

    def _fallback_response(self, prompt: str) -> SLMResponse:
        rng = random.Random(hash(prompt) % 2**32)
        mood_marker = "거부" if "거부" in prompt else ("불만" if rng.random() < 0.2 else "수락")
        action = {
            "수락": "accept",
            "불만": "reluctant_accept",
            "거부": "refuse",
        }[mood_marker]
        dialogue = rng.choice(_FALLBACK_DIALOGUES[action])
        efficiency = {"accept": 0.9, "reluctant_accept": 0.5, "refuse": 0.0}[action]
        mood_change = {"accept": 2, "reluctant_accept": -5, "refuse": -10}[action]
        side = "gossip" if action == "reluctant_accept" and rng.random() < 0.3 else None
        return SLMResponse(
            action=action,
            dialogue=dialogue,
            efficiency=efficiency,
            mood_change=mood_change,
            side_action=side,
        )

    def _parse_json_response(self, raw: str) -> SLMResponse:
        cleaned = raw.strip()
        if cleaned.startswith("```"):
            cleaned = cleaned.split("\n", 1)[-1].rsplit("```", 1)[0]
        try:
            data = json.loads(cleaned)
            return SLMResponse(
                action=str(data.get("action", "accept")),
                dialogue=str(data.get("dialogue", "...")),
                efficiency=float(data.get("efficiency", 0.5)),
                mood_change=int(data.get("mood_change", 0)),
                side_action=data.get("side_action"),
            )
        except (json.JSONDecodeError, ValueError):
            return SLMResponse(
                action="accept",
                dialogue=cleaned[:100],
                efficiency=0.6,
                mood_change=0,
                side_action=None,
            )
