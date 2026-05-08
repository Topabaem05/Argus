# Unity Bridge Implementation Plan

> **Note:** MuJoCo integration has been removed. The deterministic local fallback is now the sole physics backend.

## Scope and Goal

Create an executable implementation plan for integrating the Unity Bridge documentation package into the current Argus repository.

Source package reviewed:

- `/Users/guribbong/Downloads/AI_Unity_MuJoCo_Bridge_Documentation.zip`
- Extracted review copy: `/tmp/argus_ai_unity_mujoco_bridge_docs`

Documents reviewed:

- `AGENTS.md`
- `README.md`
- `DOCUMENTATION_SELF_REVIEW.md`
- `AI_UNITY_MUJOCO_BRIDGE_DOCUMENTATION.md`
- `docs/architecture.md`
- `docs/coding-style.md`
- `docs/robot-asset-selection.md`
- `docs/adr/0001-project-architecture.md`
- `docs/adr/0002-technology-stack.md`
- `docs/adr/0003-testing-strategy.md`
- `specs/ai-unity-mujoco-bridge/brief.md`
- `specs/ai-unity-mujoco-bridge/requirements.md`
- `specs/ai-unity-mujoco-bridge/design.md`
- `specs/ai-unity-mujoco-bridge/tasks.md`
- `specs/ai-unity-mujoco-bridge/verification.md`
- `specs/ai-unity-mujoco-bridge/risks.md`
- `specs/ai-unity-mujoco-bridge/changelog.md`

Goal:

Turn Argus into the authoritative text-simulation provider for a local Unity visualization bridge. Argus remains responsible for cognition, scenario execution, event logs, safety, metrics, and reports. A new bridge layer normalizes Argus `SimulationEvent` records into versioned Unity-facing messages, streams them over local WebSocket, stores deterministic replay logs, and routes selected abstract physical events through deterministic fallback physics.

## Current Repository Fit

Argus already has:

- `src/korean_social_simulator/models.py`: strict Pydantic v2 models for personas, plans, and `SimulationEvent`.
- `src/korean_social_simulator/simulation/dry_run.py`: deterministic offline event generation.
- `src/korean_social_simulator/storage/run_store.py`: JSONL event storage and run metadata.
- `src/korean_social_simulator/pipeline.py`: run orchestration and artifact writing.
- `src/korean_social_simulator/cli.py`: Typer CLI commands.
- `pyproject.toml`: Python 3.11+, Pydantic, Typer, YAML, and optional extras.

Important mismatch:

- The zip package describes future standalone modules such as `src/bridge`, `src/schemas`, and `src/replay`.
- Argus is packaged as `src/korean_social_simulator`. Implementation should use Argus-native modules rather than introducing top-level import roots:
  - `src/korean_social_simulator/bridge/`
  - `src/korean_social_simulator/bridge_schema/`
  - `src/korean_social_simulator/replay/` only if it cannot reuse `storage/`.

## Non-Negotiable Constraints

- Do not rewrite Argus simulation, sampling, safety, evaluation, or reporting.
- Do not alter existing `SimulationEvent` semantics to satisfy Unity.
- Do not make Unity, FastAPI, WebSocket packages, asset downloads, or live services mandatory for the offline Argus MVP.
- Keep bridge networking local by default.
- Do not log secrets, hidden prompts, private agent profiles, or sensitive scenario metadata.
- Do not download or commit third-party robot assets unless redistribution is explicitly legal.
- Keep physical events abstract and non-graphic.
- Every code task must add or update tests.
- Every implementation completion must report exact commands and results.
- Use OpenCode delegation only for bounded code-writing or review drafts. Codex remains final reviewer and test runner.

## Creative Phase Required

Creative required: yes.

Reasons:

- Architecture: map a standalone bridge spec into the existing Argus package without breaking current CLI and artifact contracts.
- Data modeling: define canonical bridge envelopes while preserving Argus `SimulationEvent` as source data.
- Workflow design: bridge server lifecycle, replay mode, observer controls, and deterministic fallback must be sequenced safely.
- UI/UX: Unity robot scene, dialogue bubbles, conflict indicators, observer controls, and asset fallback behavior need visual decisions.

Next phase after this plan: `creative`, then `build`.

Creative decision artifact:

- `memory-bank/creative/creative-ai-unity-mujoco-bridge.md`
- `memory-bank/ouroboros-seed-ai-unity-mujoco-bridge.yaml`

Chosen implementation defaults:

- Use Argus-native packages under `src/korean_social_simulator/` instead of standalone `src/bridge` roots.
- Treat Python Pydantic models as the initial canonical bridge schema, then test Unity DTOs against Python-produced fixtures.
- Keep the event adapter conservative: preserve source event references and emit `adapter.error` instead of inventing unsupported Unity semantics.
- Store bridge replay logs through a dedicated `bridge/replay_store.py` that follows `RunStore` conventions.
- Put FastAPI/Uvicorn behind an optional `bridge` extra.
- Implement deterministic fallback physics (MuJoCo removed).
- Use a split Unity architecture with Bridge Client, Scene Orchestrator, Avatar Manager, Animation Mapper, UI Managers, and Observer Controls.
- Commit robot import instructions and attribution templates before committing any raw third-party assets.
- Use phased verification with exact command evidence, and do not mark Unity tasks complete from Python-only checks.

## Ouroboros Seed

```yaml
seed:
  title: "Argus Unity Bridge with Deterministic Physics"
  mode: "brownfield"
  goal: "Implement a local bridge that streams Argus simulation events to Unity robot visualization with deterministic physical-event evaluation while preserving Argus as the authoritative text simulation."
  source_documents:
    - "/Users/guribbong/Downloads/AI_Unity_MuJoCo_Bridge_Documentation.zip"
  repository: "/Users/guribbong/code/Argus"
  constraints:
    - "Argus text simulation remains source of truth."
    - "Unity cannot mutate cognition, memories, beliefs, relationships, or hidden state."
    - "Network communication binds localhost by default."
    - "Deterministic fallback physics is the sole physics backend (MuJoCo removed)."
    - "No runtime asset downloads."
    - "No third-party raw assets committed unless license permits redistribution."
    - "Dry-run and existing tests remain network-free."
  acceptance_criteria:
    - "Bridge schema validates required envelope fields, rejects unknown major versions, invalid enums, unknown fields in strict mode, and oversized payloads."
    - "Bridge server exposes health and schema endpoints and local WebSocket `/ws/unity`."
    - "Fake Unity client can connect, send `unity.ready`, receive `bridge.ready`, receive ordered events, send ACKs, and disconnect cleanly."
    - "Argus dry-run events can be adapted to canonical bridge messages without changing original events."
    - "Replay JSONL append, load, corruption reporting, and deterministic ordering are covered by tests."
    - "Deterministic fallback physics produces consistent physical results."
    - "Unity project can parse bridge envelopes, enqueue messages on main thread, spawn robot placeholders, render dialogue/movement/emotion/conflict, and send ACK/error messages."
    - "A smoke scenario with at least 20 agents can stream spawn, dialogue, movement, emotion, conflict, and one physical fallback event."
    - "Python, Unity, and manual asset verification results are recorded exactly."
  non_goals:
    - "No Sims clone."
    - "No full-body locomotion for every avatar."
    - "No graphic harm or injury."
    - "No online multiplayer."
    - "No direct Unity-to-LLM calls."
    - "No custom robot modeling from scratch for MVP."
```

