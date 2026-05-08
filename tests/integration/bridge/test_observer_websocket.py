from __future__ import annotations

from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from korean_social_simulator.bridge.server import create_app
from korean_social_simulator.config.loader import load_bridge_config

_REPO_ROOT = Path(__file__).resolve().parents[3]


def _unity_ready(session_id: str = "observer-session") -> dict[str, object]:
    return {
        "schema_version": "1.0.0",
        "message_id": "unity-ready-observer",
        "session_id": session_id,
        "sequence": 0,
        "sent_at_ms": 0,
        "type": "unity.ready",
        "payload": {},
    }


def _observer_envelope(observer_type: str, session_id: str, suffix: str) -> dict[str, object]:
    return {
        "schema_version": "1.0.0",
        "message_id": f"observer-{observer_type}-{suffix}",
        "session_id": session_id,
        "sequence": 42,
        "sent_at_ms": 99,
        "type": observer_type,
        "payload": {},
    }


@pytest.mark.integration
def test_observer_resume_without_loaded_replay_returns_bridge_error() -> None:
    client = TestClient(create_app(load_bridge_config("configs/bridge.example.yaml")))

    with client.websocket_connect("/ws/unity") as websocket:
        websocket.send_json(_unity_ready("session-a"))
        websocket.receive_json()

        websocket.send_json(_observer_envelope("observer.resume", "session-a", "1"))
        error = websocket.receive_json()

        assert error["type"] == "bridge.error"
        detail = error["payload"]
        assert detail["severity"] == "error"
        assert "No bridge replay is loaded." in detail["message"]


@pytest.mark.integration
def test_observer_step_after_loaded_replay_returns_replay_status_with_event(
    tmp_path: Path,
) -> None:
    replay_source_lines = (
        (_REPO_ROOT / "tests/golden/bridge/simple_dialogue.expected.jsonl")
        .read_text(encoding="utf-8")
        .splitlines()
    )
    replay_path = tmp_path / "tiny.jsonl"
    replay_path.write_text(replay_source_lines[0] + "\n", encoding="utf-8")

    app_client = TestClient(create_app(load_bridge_config("configs/bridge.example.yaml")))
    response = app_client.post("/replay/load", json={"path": str(replay_path)})
    assert response.status_code == 200

    with app_client.websocket_connect("/ws/unity") as websocket:
        websocket.send_json(_unity_ready("replay-session"))
        websocket.receive_json()

        websocket.send_json(_observer_envelope("observer.step", "replay-session", "1"))
        replay_status = websocket.receive_json()

        assert replay_status["type"] == "replay.status"
        assert replay_status["payload"]["replay"]["paused"] is True
        assert replay_status["payload"]["replay"]["completed"] is True
        assert replay_status["payload"]["replay"]["next_index"] == 1
        stepped = replay_status["payload"]["stepped_event"]
        assert stepped is not None
        assert stepped["type"] == "simulation.event"


@pytest.mark.integration
def test_observer_select_agent_is_reflected_on_health_snapshot() -> None:
    app_client = TestClient(create_app(load_bridge_config("configs/bridge.example.yaml")))

    with app_client.websocket_connect("/ws/unity") as websocket:
        websocket.send_json(_unity_ready("observe-session-x"))
        websocket.receive_json()

        payload = {
            "schema_version": "1.0.0",
            "message_id": "observer-select-1",
            "session_id": "observe-session-x",
            "sequence": 99,
            "sent_at_ms": 12,
            "type": "observer.select_agent",
            "payload": {"agent_id": "agent-zzz"},
        }
        websocket.send_json(payload)

        health = app_client.get("/health").json()
        assert health["unity"]["observer_selected_agent_id"] == "agent-zzz"
