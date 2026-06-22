# AGENTS.md — bridge/

FastAPI/WebSocket bridge between Argus dry-run simulation and Unity visualization. Optional extra (`uv sync --extra bridge`); not required for offline MVP.

## STRUCTURE
```
server.py             # create_app(): FastAPI factory, wires all routes + websocket
websocket_gateway.py  # register_unity_websocket(): Unity WS endpoint, handshake, ACK loop
client_registry.py    # tracks connected Unity/observer clients
physics_coordinator.py# PhysicsCoordinator: timeout+fallback wrapper over PhysicsBackend
physics.py            # PhysicsBackend protocol + FallbackPhysicsBackend impl
fallback_physics.py   # simulate_fallback(): no-external-service physics resolver
environment_catalog.py# Static environment definitions (schoolroom, etc.) -> EnvironmentLoadEvent
event_adapter.py      # SimulationEventAdapter: Argus events -> bridge envelopes
simulation_stream.py  # stream_dry_run_to_unity(): dry-run -> WS envelope stream
ack_tracker.py        # AckTracker: tracks Unity ACKs, deterministic resume points
replay_controller.py  # ReplayController: in-memory replay cursor + HTTP routes
replay_store.py       # ReplayStore: read/write bridge replay JSONL
agent_inspection.py   # AgentInspectionController + routes: inspect agent state
config.py             # load_bridge_runtime_config() -> BridgeConfig
__init__.py           # re-exports AckTracker, ReplayController, etc.
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Add REST route | `server.py` `create_app` or `register_*_routes` in feature module |
| Add WebSocket message type | `websocket_gateway.py` inbound/outbound handlers + `bridge_schema/envelope.py` `_PAYLOAD_MODELS` + `server.py` `_SUPPORTED_MESSAGE_TYPES` |
| Add environment/background | `environment_catalog.py` `_ENVIRONMENTS` dict |
| Change physics backend | `physics_coordinator.py` `build_physics_coordinator` + `physics.py` `PhysicsBackend` protocol |
| Replay playback control | `replay_controller.py` `ReplayController` (load/pause/step/resume) |
| ACK/delivery tracking | `ack_tracker.py` `AckTracker` (inject `ClockMs` for deterministic tests) |

## CONVENTIONS
- Route registration via `register_*_routes(app, ...)` functions called from `create_app`. Do not use global app state.
- Injections over globals: `AckTracker(clock_ms)`, `ClientRegistry()`, `ReplayController()` constructed in `create_app` and passed down.
- `_SUPPORTED_MESSAGE_TYPES` in `server.py` and `_PAYLOAD_MODELS` in `bridge_schema/envelope.py` must stay in sync. Both gate envelope validation.
- Physics always returns a result: coordinator catches backend errors and delegates to `simulate_fallback` with a warning. Never raises to the WS client.
- `extra="forbid"` on all request/response models (`ReplayLoadRequest`, etc.).
- `websocket_gateway.py` is the one place `Any` is permitted (untyped WS message dict); isolate it there.

## ANTI-PATTERNS
- Do not import `fastapi`/`uvicorn` at module top in `__init__` (keeps import cheap when bridge unused). Lazy import in `pipeline.bridge_serve_command`.
- Do not block the event loop: physics evaluation and replay step are synchronous; WS reads use `receive_json`/`send_json` in async context.
- Do not connect to non-localhost Unity hosts; `BridgeConfig.server.host` validated against `_LOCAL_BRIDGE_HOSTS`.
- Do not emit envelopes with `sequence` gaps; `AckTracker` relies on monotonic sequence for reconnect resume.
