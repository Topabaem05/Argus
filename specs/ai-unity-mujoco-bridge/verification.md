# Verification Plan

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

## Verification Overview

Verification must prove that the bridge preserves the existing text simulation boundary, uses valid schemas, connects Unity over WebSocket, renders robot avatars, handles MuJoCo or fallback physical events, writes replay logs, and documents all assumptions.

No implementation is complete until the relevant automated tests and manual review items are executed or explicitly reported as not runnable with a concrete reason.

## Required Commands

### Python

```bash
uv sync
uv run pytest
uv run pytest tests/unit
uv run pytest tests/integration
uv run ruff check .
uv run ruff format --check .
uv run mypy src
```

### Unity EditMode Tests on macOS

```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath unity/EmbodiedDebate \
  -runTests \
  -testPlatform EditMode \
  -testResults reports/unity-editmode.xml \
  -quit
```

### Unity PlayMode Tests on macOS

```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath unity/EmbodiedDebate \
  -runTests \
  -testPlatform PlayMode \
  -testResults reports/unity-playmode.xml \
  -quit
```

### Manual Runtime Smoke Test

```bash
uv run kssim bridge serve --config configs/bridge.example.yaml
```

Then open Unity, load `MainSimulation.unity`, press Play, and run the smoke scenario.

## Unit Tests

Required Python unit tests:

- `test_bridge_schema.py`
  - valid envelope accepted,
  - missing field rejected,
  - unsupported major version rejected,
  - invalid enum rejected,
  - oversized payload rejected.
- `test_config_validation.py`
  - default localhost config,
  - environment override,
  - invalid MuJoCo config failure.
- `test_event_adapter.py`
  - dialogue mapping,
  - movement mapping,
  - conflict mapping,
  - unsupported event error.
- `test_ack_tracker.py`
  - ack success,
  - ack timeout,
  - reconnect resume point.
- `test_replay_event_store.py`
  - append valid event,
  - read in sequence,
  - corrupted line error.
- `test_physics_schema.py`
  - valid request,
  - invalid intensity,
  - invalid outcome.
- `test_fallback_physics.py`
  - deterministic fallback,
  - MuJoCo disabled fallback,
  - fallback result status.

Required Unity EditMode tests:

- `BridgeEnvelopeTests.cs`
  - valid JSON parses,
  - invalid JSON fails safely,
  - unknown message type handled.
- `SceneOrchestratorTests.cs`
  - route spawn,
  - route movement,
  - route dialogue,
  - unknown type produces warning/error.
- `AnimationStateMapperTests.cs`
  - idle/walk/run mapping,
  - argue/push/fall/recover mapping,
  - unknown action fallback.

## Integration Tests

Required Python integration tests:

- `test_bridge_health.py`
  - server starts,
  - `/health` returns OK,
  - `/schema/version` returns supported types.
- `test_unity_websocket_lifecycle.py`
  - fake Unity connects,
  - `unity.ready` -> `bridge.ready`,
  - invalid message -> error,
  - disconnect detected.
- `test_replay_controller.py`
  - load replay,
  - pause,
  - step emits exactly one event.
- `test_mujoco_service.py`
  - service loads MJCF when enabled,
  - deterministic push fixture,
  - timeout returns structured error,
  - missing model fails startup.
- `test_observer_controls.py`
  - pause,
  - resume,
  - step.
- `test_agent_inspection.py`
  - public state allowed,
  - hidden state excluded,
  - unknown agent error.

Required Unity PlayMode tests:

- `RobotAvatarManagerPlayModeTests.cs`
  - spawn robot,
  - duplicate spawn updates existing robot,
  - fallback prefab works.
- `DialogueBubblePlayModeTests.cs`
  - dialogue appears,
  - long dialogue handled,
  - dialogue expires.
- `EmotionGroupIndicatorTests.cs`
  - emotion intensity visual update,
  - group badge visual update.
- `ConflictVisualizationTests.cs`
  - escalation cue,
  - de-escalation cue,
  - non-graphic visual policy.
- `PhysicsResultPlayModeTests.cs`
  - fall outcome maps to animation/fallback,
  - no-contact outcome does not fall,
  - unknown outcome warns.

## Smoke Tests

## Smoke Test 1: Bridge Starts

GIVEN dependencies are installed  
WHEN `uv run kssim bridge serve --config configs/bridge.example.yaml` is executed  
THEN the bridge starts on localhost and `/health` is available.

## Smoke Test 2: Unity Connects

GIVEN bridge is running  
WHEN Unity scene starts  
THEN Unity sends `unity.ready` and receives `bridge.ready`.