> **Note:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend.

## OpenCode Delegation Protocol

Use this prompt header for each code-writing delegation:

```text
[@opencode-codex-bridge](plugin://opencode-codex-bridge@local-plugins)
Use plan mode unless build mode is explicitly requested.
Review only the provided scope.
Do not inspect unrelated files.
Do not edit files directly unless explicitly asked.
Return exactly:
1. concise diagnosis
2. minimal patch plan
3. unified diff if appropriate
4. risks and uncertainty
5. tests to run
Keep the answer short.
```

Delegation rule:

- OpenCode may draft plans, diffs, tests, or reviews for one bounded task.
- Codex must independently inspect the repository, apply any accepted patch with normal editing tools, run tests, and report exact evidence.
- Do not send `.env`, credentials, private data, full raw logs, or account-bound asset data to OpenCode.

## Parallel Task Graph

### Wave 0: Planning, Import Strategy, and Baseline Protection

Dependencies: none.

- [x] Task 0.1: Preserve source documentation package context
  - Affected files:
    - `specs/ai-unity-mujoco-bridge/brief.md`
    - `specs/ai-unity-mujoco-bridge/requirements.md`
    - `specs/ai-unity-mujoco-bridge/design.md`
    - `specs/ai-unity-mujoco-bridge/tasks.md`
    - `specs/ai-unity-mujoco-bridge/verification.md`
    - `specs/ai-unity-mujoco-bridge/risks.md`
    - `specs/ai-unity-mujoco-bridge/changelog.md`
  - Work:
    - Import or adapt the spec pack into Argus paths.
    - Rewrite source paths from standalone `src/bridge` style to Argus-native modules.
    - Mark imported ADRs as proposed until accepted for Argus.
  - QA:
    - [x] `rg -n "src/(bridge|schemas|mujoco_service|replay)|python -m bridge\\.server|tests/golden/(simple_dialogue|conflict_push)|src/korean_social_simulator/bridge/event_store.py" specs/ai-unity-mujoco-bridge`
    - [x] Pass when remaining references are either intentionally quoted from source docs or converted to Argus-native module paths.

- [x] Task 0.2: Capture current Argus event and artifact contracts
  - Affected files:
    - `docs/existing-text-simulation-analysis.md`
  - Work:
    - Document current `SimulationEvent` fields, event types, payload shapes, run artifacts, and CLI flow.
    - Include examples from dry-run output and at least one existing scenario.
    - Identify which current events can become `agent.spawn`, `agent.dialogue`, `simulation.event`, or adapter errors.
  - QA:
    - [x] `uv run kssim run --config /tmp/argus_bridge_contract_config.XXXXXX.yaml --dry-run`
    - [x] Inspect generated `events.jsonl`, `plan.json`, and `profiles.json`.
    - [x] Pass when the analysis maps real Argus output, not assumed bridge examples.

- [x] Task 0.3: Baseline regression check
  - Affected files:
    - none expected
  - Work:
    - Run current validation before bridge implementation.
    - Record failures caused by pre-existing dirty worktree state separately from bridge work.
  - QA:
    - [x] `uv run pytest`
    - [x] `uv run ruff check .`
    - [x] `uv run mypy src`
    - [x] `uv run ruff format --check .`
    - [x] Pass when baseline is green or every failure is documented before code changes.

### Wave 1: Argus-Native Bridge Schema and Config

Dependencies: Wave 0.

- [x] Task 1.1: Define bridge envelope and payload schemas
  - Affected files:
    - `src/korean_social_simulator/bridge_schema/__init__.py`
    - `src/korean_social_simulator/bridge_schema/envelope.py`
    - `src/korean_social_simulator/bridge_schema/events.py`
    - `src/korean_social_simulator/bridge_schema/errors.py`
    - `tests/unit/bridge_schema/test_envelope.py`
    - `tests/unit/bridge_schema/test_events.py`
  - Work:
    - Implement Pydantic v2 strict models for `BridgeEnvelope`, `Vec3`, `EmotionState`, `AgentState`, `AgentSpawnEvent`, `AgentMoveEvent`, `AgentDialogueEvent`, `ConflictUpdateEvent`, `UnityAck`, and `StructuredError`.
    - Enforce schema semver major compatibility with `1.x.x`.
    - Reject unknown fields in strict test mode.
  - QA:
    - [x] Unit tests for valid envelope, missing `message_id`, unsupported major version, invalid enum, invalid position, and field-specific error payload.

- [x] Task 1.2: Add physical event schemas
  - Affected files:
    - `src/korean_social_simulator/bridge_schema/physics.py`
    - `tests/unit/bridge_schema/test_physics.py`
  - Work:
    - Implement `PhysicsRequest`, `PhysicsConstraints`, and `PhysicsResult`.
    - Keep outcomes abstract: `no_contact`, `blocked`, `stumble`, `fall`, `recover`, `separate`, `unknown`.
  - QA:
    - [x] Valid request passes.
    - [x] Invalid intensity fails.
    - [x] Unknown outcome is rejected or normalized exactly as documented.

- [x] Task 1.3: Add bridge config models and example config
  - Affected files:
    - `src/korean_social_simulator/config/models.py`
    - `src/korean_social_simulator/config/loader.py`
    - `configs/bridge.example.yaml`
    - `configs/robot-assets.example.yaml`
    - `tests/unit/config/test_bridge_config_validation.py`
  - Work:
    - Add bridge-specific config without disturbing existing runtime config.
    - Support `BRIDGE_HOST`, `BRIDGE_PORT`, `MUJOCO_ENABLED`, `MUJOCO_MODEL_PATH`, `UNITY_CLIENT_TOKEN`, and `LOG_LEVEL`.
    - Require `allow_remote_clients=true` before non-localhost binding.
    - Require `model_path` when MuJoCo is enabled.
  - QA:
    - [x] Default binds to `127.0.0.1`.
    - [x] Env override changes port.
    - [x] Invalid MuJoCo config fails clearly.
    - [x] Remote bind without explicit allow flag fails.

### Wave 2: Event Adapter and Replay Foundation

Dependencies: Wave 1.

