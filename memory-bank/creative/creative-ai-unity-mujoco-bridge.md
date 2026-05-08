# Creative Phase: Unity Bridge with Deterministic Physics

> **Note:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend.

## Problem 1: Package Layout

### Constraints

- Argus is packaged under `src/korean_social_simulator`.
- The source documentation describes standalone paths such as `src/bridge` and `src/schemas`.
- Existing CLI and dry-run behavior must not be broken.

### Options

1. Add standalone top-level packages: `src/bridge`, `src/schemas`.
2. Add Argus-native packages under `src/korean_social_simulator`.
3. Create a new Python distribution inside the repo for the bridge.

### Pros and Cons

Option 1 is closest to the zip docs, but it splits the source tree and complicates packaging.

Option 2 fits the current project, keeps imports consistent, and lets bridge code reuse existing models, errors, config, and storage patterns.

Option 3 gives strong separation, but it adds packaging and dependency complexity before the MVP is stable.

### Recommended Decision

Use Argus-native packages:

- `src/korean_social_simulator/bridge/`
- `src/korean_social_simulator/bridge_schema/`

Only add `src/korean_social_simulator/replay/` if replay behavior cannot stay cleanly inside `bridge/replay_store.py` or existing `storage/` patterns.

### Implementation Notes

- Convert imported spec path references to Argus-native paths.
- Keep public CLI under `kssim bridge ...`.
- Keep optional bridge dependencies behind extras in `pyproject.toml`.

## Problem 2: Bridge Schema Ownership

### Constraints

- Python and Unity need matching message contracts.
- Pydantic v2 is already the schema boundary in Argus.
- Unity DTOs cannot import Python models.
- Schema drift is a high-risk failure mode.

### Options

1. Define schemas only in Python and manually mirror DTOs in Unity.
2. Define JSON Schema as canonical and generate Python/Unity types.
3. Define Python Pydantic models first, export JSON Schema, and keep Unity DTOs manually tested against exported examples.

### Pros and Cons

Option 1 is fastest but risks drift.

Option 2 is most rigorous but adds code generation complexity and tooling decisions before core behavior exists.

Option 3 fits Argus patterns, gives machine-readable contract output, and keeps Unity implementation simple for MVP.

### Recommended Decision

Use Python Pydantic models as the initial canonical schema, export JSON Schema/examples as build artifacts, and test Unity DTOs against the same fixture messages.

### Implementation Notes

- Add `bridge_schema` Pydantic models.
- Add golden bridge JSONL fixtures.
- Add a future task to generate `docs/bridge-schema/*.json` after core models stabilize.
- Unity EditMode tests must parse Python-produced sample envelopes.

## Problem 3: Argus Event Adapter Strategy

### Constraints

- Existing `SimulationEvent` types are `observation`, `agent_action`, `gm_decision`, `metric_hook`, `safety_block`, and `system`.
- Dry-run observations currently do not contain rich movement/dialogue/conflict fields.
- The bridge must not change source event semantics.

### Options

1. Make the adapter infer rich Unity events from sparse dry-run fields.
2. Require explicit bridge-ready payload fields and emit adapter errors for unsupported events.
3. Add a new simulation event type for every Unity action.

### Pros and Cons

Option 1 gives fast visuals but risks inventing meaning.

Option 2 preserves semantic safety and makes gaps explicit.

Option 3 creates a larger change to Argus core models and may overfit Unity.

### Recommended Decision

Use explicit adapter mapping with conservative fallback:

- Map known current events to `simulation.event` or `agent.spawn` only when fields are traceable.
- Use synthetic bridge fixtures for movement/dialogue/conflict tests until Argus emits richer payloads.
- Emit structured `adapter.error` for unsupported or insufficient events.

### Implementation Notes

- Task 0.2 must inspect real generated `events.jsonl`.
- Keep `source_event_type`, `source_turn`, and source event references in bridge payloads.
- Do not silently invent dialogue, movement, emotion, or conflict intensity from sparse fields.

## Problem 4: Replay Storage Boundary

### Constraints

- Argus already writes simulation `events.jsonl`.
- Bridge replay logs must store validated `BridgeEnvelope` lines.
- Replays need deterministic order and corruption reporting.

### Options

1. Extend `RunStore` to write bridge replay logs.
2. Create `bridge/replay_store.py` using the same style as `RunStore`.
3. Store bridge replay logs only in Unity.

### Pros and Cons

Option 1 centralizes artifact logic but risks complicating existing run storage.

Option 2 keeps concerns separate while reusing local patterns.

Option 3 prevents Python-side golden testing and weakens reproducibility.

### Recommended Decision

Create `src/korean_social_simulator/bridge/replay_store.py` and reuse `RunStore` conventions.

### Implementation Notes

- Store bridge replay JSONL separately from Argus source `events.jsonl`.
- Link paths from run metadata only after bridge export/serve tasks exist.
- Fail on corrupted lines with exact line number.