## Smoke Test 3: Robot Spawn and Dialogue

GIVEN Unity is connected  
WHEN the bridge sends `agent.spawn` and `agent.dialogue`  
THEN one robot appears and a dialogue bubble is visible.

## Smoke Test 4: Movement

GIVEN a robot exists  
WHEN the bridge sends `agent.move`  
THEN the robot moves toward target position or interpolates according to config.

## Smoke Test 5: Physical Event Fallback

GIVEN MuJoCo is disabled  
WHEN the bridge receives a `push` physical event  
THEN it emits deterministic fallback `physics.result` and Unity displays the result.

## Smoke Test 6: Replay

GIVEN a replay log was recorded  
WHEN replay mode loads it  
THEN Unity receives the same event sequence.

## Regression Tests

The following behavior must not break:

- Existing text simulation event semantics.
- Message envelope required fields.
- Schema major version rejection.
- Unity `unity.ready` handshake.
- Ack tracking.
- Replay JSONL line format.
- Fallback physics determinism.
- Robot spawn uniqueness by agent ID.
- Non-graphic conflict visualization policy.
- Localhost-only default binding.

## Golden Tests

Required golden fixtures:

```txt
tests/golden/bridge/simple_dialogue.input.jsonl
tests/golden/bridge/simple_dialogue.expected.jsonl
tests/golden/bridge/conflict_push.input.jsonl
tests/golden/bridge/conflict_push.expected.jsonl
```

Rules:

- Golden output must be deterministic.
- Golden fixtures must not include private user data.
- Golden fixture changes require reviewer approval.
- The completion report must mention any golden fixture updates.

## Manual Review Checklist

### Architecture

- [ ] Does implementation preserve the text simulation boundary?
- [ ] Are Unity, bridge, and MuJoCo responsibilities separated?
- [ ] Are message schemas versioned?
- [ ] Are event logs replayable?

### Unity Visual Review

- [ ] Is the selected robot visually cute/friendly?
- [ ] Is the robot biped or humanoid enough for the use case?
- [ ] Does the robot stand upright at correct scale?
- [ ] Does idle animation work?
- [ ] Does locomotion work or is fallback acceptable?
- [ ] Are dialogue bubbles readable?
- [ ] Are emotion indicators understandable?
- [ ] Are group/conflict indicators non-graphic?
- [ ] Does camera navigation allow inspection?

### Robot Asset Review

- [ ] License recorded.
- [ ] Attribution recorded.
- [ ] Redistribution status recorded.
- [ ] Raw third-party asset excluded if required.
- [ ] Prefab wrapper committed if allowed.
- [ ] Animation mapping documented.

### MuJoCo Review

- [ ] MuJoCo disabled mode works.
- [ ] MuJoCo enabled mode validates model path.
- [ ] Physical outcomes are abstract and non-graphic.
- [ ] Slow physics does not freeze Unity.

### AI Agent Review

- [ ] Did the agent modify only relevant files?
- [ ] Did the agent add tests?
- [ ] Did the agent run commands?
- [ ] Did the agent report exact command outputs?
- [ ] Did the agent avoid inventing unsupported APIs?
- [ ] Did the agent update docs?

## Completion Report Format

Every implementing agent must report:

```md
# Completion Report

## Summary
[What was implemented]

## Files Changed
[List files]

## Requirements Satisfied
[List requirement IDs]

## Commands Run
[Exact commands and results]

## Tests Added
[List tests]

## Manual Checks
[List manual checks performed]

## Known Limitations
[List limitations]

## Follow-up Work
[List follow-up tasks]
```

## Requirement-to-Verification Matrix

| Requirement | Verification |
|---|---|
| R1 | Adapter unit tests, existing event analysis |
| R2 | Schema unit tests |
| R3 | WebSocket integration tests, Unity PlayMode connection |
| R4 | ACK tests, Unity error tests |
| R5 | Robot avatar PlayMode tests |
| R6 | Manual asset checklist, attribution file |
| R7 | Movement PlayMode tests |
| R8 | Dialogue/emotion PlayMode tests |
| R9 | Conflict visualization PlayMode tests |
| R10 | Physics coordinator integration tests |
| R11 | Physics result PlayMode tests |
| R12 | Replay unit/integration tests |
| R13 | Observer control integration tests |
| R14 | Schema validation tests |
| R15 | Golden and deterministic replay tests |
| R16 | Config tests |
| R17 | Structured error tests |
| R18 | Verification checklist and completion report |
| R19 | Smoke performance/manual FPS check |
| R20 | Localhost/security tests and log review |
| R21 | Compatibility manual check and startup tests |
| R22 | Documentation diff review |
