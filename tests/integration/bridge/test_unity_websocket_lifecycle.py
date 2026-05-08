from __future__ import annotations

from collections.abc import Mapping

import pytest
from fastapi.testclient import TestClient
from starlette.websockets import WebSocketDisconnect

from korean_social_simulator.bridge.server import create_app
from korean_social_simulator.config.loader import load_bridge_config
from korean_social_simulator.config.models import BridgeConfig


def _config_with_max_payload(max_payload_bytes: int) -> BridgeConfig:
    config = load_bridge_config("configs/bridge.example.yaml")
    return config.model_copy(
        update={"server": config.server.model_copy(update={"max_payload_bytes": max_payload_bytes})}
    )


def _unity_ready(session_id: str = "unity-session-1") -> dict[str, object]:
    return {
        "schema_version": "1.0.0",
        "message_id": "unity-ready-1",
        "session_id": session_id,
        "sequence": 0,
        "sent_at_ms": 0,
        "type": "unity.ready",
        "payload": {},
    }


def test_unity_ready_receives_bridge_ready() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    with client.websocket_connect("/ws/unity") as websocket:
        websocket.send_json(_unity_ready())
        response = websocket.receive_json()

        assert response["type"] == "bridge.ready"
        assert response["correlation_id"] == "unity-ready-1"
        assert response["session_id"] == "unity-session-1"
        assert response["payload"]["require_ack"] is True

    health = client.get("/health").json()
    assert health["unity"]["connected"] is False
    assert health["unity"]["ready"] is False
    assert health["unity"]["last_disconnect_reason"] == "client_disconnect"


def test_invalid_json_receives_structured_error() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    with client.websocket_connect("/ws/unity") as websocket:
        websocket.send_text("{")
        response = websocket.receive_json()

        assert response["type"] == "bridge.error"
        assert response["payload"]["source"] == "bridge"
        assert response["payload"]["recoverable"] is True
        assert "not valid JSON" in response["payload"]["message"]


def test_invalid_envelope_receives_structured_error() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    with client.websocket_connect("/ws/unity") as websocket:
        websocket.send_json({"type": "unity.ready"})
        response = websocket.receive_json()

        assert response["type"] == "bridge.error"
        assert response["payload"]["message"] == "Unity bridge envelope validation failed."
        assert isinstance(response["payload"]["details"]["errors"], list)


def test_incompatible_unity_message_receives_structured_error() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)
    incompatible_message = _unity_ready()
    incompatible_message["message_id"] = "agent-spawn-from-unity"
    incompatible_message["type"] = "agent.spawn"
    incompatible_message["payload"] = {
        "agent": {
            "agent_id": "a1",
            "display_name": "Agent 1",
            "position": {"x": 0.0, "y": 0.0, "z": 0.0},
            "facing": 0.0,
            "emotion": {"label": "neutral", "intensity": 0.0},
            "current_action": "idle",
        },
        "spawn_reason": "scenario_start",
    }

    with client.websocket_connect("/ws/unity") as websocket:
        websocket.send_json(incompatible_message)
        response = websocket.receive_json()

        assert response["type"] == "bridge.error"
        assert response["correlation_id"] == "agent-spawn-from-unity"
        assert response["payload"]["details"]["message_type"] == "agent.spawn"


def test_oversize_message_is_rejected_and_connection_closes() -> None:
    app = create_app(_config_with_max_payload(16))
    client = TestClient(app)

    with client.websocket_connect("/ws/unity") as websocket:
        websocket.send_text("x" * 32)
        response = websocket.receive_json()

        assert response["type"] == "bridge.error"
        assert response["payload"]["details"]["max_payload_bytes"] == 16
        with pytest.raises(WebSocketDisconnect):
            websocket.receive_text()


def test_connected_state_is_visible_while_socket_is_open() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    with client.websocket_connect("/ws/unity") as websocket:
        websocket.send_json(_unity_ready("visible-session"))
        websocket.receive_json()

        health = client.get("/health").json()
        assert isinstance(health, Mapping)
        assert health["unity"]["connected"] is True
        assert health["unity"]["ready"] is True
        assert health["unity"]["session_id"] == "visible-session"
        assert health["unity"]["observer_selected_agent_id"] is None
