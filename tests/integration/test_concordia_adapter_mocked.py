from __future__ import annotations

import builtins
import importlib
import sys
from unittest.mock import patch

from korean_social_simulator.models import (
    AgentProfile,
    ScenarioSpec,
    SimulationPlan,
    SimulationResult,
)

_REAL_IMPORT = builtins.__import__


def _make_plan() -> SimulationPlan:
    spec = ScenarioSpec(
        scenario_id="scenario-001",
        family="product_reaction",
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

    assert isinstance(result, SimulationResult)
    assert result.run_id == "run-001"
    assert result.status == "partial"
    assert "Concordia not installed" in " ".join(result.errors)
    assert result.warnings == []


def test_concordia_adapter_mocked_no_concordia_details_leak() -> None:
    sys.modules.pop("korean_social_simulator.simulation.concordia_adapter", None)

    with patch("builtins.__import__", side_effect=_import_without_concordia):
        module = importlib.import_module("korean_social_simulator.simulation.concordia_adapter")

    assert callable(module.run_simulation)