- [x] Task 2.1: Implement Argus-to-bridge event adapter
  - Affected files:
    - `src/korean_social_simulator/bridge/event_adapter.py`
    - `tests/unit/bridge/test_event_adapter.py`
    - `tests/golden/bridge/simple_dialogue.input.jsonl`
    - `tests/golden/bridge/simple_dialogue.expected.jsonl`
  - Work:
    - Convert current `SimulationEvent` records into canonical bridge envelopes.
    - Preserve `run_id`, original `turn`, actor ID, timestamp, original event type, and a traceable source event ID or source reference.
    - Emit structured `adapter.error` for unsupported or insufficient events.
    - Allocate monotonic per-session sequences deterministically.
  - QA:
    - [x] Current dry-run `observation` maps to a documented Unity-visible event or documented adapter error.
    - [x] Movement, dialogue, conflict, and physical examples are covered by synthetic fixtures.
    - [x] Same input produces byte-stable expected JSONL after timestamp normalization.

- [x] Task 2.2: Implement replay event store
  - Affected files:
    - `src/korean_social_simulator/bridge/replay_store.py`
    - `tests/unit/bridge/test_replay_store.py`
  - Work:
    - Reuse existing storage patterns from `RunStore` where possible.
    - Append one validated `BridgeEnvelope` per JSONL line.
    - Load in sequence order and fail on corruption with line number.
  - QA:
    - [x] Append and read round trip.
    - [x] Corrupted line reports exact line.
    - [x] Replay output order is deterministic.

- [x] Task 2.3: Add deterministic fallback physics
  - Affected files:
    - `src/korean_social_simulator/mujoco_service/fallback_physics.py`
    - `tests/unit/mujoco_service/test_fallback_physics.py`
  - Work:
    - Produce deterministic `PhysicsResult` without importing MuJoCo.
    - Mark result status as `fallback`.
    - Enforce non-graphic safety constraints and maximum intensity.
  - QA:
    - [x] Same request and fixed seed produce identical result.
    - [x] Disabled MuJoCo path returns fallback result.
    - [x] Over-intensity request is clamped or rejected according to config and test expectation.

### Wave 3: Local Python Bridge Server

Dependencies: Waves 1 and 2.

- [x] Task 3.1: Add optional bridge server dependencies
  - Affected files:
    - `pyproject.toml`
    - `README.md`
    - `docs/adr/0002-technology-stack.md`
  - Work:
    - Add a `bridge` optional extra for ASGI/WebSocket dependencies.
    - Keep default install lightweight and offline.
  - QA:
    - [x] `uv lock`
    - [x] `uv sync --extra bridge`
    - [x] `uv run python -c "import fastapi, httpx, uvicorn; print('bridge imports ok')"`
    - [x] Existing dev tooling restored with `uv sync --extra dev --extra bridge`.

- [x] Task 3.2: Implement health and schema endpoints
  - Affected files:
    - `src/korean_social_simulator/bridge/server.py`
    - `src/korean_social_simulator/bridge/config.py`
    - `tests/integration/bridge/test_bridge_health.py`
  - Work:
    - Expose local server app factory.
    - Add `GET /health`.
    - Add `GET /schema/version`.
    - Report MuJoCo availability without requiring MuJoCo when disabled.
  - QA:
    - [x] Server starts through FastAPI app factory.
    - [x] Health returns OK.
    - [x] Schema endpoint returns version and supported message types.

- [x] Task 3.3: Implement `/ws/unity` lifecycle
  - Affected files:
    - `src/korean_social_simulator/bridge/websocket_gateway.py`
    - `src/korean_social_simulator/bridge/client_registry.py`
    - `tests/integration/bridge/test_unity_websocket_lifecycle.py`
  - Work:
    - Accept `unity.ready`.
    - Send `bridge.ready`.
    - Validate Unity messages.
    - Detect disconnect.
    - Reject oversized or incompatible payloads.
  - QA:
    - [x] Fake Unity connects.
    - [x] Invalid message receives structured error.
    - [x] Disconnect is observed.
    - [x] No external network calls occur.

- [x] Task 3.4: Implement ACK tracking and reconnect buffer
  - Affected files:
    - `src/korean_social_simulator/bridge/ack_tracker.py`
    - `tests/unit/bridge/test_ack_tracker.py`
  - Work:
    - Track sent message IDs and sequences.
    - Mark ACKs applied.
    - Detect timeout with injectable clock.
    - Compute reconnect resume point.
  - QA:
    - [x] ACK marks applied.
    - [x] Timeout deterministic.
    - [x] Reconnect resume point is computed from last ACK.

### Wave 4: CLI and Argus Pipeline Integration

Dependencies: Waves 2 and 3.

- [x] Task 4.1: Add bridge CLI commands
  - Affected files:
    - `src/korean_social_simulator/cli.py`
    - `src/korean_social_simulator/pipeline.py`
    - `tests/integration/test_cli_bridge_commands.py`
  - Work:
    - Add `kssim bridge validate-config`.
    - Add `kssim bridge serve --config configs/bridge.example.yaml`.
    - Add `kssim bridge export-replay --events <events.jsonl> --output <bridge.jsonl>`.
    - Keep existing CLI commands unchanged.
  - QA:
    - [x] CLI help includes bridge commands.
    - [x] Validate-config exits 0 for example config.
    - [x] Export-replay produces bridge JSONL from existing Argus run artifacts.

- [x] Task 4.2: Add replay control endpoints
  - Affected files:
    - `src/korean_social_simulator/bridge/replay_controller.py`
    - `tests/integration/bridge/test_replay_controller.py`
  - Work:
    - Load replay fixture.
    - Pause, resume, and step one event.
    - Keep observer controls from mutating hidden simulation state.
  - QA:
    - [x] Replay load succeeds.
    - [x] Pause prevents emission.
    - [x] Step emits exactly one envelope.

- [x] Task 4.3: Add selected-agent public inspection
  - Affected files:
    - `src/korean_social_simulator/bridge/agent_inspection.py`
    - `tests/integration/bridge/test_agent_inspection.py`
  - Work:
    - Return allowlisted public state only.
    - Exclude hidden prompts, raw private metadata, chain data, and credentials.
  - QA:
    - [x] Known agent returns public fields.
    - [x] Unknown agent returns structured error.
    - [x] Test asserts sensitive keys are absent.

### Wave 5: MuJoCo Service Wrapper [REMOVED]

> **Note:** MuJoCo integration has been removed. Wave 5 tasks are no longer applicable. Deterministic fallback physics (Wave 2, Task 2.3) is the sole physics backend.

Dependencies: Waves 1, 2, and 3.

- [x] Task 5.1: ~~Add optional MuJoCo dependency path~~ [REMOVED]
  - MuJoCo extra removed from `pyproject.toml`.
  - Deterministic fallback is the only backend.

- [x] Task 5.2: ~~Implement MuJoCo service interface~~ [REMOVED]
  - `mujoco_service/service.py` and `model_loader.py` no longer needed.
  - `bridge/physics_coordinator.py` now only uses deterministic fallback.

### Wave 6: Unity Project Foundation

Dependencies: Waves 1 and 3.

