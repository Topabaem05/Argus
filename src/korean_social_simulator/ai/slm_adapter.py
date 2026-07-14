"""SLM runtime adapter for deterministic, low-latency game agent decisions.

The game protocol only needs a compact structured decision, not a long-form answer.
This adapter therefore validates every model response and always has an offline,
deterministic fallback so a local runtime outage cannot stall a multiplayer round.
"""

from __future__ import annotations

import hashlib
import json
import random
import re
from collections.abc import Callable
from dataclasses import dataclass, field
from importlib import import_module
from typing import Any, Literal, cast

from korean_social_simulator.errors import SimulationError

SLMProvider = Literal["ollama", "vllm", "llamacpp", "nim", "none"]
SLMAction = Literal["accept", "reluctant_accept", "refuse", "complain"]
SLMSideAction = Literal["gossip", "consider_quit"]

_ALLOWED_ACTIONS: frozenset[str] = frozenset(
    {"accept", "reluctant_accept", "refuse", "complain"}
)
_ALLOWED_SIDE_ACTIONS: frozenset[str] = frozenset({"gossip", "consider_quit"})
_MOOD_PATTERN = re.compile(r"(?:현재\s*)?기분\s*:\s*(-?\d+)")
_LOYALTY_PATTERN = re.compile(r"호감도\s*:\s*(-?\d+)")


@dataclass(frozen=True)
class SLMResponse:
    action: SLMAction
    dialogue: str
    efficiency: float
    mood_change: int
    side_action: SLMSideAction | None


_FALLBACK_DIALOGUES: dict[SLMAction, tuple[str, ...]] = {
    "accept": ("네, 사장님! 바로 하겠습니다!", "알겠습니다. 처리하겠습니다."),
    "reluctant_accept": ("...알겠습니다.", "네, 하죠."),
    "refuse": ("저는 그런 일 안 합니다.", "죄송하지만 못 하겠습니다."),
    "complain": ("또 제가 해야 해요?", "정말 힘드네..."),
}

_PROVIDER_DEFAULT_URLS: dict[SLMProvider, str] = {
    "ollama": "http://127.0.0.1:11434/v1",
    "vllm": "http://127.0.0.1:8000/v1",
    "llamacpp": "http://127.0.0.1:8080/v1",
    "nim": "https://integrate.api.nvidia.com/v1",
    "none": "",
}


