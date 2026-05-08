# Implementation Tasks

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.


## Phase 1: Foundation

- [ ] Task 1.1: Inspect existing text simulation event format
  - Files to create or modify:
    - `docs/existing-text-simulation-analysis.md`
    - `specs/ai-unity-mujoco-bridge/design.md`
  - Requirements covered:
    - Requirement 1
    - Requirement 22
  - Acceptance checks:
    - [ ] Existing event types are listed.
    - [ ] Existing event fields are mapped to proposed bridge event types.
    - [ ] Unknown or ambiguous fields are documented.
  - Notes:
    - Do not modify the text simulation engine in this task.

- [ ] Task 1.2: Create bridge schema definitions
  - Files to create or modify:
    - `src/korean_social_simulator/bridge_schema/bridge_envelope.py`
    - `src/korean_social_simulator/bridge_schema/events.py`
    - `tests/unit/test_bridge_schema.py`
  - Requirements covered:
    - Requirement 2
    - Requirement 14
  - Acceptance checks:
    - [ ] Valid envelope passes validation.
    - [ ] Missing required field fails validation.
    - [ ] Unsupported major version fails validation.
    - [ ] Invalid enum fails validation.
  - Notes:
    - Use strict validation in tests.

- [ ] Task 1.3: Add example configuration files
  - Files to create or modify:
    - `configs/bridge.example.yaml`
    - `configs/robot-assets.example.yaml`
    - `tests/unit/test_config_validation.py`
  - Requirements covered:
    - Requirement 16
    - Requirement 20
  - Acceptance checks:
    - [ ] Default config binds to localhost.
    - [ ] Environment variable override works.
    - [ ] Invalid MuJoCo config fails clearly.
  - Notes:
    - Do not require secrets for local mode.

## Phase 2: Core Communication

- [ ] Task 2.1: Implement local bridge server skeleton
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/server.py`
    - `src/korean_social_simulator/bridge/config.py`
    - `tests/integration/test_bridge_health.py`
  - Requirements covered:
    - Requirement 3
    - Requirement 16
  - Acceptance checks:
    - [ ] Server starts locally.
    - [ ] `/health` returns status.
    - [ ] `/schema/version` returns current version.
  - Notes:
    - No Unity dependency in this task.

- [ ] Task 2.2: Implement `/ws/unity` WebSocket lifecycle
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/websocket_gateway.py`
    - `src/korean_social_simulator/bridge/client_registry.py`
    - `tests/integration/test_unity_websocket_lifecycle.py`
  - Requirements covered:
    - Requirement 3
    - Requirement 4
  - Acceptance checks:
    - [ ] Fake Unity client connects.
    - [ ] `unity.ready` receives `bridge.ready`.
    - [ ] Invalid message receives structured error.
    - [ ] Disconnect is detected.
  - Notes:
    - Use local fake client tests before Unity implementation.

- [ ] Task 2.3: Implement message acknowledgement tracking
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/ack_tracker.py`
    - `tests/unit/test_ack_tracker.py`
  - Requirements covered:
    - Requirement 4
    - Requirement 19
  - Acceptance checks:
    - [ ] ACK marks message applied.
    - [ ] Timeout is detected.
    - [ ] Reconnect resume point can be computed.
  - Notes:
    - Keep deterministic clocks injectable.

## Phase 3: Event Adapter and Replay

- [ ] Task 3.1: Implement simulation event adapter
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/event_adapter.py`
    - `tests/unit/test_event_adapter.py`
    - `tests/golden/bridge/simple_dialogue.input.jsonl`
    - `tests/golden/bridge/simple_dialogue.expected.jsonl`
  - Requirements covered:
    - Requirement 1
    - Requirement 2
    - Requirement 15
  - Acceptance checks:
    - [ ] Dialogue event maps correctly.
    - [ ] Movement event maps correctly.
    - [ ] Conflict event maps correctly.
    - [ ] Unsupported event emits adapter error.
  - Notes:
    - Use existing text simulation fixtures if available.