- [ ] Task 6.1: Create Unity project skeleton
  - Affected files:
    - `unity/EmbodiedDebate/Packages/manifest.json`
    - `unity/EmbodiedDebate/Assets/Project/Scenes/MainSimulation.unity`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/`
    - `unity/EmbodiedDebate/Assets/Tests/EditMode/`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/`
    - `.gitignore`
  - Work:
    - Create minimal Unity 2022.3 LTS or selected Unity 6 LTS structure.
    - Exclude `Library/`, `Temp/`, `Obj/`, build outputs, and account-bound assets.
  - QA:
    - Unity project opens.
    - Empty scene runs.
    - Git status does not include generated Unity directories.

- [x] Task 6.2: Add Unity DTOs and WebSocket client
  - Affected files:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Bridge/BridgeEnvelope.cs`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Bridge/UnityBridgeClient.cs`
    - `unity/EmbodiedDebate/Assets/Tests/EditMode/BridgeEnvelopeTests.cs`
  - Work:
    - Parse bridge envelopes.
    - Preserve unknown message error behavior.
    - Queue received messages for main-thread application.
    - Send `unity.ready`, `unity.ack`, and `unity.error`.
  - QA:
    - [x] EditMode parse tests pass through local C# DTO smoke; Unity Editor batch is blocked until editor is installed.
    - [x] Fake/local server connection works.
    - [x] Malformed JSON does not crash DTO parsing.

- [x] Task 6.3: Implement scene orchestrator shell
  - Affected files:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Scene/SimulationSceneOrchestrator.cs`
    - `unity/EmbodiedDebate/Assets/Tests/EditMode/SceneOrchestratorTests.cs`
  - Work:
    - Route `agent.spawn`, `agent.move`, `agent.dialogue`, `agent.emotion`, `group.update`, `conflict.update`, and `physics.result` to handlers.
    - Return warning/error for unknown types.
  - QA:
    - [x] EditMode route tests compile through local C# test assembly; Unity Editor batch is blocked until editor is installed.
    - [x] Unknown type produces error without crash.

### Wave 7: Robot Asset, Avatar, and Visual Layer

Dependencies: Wave 6.

- [x] Task 7.1: Validate robot asset path and attribution
  - Affected files:
    - `docs/robot-asset-selection.md`
    - `unity/EmbodiedDebate/Assets/Project/Robots/Attribution/ROBOT_ASSET_ATTRIBUTION.md`
    - `unity/EmbodiedDebate/Assets/Project/Robots/Prefabs/FallbackRobot.prefab`
  - Work:
    - Do not automate downloads.
    - Document local import steps.
    - Prefer cute rigged Sketchfab candidate only after license/import validation.
    - Use Robot Kyle or a project-owned placeholder only as fallback.
  - QA:
    - [x] Attribution includes title, author/publisher, source, license, download date, redistribution status, and modifications.
    - [x] Raw third-party assets are absent from Git unless license permits.

- [x] Task 7.2: Implement robot avatar manager
  - Affected files:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Robots/RobotAvatarManager.cs`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Robots/RobotAvatar.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/RobotAvatarManagerPlayModeTests.cs`
  - Work:
    - Spawn exactly one avatar per `agent_id`.
    - Duplicate spawn updates existing avatar.
    - Missing prefab uses fallback.
    - Apply group colors or badges.
  - QA:
    - [x] PlayMode spawn tests compile through local C# test assembly; Unity Editor batch is blocked until editor is installed.
    - [x] Duplicate spawn does not duplicate avatar registry entry.
    - [x] Missing prefab warning is visible.

- [x] Task 7.3: Implement animation mapping
  - Affected files:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Robots/AnimationStateMapper.cs`
    - `unity/EmbodiedDebate/Assets/Tests/EditMode/AnimationStateMapperTests.cs`
  - Work:
    - Centralize mapping for idle, walk, run, speak, argue, push, block, stumble, fall, recover.
    - Avoid hardcoding third-party clip names outside mapping config.
  - QA:
    - [x] Known actions map.
    - [x] Unknown action falls back to idle warning.

### Wave 8: Dialogue, Emotion, Conflict, Observer UI

Dependencies: Waves 6 and 7.

- [ ] Task 8.1: Implement dialogue and emotion UI
  - Affected files:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/DialogueBubbleManager.cs`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/EmotionIndicatorManager.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/DialogueEmotionPlayModeTests.cs`
  - Work:
    - Display dialogue near speaker.
    - Handle long dialogue by truncation or scrolling.
    - Expire dialogue after configured duration.
    - Render emotion intensity.
  - QA:
    - Dialogue appears and expires.
    - Long dialogue remains readable.
    - Emotion visual changes with intensity.

- [ ] Task 8.2: Implement group and conflict visualization
  - Affected files:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/GroupIndicatorManager.cs`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/ConflictVisualizationManager.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/ConflictVisualizationTests.cs`
  - Work:
    - Show group badge or color.
    - Show non-graphic conflict escalation and de-escalation cues.
  - QA:
    - Conflict escalation cue appears.
    - De-escalation cue fades/removes.
    - Manual review confirms no graphic harm.

- [ ] Task 8.3: Implement observer controls in Unity
  - Affected files:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/ObserverControls.cs`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/AgentInspectorPanel.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/ObserverControlsPlayModeTests.cs`
  - Work:
    - Send pause, resume, step, select-agent, and camera state messages.
    - Display public inspectable agent state only.
  - QA:
    - UI sends expected messages.
    - Inspect panel excludes hidden data.

### Wave 9: Full Smoke Scenario and Verification

Dependencies: Waves 0 through 8.

- [ ] Task 9.1: Add bridge smoke fixture
  - Affected files:
    - `configs/scenarios/conflict-push-smoke.yaml`
    - `tests/golden/bridge/conflict_push.input.jsonl`
    - `tests/golden/bridge/conflict_push.expected.jsonl`
  - Work:
    - Include at least 20 agents for final smoke target, with a smaller fast fixture if needed for unit tests.
    - Include spawn, dialogue, movement, emotion, group, conflict, and physical fallback event.
  - QA:
    - Replay is deterministic.
    - Golden fixture contains no private or copyrighted text.

- [ ] Task 9.2: Execute Python verification
  - Affected files:
    - `reports/verification/python.md`
  - Work:
    - Run and record Python checks.
  - QA:
    - `uv sync --extra dev`
    - `uv run pytest`
    - `uv run pytest tests/unit`
    - `uv run pytest tests/integration`
    - `uv run ruff check .`
    - `uv run ruff format --check .`
    - `uv run mypy src`

- [ ] Task 9.3: Execute Unity verification
  - Affected files:
    - `reports/unity-editmode.xml`
    - `reports/unity-playmode.xml`
    - `reports/verification/unity.md`
  - Work:
    - Run Unity tests when editor is installed.
    - If not runnable, record exact editor/path blocker and manual checks performed.
  - QA:
    - Unity EditMode batch command exits 0 or blocker is documented.
    - Unity PlayMode batch command exits 0 or blocker is documented.

