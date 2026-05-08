# ADR 0002: Technology Stack

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

## Status

Proposed

## Context

The bridge needs to connect an existing Python-oriented text simulation to Unity and MuJoCo. The selected stack must be simple enough for fast implementation, testable in automation, and compatible with local-first development.

## Decision

Use this stack:

- Python 3.11+ for bridge, schemas, replay, and MuJoCo service.
- Official MuJoCo Python package (`mujoco` from PyPI, not `mujoco-py`) for optional physical-event evaluation.
- `uv` for Python dependency management.
- FastAPI or an equivalent ASGI framework for local WebSocket/health endpoints.
- Argus optional `bridge` extra for FastAPI, Uvicorn, and HTTPX test-client support.
- Pydantic or equivalent for schema validation.
- Official MuJoCo Python package for physical-event evaluation.
- Unity 2022.3 LTS or selected Unity 6 LTS for the 3D client.
- Unity URP for broad platform compatibility and stylized rendering.
- NativeWebSocket or equivalent Unity WebSocket package for local communication.
- Unity Test Framework for EditMode and PlayMode tests.
- JSONL for replay logs.

## Consequences

What becomes easier:

- Python can integrate with the existing simulation and MuJoCo.
- WebSocket communication is easy to inspect and debug.
- JSON messages are readable and golden-test friendly.
- Unity can remain focused on visual state application.
- URP supports stylized visuals across many platforms.

What becomes harder:

- JSON schemas need strict versioning.
- Unity package dependencies must be managed separately.
- Python and Unity test runners must both be configured.
- Binary Unity assets cannot be reviewed like plain code.

Tradeoffs accepted:

- JSON is less efficient than binary protocols but easier to debug for MVP.
- FastAPI/ASGI adds dependency weight but simplifies local server development.
- Unity URP may not provide the highest visual fidelity, but it is suitable for stylized robot avatars.

## Alternatives Considered

- gRPC.
  - Strong typed contracts but heavier Unity setup and less convenient for quick live visualization.
- UDP.
  - Lower latency but harder reliability, ordering, and debugging.
- HTTP polling.
  - Simpler but poor for live events and acknowledgements.
- Godot.
  - Open-source and lightweight, but Unity has stronger asset ecosystem and existing robot/avatar workflows.
- Unreal.
  - High visual fidelity but heavier for this MVP.
- MuJoCo Unity plug-in only.
  - Useful later, but external Python service is simpler for deterministic physical-event MVP.

## Validation

This stack is working when:

- Python bridge starts with one command.
- Unity connects to the local bridge.
- Schema validation rejects malformed messages.
- Unity tests parse and apply sample events.
- MuJoCo physical-event tests run without Unity.
- All dependencies are documented and reproducible.