## Problem 5: WebSocket Server Stack

### Constraints

- Existing Argus default install is lightweight.
- The docs recommend FastAPI or equivalent ASGI.
- WebSocket fake-client tests are required.

### Options

1. Add FastAPI/Uvicorn under an optional `bridge` extra.
2. Use the `websockets` package directly.
3. Build on Python stdlib only.

### Pros and Cons

Option 1 gives health endpoints, WebSocket routing, and test-client ergonomics.

Option 2 is smaller but requires separate HTTP endpoint handling.

Option 3 is impractical for WebSocket support.

### Recommended Decision

Use FastAPI plus Uvicorn in an optional `bridge` extra.

### Implementation Notes

- No bridge dependency should be required for `uv run pytest` unless bridge tests are selected.
- Health and schema endpoints come before runtime streaming.
- Bind to `127.0.0.1` by default and require explicit allow flag for remote clients.

## Problem 6: Physics Integration Boundary

> **Note:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend.

### Constraints

- Physical events must stay abstract and non-graphic.
- Unity should not depend on physics internals.
- Tests must pass without optional physics dependencies.

### Recommended Decision

Implement deterministic fallback as the sole physics backend.

### Implementation Notes

- Define `PhysicalEventBackend` with `FallbackPhysicsBackend` as the only implementation.
- Coordinator always returns validated `PhysicsResult` or structured error.

## Problem 7: Unity Runtime Architecture

### Constraints

- Unity networking must not block the main thread.
- Message parsing, connection lifecycle, scene application, avatar management, animation, and UI should be independently testable.

### Options

1. One monolithic Unity component handles WebSocket, parsing, scene, UI, and avatars.
2. Split into Bridge Client, Scene Orchestrator, Avatar Manager, Animation Mapper, UI Managers, and Observer Controls.
3. Use a third-party game framework.

### Pros and Cons

Option 1 is fast initially but hard to test and maintain.

Option 2 matches the source docs and supports EditMode/PlayMode test layers.

Option 3 adds dependency and learning overhead without solving the bridge-specific contract.

### Recommended Decision

Use the split Unity architecture from the docs:

- `UnityBridgeClient`
- `SimulationSceneOrchestrator`
- `RobotAvatarManager`
- `AnimationStateMapper`
- `DialogueBubbleManager`
- `EmotionIndicatorManager`
- `GroupIndicatorManager`
- `ConflictVisualizationManager`
- `ObserverControls`

### Implementation Notes

- Network callbacks enqueue parsed envelopes.
- Scene application happens in `Update()`.
- Every handled envelope produces `unity.ack`, `unity.warning`, or `unity.error`.
- Unknown message types do not crash the scene.

## Problem 8: Robot Asset Policy

### Constraints

- The selected robot must be cute/friendly, biped or humanoid, rigged, and animation-ready.
- Asset licensing may prevent repository redistribution.
- Runtime asset downloads are forbidden.

### Options

1. Commit a downloaded third-party robot asset immediately.
2. Commit only import instructions and attribution template until license is verified.
3. Build a custom placeholder robot from primitives and skip third-party assets.

### Pros and Cons

Option 1 risks license violation.

Option 2 is legally safest and matches the docs.

Option 3 is useful as fallback but does not satisfy the final cute rigged robot goal by itself.

### Recommended Decision

Commit import instructions, attribution template, project-owned wrappers, and a simple fallback placeholder. Do not commit raw third-party robot assets unless redistribution is explicitly allowed.

### Implementation Notes

- Primary candidate remains the cute rigged Sketchfab robot after license/import validation.
- Robot Kyle URP remains a reliability fallback if legally usable.
- Fallback prefab can be primitive/project-owned for tests.

## Problem 9: Verification Gate

### Constraints

- Python, Unity, replay, and manual asset review all have distinct failure modes.
- Unity may not be installed on every machine.
- Completion must not rely on unrun tests.

### Options

1. Require full Python and Unity verification before any bridge code can merge.
2. Phase verification by task type and document unavailable Unity blockers.
3. Rely on Python tests until the end.

### Pros and Cons

Option 1 is ideal but may block early Python work when Unity is unavailable.

Option 2 keeps progress possible while forcing exact evidence.

Option 3 misses Unity-specific failures.

### Recommended Decision

Use phased verification with exact evidence:

- Python schema/adapter/replay/fallback tests are mandatory from early waves.
- Fake Unity WebSocket integration tests are mandatory before Unity client work.
- Unity EditMode/PlayMode tests are mandatory once Unity project exists, unless the exact local blocker is recorded.
- Manual visual and asset review is mandatory before claiming visual MVP completion.

### Implementation Notes

- Write `memory-bank/progress.md` during build.
- Every completed task records commands, exit status, and observed output.
- Do not mark Unity tasks complete from Python-only checks.