- [ ] Task 3.2: Implement replay event store
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/replay_store.py`
    - `tests/unit/test_replay_event_store.py`
  - Requirements covered:
    - Requirement 12
    - Requirement 15
  - Acceptance checks:
    - [ ] Valid event appends to JSONL.
    - [ ] Replay loads in sequence order.
    - [ ] Corrupted line reports line number.
  - Notes:
    - Use JSONL for diffable logs.

- [ ] Task 3.3: Add replay control endpoints
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/replay_controller.py`
    - `tests/integration/test_replay_controller.py`
  - Requirements covered:
    - Requirement 12
    - Requirement 13
  - Acceptance checks:
    - [ ] Replay can load a fixture.
    - [ ] Replay pause works.
    - [ ] Replay step emits one event.
  - Notes:
    - Use a fake Unity client for tests.

## Phase 4: Unity Client and Scene Foundation

- [ ] Task 4.1: Create Unity project structure
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Scenes/MainSimulation.unity`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/`
    - `unity/EmbodiedDebate/Packages/manifest.json`
  - Requirements covered:
    - Requirement 3
    - Requirement 21
  - Acceptance checks:
    - [ ] Unity project opens.
    - [ ] Required package manifest is present.
    - [ ] Empty scene runs.
  - Notes:
    - Do not commit Unity `Library/` or build outputs.

- [ ] Task 4.2: Add Unity WebSocket client
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Bridge/UnityBridgeClient.cs`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Bridge/BridgeEnvelope.cs`
    - `unity/EmbodiedDebate/Assets/Tests/EditMode/BridgeEnvelopeTests.cs`
  - Requirements covered:
    - Requirement 3
    - Requirement 4
  - Acceptance checks:
    - [ ] Client can connect to fake/local server.
    - [ ] Valid JSON parses.
    - [ ] Invalid JSON produces Unity error.
    - [ ] ACK message can be sent.
  - Notes:
    - Dispatch received messages on Unity main thread.

- [ ] Task 4.3: Implement Scene Orchestrator
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Scene/SimulationSceneOrchestrator.cs`
    - `unity/EmbodiedDebate/Assets/Tests/EditMode/SceneOrchestratorTests.cs`
  - Requirements covered:
    - Requirement 5
    - Requirement 7
    - Requirement 8
  - Acceptance checks:
    - [ ] `agent.spawn` calls avatar manager.
    - [ ] `agent.move` routes to movement handler.
    - [ ] `agent.dialogue` routes to dialogue UI.
    - [ ] Unknown type sends warning/error.
  - Notes:
    - Keep rendering logic out of WebSocket client.

## Phase 5: Robot Asset Integration

- [ ] Task 5.1: Validate and import selected cute robot asset
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Robots/Attribution/ROBOT_ASSET_ATTRIBUTION.md`
    - `unity/EmbodiedDebate/Assets/Project/Robots/Prefabs/SimulationRobot.prefab`
    - `docs/robot-asset-selection.md`
  - Requirements covered:
    - Requirement 5
    - Requirement 6
    - Requirement 22
  - Acceptance checks:
    - [ ] License is recorded.
    - [ ] Redistribution status is recorded.
    - [ ] Prefab loads in Unity.
    - [ ] Idle and locomotion are verified or fallback documented.
  - Notes:
    - Prefer `Cute Robots - Low Poly - Rigged - Animated` if it passes license/import validation.
    - Use `Robot Kyle | URP` as fallback.

- [ ] Task 5.2: Implement Robot Avatar Manager
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Robots/RobotAvatarManager.cs`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Robots/RobotAvatar.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/RobotAvatarManagerPlayModeTests.cs`
  - Requirements covered:
    - Requirement 5
    - Requirement 7
    - Requirement 8
  - Acceptance checks:
    - [ ] Robot spawns once per agent ID.
    - [ ] Duplicate spawn updates existing robot.
    - [ ] Missing prefab uses fallback.
    - [ ] Group visual can be applied.
  - Notes:
    - Keep third-party asset files separate from project-owned wrappers.

