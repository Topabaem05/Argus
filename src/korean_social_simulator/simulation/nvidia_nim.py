"""Nvidia NIM LLM adapter for Korean Social Simulation Lab."""

from __future__ import annotations

import os
from datetime import UTC, datetime
from typing import TYPE_CHECKING

from korean_social_simulator.models import AgentProfile, SimulationEvent, SimulationPlan

if TYPE_CHECKING:
    from openai import OpenAI

NVIDIA_BASE_URL = "https://integrate.api.nvidia.com/v1"
NVIDIA_MODEL = "deepseek-ai/deepseek-v4-pro"
NVIDIA_ENV_KEY = "NVIDIA_API_KEY"


def _load_api_key() -> str:
    key = os.environ.get(NVIDIA_ENV_KEY, "")
    if not key:
        try:
            from dotenv import load_dotenv

            load_dotenv()
            key = os.environ.get(NVIDIA_ENV_KEY, "")
        except ImportError:
            pass
    return key


def _get_openai_client() -> OpenAI:
    from openai import OpenAI

    return OpenAI(base_url=NVIDIA_BASE_URL, api_key=_load_api_key())


def is_nvidia_nim_available() -> bool:
    """Return True if the Nvidia NIM API key is configured in the environment."""
    return bool(_load_api_key())


def _build_system_prompt(profile: AgentProfile) -> str:
    goals = ", ".join(profile.goals) if profile.goals else "없음"
    rules = ", ".join(profile.behavior_rules) if profile.behavior_rules else "없음"
    return (
        f"당신은 {profile.display_name}입니다.\n"
        f"배경: {profile.background}\n"
        f"목표: {goals}\n"
        f"행동 규칙: {rules}\n"
        f"언어: {profile.language}"
    )


def _build_user_message(plan: SimulationPlan, turn: int) -> str:
    return (
        f"시나리오: {plan.scenario_spec.title}\n"
        f"가설: {plan.scenario_spec.hypothesis}\n"
        f"현재 {turn}번째 턴입니다.\n"
        "이 시나리오에 대한 당신의 생각과 반응을 한국어로 2-3문장으로 말해주세요."
    )


def run_nvidia_nim_simulation(
    plan: SimulationPlan,
    profiles: list[AgentProfile],
) -> list[SimulationEvent]:
    """Run a simulation using Nvidia NIM as the LLM backend."""
    client = _get_openai_client()
    events: list[SimulationEvent] = []
    max_turns = min(plan.max_turns, plan.scenario_spec.max_turns)
    participant_count = min(len(profiles), plan.scenario_spec.participant_count)

    for turn in range(1, max_turns + 1):
        for profile in profiles[:participant_count]:
            system_prompt = _build_system_prompt(profile)
            user_message = _build_user_message(plan, turn)

            try:
                response = client.chat.completions.create(
                    model=NVIDIA_MODEL,
                    messages=[
                        {"role": "system", "content": system_prompt},
                        {"role": "user", "content": user_message},
                    ],
                    temperature=1.0,
                    top_p=0.95,
                    max_tokens=16384,
                    extra_body={"chat_template_kwargs": {"thinking": False}},
                )
                events.append(
                    SimulationEvent(
                        run_id=plan.run_id,
                        turn=turn,
                        event_type="agent_action",
                        actor_id=profile.agent_id,
                        timestamp=datetime.now(UTC).isoformat(),
                        payload={
                            "display_name": profile.display_name,
                            "response": response.choices[0].message.content or "",
                            "language": profile.language,
                            "backend": "nvidia_nim",
                        },
                    )
                )
            except Exception as e:
                events.append(
                    SimulationEvent(
                        run_id=plan.run_id,
                        turn=turn,
                        event_type="system",
                        actor_id=profile.agent_id,
                        timestamp=datetime.now(UTC).isoformat(),
                        payload={
                            "error": str(e),
                            "agent": profile.display_name,
                            "backend": "nvidia_nim",
                        },
                    )
                )

    return events
