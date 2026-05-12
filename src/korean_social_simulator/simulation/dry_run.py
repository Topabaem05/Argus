from __future__ import annotations

from datetime import UTC, datetime

from korean_social_simulator.bridge.environment_catalog import build_environment_load_event
from korean_social_simulator.models import (
    AgentProfile,
    IndividualEvaluation,
    SimulationEvent,
    SimulationPlan,
)
from korean_social_simulator.simulation.behavior_planner import build_behavior_intents
from korean_social_simulator.simulation.interaction import InteractionContext


def run_dry_run(
    plan: SimulationPlan,
    profiles: list[AgentProfile],
    interaction_context: InteractionContext | None = None,
    background_id: str = "schoolroom",
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
    evaluations_by_agent = {
        evaluation.agent_id: evaluation
        for evaluation in (interaction_context.evaluations if interaction_context else [])
    }

    if interaction_context is not None:
        events.extend(
            _interaction_prelude_events(
                plan,
                profiles,
                interaction_context,
                background_id=background_id,
            )
        )

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
            evaluation = evaluations_by_agent.get(profile.agent_id)
            if evaluation is not None:
                events.append(_dialogue_event(plan, profile, evaluation, turn))

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

    if interaction_context is not None and len(profiles) >= 2:
        events.append(_conflict_summary_event(plan, profiles))

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


def _interaction_prelude_events(
    plan: SimulationPlan,
    profiles: list[AgentProfile],
    context: InteractionContext,
    *,
    background_id: str,
) -> list[SimulationEvent]:
    selected_ids = [selection.agent_id for selection in context.selections]
    events = [
        _environment_load_event(plan, background_id, len(profiles)),
        SimulationEvent(
            run_id=plan.run_id,
            turn=0,
            event_type="system",
            timestamp=datetime.now(UTC).isoformat(),
            payload={
                "phase": "input_summary",
                "dry_run": True,
                "input": context.summary.model_dump(mode="json"),
            },
        ),
        SimulationEvent(
            run_id=plan.run_id,
            turn=0,
            event_type="system",
            timestamp=datetime.now(UTC).isoformat(),
            payload={
                "phase": "persona_selection",
                "dry_run": True,
                "selected_personas": [
                    selection.model_dump(mode="json") for selection in context.selections
                ],
            },
        ),
    ]

    if selected_ids:
        events.append(
            SimulationEvent(
                run_id=plan.run_id,
                turn=0,
                event_type="agent_action",
                actor_id=None,
                timestamp=datetime.now(UTC).isoformat(),
                payload={
                    "phase": "group_formation",
                    "dry_run": True,
                    "bridge_type": "group.update",
                    "bridge_payload": {
                        "group_id": f"{plan.run_id}-discussion",
                        "member_agent_ids": selected_ids,
                        "badge_label": "discussion",
                    },
                },
            )
        )

    for profile in profiles:
        evaluation = next(
            (
                candidate
                for candidate in context.evaluations
                if candidate.agent_id == profile.agent_id
            ),
            None,
        )
        if evaluation is None:
            continue
        events.append(
            SimulationEvent(
                run_id=plan.run_id,
                turn=0,
                event_type="agent_action",
                actor_id=profile.agent_id,
                timestamp=datetime.now(UTC).isoformat(),
                payload={
                    "phase": "individual_evaluation",
                    "dry_run": True,
                    "evaluation": evaluation.model_dump(mode="json"),
                    "bridge_type": "agent.emotion",
                    "bridge_payload": {
                        "agent_id": profile.agent_id,
                        "label": _emotion_for_stance(evaluation.stance),
                        "intensity": evaluation.confidence,
                    },
                },
            )
        )

    for intent in build_behavior_intents(profiles, context.evaluations):
        events.append(
            SimulationEvent(
                run_id=plan.run_id,
                turn=0,
                event_type="agent_action",
                actor_id=intent.agent_id,
                timestamp=datetime.now(UTC).isoformat(),
                payload={
                    "phase": "behavior_intent",
                    "dry_run": True,
                    "bridge_type": "agent.behavior",
                    "bridge_payload": intent.model_dump(mode="json"),
                },
            )
        )

    return events


def _environment_load_event(
    plan: SimulationPlan,
    background_id: str,
    agent_count: int,
) -> SimulationEvent:
    environment = build_environment_load_event(background_id, agent_count)
    return SimulationEvent(
        run_id=plan.run_id,
        turn=0,
        event_type="system",
        timestamp=datetime.now(UTC).isoformat(),
        payload={
            "phase": "environment_load",
            "dry_run": True,
            "bridge_type": "environment.load",
            "bridge_payload": environment.model_dump(mode="json"),
        },
    )


def _dialogue_event(
    plan: SimulationPlan,
    profile: AgentProfile,
    evaluation: IndividualEvaluation,
    turn: int,
) -> SimulationEvent:
    stance = evaluation.stance
    confidence = evaluation.confidence
    text = f"{profile.display_name}: stance={stance}, confidence={confidence:.2f}."
    return SimulationEvent(
        run_id=plan.run_id,
        turn=turn,
        event_type="agent_action",
        actor_id=profile.agent_id,
        timestamp=datetime.now(UTC).isoformat(),
        payload={
            "phase": "discussion_turn",
            "dry_run": True,
            "stance": stance,
            "confidence": confidence,
            "bridge_type": "agent.dialogue",
            "bridge_payload": {
                "speaker_id": profile.agent_id,
                "target_ids": [],
                "text": text,
                "emotion": {
                    "label": _emotion_for_stance(stance),
                    "intensity": min(1.0, confidence),
                },
                "speech_act": "say",
                "duration_ms": 1800,
            },
        },
    )


def _conflict_summary_event(plan: SimulationPlan, profiles: list[AgentProfile]) -> SimulationEvent:
    participant_ids = [profile.agent_id for profile in profiles[: min(4, len(profiles))]]
    return SimulationEvent(
        run_id=plan.run_id,
        turn=plan.max_turns,
        event_type="agent_action",
        timestamp=datetime.now(UTC).isoformat(),
        payload={
            "phase": "relationship_update",
            "dry_run": True,
            "relationship_state": {
                "trust": 0.55,
                "affinity": 0.05,
                "tension": 0.35,
                "influence": 0.25,
            },
            "bridge_type": "conflict.update",
            "bridge_payload": {
                "conflict_id": f"{plan.run_id}-symbolic-tension",
                "participant_ids": participant_ids,
                "intensity": 0.35,
                "stage": "tension",
                "public_summary": "Symbolic dry-run disagreement; not a real-world prediction.",
            },
        },
    )


def _emotion_for_stance(stance: str) -> str:
    if stance == "supports":
        return "happy"
    if stance == "opposes":
        return "angry"
    if stance == "mixed":
        return "confused"
    return "neutral"
