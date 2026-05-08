# Requirements

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

## Core Functional Requirements

## Requirement 1: Preserve Existing Text Simulation Boundary

### User Story

As a simulation developer, I want the existing text simulation to remain the source of truth, so that the 3D system does not alter social reasoning behavior.

### Acceptance Criteria

#### Scenario 1: Existing simulation event is adapted without semantic change

GIVEN the text simulation emits a dialogue event  
WHEN the adapter converts it to a bridge message  
THEN the original speaker, target, content, timestamp, emotion, and event ID are preserved or traceable.

#### Scenario 2: Unity cannot override cognition

GIVEN Unity sends an observer command  
WHEN the command reaches the bridge  
THEN it may pause, resume, step, select, or inspect the simulation but must not directly rewrite agent beliefs, memories, or relationship states.

#### Scenario 3: Unsupported text event

GIVEN the text simulation emits an unsupported event type  
WHEN the adapter receives the event  
THEN the adapter emits a structured `adapter.error` and does not send an invalid event to Unity.

## Requirement 2: Versioned Bridge Message Envelope

### User Story

As a bridge implementer, I want every message to use a versioned envelope, so that Python, Unity, replay, and MuJoCo can evolve safely.

### Acceptance Criteria

#### Scenario 1: Valid envelope

GIVEN a message with `schema_version`, `message_id`, `session_id`, `sequence`, `sent_at_ms`, `type`, and `payload`  
WHEN it is validated  
THEN the validator accepts it and routes it according to `type`.

#### Scenario 2: Missing required field

GIVEN a message without `message_id`  
WHEN it is validated  
THEN validation fails with a structured schema error.

#### Scenario 3: Unsupported major version

GIVEN a message with schema major version `2` while the bridge supports only major version `1`  
WHEN the bridge receives it  
THEN the bridge rejects it and returns an incompatible-version error.

## Requirement 3: AI-to-Unity WebSocket Streaming

### User Story

As an observer, I want text simulation events streamed into Unity, so that I can watch social behavior as it unfolds.

### Acceptance Criteria

#### Scenario 1: Unity connects

GIVEN the bridge server is running  
WHEN Unity connects to `/ws/unity` and sends `unity.ready`  
THEN the bridge records the client as connected and sends a `bridge.ready` response.

#### Scenario 2: Event stream

GIVEN Unity is connected and the simulation emits valid events  
WHEN the bridge receives those events  
THEN Unity receives them in monotonically increasing sequence order.

#### Scenario 3: Unity disconnects

GIVEN Unity disconnects during a running simulation  
WHEN new events arrive  
THEN the bridge buffers events up to the configured maximum or pauses according to config.

#### Scenario 4: Reconnect

GIVEN Unity reconnects with the same session ID  
WHEN the bridge has buffered events  
THEN the bridge resumes from the last acknowledged sequence when possible.

## Requirement 4: Unity Acknowledgement and Error Reporting

### User Story

As a bridge operator, I want Unity to acknowledge messages and report client-side errors, so that synchronization failures are visible.

### Acceptance Criteria

#### Scenario 1: Acknowledged message

GIVEN Unity successfully applies a message  
WHEN the scene update completes  
THEN Unity sends `unity.ack` with the original `message_id` and applied sequence.

#### Scenario 2: Unity parse failure

GIVEN Unity receives malformed JSON  
WHEN parsing fails  
THEN Unity logs the error locally and sends `unity.error` if the connection is still open.

#### Scenario 3: Missing handler

GIVEN Unity receives a valid envelope with an unknown message type  
WHEN no handler exists  
THEN Unity sends `unity.error` and does not crash the scene.

## Requirement 5: Robot Avatar Spawning

### User Story

As an observer, I want each simulated agent to appear as a cute biped robot avatar, so that the simulation is visually understandable.

### Acceptance Criteria

#### Scenario 1: Spawn agent

GIVEN Unity receives `agent.spawn` for `agent_001`  
WHEN the selected robot prefab is available  
THEN Unity instantiates one robot avatar, labels it `agent_001`, and stores the mapping.

