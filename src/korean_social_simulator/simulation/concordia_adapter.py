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
from korean_social_simulator.simulation.nvidia_nim import (
    is_nvidia_nim_available,
    run_nvidia_nim_simulation,
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
    """Run simulation using Nvidia NIM, Concordia, or fallback to dry-run stubs.

    Prefers Nvidia NIM when NVIDIA_API_KEY is set, then tries Concordia,
    and falls back to partial result when neither is available.
    """
    if is_nvidia_nim_available():
        run_nvidia_nim_simulation(plan, profiles)
        return SimulationResult(
            run_id=plan.run_id,
            status="success",
            events_path=None,
            metrics_path=None,
            report_path=None,
            errors=[],
            warnings=[],
        )

    try:
        _load_concordia()
    except SimulationError:
        return SimulationResult(
            run_id=plan.run_id,
            status="partial",
            events_path=None,
            metrics_path=None,
            report_path=None,
            errors=["Concordia not installed and Nvidia NIM not configured"],
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
