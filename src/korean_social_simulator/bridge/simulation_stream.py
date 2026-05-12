"""Stream dry-run simulation events to the Unity WebSocket."""

from __future__ import annotations

import asyncio

from korean_social_simulator.bridge.client_registry import ClientRegistry
from korean_social_simulator.bridge.event_adapter import SimulationEventAdapter
from korean_social_simulator.bridge.physics_coordinator import PhysicsCoordinator
from korean_social_simulator.bridge_schema import BridgeEnvelope
from korean_social_simulator.bridge_schema.events import Vec3
from korean_social_simulator.bridge_schema.physics import PhysicsConstraints, PhysicsRequest
from korean_social_simulator.config.models import BridgeConfig, RuntimeConfig
from korean_social_simulator.models import AgentProfile, SimulationEvent, SimulationPlan

_UNITY_FLOOR_Y = 0.0


def _grid_vec3(index: int, y: float = _UNITY_FLOOR_Y) -> Vec3:
    return Vec3(x=float((index % 5) * 2), y=y, z=float((index // 5) * 2))


def _build_spawn_envelope(
    *,
    agent_id: str,
    display_name: str,
    group_id: str | None,
    position: Vec3,
    schema_version: str,
    session_id: str,
    sequence: int,
) -> BridgeEnvelope:
    return BridgeEnvelope(
        schema_version=schema_version,
        message_id=f"spawn-{agent_id}-{sequence}",
        session_id=session_id,
        sequence=sequence,
        sent_at_ms=0,
        type="agent.spawn",
        payload={
            "agent": {
                "agent_id": agent_id,
                "display_name": display_name,
                "group_id": group_id,
                "position": {"x": position.x, "y": position.y, "z": position.z},
                "facing": 0.0,
                "emotion": {"label": "neutral", "intensity": 0.0},
                "current_action": "idle",
                "visible": True,
            },
            "spawn_reason": "scenario_start",
        },
    )


def _first_conflict(envelopes: list[BridgeEnvelope]) -> tuple[str, str, float] | None:
    for envelope in envelopes:
        if envelope.type != "conflict.update":
            continue
        participant_ids = envelope.payload.get("participant_ids")
        if not isinstance(participant_ids, list) or len(participant_ids) < 2:
            continue
        actor_id = participant_ids[0]
        target_id = participant_ids[1]
        if not isinstance(actor_id, str) or not isinstance(target_id, str):
            continue
        intensity_value = envelope.payload.get("intensity")
        intensity = intensity_value if isinstance(intensity_value, (int, float)) else 0.35
        return actor_id, target_id, float(intensity)
    return None


def _position_for_profile(profiles: list[AgentProfile], agent_id: str) -> Vec3:
    for index, profile in enumerate(profiles):
        if profile.agent_id == agent_id:
            return _grid_vec3(index)
    return _grid_vec3(0)


async def stream_dry_run_to_unity(
    *,
    events: list[SimulationEvent],
    profiles: list[AgentProfile],
    plan: SimulationPlan,
    runtime_config: RuntimeConfig,
    registry: ClientRegistry,
    bridge_config: BridgeConfig,
    coordinator: PhysicsCoordinator,
    session_id: str,
) -> dict[str, object]:
    """Adapt dry-run events, spawn agents, then stream public state to Unity."""
    sv = bridge_config.schema_config.version
    sent = 0
    adapter = SimulationEventAdapter(schema_version=sv, session_id=session_id)
    envelopes = adapter.adapt_events(events)

    for envelope in envelopes:
        if envelope.type != "environment.load":
            continue
        seq = registry.next_sequence()
        out = envelope.model_copy(update={"session_id": session_id, "sequence": seq})
        await registry.send_envelope(out)
        sent += 1

    for i, profile in enumerate(profiles):
        pos = _grid_vec3(i)
        seq = registry.next_sequence()
        spawn_env = _build_spawn_envelope(
            agent_id=profile.agent_id,
            display_name=profile.display_name or profile.agent_id,
            group_id=None,
            position=pos,
            schema_version=sv,
            session_id=session_id,
            sequence=seq,
        )
        await registry.send_envelope(spawn_env)
        sent += 1

    for envelope in envelopes:
        if envelope.type in {"adapter.error", "environment.load", "agent.spawn"}:
            continue
        seq = registry.next_sequence()
        out = envelope.model_copy(update={"session_id": session_id, "sequence": seq})
        await registry.send_envelope(out)
        sent += 1

    physics_emitted = False

    conflict = _first_conflict(envelopes)
    if conflict is not None:
        p0, p1, intensity = conflict
        req = PhysicsRequest(
            request_id=f"{plan.run_id}-conflict-physics-req",
            event_id=f"{plan.run_id}-conflict-physics",
            actor_id=p0,
            target_id=p1,
            action="push",
            actor_position=_position_for_profile(profiles, p0),
            target_position=_position_for_profile(profiles, p1),
            intensity=min(bridge_config.safety.max_physical_intensity, intensity),
            duration_ms=500,
            seed=42,
            constraints=PhysicsConstraints(
                max_force=10.0,
                allow_fall=True,
                allow_contact=True,
                non_graphic_mode=bridge_config.safety.non_graphic_mode,
            ),
        )
        result = await asyncio.to_thread(coordinator.evaluate, req)
        seq = registry.next_sequence()
        phys_env = BridgeEnvelope(
            schema_version=sv,
            message_id=f"{plan.run_id}-conflict-physics-result",
            correlation_id=f"{plan.run_id}-conflict-physics",
            session_id=session_id,
            sequence=seq,
            sent_at_ms=0,
            type="physics.result",
            payload=result.model_dump(mode="json"),
        )
        await registry.send_envelope(phys_env)
        sent += 1
        physics_emitted = True

    seq = registry.next_sequence()
    summary_env = BridgeEnvelope(
        schema_version=sv,
        message_id=f"{plan.run_id}-simulation-summary",
        session_id=session_id,
        sequence=seq,
        sent_at_ms=0,
        type="simulation.summary",
        payload={
            "run_id": plan.run_id,
            "status": "success",
            "agent_count": len(profiles),
            "event_count": len(events),
            "public_summary": "Deterministic dry-run bridge stream completed.",
        },
    )
    await registry.send_envelope(summary_env)
    sent += 1

    return {
        "streamed_envelopes": sent,
        "physics_emitted": physics_emitted,
        "run_id": plan.run_id,
        "agent_count": len(profiles),
        "event_count": len(events),
        "scenario_title": plan.scenario_spec.title,
        "dry_run": runtime_config.runtime.dry_run,
    }