#### Scenario 2: Duplicate spawn

GIVEN `agent_001` already exists in the scene  
WHEN Unity receives another `agent.spawn` for `agent_001`  
THEN Unity updates the existing avatar rather than instantiating a duplicate.

#### Scenario 3: Missing robot prefab

GIVEN the selected robot prefab is missing  
WHEN Unity receives `agent.spawn`  
THEN Unity instantiates a documented fallback placeholder and displays a warning.

## Requirement 6: Robot Asset Legal and Technical Validation

### User Story

As a project maintainer, I want robot assets to be legally and technically validated, so that the project can be shared safely and implemented reliably.

### Acceptance Criteria

#### Scenario 1: Asset attribution

GIVEN a third-party robot asset is selected  
WHEN it is imported into Unity  
THEN an attribution file records title, author/publisher, source, license, download date, redistribution status, and modifications.

#### Scenario 2: Rig validation

GIVEN the selected robot asset is imported  
WHEN the validation checklist runs manually or via editor test  
THEN idle and locomotion animation states must be verified or a documented fallback must exist.

#### Scenario 3: Non-redistributable asset

GIVEN a third-party asset license does not allow repository redistribution  
WHEN the project is committed  
THEN the raw asset files are excluded and only import instructions/wrappers are committed.

## Requirement 7: Movement Visualization

### User Story

As an observer, I want robots to move according to simulation events, so that spatial relationships are visible.

### Acceptance Criteria

#### Scenario 1: Move to target

GIVEN Unity receives `agent.move` with a valid target position  
WHEN the message is applied  
THEN the robot moves toward the target and faces the movement direction.

#### Scenario 2: Invalid position

GIVEN Unity receives a movement event with NaN, infinite, or out-of-bounds coordinates  
WHEN validation runs  
THEN Unity rejects or clamps the transform according to config and emits `unity.error` or `unity.warning`.

#### Scenario 3: Out-of-order move

GIVEN Unity receives a movement event with a sequence lower than the last applied sequence  
WHEN sequence policy is strict  
THEN Unity skips the event and records an out-of-order warning.

## Requirement 8: Dialogue and Emotion Rendering

### User Story

As an observer, I want speech and emotional state to appear near robots, so that I can understand discussions and conflicts.

### Acceptance Criteria

#### Scenario 1: Dialogue bubble

GIVEN Unity receives `agent.dialogue`  
WHEN the target agent exists  
THEN Unity displays a speech bubble with the text for the configured duration.

#### Scenario 2: Long dialogue

GIVEN a dialogue message exceeds the configured character limit  
WHEN Unity renders it  
THEN Unity truncates or scrolls according to config without blocking the scene.

#### Scenario 3: Emotion indicator

GIVEN Unity receives `agent.emotion` with emotion `angry` and intensity `0.8`  
WHEN the event is applied  
THEN the avatar displays the configured angry visual indicator.

## Requirement 9: Group and Conflict Visualization

### User Story

As a researcher, I want group formation and conflict intensity visible, so that I can inspect social dynamics.

### Acceptance Criteria

#### Scenario 1: Group assignment

GIVEN Unity receives group membership updates  
WHEN robots are visible  
THEN Unity displays group color, badge, or label.

#### Scenario 2: Conflict escalation

GIVEN conflict intensity crosses the configured threshold  
WHEN Unity receives `conflict.update`  
THEN Unity displays conflict state with non-graphic visual cues.

#### Scenario 3: Conflict de-escalation

GIVEN conflict intensity falls below threshold  
WHEN Unity receives `conflict.update`  
THEN Unity removes or softens conflict visual cues.

## Requirement 10: MuJoCo Physical Event Request

### User Story

As a simulation developer, I want physical interaction events evaluated by MuJoCo, so that pushing, stumbling, and falling have consistent physical outcomes.

### Acceptance Criteria

#### Scenario 1: Push event routed to MuJoCo

GIVEN the text simulation emits a physical action intent `push`  
WHEN MuJoCo is enabled  
THEN the bridge creates a `physics.request` with actor, target, intensity, positions, and constraints.