- [ ] Task 9.4: Manual runtime smoke
  - Affected files:
    - `reports/verification/manual-smoke.md`
  - Work:
    - Start bridge.
    - Open Unity scene.
    - Confirm `unity.ready` -> `bridge.ready`.
    - Stream smoke fixture.
    - Confirm robot spawn, dialogue, movement, emotion/conflict cue, and physical fallback result.
  - QA:
    - Record observed behavior and exact commands.
    - Pass when the MVP is visible and replay log is saved.

- [ ] Task 9.5: Documentation and changelog update
  - Affected files:
    - `README.md`
    - `docs/architecture.md`
    - `docs/robot-asset-selection.md`
    - `docs/adr/0001-project-architecture.md`
    - `docs/adr/0002-technology-stack.md`
    - `docs/adr/0003-testing-strategy.md`
    - `specs/ai-unity-mujoco-bridge/changelog.md`
  - Work:
    - Align docs with implemented behavior only.
    - Explicitly list remaining limitations.
  - QA:
    - Documentation command snippets are executable or marked as future/manual.
    - Changelog lists completed additions.

## Requirement Coverage Map

| Requirement | Planned coverage |
|---|---|
| R1 Preserve text simulation boundary | Tasks 0.2, 2.1, 4.2, 4.3 |
| R2 Versioned bridge envelope | Tasks 1.1, 2.1 |
| R3 WebSocket streaming | Tasks 3.2, 3.3, 6.2, 9.4 |
| R4 Unity ACK/error | Tasks 3.3, 3.4, 6.2 |
| R5 Robot spawning | Tasks 6.3, 7.2, 9.4 |
| R6 Asset validation | Task 7.1 |
| R7 Movement visualization | Tasks 6.3, 7.2, 9.1 |
| R8 Dialogue/emotion | Tasks 8.1, 9.1 |
| R9 Group/conflict | Tasks 8.2, 9.1 |
| R10 Deterministic physics request | Tasks 1.2, 2.3 |
| R11 Physical result rendering | Tasks 6.3, 7.3, 8.2 |
| R12 Replay logging | Tasks 2.2, 4.2, 9.1 |
| R13 Observer controls | Tasks 4.2, 4.3, 8.3 |
| R14 Input validation | Tasks 1.1, 1.2, 3.3 |
| R15 Determinism | Tasks 2.1, 2.2, 2.3, 9.1 |
| R16 Configurable local runtime | Tasks 1.3, 3.2, 4.1 |
| R17 Structured errors | Tasks 1.1, 3.3, 4.3, 5.2, 6.2 |
| R18 Verification coverage | Tasks 9.2, 9.3, 9.4 |
| R19 Runtime performance | Tasks 3.4, 9.4 |
| R20 Local-first security | Tasks 1.3, 3.3, 4.3, 8.2 |
| R21 Unity compatibility | Tasks 6.1, 9.3 |
| R22 Documentation updates | Tasks 0.1, 9.5 |

## Test Plan

### Objective

Verify that Argus can keep its existing offline simulation pipeline intact while adding a local, deterministic, versioned Unity bridge with deterministic physics fallback.

### Prerequisites

- Python dependencies installed with `uv`.
- Unity 2022.3 LTS or selected Unity 6 LTS installed before Unity tasks.
- No API keys required for dry-run, bridge schema, replay, fallback physics, or fake Unity tests.
- Third-party robot asset imported manually only after license review.

### Test Cases

1. Baseline Argus dry-run:
   - Input: `examples/run_product_reaction.yaml`
   - Expected: run artifacts are created and current tests remain green.
   - Verify: `uv run kssim run --config examples/run_product_reaction.yaml --dry-run`

2. Schema validation:
   - Input: valid and invalid bridge envelopes.
   - Expected: valid passes, missing required fields and unsupported major versions fail.
   - Verify: `uv run pytest tests/unit/bridge_schema`

3. Adapter determinism:
   - Input: fixed Argus event JSONL fixture.
   - Expected: canonical bridge JSONL matches golden output.
   - Verify: `uv run pytest tests/unit/bridge tests/golden`

4. Local bridge lifecycle:
   - Input: fake Unity WebSocket client.
   - Expected: connect, ready, ordered events, ACK, disconnect all work.
   - Verify: `uv run pytest tests/integration/bridge`

5. Deterministic physics fallback:
   - Input: physical `push` request with fixed seed.
   - Expected: deterministic fallback `physics.result`.
   - Verify: `uv run pytest tests/unit/mujoco_service/test_fallback_physics.py`

6. Unity DTO and scene routing:
   - Input: sample bridge JSON envelopes.
   - Expected: Unity parses and routes without main-thread blocking.
   - Verify: Unity EditMode tests.

7. Unity visual smoke:
   - Input: smoke replay with spawn, movement, dialogue, emotion, conflict, and physical fallback.
   - Expected: at least one robot visible with dialogue and movement; final target is 20 visible agents at 30 FPS.
   - Verify: Unity PlayMode tests and manual smoke report.

### Success Criteria

All applicable automated tests pass, manual Unity checks are recorded, fallback behavior is deterministic, and no existing Argus dry-run behavior regresses.

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Current Argus event payloads are too sparse for Unity semantics | Start with event analysis; add adapter errors and synthetic bridge fixtures before claiming full visual mapping. |
| Standalone docs conflict with Argus package layout | Convert implementation paths to `korean_social_simulator.*` and document all deviations. |
| Bridge dependencies make offline MVP heavier | Put server dependencies in optional `bridge` extra. |
| Physics implementation must remain deterministic | Keep fallback physics as the sole backend (MuJoCo removed). |
| Unity package or editor version is unavailable | Record exact blocker and keep Python fake-client tests runnable. |
| Robot asset cannot be redistributed | Commit import instructions, wrappers, and attribution only; exclude raw asset. |
| Logs expose sensitive persona or scenario text | Use allowlisted log fields and add log redaction tests. |
| Replay output drifts | Use sorted JSON, fixed seeds, sequence numbers, and golden tests. |
| Agents overbuild a game | Keep each task scoped to bridge, visualization, replay, and physical-event MVP. |

## Plan Addendum: URP Shaders and Mini-Bot Wall Recovery

### Scope and Goal

Goal: upgrade the Unity visualization to use URP-compatible materials/shaders and fix Mini-bot roaming so agents do not keep driving into one wall or orbiting a single useless spot. Desk movement is acceptable: desks and similar furniture may be pushed by Mini-bots as long as the agent recovers and keeps roaming.

User requirements:

- Use URP (Universal Render Pipeline).
- Use URP-compatible shaders instead of Built-in `Standard` as the primary material path.
- It is acceptable if the desk is pushed.
- Mini-bots must not keep moving around one bad place or go straight into a wall indefinitely.
- When a Mini-bot meets a wall, it should rotate 90 degrees to go somewhere else or choose a random new route.

