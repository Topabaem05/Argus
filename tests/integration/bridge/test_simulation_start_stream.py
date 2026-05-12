from __future__ import annotations

from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from korean_social_simulator.bridge.server import create_app
from korean_social_simulator.config.loader import load_bridge_config


def _unity_ready(session_id: str = "stream-session") -> dict[str, object]:
    return {
        "schema_version": "1.0.0",
        "message_id": "unity-ready-stream",
        "session_id": session_id,
        "sequence": 0,
        "sent_at_ms": 0,
        "type": "unity.ready",
        "payload": {},
    }


@pytest.mark.integration
def test_simulation_start_streams_spawn_and_physics_to_unity_ws() -> None:
    assert Path("examples/run_product_reaction.yaml").is_file()
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    with client.websocket_connect("/ws/unity") as ws:
        ws.send_json(_unity_ready())
        handshake = ws.receive_json()
        assert handshake["type"] == "bridge.ready"

        response = client.post(
            "/simulation/start",
            json={
                "config_path": "examples/run_product_reaction.yaml",
                "max_turns_override": 1,
                "persona_count_override": 3,
                "scenario_text": "A community center debates a kiosk policy.",
                "chat_text": "Show stance-driven public mini-bot behavior.",
                "background_id": "community_center",
            },
        )
        assert response.status_code == 200, response.text
        summary = response.json()
        assert summary["physics_emitted"] is True
        assert summary["streamed_envelopes"] >= 2
        n_out = int(summary["streamed_envelopes"])

        seen: list[str] = []
        for _ in range(n_out):
            msg = ws.receive_json()
            seen.append(msg["type"])

        assert "agent.spawn" in seen
        assert seen[0] == "environment.load"
        assert "agent.behavior" in seen
        assert "simulation.summary" in seen
        has_physics = "physics.result" in seen or "agent.move" in seen
        assert has_physics, f"Expected physics.result or agent.move in {seen}"


@pytest.mark.integration
def test_simulation_start_without_connected_unity_returns_503() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    response = client.post(
        "/simulation/start",
        json={"config_path": "examples/run_product_reaction.yaml", "max_turns_override": 1},
    )

    assert response.status_code == 503


@pytest.mark.integration
def test_simulation_start_rejects_unknown_background_before_streaming() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    response = client.post(
        "/simulation/start",
        json={
            "config_path": "examples/run_product_reaction.yaml",
            "background_id": "unknown_map",
        },
    )

    assert response.status_code == 422
    assert "Unsupported background_id" in response.text
