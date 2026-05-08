"""Stream dry-run simulation events to the Unity WebSocket with deterministic physics moves."""

from __future__ import annotations

import asyncio
import math
import random

from korean_social_simulator.bridge.client_registry import ClientRegistry
from korean_social_simulator.bridge.event_adapter import SimulationEventAdapter
from korean_social_simulator.bridge.physics_coordinator import PhysicsCoordinator
from korean_social_simulator.bridge_schema import BridgeEnvelope
from korean_social_simulator.bridge_schema.events import Vec3
from korean_social_simulator.bridge_schema.physics import PhysicsConstraints, PhysicsRequest
from korean_social_simulator.config.models import BridgeConfig, RuntimeConfig
from korean_social_simulator.models import AgentProfile, SimulationEvent, SimulationPlan

_MOVE_FRAMES = 20
_STEPS_PER_FRAME = 50
_FRAME_DELAY_S = 0.15


def _grid_vec3(index: int, y: float = 0.6) -> Vec3:
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


def _build_move_envelope(
    *,
    agent_id: str,
    target: Vec3,
    speed: float,
    schema_version: str,
    session_id: str,
    sequence: int,
) -> BridgeEnvelope:
    return BridgeEnvelope(
        schema_version=schema_version,
        message_id=f"move-{agent_id}-{sequence}",
        session_id=session_id,
        sequence=sequence,
        sent_at_ms=0,
        type="agent.move",
        payload={
            "agent_id": agent_id,
            "target_position": {"x": target.x, "y": target.y, "z": target.z},
            "speed_mps": speed,
            "movement_style": "walk",
        },
    )


async def _run_deterministic_motion_frames(
    *,
    agent_body_map: dict[str, str],
    registry: ClientRegistry,
    bridge_config: BridgeConfig,
    session_id: str,
) -> int:
    """Generate deterministic agent.move envelopes for visualization."""
    rng = random.Random(42)
    sent = 0

    for _frame in range(_MOVE_FRAMES):
        for body_name, agent_id in agent_body_map.items():
            angle = rng.uniform(0, 2 * math.pi)
            distance = rng.uniform(0.1, 0.5)
            idx = list(agent_body_map.keys()).index(body_name)
            base_pos = _grid_vec3(idx)
            pos = Vec3(
                x=base_pos.x + math.cos(angle) * distance,
                y=max(0.6, base_pos.y),
                z=base_pos.z + math.sin(angle) * distance,
            )
            seq = registry.next_sequence()
            env = _build_move_envelope(
                agent_id=agent_id,
                target=pos,
                speed=2.5,
                schema_version=bridge_config.schema_config.version,
                session_id=session_id,
                sequence=seq,
            )
            await registry.send_envelope(env)
            sent += 1

        await asyncio.sleep(_FRAME_DELAY_S)

    return sent


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
    """Adapt dry-run events, spawn agents, then stream deterministic physics moves to Unity."""
    sv = bridge_config.schema_config.version
    sent = 0

    agent_body_map: dict[str, str] = {}
    for i, profile in enumerate(profiles):
        body_name = f"agent_{i}"
        agent_body_map[body_name] = profile.agent_id
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

    adapter = SimulationEventAdapter(schema_version=sv, session_id=session_id)
    envelopes = adapter.adapt_events(events)
    for envelope in envelopes:
        if envelope.type == "adapter.error":
            continue
        seq = registry.next_sequence()
        out = envelope.model_copy(update={"session_id": session_id, "sequence": seq})
        await registry.send_envelope(out)
        sent += 1

    physics_emitted = False

    if len(profiles) >= 2:
        p0, p1 = profiles[0].agent_id, profiles[1].agent_id
        req = PhysicsRequest(
            request_id=f"{plan.run_id}-phys-demo-req",
            event_id=f"{plan.run_id}-phys-demo-ev",
            actor_id=p0,
            target_id=p1,
            action="push",
            actor_position=_grid_vec3(0),
            target_position=_grid_vec3(1),
            intensity=min(bridge_config.safety.max_physical_intensity, 0.75),
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
            message_id=f"{plan.run_id}-physics-result-demo",
            correlation_id=f"{plan.run_id}-phys-demo-ev",
            session_id=session_id,
            sequence=seq,
            sent_at_ms=0,
            type="physics.result",
            payload=result.model_dump(mode="json"),
        )
        await registry.send_envelope(phys_env)
        sent += 1
        physics_emitted = True

    return {
        "streamed_envelopes": sent,
        "physics_emitted": physics_emitted,
        "run_id": plan.run_id,
        "agent_count": len(profiles),
        "event_count": len(events),
        "scenario_title": plan.scenario_spec.title,
        "dry_run": runtime_config.runtime.dry_run,
    }