@dataclass
class SLMRuntimeAdapter:
    provider: SLMProvider = "none"
    model: str = "qwen3.5:4b"
    base_url: str | None = None
    api_key: str | None = None
    temperature: float = 0.35
    max_tokens: int = 192
    timeout_seconds: float = 20.0
    max_retries: int = 1
    fallback_on_error: bool = True
    _client: object | None = field(default=None, init=False, repr=False)
    last_error: str | None = field(default=None, init=False)

    def __post_init__(self) -> None:
        if not 0.0 <= self.temperature <= 2.0:
            raise ValueError("temperature must be between 0.0 and 2.0")
        if self.max_tokens < 1:
            raise ValueError("max_tokens must be positive")
        if self.timeout_seconds <= 0:
            raise ValueError("timeout_seconds must be positive")
        if self.max_retries < 0:
            raise ValueError("max_retries must not be negative")
        if self.provider != "none":
            self._client = self._init_client()

    def _init_client(self) -> object:
        if self.provider not in _PROVIDER_DEFAULT_URLS or self.provider == "none":
            raise SimulationError(f"Unknown SLM provider: {self.provider}")

        base = self.base_url or _PROVIDER_DEFAULT_URLS[self.provider]
        try:
            openai_mod = import_module("openai")
            factory = cast(Callable[..., object], vars(openai_mod)["OpenAI"])
            default_key = "EMPTY" if self.provider in {"vllm", "llamacpp"} else self.provider
            return factory(
                base_url=base,
                api_key=self.api_key or default_key,
                timeout=self.timeout_seconds,
                max_retries=self.max_retries,
            )
        except ImportError as exc:
            raise SimulationError(
                f"{self.provider} SLM requires the optional LLM dependencies: uv sync --extra llm"
            ) from exc

    def generate(self, prompt: str, system_prompt: str = "") -> SLMResponse:
        """Generate one validated employee decision.

        A runtime failure falls back to deterministic local behavior by default. This is
        intentional: one unavailable model process must not freeze a real-time game round.
        Set ``fallback_on_error=False`` in integration tests that need fail-fast behavior.
        """

        if self.provider == "none" or self._client is None:
            return self._fallback_response(prompt)

        try:
            response = self._live_response(prompt, system_prompt)
            self.last_error = None
            return response
        except Exception as exc:
            self.last_error = f"{type(exc).__name__}: {exc}"
            if not self.fallback_on_error:
                raise SimulationError(f"SLM generation failed: {self.last_error}") from exc
            return self._fallback_response(prompt)

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
        choices = cast(Any, response).choices
        if not choices:
            raise SimulationError("SLM returned no choices")
        raw_text = choices[0].message.content or ""
        return self._parse_json_response(str(raw_text))

    def _fallback_response(self, prompt: str) -> SLMResponse:
        digest = hashlib.sha256(prompt.encode("utf-8")).digest()
        rng = random.Random(int.from_bytes(digest[:8], byteorder="big", signed=False))

        prompt_lower = prompt.lower()
        mood = self._extract_metric(_MOOD_PATTERN, prompt_lower)
        loyalty = self._extract_metric(_LOYALTY_PATTERN, prompt_lower)
        explicit_refusal = any(
            marker in prompt_lower for marker in ("거부 임계", "업무 거부", "명령 거부")
        )
        if (
            (mood is not None and mood <= 15)
            or (loyalty is not None and loyalty <= -80)
            or explicit_refusal
        ):
            action: SLMAction = "refuse"
        elif (
            (mood is not None and mood <= 35)
            or (loyalty is not None and loyalty < 0)
            or any(marker in prompt_lower for marker in ("우울", "불만"))
        ):
            action = "reluctant_accept" if rng.random() < 0.75 else "complain"
        else:
            action = "accept" if rng.random() < 0.82 else "reluctant_accept"

        dialogue = rng.choice(_FALLBACK_DIALOGUES[action])
        efficiency = {
            "accept": 0.9,
            "reluctant_accept": 0.55,
            "refuse": 0.0,
            "complain": 0.4,
        }[action]
        mood_change = {
            "accept": 2,
            "reluctant_accept": -4,
            "refuse": -8,
            "complain": -6,
        }[action]
        side_action: SLMSideAction | None = None
        if action in {"reluctant_accept", "complain"} and rng.random() < 0.3:
            side_action = "gossip"
        elif action == "refuse" and rng.random() < 0.15:
            side_action = "consider_quit"

        return SLMResponse(
            action=action,
            dialogue=dialogue,
            efficiency=efficiency,
            mood_change=mood_change,
            side_action=side_action,
        )

    def _parse_json_response(self, raw: str) -> SLMResponse:
        data = self._decode_first_json_object(raw)
        if data is None:
            cleaned = raw.strip()
            return SLMResponse(
                action="accept",
                dialogue=(cleaned[:160] or "알겠습니다."),
                efficiency=0.6,
                mood_change=0,
                side_action=None,
            )

        action_raw = str(data.get("action", "accept"))
        action: SLMAction = cast(
            SLMAction,
            action_raw if action_raw in _ALLOWED_ACTIONS else "accept",
        )
        dialogue = str(data.get("dialogue", "...")).strip()[:160] or "..."
        efficiency = self._clamp_float(data.get("efficiency", 0.5), 0.0, 1.0, 0.5)
        mood_change = int(self._clamp_float(data.get("mood_change", 0), -10.0, 10.0, 0.0))
        side_raw = data.get("side_action")
        side_action = (
            cast(SLMSideAction, side_raw)
            if isinstance(side_raw, str) and side_raw in _ALLOWED_SIDE_ACTIONS
            else None
        )
        return SLMResponse(
            action=action,
            dialogue=dialogue,
            efficiency=efficiency,
            mood_change=mood_change,
            side_action=side_action,
        )

    @staticmethod
    def _extract_metric(pattern: re.Pattern[str], prompt: str) -> int | None:
        match = pattern.search(prompt)
        if match is None:
            return None
        try:
            return int(match.group(1))
        except ValueError:
            return None

    @staticmethod
    def _decode_first_json_object(raw: str) -> dict[str, object] | None:
        cleaned = raw.strip()
        if cleaned.startswith("```"):
            first_newline = cleaned.find("\n")
            if first_newline >= 0:
                cleaned = cleaned[first_newline + 1 :]
            if cleaned.endswith("```"):
                cleaned = cleaned[:-3]

        decoder = json.JSONDecoder()
        for index, character in enumerate(cleaned):
            if character != "{":
                continue
            try:
                value, _end = decoder.raw_decode(cleaned[index:])
            except json.JSONDecodeError:
                continue
            if isinstance(value, dict):
                return cast(dict[str, object], value)
        return None

    @staticmethod
    def _clamp_float(value: object, minimum: float, maximum: float, default: float) -> float:
        try:
            parsed = float(cast(Any, value))
        except (TypeError, ValueError):
            return default
        if parsed != parsed:  # NaN
            return default
        return max(minimum, min(maximum, parsed))
