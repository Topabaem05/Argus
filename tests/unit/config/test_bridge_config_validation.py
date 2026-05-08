from __future__ import annotations

from pathlib import Path
from tempfile import NamedTemporaryFile

import pytest
import yaml

from korean_social_simulator.config.loader import ConfigurationError, load_bridge_config


def _write_temp_yaml(data: dict[str, object]) -> Path:
    tmp = NamedTemporaryFile(mode="w", suffix=".yaml", delete=False)
    yaml.safe_dump(data, tmp)
    tmp.close()
    return Path(tmp.name)


_MINIMAL_BRIDGE_CONFIG: dict[str, object] = {
    "server": {
        "host": "127.0.0.1",
        "port": 8765,
        "allow_remote_clients": False,
        "max_payload_bytes": 1_048_576,
    },
    "schema": {
        "version": "1.0.0",
        "strict": True,
    },
    "unity": {
        "require_ack": True,
        "ack_timeout_ms": 2000,
        "reconnect_buffer_size": 1000,
        "out_of_order_policy": "skip",
    },
    "replay": {
        "enabled": True,
        "output_dir": "reports/replays",
        "write_policy": "pause_on_failure",
    },
    "world": {
        "bounds": {
            "min": [-20.0, 0.0, -20.0],
            "max": [20.0, 5.0, 20.0],
        }
    },
    "safety": {
        "non_graphic_mode": True,
        "max_physical_intensity": 0.7,
    },
    "log_level": "INFO",
}


def test_bridge_example_config_loads() -> None:
    config = load_bridge_config("configs/bridge.example.yaml")

    assert config.server.host == "127.0.0.1"
    assert config.server.port == 8765


def test_bridge_port_environment_override(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("BRIDGE_PORT", "9000")
    data = _MINIMAL_BRIDGE_CONFIG.copy()
    path = _write_temp_yaml(data)
    try:
        config = load_bridge_config(path)
        assert config.server.port == 9000
    finally:
        path.unlink(missing_ok=True)


def test_bridge_host_environment_override(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("BRIDGE_HOST", "localhost")
    data = _MINIMAL_BRIDGE_CONFIG.copy()
    path = _write_temp_yaml(data)
    try:
        config = load_bridge_config(path)
        assert config.server.host == "localhost"
    finally:
        path.unlink(missing_ok=True)


def test_non_localhost_requires_remote_client_flag() -> None:
    data = _MINIMAL_BRIDGE_CONFIG.copy()
    data["server"]["host"] = "0.0.0.0"
    data["server"]["allow_remote_clients"] = False
    path = _write_temp_yaml(data)
    try:
        with pytest.raises(ConfigurationError, match="allow_remote_clients"):
            load_bridge_config(path)
    finally:
        path.unlink(missing_ok=True)


def test_remote_binding_allowed_when_explicit() -> None:
    data = _MINIMAL_BRIDGE_CONFIG.copy()
    data["server"]["host"] = "0.0.0.0"
    data["server"]["allow_remote_clients"] = True
    path = _write_temp_yaml(data)
    try:
        config = load_bridge_config(path)
        assert config.server.host == "0.0.0.0"
    finally:
        path.unlink(missing_ok=True)


def test_unity_client_token_environment_override(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("UNITY_CLIENT_TOKEN", "local-token")
    data = _MINIMAL_BRIDGE_CONFIG.copy()
    path = _write_temp_yaml(data)
    try:
        config = load_bridge_config(path)
        assert config.unity.client_token == "local-token"
    finally:
        path.unlink(missing_ok=True)


def test_log_level_environment_override(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("LOG_LEVEL", "debug")
    data = _MINIMAL_BRIDGE_CONFIG.copy()
    path = _write_temp_yaml(data)
    try:
        config = load_bridge_config(path)
        assert config.log_level == "DEBUG"
    finally:
        path.unlink(missing_ok=True)


def test_invalid_world_bounds_fail_validation() -> None:
    data = _MINIMAL_BRIDGE_CONFIG.copy()
    data["world"]["bounds"]["min"] = [10.0, 0.0, 0.0]
    data["world"]["bounds"]["max"] = [1.0, 1.0, 1.0]
    path = _write_temp_yaml(data)
    try:
        with pytest.raises(ConfigurationError, match=r"world\.bounds\.min"):
            load_bridge_config(path)
    finally:
        path.unlink(missing_ok=True)
