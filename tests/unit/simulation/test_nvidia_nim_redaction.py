from __future__ import annotations

from typing import Any

from korean_social_simulator.models import AgentProfile, ScenarioSpec, SimulationPlan
from korean_social_simulator.simulation import nvidia_nim
from korean_social_simulator.simulation.nvidia_nim import run_nvidia_nim_simulation


def _make_plan() -> SimulationPlan:
    spec = ScenarioSpec(
        scenario_id="scenario-001",
        family="product_market",
        title="신규 서비스 반응 테스트",
        hypothesis="사용자는 핵심 가치 제안에 관심을 보일 수 있다.",
        participant_count=1,
        max_turns=1,
        metrics=["trust_score"],
        rag_queries=[],
    )
    return SimulationPlan(
        plan_id="plan-001",
        run_id="run-001",
        scenario_spec=spec,
        agent_count=1,
        max_turns=1,
        language="ko",
        dry_run=False,
    )


def _make_profile() -> AgentProfile:
    return AgentProfile(
        agent_id="agent-1",
        persona_uuid="persona-1",
        display_name="에이전트 1",
        language="ko",
        background="서울에 거주하는 합성 사용자입니다.",
        memory_seeds=["합성 페르소나입니다."],
        goals=["시나리오 상황에 자연스럽게 반응합니다."],
        behavior_rules=["한국어로 응답합니다."],
        safety_notes=["실존 인물이 아닙니다."],
    )


class _FailingCompletions:
    def create(self, **_: Any) -> Any:
        raise RuntimeError("provider rejected key nvapi-live-secret-123456")


class _FailingChat:
    completions = _FailingCompletions()


class _FailingClient:
    chat = _FailingChat()


def test_nim_event_errors_redact_known_secrets(monkeypatch) -> None:
    secret = "nvapi-live-secret-123456"
    monkeypatch.setenv("NVIDIA_API_KEY", secret)
    monkeypatch.setattr(nvidia_nim, "_get_openai_client", lambda: _FailingClient())

    events = run_nvidia_nim_simulation(_make_plan(), [_make_profile()])

    assert len(events) == 1
    error = events[0].payload["error"]
    assert secret not in error
    assert "***REDACTED***" in error