- [ ] Task 5.3: Implement animation state mapping
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Robots/AnimationStateMapper.cs`
    - `unity/EmbodiedDebate/Assets/Tests/EditMode/AnimationStateMapperTests.cs`
  - Requirements covered:
    - Requirement 7
    - Requirement 8
    - Requirement 11
  - Acceptance checks:
    - [ ] Idle maps to configured Animator state.
    - [ ] Walk/run maps correctly.
    - [ ] Argue/push/fall/recover map or fallback.
    - [ ] Unknown action falls back to idle warning.
  - Notes:
    - Do not hardcode third-party animation clip names outside mapping config.

## Phase 6: Dialogue, Emotion, Group, and Conflict UI

- [ ] Task 6.1: Implement dialogue bubble manager
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/DialogueBubbleManager.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/DialogueBubblePlayModeTests.cs`
  - Requirements covered:
    - Requirement 8
  - Acceptance checks:
    - [ ] Dialogue appears near speaker.
    - [ ] Long dialogue truncates or scrolls.
    - [ ] Dialogue expires after configured duration.
  - Notes:
    - Avoid blocking scene updates.

- [ ] Task 6.2: Implement emotion and group indicators
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/EmotionIndicatorManager.cs`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/GroupIndicatorManager.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/EmotionGroupIndicatorTests.cs`
  - Requirements covered:
    - Requirement 8
    - Requirement 9
  - Acceptance checks:
    - [ ] Emotion intensity changes visual state.
    - [ ] Group badge/color appears.
    - [ ] Missing group uses neutral visual.
  - Notes:
    - Keep cues non-graphic and readable.

