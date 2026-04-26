from __future__ import annotations

from importlib import import_module
from types import ModuleType

from korean_social_simulator.errors import SimulationError
from korean_social_simulator.models import (
    AgentProfile,
    SimulationEvent,
    SimulationPlan,
    SimulationResult,
)

CONCORDIA_IMPORT_ERROR_MESSAGE = (
    "Concordia is not installed. Install with: uv sync --extra concordia"
)


def _load_concordia() -> ModuleType:
    try:
        return import_module("concordia")
    except ImportError as err:
        raise SimulationError(CONCORDIA_IMPORT_ERROR_MESSAGE) from err


def _build_stub_events(
    plan: SimulationPlan,
    profiles: list[AgentProfile],
) -> list[SimulationEvent]:
    if len(profiles) < plan.agent_count:
        raise SimulationError(
            f"Simulation plan requires {plan.agent_count} agent profiles, got {len(profiles)}."
        )

    participant_profiles = profiles[: plan.agent_count]
    turn_count = min(plan.max_turns, plan.scenario_spec.max_turns)
    events: list[SimulationEvent] = []

    for turn in range(1, turn_count + 1):
        events.append(
            SimulationEvent(
                run_id=plan.run_id,
                turn=turn,
                event_type="observation",
                payload={
                    "scenario_id": plan.scenario_spec.scenario_id,
                    "title": plan.scenario_spec.title,
                    "participant_count": len(participant_profiles),
                },
            )
        )

        for profile in participant_profiles:
            events.append(
                SimulationEvent(
                    run_id=plan.run_id,
                    turn=turn,
                    event_type="agent_action",
                    actor_id=profile.agent_id,
                    payload={
                        "display_name": profile.display_name,
                        "language": profile.language,
                        "goal": profile.goals[0] if profile.goals else "",
                        "mode": "stub_concordia_loop",
                    },
                )
            )

    return events


def run_simulation(
    plan: SimulationPlan,
    profiles: list[AgentProfile],
) -> SimulationResult:
    """Run a simulation through the optional Concordia adapter boundary.

    Concordia is imported lazily to avoid module-level import failures in the
    base install. When the dependency is unavailable, the adapter returns a
    partial SimulationResult instead of raising an ImportError.

    Raises:
        SimulationError: If Concordia is available but the MVP adapter cannot
            construct the stub simulation loop.
    """
    try:
        _load_concordia()
    except SimulationError:
        return SimulationResult(
            run_id=plan.run_id,
            status="partial",
            events_path=None,
            metrics_path=None,
            report_path=None,
            errors=["Concordia not installed"],
            warnings=[],
        )

    _build_stub_events(plan, profiles)

    return SimulationResult(
        run_id=plan.run_id,
        status="success",
        events_path=None,
        metrics_path=None,
        report_path=None,
        errors=[],
        warnings=[],
    )
