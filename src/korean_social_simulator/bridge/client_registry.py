from __future__ import annotations

from dataclasses import dataclass

from starlette.websockets import WebSocket

from korean_social_simulator.bridge_schema import BridgeEnvelope


@dataclass(frozen=True)
class UnityClientSnapshot:
    """Current Unity client connection state."""

    connected: bool
    ready: bool
    session_id: str | None
    last_disconnect_reason: str | None
    observer_selected_agent_id: str | None = None


class ClientRegistry:
    """In-memory registry for the local Unity bridge client."""

    def __init__(self) -> None:
        self._websocket: WebSocket | None = None
        self._connected = False
        self._ready = False
        self._session_id: str | None = None
        self._last_disconnect_reason: str | None = None
        self._next_sequence = 0
        self._observer_selected_agent_id: str | None = None
        self._observer_camera_payload: dict[str, object] | None = None

    async def connect(self, websocket: WebSocket) -> None:
        """Register an accepted WebSocket connection for outbound bridge messages."""
        self._websocket = websocket
        self._connected = True
        self._ready = False
        self._session_id = None
        self._last_disconnect_reason = None
        self._next_sequence = 0
        self._observer_selected_agent_id = None
        self._observer_camera_payload = None

    def mark_ready(self, session_id: str) -> None:
        self._ready = True
        self._session_id = session_id

    def disconnect(self, reason: str) -> None:
        self._websocket = None
        self._connected = False
        self._ready = False
        self._session_id = None
        self._last_disconnect_reason = reason
        self._observer_selected_agent_id = None
        self._observer_camera_payload = None

    def has_connected_client(self) -> bool:
        return self._connected

    def snapshot(self) -> UnityClientSnapshot:
        return UnityClientSnapshot(
            connected=self._connected,
            ready=self._ready,
            session_id=self._session_id,
            last_disconnect_reason=self._last_disconnect_reason,
            observer_selected_agent_id=self._observer_selected_agent_id,
        )

    def set_observer_selection(self, agent_id: str | None) -> None:
        """Last agent id selected by the Unity observer."""
        self._observer_selected_agent_id = agent_id

    def record_observer_camera(self, payload: dict[str, object]) -> None:
        """Store the latest unobtrusive camera telemetry from Unity."""
        self._observer_camera_payload = payload

    def observer_camera_payload(self) -> dict[str, object] | None:
        return (
            dict(self._observer_camera_payload)
            if self._observer_camera_payload is not None
            else None
        )

    def next_sequence(self) -> int:
        sequence = self._next_sequence
        self._next_sequence += 1
        return sequence

    async def send_envelope(self, envelope: BridgeEnvelope) -> bool:
        """Send a validated envelope to Unity; no-op when disconnected."""
        if not self._connected or self._websocket is None:
            return False
        await self._websocket.send_json(envelope.model_dump(mode="json"))
        return True