Next phase: `build`.

Creative required: no. The visual and movement behavior is specific enough to implement with conservative defaults rather than a separate creative decision phase.

### Affected Files and Systems

Unity render pipeline:

- `unity/EmbodiedDebate/Packages/manifest.json`
- `unity/EmbodiedDebate/Packages/packages-lock.json`
- `unity/EmbodiedDebate/ProjectSettings/GraphicsSettings.asset`
- `unity/EmbodiedDebate/ProjectSettings/QualitySettings.asset`
- new URP pipeline asset under `unity/EmbodiedDebate/Assets/Project/Settings/`
- existing materials under `unity/EmbodiedDebate/Assets/Project/Materials/`
- editor scene builders:
  - `unity/EmbodiedDebate/Assets/Editor/MiniBotScenarioBuilder.cs`
  - `unity/EmbodiedDebate/Assets/Editor/HideAndSeekDesignBuilder.cs`
  - `unity/EmbodiedDebate/Assets/Editor/IsolatedPrototypeSpaceBuilder.cs`
- runtime/UI material creation:
  - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/DialogueBubbleManager.cs`
  - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/ConflictVisualizationManager.cs`
  - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/AgentInspectorPanel.cs`
  - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/EmotionIndicatorManager.cs`

Mini-bot movement and collision recovery:

- `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotHideAndSeekScenario.cs`
- `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotRunAroundScenario.cs`
- optional shared helper: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/RoomNavigationMath.cs` if the existing nested helper should be extracted
- `unity/EmbodiedDebate/Assets/Editor/HideAndSeekDesignBuilder.cs`
- `unity/EmbodiedDebate/Assets/Editor/MiniBotScenarioBuilder.cs`
- `unity/EmbodiedDebate/Assets/Tests/EditMode/AgentLocomotionDriverTests.cs`
- new EditMode tests for wall-contact recovery and URP shader selection
- optional PlayMode smoke test for Mini-bot roam recovery

### Implementation Plan

1. Add URP dependency and pipeline assets.
   - Add `com.unity.render-pipelines.universal` to `Packages/manifest.json`.
   - Let Unity refresh `packages-lock.json`.
   - Create a URP pipeline asset and renderer asset under `Assets/Project/Settings/`.
   - Assign the URP asset in `GraphicsSettings.asset` and relevant quality tiers.
   - Keep changes local to the Unity project; do not affect Argus Python offline tests.

2. Replace Built-in shader assumptions.
   - Replace `Shader.Find("Standard")` factory calls with a helper that prefers `Universal Render Pipeline/Lit` and falls back only when URP is unavailable.
   - Replace transparent UI material lookup with `Universal Render Pipeline/Unlit` configured for alpha, with fallback to the current unlit/standard path.
   - Convert generated scene materials and checked-in material assets to URP-compatible shader references through Unity's material upgrade path or a small editor migration command.
   - Verify no new material creation path hardcodes `Standard` as the first choice.

3. Preserve pushable furniture behavior.
   - Keep desks and table-like furniture as non-kinematic rigidbodies when configured as pushable.
   - Do not classify desk displacement as a failure.
   - Add constraints so pushed furniture stays on the floor plane and does not tip over: freeze Y position, X/Z rotation, and use continuous/speculative collision detection.
   - Ensure obstacle checks do not cause the Mini-bot to freeze just because a pushable desk is in front of it.

4. Implement wall-contact recovery.
   - Add an explicit wall-contact recovery path in the Mini-bot roam logic.
   - When collision or boundary pressure indicates a wall, compute a planar recovery direction:
     - primary: rotate the current heading by 90 degrees left or right while biasing away from the wall normal,
     - fallback: choose a seeded random interior target when the 90-degree direction is blocked or makes no progress.
   - Apply a short recovery cooldown so the agent does not immediately choose the same wall-facing target again.
   - Reset low-progress timers and steering state after recovery so the Mini-bot visibly turns and leaves.

5. Fix the run-around demo behavior.
   - Stop directly teleporting Mini-bots in `MiniBotRunAroundScenario` through walls.
   - Either constrain circular paths fully inside the room or move the demo to the same Rigidbody-driven bounded roam/recovery logic used by hide-and-seek.
   - Prefer the shared bounded-roam logic so all Mini-bot scenes get the same wall recovery.

6. Add deterministic tests.
   - Add tests for 90-degree wall turn direction near each wall.
   - Add tests that a boundary collision produces an inward/tangent recovery vector rather than continuing forward into the wall.
   - Add tests for seeded random fallback target staying inside room bounds.
   - Add a test that pushable furniture colliders are still avoidable/contact-safe and are not treated as agent-owned colliders.
   - Add a shader helper test that resolves URP shader names before fallback when URP is installed.

7. Rebuild scenes and validate visible behavior.
   - Re-run the Mini-bot scene builder menu command or batch editor method for `MiniBotHideAndSeekDesign` and `MiniBotRunAround`.
   - Run EditMode tests.
   - Run PlayMode/manual smoke if Unity editor is available.
   - Confirm that a Mini-bot hitting a wall rotates roughly 90 degrees or chooses a new random interior path, and that pushed desk displacement is acceptable.

### Success Criteria

- URP is installed in the Unity project and assigned as the active render pipeline.
- Existing checked-in and generated materials use URP-compatible shaders, or documented fallback logic handles missing URP without pink materials.
- No code path prefers Built-in `Standard` when creating new runtime/editor materials.
- A Mini-bot that reaches a wall does not continue driving straight into it for more than one recovery cooldown.
- Wall contact produces a visible 90-degree turn or a seeded random interior retarget.
- Desks may move when pushed, but remain floor-bound and do not break agent roaming.
- `MiniBotRunAround` no longer teleports agents through walls or loops them into one bad wall-facing path.
- Tests cover URP shader selection, wall-turn math, random fallback bounds, and pushable desk handling.

### Verification Plan

Python checks are not the primary validation for this Unity-only addendum, but run them only if shared repo files are touched outside Unity/memory-bank:

```bash
uv run ruff format .
uv run ruff check . --fix
uv run mypy src
```

Unity checks:

```bash
/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath unity/EmbodiedDebate \
  -runTests \
  -testPlatform EditMode \
  -testResults reports/unity-editmode.xml \
  -quit
```

Optional PlayMode/manual smoke:

```bash
/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath unity/EmbodiedDebate \
  -runTests \
  -testPlatform PlayMode \
  -testResults reports/unity-playmode.xml \
  -quit
