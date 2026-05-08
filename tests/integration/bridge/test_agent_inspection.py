from __future__ import annotations

from pathlib import Path

from fastapi.testclient import TestClient

from korean_social_simulator.bridge.server import create_app
from korean_social_simulator.config.loader import load_bridge_config
from korean_social_simulator.models import AgentProfile


def _client() -> TestClient:
    return TestClient(create_app(load_bridge_config("configs/bridge.example.yaml")))


def _write_profiles(path: Path) -> None:
    profiles = [
        AgentProfile(
            agent_id="agent-001",
            persona_uuid="persona-001",
            display_name="Agent One",
            language="ko",
            background="Public background only.",
            memory_seeds=["private memory seed"],
            goals=["Respond from a public synthetic perspective."],
            behavior_rules=["internal behavior rule"],
            safety_notes=["Synthetic persona."],
        )
    ]
    path.write_text(
        "[" + ",".join(profile.model_dump_json() for profile in profiles) + "]\n",
        encoding="utf-8",
    )


def test_agent_profiles_load_and_report_public_status(tmp_path: Path) -> None:
    profiles_path = tmp_path / "profiles.json"
    _write_profiles(profiles_path)
    client = _client()

    response = client.post("/agents/load", json={"path": str(profiles_path)})

    assert response.status_code == 200
    payload = response.json()
    assert payload["loaded"] is True
    assert payload["loaded_path"] == str(profiles_path)
    assert payload["agent_count"] == 1


def test_known_agent_returns_only_public_allowlisted_fields(tmp_path: Path) -> None:
    profiles_path = tmp_path / "profiles.json"
    _write_profiles(profiles_path)
    client = _client()
    client.post("/agents/load", json={"path": str(profiles_path)})

    response = client.get("/agents/agent-001/public")

    assert response.status_code == 200
    payload = response.json()
    assert payload == {
        "agent_id": "agent-001",
        "display_name": "Agent One",
        "language": "ko",
        "background": "Public background only.",
        "goals": ["Respond from a public synthetic perspective."],
        "safety_notes": ["Synthetic persona."],
    }


def test_public_agent_response_excludes_sensitive_keys(tmp_path: Path) -> None:
    profiles_path = tmp_path / "profiles.json"
    _write_profiles(profiles_path)
    client = _client()
    client.post("/agents/load", json={"path": str(profiles_path)})

    response = client.get("/agents/agent-001/public")

    assert response.status_code == 200
    payload = response.json()
    forbidden_keys = {
        "persona_uuid",
        "memory_seeds",
        "behavior_rules",
        "metadata",
        "prompt",
        "chain",
        "credentials",
    }
    assert forbidden_keys.isdisjoint(payload)


def test_unknown_agent_returns_structured_404(tmp_path: Path) -> None:
    profiles_path = tmp_path / "profiles.json"
    _write_profiles(profiles_path)
    client = _client()
    client.post("/agents/load", json={"path": str(profiles_path)})

    response = client.get("/agents/missing-agent/public")

    assert response.status_code == 404
    error = response.json()["detail"]
    assert error["source"] == "bridge"
    assert error["severity"] == "error"
    assert error["message"] == "Public agent not found."
    assert error["details"]["agent_id"] == "missing-agent"
