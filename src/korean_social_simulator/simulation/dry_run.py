from __future__ import annotations

from datetime import UTC, datetime

from korean_social_simulator.models import AgentProfile, SimulationEvent, SimulationPlan


def run_dry_run(
    plan: SimulationPlan,
    profiles: list[AgentProfile],
) -> list[SimulationEvent]:
    """Emit structural placeholder events for a dry-run simulation.

    Produces synthetic events for each turn without calling any LLM,
    network service, or external runtime.

    Raises:
        ValueError: If ``plan.max_turns`` is less than 1.
    """
    if plan.max_turns < 1:
        raise ValueError("SimulationPlan.max_turns must be at least 1.")

    events: list[SimulationEvent] = []

    for turn in range(1, plan.max_turns + 1):
        events.append(
            SimulationEvent(
                run_id=plan.run_id,
                turn=turn,
                event_type="system",
                timestamp=datetime.now(UTC).isoformat(),
                payload={
                    "phase": "turn_start",
                    "dry_run": True,
                    "plan_id": plan.plan_id,
                },
            )
        )

        for profile in profiles:
            events.append(
                SimulationEvent(
                    run_id=plan.run_id,
                    turn=turn,
                    event_type="observation",
                    actor_id=profile.agent_id,
                    timestamp=datetime.now(UTC).isoformat(),
                    payload={
                        "phase": "observation",
                        "dry_run": True,
                        "display_name": profile.display_name,
                        "language": profile.language,
                    },
                )
            )

        events.append(
            SimulationEvent(
                run_id=plan.run_id,
                turn=turn,
                event_type="system",
                timestamp=datetime.now(UTC).isoformat(),
                payload={
                    "phase": "turn_end",
                    "dry_run": True,
                    "observation_count": len(profiles),
                },
            )
        )

    events.append(
        SimulationEvent(
            run_id=plan.run_id,
            turn=plan.max_turns,
            event_type="metric_hook",
            actor_id=None,
            timestamp=datetime.now(UTC).isoformat(),
            payload={
                "phase": "turn_limit_reached",
                "max_turns": plan.max_turns,
            },
        )
    )

    return events
