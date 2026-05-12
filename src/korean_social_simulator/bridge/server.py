from __future__ import annotations

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager
from pathlib import Path

from fastapi import FastAPI, HTTPException, Request
from pydantic import BaseModel, ConfigDict, Field, field_validator

from korean_social_simulator.bridge.agent_inspection import (
    AgentInspectionController,
    register_agent_inspection_routes,
)
from korean_social_simulator.bridge.client_registry import ClientRegistry
from korean_social_simulator.bridge.config import load_bridge_runtime_config
from korean_social_simulator.bridge.environment_catalog import (
    get_environment,
    list_supported_background_ids,
)
from korean_social_simulator.bridge.physics_coordinator import (
    PhysicsCoordinator,
    build_physics_coordinator,
)
from korean_social_simulator.bridge.replay_controller import (
    ReplayController,
    register_replay_routes,
)
from korean_social_simulator.bridge.simulation_stream import stream_dry_run_to_unity
from korean_social_simulator.bridge.websocket_gateway import register_unity_websocket
from korean_social_simulator.config.models import BridgeConfig
from korean_social_simulator.errors import ConfigurationError, KoreanSocialSimulationError
from korean_social_simulator.models import AttachmentInput
from korean_social_simulator.pipeline import bridge_prepare_dry_run_stream

_SUPPORTED_MESSAGE_TYPES = (
    "bridge.ready",
    "bridge.error",
    "simulation.snapshot",
    "simulation.event",
    "simulation.summary",
    "environment.load",
    "ui.status",
    "agent.spawn",
    "agent.move",
    "agent.behavior",
    "agent.animation",
    "agent.dialogue",
    "agent.emotion",
    "group.update",
    "conflict.update",
    "physics.request",
    "physics.result",
    "replay.status",
    "adapter.error",
    "unity.ready",
    "unity.ack",
    "unity.error",
    "observer.pause",
    "observer.resume",
    "observer.step",
    "observer.select_agent",
    "observer.camera_state",
)
_SUPPORTED_BACKGROUNDS = frozenset(list_supported_background_ids())


class SimulationStartBody(BaseModel):
    """HTTP body for starting a dry-run stream to Unity."""

    model_config = ConfigDict(extra="forbid")

    config_path: str = Field(
        default="examples/run_product_reaction.yaml",
        min_length=1,
        description="Path to an Argus runtime YAML config (relative to process cwd).",
    )
    max_turns_override: int | None = Field(
        default=None,
        ge=1,
        description="Optional cap on turns for faster bridge smoke tests.",
    )
    persona_count_override: int | None = Field(
        default=None,
        ge=1,
        le=20,
        description="Optional cap on selected personas for Unity-driven runs.",
    )
    chat_text: str | None = Field(
        default=None,
        description="Optional user chat text to drive persona selection.",
    )
    scenario_text: str | None = Field(
        default=None,
        description="Optional Unity-authored scenario text to compile into the run.",
    )
    background_id: str = Field(
        default="schoolroom",
        min_length=1,
        description="Unity environment identifier for the initial environment.load message.",
    )
    attachments: list[AttachmentInput] | None = Field(
        default=None,
        description="Optional attachment metadata; files are never executed.",
    )
    ui_session_id: str | None = Field(
        default=None,
        description="Optional Unity UI session correlation identifier.",
    )
    dry_run: bool = Field(default=True, description="Keep baseline bridge runs offline.")

    @field_validator("background_id")
    @classmethod
    def _validate_background_id(cls, value: str) -> str:
        try:
            get_environment(value)
        except ConfigurationError as exc:
            raise ValueError(str(exc)) from exc
        return value


