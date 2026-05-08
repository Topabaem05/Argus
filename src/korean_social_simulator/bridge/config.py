from __future__ import annotations

from pathlib import Path

from korean_social_simulator.config.loader import load_bridge_config
from korean_social_simulator.config.models import BridgeConfig


def load_bridge_runtime_config(path: str | Path) -> BridgeConfig:
    """Load the bridge runtime config through the shared config loader."""
    return load_bridge_config(path)
