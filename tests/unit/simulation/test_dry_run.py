from __future__ import annotations

from korean_social_simulator.models import AgentProfile, ScenarioSpec, SimulationPlan
from korean_social_simulator.simulation import dry_run


def _make_plan(max_turns: int = 5, agent_count: int = 1) -> SimulationPlan:
    return SimulationPlan(
        plan_id="plan-001",
        run_id="run-001",
        scenario_spec=ScenarioSpec(
            scenario_id="scenario-001",
            family="community_operation",
            title="Community notice dry run",
            hypothesis="Placeholder observations model turn structure",
            participant_count=agent_count,
            max_turns=max_turns,
        ),
        agent_count=agent_count,
        max_turns=max_turns,
        language="ko",
        dry_run=True,
    )


def _make_profiles(count: int = 1) -> list[AgentProfile]:
    return [
        AgentProfile(
            agent_id=f"agent-{index:03d}",
            persona_uuid=f"persona-{index:03d}",
            display_name=f"테스트 사용자 {index}",
            language="ko",
            background="합성 페르소나 배경",
            memory_seeds=["서울 거주"],
            goals=["상황을 관찰한다"],
            behavior_rules=["한국어로 응답한다"],
            safety_notes=["합성 페르소나만 사용"],
        )
        for index in range(count)
    ]


def test_dry_run_emits_structural_events() -> None:
    """GIVEN a dry-run plan WHEN it executes THEN it emits system and observation events."""
    plan = _make_plan(max_turns=2, agent_count=1)
    profiles = _make_profiles(1)

    events = dry_run.run_dry_run(plan, profiles)

    assert {event.event_type for event in events} == {"system", "observation", "metric_hook"}
    assert events[0].payload["phase"] == "turn_start"
    assert events[-2].payload["phase"] == "turn_end"
    assert events[-1].payload["phase"] == "turn_limit_reached"


def test_dry_run_no_llm_calls() -> None:
    """GIVEN a dry-run plan WHEN it executes THEN it returns placeholder events without external dependencies."""
    plan = _make_plan(max_turns=1, agent_count=1)
    profiles = _make_profiles(1)

    events = dry_run.run_dry_run(plan, profiles)

    assert len(events) > 0
    assert all(
        event.payload.get("dry_run") is True
        for event in events
        if event.payload.get("phase") != "turn_limit_reached"
    )


def test_dry_run_enforces_max_turns() -> None:
    """GIVEN max_turns equals three WHEN the dry run executes THEN it emits exactly three turns of events."""
    plan = _make_plan(max_turns=3, agent_count=1)
    profiles = _make_profiles(1)

    events = dry_run.run_dry_run(plan, profiles)

    assert sorted({event.turn for event in events}) == [1, 2, 3]
    assert len(events) == (3 * (2 + len(profiles))) + 1


def test_dry_run_agent_events() -> None:
    """GIVEN two agent profiles WHEN the dry run executes THEN each turn includes observation events for both agents."""
    plan = _make_plan(max_turns=2, agent_count=2)
    profiles = _make_profiles(2)

    events = dry_run.run_dry_run(plan, profiles)

    for turn in range(1, plan.max_turns + 1):
        observation_ids = [
            event.actor_id
            for event in events
            if event.turn == turn and event.event_type == "observation"
        ]
        assert observation_ids == [profile.agent_id for profile in profiles]


def test_dry_run_records_turn_limit_reached() -> None:
    plan = _make_plan(max_turns=2, agent_count=1)
    profiles = _make_profiles(1)

    events = dry_run.run_dry_run(plan, profiles)

    assert events[-1].event_type == "metric_hook"
    assert events[-1].turn == plan.max_turns
    assert events[-1].actor_id is None
    assert events[-1].payload == {
        "phase": "turn_limit_reached",
        "max_turns": plan.max_turns,
    }