- [ ] Task 6.3: Implement conflict visualization
  - Files to create or modify:
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/ConflictVisualizationManager.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/ConflictVisualizationTests.cs`
  - Requirements covered:
    - Requirement 9
    - Requirement 20
  - Acceptance checks:
    - [ ] Conflict escalation cue appears.
    - [ ] Conflict de-escalation cue fades/removes.
    - [ ] Cues remain non-graphic.
  - Notes:
    - Use icons, color intensity, spacing, and labels rather than injury visuals.

## Phase 7: MuJoCo Physical Event Layer

- [ ] Task 7.1: Define physical event schemas
  - Files to create or modify:
    - `src/korean_social_simulator/bridge_schema/physics.py`
    - `tests/unit/test_physics_schema.py`
  - Requirements covered:
    - Requirement 10
    - Requirement 11
  - Acceptance checks:
    - [ ] Valid `PhysicsRequest` passes.
    - [ ] Invalid intensity fails.
    - [ ] Unknown outcome fails or maps to unknown according to schema.
  - Notes:
    - Keep outcomes abstract and non-graphic.

- [ ] Task 7.2: Implement deterministic fallback physics
  - Files to create or modify:
    - `src/korean_social_simulator/mujoco_service/fallback_physics.py`
    - `tests/unit/test_fallback_physics.py`
  - Requirements covered:
    - Requirement 10
    - Requirement 15
  - Acceptance checks:
    - [ ] Same input gives same output.
    - [ ] MuJoCo disabled produces fallback result.
    - [ ] Result includes fallback status.
  - Notes:
    - This must work before real MuJoCo service.

- [ ] Task 7.3: Implement MuJoCo service wrapper
  - Files to create or modify:
    - `src/korean_social_simulator/mujoco_service/service.py`
    - `src/korean_social_simulator/mujoco_service/model_loader.py`
    - `tests/integration/test_mujoco_service.py`
  - Requirements covered:
    - Requirement 10
    - Requirement 21
  - Acceptance checks:
    - [ ] Service loads configured MJCF.
    - [ ] Push fixture returns deterministic outcome.
    - [ ] Timeout returns structured error.
    - [ ] Missing model fails startup when enabled.
  - Notes:
    - Use simplified physical model for MVP.

- [ ] Task 7.4: Connect physical results to Unity
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/physics_coordinator.py`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/Physics/PhysicsResultHandler.cs`
    - `unity/EmbodiedDebate/Assets/Tests/PlayMode/PhysicsResultPlayModeTests.cs`
  - Requirements covered:
    - Requirement 10
    - Requirement 11
  - Acceptance checks:
    - [ ] Push event routes to MuJoCo or fallback.
    - [ ] `fall` outcome plays fall animation or fallback.
    - [ ] Unknown outcome warns and returns idle.
  - Notes:
    - Unity should not directly depend on MuJoCo Python internals.

## Phase 8: Observer Controls and Replay UI

- [ ] Task 8.1: Implement observer pause/resume/step commands
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/observer_controls.py`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/ObserverControls.cs`
    - `tests/integration/test_observer_controls.py`
  - Requirements covered:
    - Requirement 13
  - Acceptance checks:
    - [ ] Pause stops event emission.
    - [ ] Resume continues event emission.
    - [ ] Step emits exactly one event/tick.
  - Notes:
    - Observer controls must not mutate hidden agent cognition.

- [ ] Task 8.2: Implement selected-agent inspection
  - Files to create or modify:
    - `src/korean_social_simulator/bridge/agent_inspection.py`
    - `unity/EmbodiedDebate/Assets/Project/Scripts/UI/AgentInspectorPanel.cs`
    - `tests/integration/test_agent_inspection.py`
  - Requirements covered:
    - Requirement 13
    - Requirement 20
  - Acceptance checks:
    - [ ] Public agent state is returned.
    - [ ] Hidden prompts/private metadata are not returned.
    - [ ] Unknown agent returns structured error.
  - Notes:
    - Use explicit allowlist for inspectable fields.

## Phase 9: Verification and Documentation

- [ ] Task 9.1: Add full smoke scenario
  - Files to create or modify:
    - `tests/golden/bridge/conflict_push.input.jsonl`
    - `tests/golden/bridge/conflict_push.expected.jsonl`
    - `configs/scenarios/conflict-push-smoke.yaml`
  - Requirements covered:
    - Requirement 3
    - Requirement 5
    - Requirement 8
    - Requirement 10
    - Requirement 11
    - Requirement 12
  - Acceptance checks:
    - [ ] Scenario has at least 3 agents.
    - [ ] Scenario includes dialogue, movement, conflict, and physical event.
    - [ ] Replay is deterministic.
  - Notes:
    - Keep the smoke scenario small and readable.

- [ ] Task 9.2: Run full verification suite
  - Files to create or modify:
    - `reports/verification/`
  - Requirements covered:
    - Requirement 18
    - Requirement 19
    - Requirement 21
  - Acceptance checks:
    - [ ] Python tests pass.
    - [ ] Python lint passes.
    - [ ] Python type check passes.
    - [ ] Unity EditMode tests pass or documented why not runnable.
    - [ ] Unity PlayMode tests pass or documented why not runnable.
    - [ ] Manual robot asset checklist is complete.
  - Notes:
    - Do not claim tests passed unless commands were run.

- [ ] Task 9.3: Update documentation and changelog
  - Files to create or modify:
    - `README.md`
    - `docs/architecture.md`
    - `docs/robot-asset-selection.md`
    - `specs/ai-unity-mujoco-bridge/changelog.md`
  - Requirements covered:
    - Requirement 22
  - Acceptance checks:
    - [ ] Documentation matches implemented behavior.
    - [ ] Changelog lists completed additions.
    - [ ] Remaining limitations are explicit.
  - Notes:
    - Keep docs in English.