```

Manual acceptance check:

- Open `MiniBotHideAndSeekDesign`.
- Let the scene run for at least 60 seconds.
- Confirm agents roam away from walls after contact.
- Confirm a desk can be pushed without stopping the agent permanently.
- Confirm URP materials render without pink/missing shader surfaces.

### Failure Behavior

- If URP package resolution fails, keep fallback shader paths but do not mark URP complete.
- If Unity editor is unavailable, record the exact editor path/blocker and rely only on C# compile/EditMode checks that can run locally.
- If random recovery can still select a blocked target, increase attempts and fall back to a deterministic center-biased target inside room bounds.
- If pushable furniture causes repeated stuck recovery, keep the desk pushable but lower mass/friction or widen wall/obstacle recovery cooldown before making the desk static.

### Privacy and Security

- This work is entirely local Unity visualization behavior.
- Do not add network calls, runtime asset downloads, telemetry, or account-bound package sources.
- Do not commit paid or unverified third-party assets while updating materials.

## Completion Gate

Planning is complete when:

- The plan identifies Argus-native implementation paths.
- All 22 requirements have planned task coverage.
- Every task has a QA field.
- The next execution phase is explicit.

Next phase for the original bridge plan: `creative`.
Next phase for the URP and Mini-bot wall-recovery addendum: `build`.

## Plan Addendum: Mini-Bot Locomotion Animator Parameters and Blend Tree Verification

### Scope and Goal

Goal: ensure the Mini-bot does not physically move in the scene while the Animator remains idle, and ensure walk/turn walking is driven by one locomotion blend tree instead of repeated separate-state restarts.

User-reported likely issues to address:

- Animator movement parameters may not be passed from the script that moves the character.
- Blend tree thresholds may not match actual transmitted speed and turn values.
- Rigidbody/NavMesh/Transform movement may be separated from animation state updates.
- Root motion may conflict with code-driven movement.

Next phase: `build`.

Creative required: no. The desired behavior is a concrete locomotion wiring and verification pass.

### Affected Files and Systems

Primary Unity runtime files:

- `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/AgentLocomotionDriver.cs`
- `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotHideAndSeekScenario.cs`
- `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotRunAroundScenario.cs`

Animator/controller generation:

- `unity/EmbodiedDebate/Assets/Editor/HideAndSeekDesignBuilder.cs`
- `unity/EmbodiedDebate/Assets/Project/Resources/Animations/Controllers/MiniBotLocomotion.controller`
- `unity/EmbodiedDebate/Assets/Project/Resources/Animations/Locomotion/Idle.fbx`
- `unity/EmbodiedDebate/Assets/Project/Resources/Animations/Locomotion/Walking-2.fbx`
- `unity/EmbodiedDebate/Assets/Project/Resources/Animations/FBXTurns/Happy Right Turn.fbx`
- `unity/EmbodiedDebate/Assets/Project/Resources/Animations/FBXTurns/Happy Right Turn-2.fbx`

Tests and generated scene:

- `unity/EmbodiedDebate/Assets/Tests/EditMode/AgentLocomotionDriverTests.cs`
- `unity/EmbodiedDebate/Assets/Project/Scenes/MiniBotHideAndSeekDesign.unity`

### Requirements and Implementation Coverage

| Requirement | Planned coverage |
|---|---|
| Moving agents must pass nonzero animation parameters | `AgentLocomotionDriver.LateUpdate()` computes planar world-position delta and calls `Animator.SetFloat("Speed", ...)` and `Animator.SetFloat("Turn", ...)` with damping. |
| Idle must remain idle when stopped | Use `Speed < 0.05` transition back to `Idle`; keep stop hysteresis so tiny drift does not restart walking. |
| Walking must use `Walking-2.fbx` | Build `Walk_InPlace` clip from `Walking-2.fbx` and place it in the blend tree at `(Turn=0, Speed=1)`. |
| Left turn must use `Happy Right Turn-2.fbx` | Place `TurnLeft_Happy` in the blend tree at `(Turn=-1, Speed=1)`. |
| Right turn must use `Happy Right Turn.fbx` | Place `TurnRight_Happy` in the blend tree at `(Turn=1, Speed=1)`. |
| Animation FBXs must share the Mini-bot Humanoid avatar | Import `Idle.fbx` from the main model first, then import walking and happy turn clips with `CopyFromOther` when the main avatar is valid. |
| Blend tree thresholds must match script output | Normalize speed to `0..1` and turn to `-1..1`; use a `2D Freeform Cartesian` blend tree with X=`Turn`, Y=`Speed`. |
| Code movement remains authoritative | Keep `Animator.applyRootMotion = false` and never toggle it at runtime after setup. |
| Turn values must not flicker | Add signed yaw hysteresis: start turn above the larger threshold, keep the current turn until it falls below the stop threshold. |
| Active spawned Mini-bots use one controller path | Assign `MiniBotLocomotion.controller` in the hide-and-seek/bridge-spawn path; leave `MiniBotWalkAnimator` only for older preview scenes if still needed. |

### Data Flow

1. Rigidbody or scripted movement changes the Mini-bot world position.
2. `AgentLocomotionDriver.LateUpdate()` compares the current position with the previous frame.
3. Vertical movement is ignored; planar speed is computed from X/Z delta.
4. Desired planar yaw is computed from the movement vector.
5. Signed yaw delta maps left to negative `Turn` and right to positive `Turn`.
6. Speed and turn are normalized to the blend tree ranges:
   - stopped: `Speed=0`, `Turn=0`
   - forward walk: `Speed>0.05`, `Turn` near `0`
   - left turn walk: `Speed>0.05`, `Turn<0`
   - right turn walk: `Speed>0.05`, `Turn>0`
7. Animator transitions:
   - `Idle -> LocomotionBlendTree` when `Speed > 0.05`
   - `LocomotionBlendTree -> Idle` when `Speed < 0.05`
8. The blend tree blends:
   - `Idle` at `(0,0)`
   - `Walk_InPlace` at `(0,1)`
   - `TurnLeft_Happy` at `(-1,1)`
   - `TurnRight_Happy` at `(1,1)`

### Implementation Plan

1. Confirm the Animator parameter contract.
   - Controller parameters must be only the locomotion contract needed by the blend tree: `Speed` float and `Turn` float.
   - Runtime code must query both parameters at startup and log whether they exist.
   - If either parameter is missing, log a warning with the controller name and do not silently assume animation is working.

2. Confirm movement-to-animation coupling.
   - Ensure the active Mini-bot prefab or spawned bridge agent has `AgentLocomotionDriver` and `Animator` on the same hierarchy where movement is observed.
   - Compute speed from actual world-position delta, not from a separate command intent that may drift from physics.
   - Apply Animator floats in `LateUpdate()` after movement for the frame has already happened.

3. Confirm blend tree thresholds.
   - Rebuild `MiniBotLocomotion.controller` with a single `LocomotionBlendTree`.
   - Use `2D Freeform Cartesian`, X=`Turn`, Y=`Speed`.
   - Place motions at `(0,0)`, `(0,1)`, `(-1,1)`, and `(1,1)`.
   - Transition only between `Idle` and `LocomotionBlendTree`, not `Any State` to walk/turn clips.

4. Confirm root-motion authority.
   - Set `Animator.applyRootMotion = false` in setup.
   - Keep FBX clip root motion locked/baked so animation pose cannot move the Rigidbody independently.
   - Do not switch `applyRootMotion` during gameplay.

5. Add or keep deterministic EditMode tests.
   - Planar speed ignores Y movement.
   - Signed yaw maps left to negative turn and right to positive turn.
   - Hysteresis prevents rapid turn flicker around the threshold.
   - Turn values clamp to `[-1, 1]`.
   - Stopping movement resets turn to `0`.

6. Rebuild and smoke-test the scene.
   - Rebuild `MiniBotHideAndSeekDesign` so spawned agents receive the regenerated controller.
   - Capture Play Mode with video frames enabled.
   - Verify logs include active locomotion sources for walk, left turn, and right turn.
   - Verify logs have no compile errors, null references, or missing component exceptions.

### Verification Plan

Unity EditMode tests:

```bash
"/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath unity/EmbodiedDebate \
  -runTests \
  -testPlatform EditMode \
  -testResults tmp/urp-minibot-editmode-results-turns.xml \
  -logFile tmp/urp-minibot-editmode-turns.log
