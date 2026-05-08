from __future__ import annotations

from pathlib import Path

from fastapi.testclient import TestClient

from korean_social_simulator.bridge.server import create_app
from korean_social_simulator.config.loader import load_bridge_config

_FIXTURE_PATH = Path("tests/golden/bridge/simple_dialogue.expected.jsonl").resolve()


def _client() -> TestClient:
    return TestClient(create_app(load_bridge_config("configs/bridge.example.yaml")))


def _load_fixture(client: TestClient) -> dict[str, object]:
    response = client.post("/replay/load", json={"path": str(_FIXTURE_PATH)})
    assert response.status_code == 200, response.text
    return response.json()


def test_replay_loads_fixture_and_reports_status() -> None:
    client = _client()

    status = _load_fixture(client)

    assert status["loaded"] is True
    assert status["loaded_path"] == str(_FIXTURE_PATH)
    assert status["paused"] is True
    assert status["event_count"] == 4
    assert status["next_index"] == 0
    assert status["completed"] is False


def test_pause_and_resume_toggle_replay_state() -> None:
    client = _client()
    _load_fixture(client)

    resumed = client.post("/replay/resume")
    assert resumed.status_code == 200
    assert resumed.json()["paused"] is False

    paused = client.post("/replay/pause")
    assert paused.status_code == 200
    assert paused.json()["paused"] is True


def test_pause_does_not_advance_replay_without_explicit_step() -> None:
    client = _client()
    _load_fixture(client)

    pause = client.post("/replay/pause")
    before = client.get("/replay/status")
    after = client.get("/replay/status")

    assert pause.status_code == 200
    assert before.json()["next_index"] == 0
    assert after.json()["next_index"] == 0


def test_step_emits_exactly_one_event_in_sequence_order() -> None:
    client = _client()
    _load_fixture(client)

    first = client.post("/replay/step")
    second = client.post("/replay/step")

    assert first.status_code == 200
    assert second.status_code == 200
    assert first.json()["event"]["sequence"] == 0
    assert first.json()["status"]["next_index"] == 1
    assert second.json()["event"]["sequence"] == 1
    assert second.json()["status"]["next_index"] == 2


def test_replay_controls_do_not_expose_hidden_simulation_state() -> None:
    client = _client()
    _load_fixture(client)

    response = client.post("/replay/step")

    assert response.status_code == 200
    payload = response.json()
    assert set(payload) == {"event", "status"}
    assert "profiles" not in payload
    assert "sample" not in payload
    assert "private" not in payload["event"]
    assert payload["status"]["event_count"] == 4


def test_step_without_loaded_replay_fails_clearly() -> None:
    client = _client()

    response = client.post("/replay/step")

    assert response.status_code == 400
    assert "No bridge replay is loaded" in response.json()["detail"]
