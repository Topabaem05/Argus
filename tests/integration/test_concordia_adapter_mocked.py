from __future__ import annotations

import builtins
import importlib
import sys
from unittest.mock import patch

from korean_social_simulator.models import (
    AgentProfile,
    ScenarioSpec,
    SimulationExecution,
    SimulationPlan,
)

_REAL_IMPORT = builtins.__import__


def _make_plan() -> SimulationPlan:
    spec = ScenarioSpec(
        scenario_id="scenario-001",
        family="product_market",
        title="신규 서비스 반응 테스트",
        hypothesis="사용자는 핵심 가치 제안에 관심을 보일 수 있다.",
        participant_count=2,
        max_turns=3,
        metrics=["trust_score"],
        rag_queries=[],
    )

    return SimulationPlan(
        plan_id="plan-001",
        run_id="run-001",
        scenario_spec=spec,
        agent_count=2,
        max_turns=3,
        language="ko",
        dry_run=True,
    )


def _make_profiles(count: int = 2) -> list[AgentProfile]:
    return [
        AgentProfile(
            agent_id=f"agent-{idx}",
            persona_uuid=f"persona-{idx}",
            display_name=f"에이전트 {idx}",
            language="ko",
            background="서울에 거주하는 합성 사용자입니다.",
            memory_seeds=["합성 페르소나입니다."],
            goals=["시나리오 상황에 자연스럽게 반응합니다."],
            behavior_rules=["한국어로 응답합니다."],
            safety_notes=["실존 인물이 아닙니다."],
        )
        for idx in range(1, count + 1)
    ]


def _import_without_concordia(
    name: str,
    globals: dict[str, object] | None = None,
    locals: dict[str, object] | None = None,
    fromlist: tuple[str, ...] = (),
    level: int = 0,
) -> object:
    if name == "concordia":
        raise ImportError("No module named 'concordia'")

    return _REAL_IMPORT(name, globals, locals, fromlist, level)


def test_concordia_adapter_mocked_runs_with_mocked_llm() -> None:
    from korean_social_simulator.simulation.concordia_adapter import run_simulation

    with (
        patch(
            "korean_social_simulator.simulation.concordia_adapter.import_module",
            side_effect=ImportError("No module named 'concordia'"),
        ),
        patch(
            "korean_social_simulator.simulation.concordia_adapter.is_nvidia_nim_available",
            return_value=False,
        ),
    ):
        result = run_simulation(_make_plan(), _make_profiles())

    assert isinstance(result, SimulationExecution)
    assert result.run_id == "run-001"
    assert result.status == "partial"
    assert result.events == []
    assert "Concordia not installed" in " ".join(result.errors)
    assert result.warnings == []


def test_concordia_adapter_preserves_nim_events() -> None:
    from korean_social_simulator.models import SimulationEvent
    from korean_social_simulator.simulation.concordia_adapter import run_simulation

    nim_events = [
        SimulationEvent(
            run_id="run-001",
            turn=1,
            event_type="agent_action",
            actor_id="agent-1",
            payload={"response": "ok"},
        )
    ]

    with (
        patch(
            "korean_social_simulator.simulation.concordia_adapter.is_nvidia_nim_available",
            return_value=True,
        ),
        patch(
            "korean_social_simulator.simulation.concordia_adapter.run_nvidia_nim_simulation",
            return_value=nim_events,
        ),
    ):
        result = run_simulation(_make_plan(), _make_profiles())

    assert result.status == "success"
    assert result.events == nim_events


def test_concordia_adapter_reports_nim_failure() -> None:
    from korean_social_simulator.simulation.concordia_adapter import run_simulation

    with (
        patch(
            "korean_social_simulator.simulation.concordia_adapter.is_nvidia_nim_available",
            return_value=True,
        ),
        patch(
            "korean_social_simulator.simulation.concordia_adapter.run_nvidia_nim_simulation",
            side_effect=RuntimeError("missing optional llm extra"),
        ),
    ):
        result = run_simulation(_make_plan(), _make_profiles())

    assert result.status == "failed"
    assert result.events == []
    assert "missing optional llm extra" in " ".join(result.errors)


def test_concordia_adapter_marks_nim_system_only_events_partial() -> None:
    from korean_social_simulator.models import SimulationEvent
    from korean_social_simulator.simulation.concordia_adapter import run_simulation

    nim_events = [
        SimulationEvent(
            run_id="run-001",
            turn=1,
            event_type="system",
            actor_id="agent-1",
            payload={"error": "provider timeout"},
        )
    ]

    with (
        patch(
            "korean_social_simulator.simulation.concordia_adapter.is_nvidia_nim_available",
            return_value=True,
        ),
        patch(
            "korean_social_simulator.simulation.concordia_adapter.run_nvidia_nim_simulation",
            return_value=nim_events,
        ),
    ):
        result = run_simulation(_make_plan(), _make_profiles())

    assert result.status == "partial"
    assert result.events == nim_events
    assert "no agent responses" in " ".join(result.warnings)


def test_concordia_adapter_redacts_nim_failure_secret(monkeypatch) -> None:
    from korean_social_simulator.simulation.concordia_adapter import run_simulation

    secret = "nvapi-live-secret-123456"
    monkeypatch.setenv("NVIDIA_API_KEY", secret)

    with (
        patch(
            "korean_social_simulator.simulation.concordia_adapter.is_nvidia_nim_available",
            return_value=True,
        ),
        patch(
            "korean_social_simulator.simulation.concordia_adapter.run_nvidia_nim_simulation",
            side_effect=RuntimeError(f"provider rejected key {secret}"),
        ),
    ):
        result = run_simulation(_make_plan(), _make_profiles())

    error_text = " ".join(result.errors)
    assert secret not in error_text
    assert "***REDACTED***" in error_text


def test_concordia_adapter_mocked_no_concordia_details_leak() -> None:
    sys.modules.pop("korean_social_simulator.simulation.concordia_adapter", None)

    with patch("builtins.__import__", side_effect=_import_without_concordia):
        module = importlib.import_module("korean_social_simulator.simulation.concordia_adapter")

    assert callable(module.run_simulation)