```

Scene rebuild:

```bash
"/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath unity/EmbodiedDebate \
  -executeMethod ArgusUnity.Editor.HideAndSeekDesignBuilder.BuildScene \
  -logFile tmp/hide-and-seek-happy-turn-build.log
```

Play Mode capture:

```bash
ARGUS_UNITY_VIDEO_CAPTURE=1 \
ARGUS_UNITY_VIDEO_DIR=/Users/guribbong/code/Argus/tmp/minibot_locomotion_frames \
ARGUS_UNITY_VIDEO_PREFIX=minibot_locomotion \
"/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath unity/EmbodiedDebate \
  -executeMethod ArgusUnity.Editor.HideAndSeekDesignBuilder.OpenSceneInPlayMode \
  -logFile tmp/hide-and-seek-locomotion-capture.log
```

Log assertions:

- Pass if logs include `Animator locomotion ready` with `hasSpeed=True`, `hasTurn=True`, and `rootMotion=False`.
- Pass if logs include `LocomotionBlendTree`, `Walking-2.fbx`, `Happy Right Turn.fbx`, and `Happy Right Turn-2.fbx`.
- Fail if logs include `CS####`, `NullReferenceException`, or `MissingComponentException`.

Controller asset checks:

```bash
rg -n "LocomotionBlendTree|m_BlendParameter: Turn|m_BlendParameterY: Speed|m_BlendType: 3|m_Name: (Speed|Turn)" \
  unity/EmbodiedDebate/Assets/Project/Resources/Animations/Controllers/MiniBotLocomotion.controller
```

### Failure Behavior

- If `Walking-2.fbx` is missing, log a warning and do not overwrite a known-good controller.
- If either happy turn FBX is missing, keep walking functional and log which blend tree child was skipped.
- If the Animator lacks `Speed` or `Turn`, log the mismatch once and keep code movement running without pretending animation is valid.
- If an FBX avatar is invalid or non-human, log avatar validity and keep root motion disabled.
- If Unity batch mode completes the build but does not exit, terminate only after the expected completion log appears and record that behavior.

### Privacy and Security

- This is a local Unity animation and asset-import pass.
- Do not add network calls, telemetry, runtime downloads, or account-bound package sources.
- The three FBX files are user-provided local assets; keep attribution notes and do not treat redistribution rights as confirmed.

### Success Criteria

- Active Mini-bots no longer move while the Animator remains stuck in idle.
- `MiniBotLocomotion.controller` uses one `LocomotionBlendTree` for idle/walk/left-turn/right-turn blending.
- Runtime logs prove `Speed` and `Turn` are passed and root motion is disabled.
- Walking uses `Walking-2.fbx`; left turn uses `Happy Right Turn-2.fbx`; right turn uses `Happy Right Turn.fbx`.
- EditMode tests pass.
- Scene rebuild and Play Mode capture complete without compile errors, `NullReferenceException`, or `MissingComponentException`.

## Updated Completion Gate

Planning for the Mini-bot locomotion animator issue is complete when:

- The exact Animator parameter contract is documented.
- The blend tree thresholds and script value ranges are documented.
- Movement-to-animation data flow is documented.
- Root-motion failure behavior is documented.
- The next execution phase is explicit.

Next phase for the Mini-bot locomotion animator addendum: `build`.

### Build Execution Result

Status: completed on 2026-05-06.

Implemented:

- `AgentLocomotionDriver` now warns once when the assigned AnimatorController is missing the expected `Speed` or `Turn` float parameter.
- `HideAndSeekDesignBuilder` imports the Mini-bot idle model first, then imports `Walking-2.fbx`, `Happy Right Turn.fbx`, and `Happy Right Turn-2.fbx` with `CopyFromOther` when the main Mini-bot Humanoid avatar is valid.
- EditMode coverage now includes the missing-parameter warning behavior.

Verification evidence:

- EditMode: `unity/EmbodiedDebate/tmp/urp-minibot-editmode-results-delegated-after-review3.xml` reported `62` total, `62` passed, `0` failed.
- Scene rebuild: `tmp/hide-and-seek-happy-turn-build-delegated-after-review.log` logged `LocomotionBlendTree`, `Walk_InPlace@turn0-speed1`, `TurnLeft_Happy@turn-1-speed1`, and `TurnRight_Happy@turn1-speed1`.
- Play Mode capture: `tmp/hide-and-seek-locomotion-capture-delegated-after-review.log` logged `rootMotion=False`, `hasSpeed=True`, `hasTurn=True`, `Walking-2.fbx`, `Happy Right Turn.fbx`, `Happy Right Turn-2.fbx`, and `ArgusVideo: wrote 240 frames`.
- Rendered smoke video: `/Users/guribbong/code/Argus/tmp/delegated_locomotion_after_review_video.mp4`.

Delegated review:

- Review note: `/Users/guribbong/code/Argus/tmp/delegation-minibot-locomotion-review.md`.
- Finding addressed: missing Animator parameter mismatch is now logged as a warning and covered by an EditMode test.

Direction alignment follow-up:

- `AgentLocomotionDriver` no longer rotates the agent transform. It observes actual movement speed and actual yaw-rate, then drives `Speed` and `Turn`.
- `MiniBotHideAndSeekScenario` now moves agents along their facing direction during wall recovery instead of applying sideways recovery velocity.
- `AgentMoveHandler` now turns bridge-spawned agents toward the target and moves them along their facing direction instead of moving directly to the target while rotation/animation lags behind.
- EditMode: `unity/EmbodiedDebate/tmp/urp-minibot-editmode-results-direction-match.xml` reported `63` total, `63` passed, `0` failed.
- Play Mode capture: `tmp/hide-and-seek-direction-match-capture.log` logged yaw-rate-driven happy left/right turns and wrote `240` frames.
- Rendered direction-match smoke video: `/Users/guribbong/code/Argus/tmp/direction_match_video.mp4`.