def create_app(config: BridgeConfig) -> FastAPI:
    """Create the local bridge ASGI application."""
    registry = ClientRegistry()
    replay_controller = ReplayController()
    agent_inspection = AgentInspectionController()

    coordinator = build_physics_coordinator(config)

    @asynccontextmanager
    async def lifespan(app: FastAPI) -> AsyncIterator[None]:
        yield
        coordinator.close()

    app = FastAPI(
        title="Argus Unity Bridge",
        version=config.schema_config.version,
        lifespan=lifespan,
    )
    app.state.client_registry = registry
    app.state.replay_controller = replay_controller
    app.state.agent_inspection = agent_inspection
    app.state.physics_coordinator = coordinator
    app.state.bridge_config = config
    register_unity_websocket(app, config, registry, replay_controller)
    register_replay_routes(app, replay_controller)
    register_agent_inspection_routes(app, agent_inspection)

    @app.get("/health")
    def health(request: Request) -> dict[str, object]:
        unity_snapshot = registry.snapshot()
        replay_status = replay_controller.status()
        agent_status = agent_inspection.status()
        coordinator: PhysicsCoordinator = request.app.state.physics_coordinator
        physics_health = coordinator.health()
        backend = str(physics_health.get("backend", "unknown"))
        available = bool(physics_health.get("available", False))
        return {
            "status": "ok",
            "schema_version": config.schema_config.version,
            "server": {
                "host": config.server.host,
                "port": config.server.port,
                "allow_remote_clients": config.server.allow_remote_clients,
            },
            "unity": {
                "connected": unity_snapshot.connected,
                "ready": unity_snapshot.ready,
                "session_id": unity_snapshot.session_id,
                "last_disconnect_reason": unity_snapshot.last_disconnect_reason,
                "observer_selected_agent_id": unity_snapshot.observer_selected_agent_id,
                "require_ack": config.unity.require_ack,
            },
            "physics": {
                "backend": backend,
                "available": available,
                "fallback_on_error": True,
                "model_path": physics_health.get("model_path"),
            },
            "replay": {
                "enabled": config.replay.enabled,
                "output_dir": config.replay.output_dir,
                "loaded": replay_status["loaded"],
                "paused": replay_status["paused"],
                "event_count": replay_status["event_count"],
            },
            "agents": {
                "loaded": agent_status["loaded"],
                "agent_count": agent_status["agent_count"],
            },
        }

    @app.get("/schema/version")
    def schema_version() -> dict[str, object]:
        return {
            "schema_version": config.schema_config.version,
            "strict": config.schema_config.strict,
            "supported_message_types": list(_SUPPORTED_MESSAGE_TYPES),
            "supported_background_ids": sorted(_SUPPORTED_BACKGROUNDS),
        }

    @app.post("/simulation/start")
    async def simulation_start(request: Request, body: SimulationStartBody) -> dict[str, object]:
        cfg_path = Path(body.config_path)
        if not cfg_path.is_file():
            raise HTTPException(
                status_code=400,
                detail=f"Config file not found: {body.config_path}",
            )

        unity_snapshot = registry.snapshot()
        if not unity_snapshot.connected or not unity_snapshot.ready:
            raise HTTPException(
                status_code=503,
                detail="Unity client must be connected and have completed unity.ready handshake.",
            )

        session_id = body.ui_session_id or unity_snapshot.session_id or "unity-session"
        coordinator: PhysicsCoordinator = request.app.state.physics_coordinator

        try:
            events, profiles, plan, runtime_config = bridge_prepare_dry_run_stream(
                cfg_path,
                dry_run=body.dry_run,
                max_turns_override=body.max_turns_override,
                persona_count_override=body.persona_count_override,
                chat_text_override=body.chat_text,
                scenario_text_override=body.scenario_text,
                attachments_override=body.attachments,
                background_id=body.background_id,
            )
        except KoreanSocialSimulationError as exc:
            raise HTTPException(status_code=400, detail=str(exc)) from exc

        bridge_cfg: BridgeConfig = request.app.state.bridge_config
        summary = await stream_dry_run_to_unity(
            events=events,
            profiles=profiles,
            plan=plan,
            runtime_config=runtime_config,
            registry=registry,
            bridge_config=bridge_cfg,
            coordinator=coordinator,
            session_id=session_id,
        )
        return summary

    return app


def create_app_from_config(config_path: str) -> FastAPI:
    """Create the bridge app from a YAML config file."""
    return create_app(load_bridge_runtime_config(config_path))
