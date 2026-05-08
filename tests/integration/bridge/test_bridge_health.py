from fastapi.testclient import TestClient

from korean_social_simulator.bridge.server import create_app
from korean_social_simulator.config.loader import load_bridge_config


def test_bridge_health_returns_status() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    response = client.get("/health")

    assert response.status_code == 200
    payload = response.json()
    assert payload["status"] == "ok"
    assert payload["server"]["host"] == "127.0.0.1"
    assert payload["unity"]["connected"] is False
    p = payload["physics"]
    assert p["backend"] == "fallback"
    assert p["available"] is True


def test_schema_version_returns_supported_types() -> None:
    app = create_app(load_bridge_config("configs/bridge.example.yaml"))
    client = TestClient(app)

    response = client.get("/schema/version")

    assert response.status_code == 200
    payload = response.json()
    assert payload["schema_version"] == "1.0.0"
    assert "agent.spawn" in payload["supported_message_types"]
    assert "physics.result" in payload["supported_message_types"]
