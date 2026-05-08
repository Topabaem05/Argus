# Coding Style

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

## Language and Framework Rules

### Python

- Use Python 3.11 or newer.
- Use `uv` for dependency management.
- Use `pydantic` or equivalent schema validation for external JSON contracts.
- Use `asyncio` for WebSocket and bridge server logic.
- Use `pytest` for tests.
- Use `ruff` for linting and formatting.
- Use `mypy` for type checking.
- Keep MuJoCo service code separated from bridge routing code.

### C# / Unity

- Use Unity 2022.3 LTS or the selected Unity 6 LTS version.
- Use C# with explicit public interfaces for bridge-facing systems.
- Use Unity Test Framework for EditMode and PlayMode tests.
- Keep WebSocket connection code separated from scene/animation code.
- Do not block the Unity main thread.
- Dispatch network messages into Unity scene changes through a queue processed on the main thread.

### Data Contracts

- All bridge messages must use versioned JSON.
- Unknown fields are rejected in strict test mode.
- Backward-compatible fields may be added only with a minor schema version bump.
- Breaking changes require a major schema version bump and ADR update.

## Naming Conventions

### Files

- Python modules: `snake_case.py`
- Python tests: `test_<module_name>.py`
- C# classes: `PascalCase.cs`
- Unity scenes: `PascalCase.unity`
- Unity prefabs: `PascalCase.prefab`
- Config files: `kebab-case.yaml`
- Specs: `kebab-case.md`
- Replay fixtures: `scenario-name.expected.jsonl`

### Python

- Classes: `PascalCase`
- Functions: `snake_case`
- Variables: `snake_case`
- Constants: `UPPER_SNAKE_CASE`
- Private helpers: prefix with `_`

### C# / Unity

- Classes: `PascalCase`
- Interfaces: `IName`
- Methods: `PascalCase`
- Private fields: `_camelCase`
- Serialized private fields: `_camelCase` with `[SerializeField]`
- Constants: `PascalCase` or `UPPER_SNAKE_CASE`, selected consistently per Unity style.

### Message Types

Use dot-separated lowercase names:

```txt
unity.ready
unity.ack
simulation.snapshot
simulation.event
simulation.error
agent.spawn
agent.move
agent.dialogue
agent.emotion
conflict.update
physics.request
physics.result
physics.error
replay.control
```

## Type Rules

### Python

- Every public function must have type annotations.
- Every dataclass or Pydantic model must define field types.
- Avoid `Any` unless handling raw input before validation.
- Use `Literal` for enumerated values.
- Use `TypedDict`, dataclasses, or Pydantic models for structured data.
- Keep raw input dictionaries at the boundary only.

### C# / Unity

- Avoid dynamic typing.
- Use serializable DTO classes for bridge messages.
- Use enums for message types where practical, but preserve unknown message handling.
- Avoid stringly typed Animator parameters by centralizing names or hashes.
- Public Unity components must declare required dependencies clearly.

## Error Handling

### Python

- Define custom errors for:
  - schema validation failure,
  - bridge connection failure,
  - replay write failure,
  - MuJoCo unavailable,
  - physics timeout,
  - unsupported event type.
- Convert internal exceptions to structured error messages before sending them over the bridge.
- Do not swallow exceptions silently.
- Include `correlation_id` in errors when available.
- Log tracebacks only in local debug mode.

### C# / Unity

- Wrap JSON parsing failures.
- A single malformed message must not crash the scene.
- Unity errors must be sent back as `unity.error` when connected.
- Scene update failures must surface in the overlay and logs.
- Missing asset references must fall back to a placeholder robot and show a warning.

## Logging

### Required Fields

- timestamp
- level
- source
- session_id
- message_id when available
- correlation_id when available
- event_type when available

### Log Levels

- `DEBUG`: schema internals, routing decisions, detailed physics inputs.
- `INFO`: server start, client connect, scenario start, scenario complete.
- `WARNING`: fallback behavior, skipped non-critical event, missing optional asset.
- `ERROR`: failed validation, failed physics request, failed Unity apply step.
- `FATAL`: bridge cannot continue.

### Do Not Log

- credentials,
- Unity account data,
- API keys,
- raw private user data,
- full sensitive agent profiles,
- hidden prompts,
- large binary asset content.

## Testing Style

### Unit Tests

Use unit tests for:

- schema validation,
- adapter mapping,
- message envelope construction,
- error serialization,
- replay log append/read,
- physics request generation,
- Unity DTO parsing,
- Unity animator state mapping.

### Integration Tests

Use integration tests for:

- bridge WebSocket lifecycle,
- Unity fake-client communication,
- MuJoCo service request/response,
- replay round-trip,
- reconnect behavior,
- invalid payload handling.

### Regression Tests

Use regression tests for:

- golden event schema outputs,
- replay determinism,
- preserved existing text simulation event semantics,
- known Unity asset mapping behavior.

### Smoke Tests

Use smoke tests for:

- server starts,
- Unity client connects,
- one robot spawns,
- one dialogue event appears,
- one movement event applies,
- one physics request produces a result or fallback.

### Golden Tests

Golden tests must store deterministic JSONL fixtures under:

```txt
tests/golden/
```

Golden files must be reviewed manually before update.

## Dependency Rules

- Add dependencies only when required by a task.
- Prefer official packages and widely used libraries.
- Pin or constrain versions for reproducibility.
- Avoid dependencies that require account credentials during automated tests.
- Avoid runtime asset downloads.
- Avoid Unity packages that require closed-source services for local testing.
- Document every new dependency in `README.md` and the relevant ADR.

## Formatting and Linting

### Python

```bash
uv run ruff check .
uv run ruff format --check .
uv run mypy src
uv run pytest
```

### Unity

- Use Unity's C# formatting settings or a committed `.editorconfig`.
- Run EditMode tests.
- Run PlayMode tests for scene behavior.
- Do not commit generated Library, Temp, or Build directories.

## Documentation Rules

- Update feature specs when behavior changes.
- Update ADRs when architecture decisions change.
- Update `docs/robot-asset-selection.md` when robot asset choice changes.
- Keep message schema examples synchronized with tests.
- All documentation files must be written in English.
- Avoid vague claims such as "smart", "good", or "robust" unless measurable.
- Record assumptions in completion reports.