#### Scenario 2: MuJoCo disabled

GIVEN the text simulation emits `push`  
WHEN MuJoCo is disabled  
THEN the bridge emits a deterministic fallback `physics.result` and marks it as fallback.

#### Scenario 3: MuJoCo timeout

GIVEN a MuJoCo request exceeds the configured timeout  
WHEN the bridge handles the timeout  
THEN the bridge emits `physics.error`, uses fallback if enabled, and does not block unrelated Unity events.

## Requirement 11: MuJoCo Physical Result Rendering

### User Story

As an observer, I want physical outcomes rendered in Unity, so that conflict events have visible consequences.

### Acceptance Criteria

#### Scenario 1: Fall outcome

GIVEN Unity receives `physics.result` with outcome `fall`  
WHEN the target avatar exists  
THEN Unity plays the configured fall animation or fallback pose.

#### Scenario 2: No-contact outcome

GIVEN Unity receives `physics.result` with outcome `no_contact`  
WHEN the event is applied  
THEN Unity does not play a fall animation and may show a missed-contact indicator.

#### Scenario 3: Unknown outcome

GIVEN Unity receives an unknown physical outcome  
WHEN the event is applied  
THEN Unity falls back to idle and reports `unity.warning`.

## Requirement 12: Replay Logging

### User Story

As a researcher, I want every accepted bridge event logged, so that I can replay and analyze simulations.

### Acceptance Criteria

#### Scenario 1: Event append

GIVEN the bridge accepts a valid event  
WHEN replay logging is enabled  
THEN the event is appended to a session JSONL log.

#### Scenario 2: Replay load

GIVEN a valid JSONL replay log  
WHEN replay mode starts  
THEN the bridge streams events in recorded sequence order.

#### Scenario 3: Corrupted replay line

GIVEN a replay file contains one invalid line  
WHEN replay validation runs  
THEN validation reports the line number and does not silently ignore the corruption.

## Requirement 13: Observer Control

### User Story

As an observer, I want to pause, resume, step, and select agents, so that I can inspect behavior without changing the simulation.

### Acceptance Criteria

#### Scenario 1: Pause

GIVEN the simulation is running  
WHEN Unity sends `observer.pause`  
THEN the bridge pauses event emission and records the control event.

#### Scenario 2: Step

GIVEN the simulation is paused  
WHEN Unity sends `observer.step`  
THEN the bridge emits exactly one simulation tick or one replay event according to mode.

#### Scenario 3: Select agent

GIVEN the observer selects `agent_001` in Unity  
WHEN Unity sends `observer.select_agent`  
THEN the bridge may return inspectable public agent state but not hidden prompts or private chain data.

## Input Requirements

## Requirement 14: Input Validation

### User Story

As a maintainer, I want all external inputs validated, so that malformed messages do not crash the system.

### Acceptance Criteria

#### Scenario 1: Oversized payload

GIVEN a message larger than the configured max payload size  
WHEN the bridge receives it  
THEN the bridge rejects it and records a warning.

#### Scenario 2: Unknown field in strict mode

GIVEN a message contains unknown fields  
WHEN strict validation is enabled  
THEN validation fails.

#### Scenario 3: Invalid enum

GIVEN a message contains emotion `super_angry_v999` not in the allowed enum  
WHEN validation runs  
THEN validation fails with a field-specific error.

## Output Requirements

## Requirement 15: Output Determinism

### User Story

As a researcher, I want deterministic replay outputs, so that experiments can be reviewed reliably.

### Acceptance Criteria

#### Scenario 1: Same input replay

GIVEN the same replay log and same config  
WHEN replay is run twice  
THEN Unity receives the same ordered message sequence.

#### Scenario 2: Fixed seed physics

GIVEN a MuJoCo physical event with fixed seed and identical inputs  
WHEN it is evaluated twice  
THEN the high-level physical outcome is identical.

## Configuration Requirements

## Requirement 16: Configurable Local Runtime

### User Story

As a developer, I want local runtime settings in configuration files, so that the system can run without code changes.

### Acceptance Criteria

#### Scenario 1: Default config

GIVEN only `configs/bridge.example.yaml`  
WHEN the bridge starts  
THEN it binds to localhost and uses safe defaults.

#### Scenario 2: Environment override

GIVEN `BRIDGE_PORT=9000`  
WHEN the bridge starts  
THEN it uses port `9000`.

#### Scenario 3: Invalid config

GIVEN `MUJOCO_ENABLED=true` and no MuJoCo model path  
WHEN the bridge starts  
THEN startup fails with a clear config validation error.

## Error Handling Requirements

## Requirement 17: Structured Errors

### User Story

As a developer, I want structured errors across Python and Unity, so that failures can be debugged quickly.

### Acceptance Criteria

#### Scenario 1: Python schema error

GIVEN a malformed bridge message  
WHEN Python validation fails  
THEN the error includes source, severity, field path, correlation ID, and recoverability.

#### Scenario 2: Unity apply error

GIVEN Unity cannot apply a valid event due to missing scene object  
WHEN the error occurs  
THEN Unity sends `unity.error` and continues processing later events when safe.

## Testing Requirements

## Requirement 18: Verification Coverage

### User Story

As a maintainer, I want requirements mapped to tests, so that completion cannot be faked.

### Acceptance Criteria

#### Scenario 1: Requirement coverage

GIVEN implementation is marked complete  
WHEN the verification checklist is reviewed  
THEN every requirement has at least one automated or manual verification item.

#### Scenario 2: Command report

GIVEN an AI coding agent completes a task  
WHEN it reports completion  
THEN it lists exact commands run and results.

## Performance Requirements

## Requirement 19: MVP Runtime Performance

### User Story

As an observer, I want the MVP to remain interactive, so that 3D visualization is usable.

### Acceptance Criteria

#### Scenario 1: Agent count

GIVEN a scenario with 20 agents  
WHEN Unity renders the scene  
THEN it maintains at least 30 FPS on the target development machine after assets are loaded.

#### Scenario 2: Event throughput

GIVEN 10 events per second for 60 seconds  
WHEN Unity is connected locally  
THEN the bridge delivers events without unbounded memory growth.

#### Scenario 3: Physics isolation

GIVEN a MuJoCo request is slow  
WHEN other non-physical events arrive  
THEN the bridge does not block unrelated event delivery longer than the configured policy allows.

## Security Requirements

## Requirement 20: Local-First Security

### User Story

As a user, I want the bridge to be local-first and private, so that simulation data is not exposed unintentionally.

### Acceptance Criteria

#### Scenario 1: Localhost default

GIVEN default config  
WHEN the bridge starts  
THEN it listens only on localhost.

#### Scenario 2: Remote binding

GIVEN config requests non-localhost binding  
WHEN the bridge starts  
THEN it requires explicit `allow_remote_clients=true`.

#### Scenario 3: Sensitive logging

GIVEN debug logging is disabled  
WHEN dialogue and private agent metadata are processed  
THEN logs do not include raw private metadata beyond approved event summaries.

## Compatibility Requirements

## Requirement 21: Unity and MuJoCo Compatibility

### User Story

As a developer, I want clear compatibility targets, so that implementation does not depend on unknown versions.

### Acceptance Criteria

#### Scenario 1: Unity version

GIVEN the Unity project is opened  
WHEN the editor version is checked  
THEN it must be Unity 2022.3 LTS or selected Unity 6 LTS unless an ADR changes the target.

#### Scenario 2: MuJoCo Python service

GIVEN MuJoCo mode is enabled  
WHEN the service starts  
THEN it must load the configured MJCF model or fail before scenario start.

## Documentation Requirements

## Requirement 22: Documentation Updates

### User Story

As a future AI coding agent, I want documentation kept current, so that I can implement safely.

### Acceptance Criteria

#### Scenario 1: Protocol change

GIVEN a message schema changes  
WHEN implementation is completed  
THEN `design.md`, tests, and changelog are updated.

#### Scenario 2: Asset change

GIVEN the selected robot asset changes  
WHEN implementation is completed  
THEN `docs/robot-asset-selection.md` and attribution instructions are updated.
