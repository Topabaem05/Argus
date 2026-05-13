# MiniBot Walk Fix

## 1. Problem

MiniBot walking failures usually come from a mismatch between backend bridge events and the Unity embodiment layer.

| Area | Common failure |
| --- | --- |
| Scene | MiniBot root is below the floor or colliding with the wrong ground object. |
| Prefab | Rigidbody, Collider, Animator, or runtime movement driver is missing. |
| Animator | Bridge movement changes position, but Animator `Speed` never changes. |
| Physics | Direct `transform.position` movement fights Rigidbody movement. |
| Backend Contract | `agent.move` is interpreted as teleport, animation command, velocity, or target position inconsistently. |
| Video/Model | Walk clip loop, avatar, root motion, or retargeting is wrong. |

## 2. Argus Unity Runtime

Argus currently routes bridge events like this:

```txt
BridgeReceiver
-> SimulationSceneOrchestrator
-> AgentSpawnHandler / AgentMoveHandler / MiniBotBehaviorHandler
-> AgentLocomotionDriver
-> Animator Speed / Turn
```

The intended split is:

```txt
Backend: decide where the agent should go and why.
Unity: move, rotate, ground, and animate the MiniBot.
```

## 3. Scene Checks

For `unity/EmbodiedDebate`, verify:

| Item | Expected state |
| --- | --- |
| Ground | Floor, ground, or terrain object has a Collider. |
| Spawn | Spawned MiniBot is grounded at `AgentSpawnHandler.BridgeFloorY`. |
| Camera | Camera can see full body motion and foot contact. |
| BridgeReceiver | Exactly one runtime receiver. |
| Agent handlers | `AgentSpawnHandler` and `AgentMoveHandler` are initialized by `SimulationBootstrap`. |
| Lighting | Shadows make grounding visible. |

If the bot slides or spins in place, check whether `AgentLocomotionDriver.LastAnimatorSpeed` stays near zero while the transform is moving.

## 4. Prefab / Runtime Components

The authored MiniBot model may not contain every runtime component. `SimulationBootstrap` and `AgentSpawnHandler` currently add/configure the bridge movement layer at runtime.

Required runtime structure:

```txt
MiniBot runtime object
├─ Visual model
├─ Animator
├─ optional Rigidbody / Collider
├─ AgentLocomotionDriver
└─ bridge route via AgentSpawnHandler + AgentMoveHandler
```

Recommended physics settings when a Rigidbody is present:

| Component | Expected value |
| --- | --- |
| Rigidbody Use Gravity | ON for physical characters, OFF for bridge visual agents if transform-driven. |
| Rigidbody Is Kinematic | OFF for Rigidbody motors, ON for transform-driven bridge visuals. |
| Interpolate | Interpolate |
| Constraints | Freeze Rotation X/Z |
| Animator Apply Root Motion | OFF for current Argus bridge movement |

## 5. Animator Checks

Minimum Animator parameters:

| Parameter | Type | Purpose |
| --- | --- | --- |
| `Speed` | Float | Idle/Walk blend from actual planar movement. |
| `Turn` | Float | Side turn blend from actual heading change. |

Current production controller:

```txt
Resources/Animations/Controllers/MiniBotLocomotion
```

Current runtime driver:

```txt
unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/AgentLocomotionDriver.cs
```

Rules:

- `Speed` must be based on actual position delta, not requested speed alone.
- `Turn` must be based on actual yaw/heading delta.
- `Animator.applyRootMotion` remains OFF for bridge-driven agents.
- Walk clips must loop cleanly.
- If feet slide, tune movement speed against the walk clip rather than using backend velocity as animation truth.

## 6. Quick Diagnosis

| Symptom | Likely cause | Check |
| --- | --- | --- |
| Position changes but legs do not move | `Speed` parameter missing or not written | `animator_dump.json`, runtime trace `animator_speed` |
| Bot rotates but does not walk | `agent.move` target missing or too close | `bridge_event_dump.json` |
| Bot slides | Movement speed and walk clip cadence mismatch | `minibot_runtime_trace.jsonl` |
| Bot jitters | Rigidbody and transform movement mixed | `physics_dump.json` |
| T-pose | Avatar/controller missing | `animator_dump.json` |
| Sinks into floor | Bounds, pivot, or collider center issue | `prefab_dump.json`, `physics_dump.json` |

## 7. Gait Sync Diagnosis

The current RunAround capture path is a gait synchronization problem, not a basic animation activation problem.

```txt
MiniBotRunAroundScenario
-> timeline target position
-> MinibotMovementController speed-limited kinematic pose
-> MiniBotWalkAnimator distance-synced gait
```

Rules for the showcase path:

- `maxSpeedMetersPerSecond` limits actual transform movement, not only reported speed.
- Interaction approach/disperse phases are expanded from planar distance and natural MiniBot walk speed.
- Normal walk should stay near `0.40-0.65 m/s`.
- Fast walk should stay below `0.85 m/s` unless a run clip is used.
- `MiniBotWalkAnimator.metersPerWalkCycle` starts at `0.75` so the feet do not cycle too quickly for the small model.
- `MiniBotWalkAnimator.minimumWalkCycleSeconds` starts at `1.0` so the visible walk cycle cannot restart before a 30 FPS one-second cycle completes.
- In RunAround, `SampleDistanceSyncedPose()` is authoritative for that rendered frame; `LateUpdate()` skips its own phase advance afterward to avoid applying two gait samples in one frame.
- During approach/disperse, the MiniBot body faces the movement direction. It turns to face the partner only after entering chat/react, so walking does not stage as a sideways crab walk.
- `MinibotMovementController` also guards against bad facing input: while a visible planar step is being applied, the final body rotation is derived from the actual speed-limited movement delta rather than a partner-gaze vector.
- Per Unity's kinematic Rigidbody guidance, `MovePosition`/`MoveRotation` provide the root transform movement with interpolation. Rotation smoothing must not leave the translating root pointed away from its applied movement delta.

Use `reports/unity_dumps/minibot_gait_trace.jsonl` to check:

| Field | Meaning |
| --- | --- |
| `actual_speed_mps` | Visible transform speed after speed cap. |
| `walk_speed_limit_mps` | Per-agent movement cap. |
| `actual_step_meters` | Applied planar movement this frame. |
| `allowed_step_meters` | Maximum allowed planar movement this frame. |
| `cycle_rate_hz` | Estimated walk cycles per second from movement distance. |
| `visual_cycle_rate_hz` | Visible BVH walk cycle rate after clamping. |
| `externally_sampled_pose` | Whether the frame used the distance-synced pose as its only gait authority. |
| `heading_alignment_degrees` | Angle between travel direction and body facing for visible walking steps. |
| `stride_warning` | `timeline_target_exceeded_speed_limit`, `visual_cycle_restarted_too_fast`, `body_facing_sideways_while_walking`, or `too_fast_for_walk` when cadence is suspect. |

## 8. Validation

Run Unity EditMode tests:

```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate \
  -runTests \
  -testPlatform EditMode \
  -testResults /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/minibot-walk-fix-editmode-results.xml \
  -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/minibot-walk-fix-editmode.log
```

Run the capture smoke:

```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate \
  -executeMethod ArgusUnity.Editor.MiniBotScenarioBuilder.CaptureRunAroundVideo \
  -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/minibot-walk-fix-capture.log
```

Expected outputs:

- `tmp/mini_bot_run_frames`
- `tmp/mini_bot_run_working.mp4`
- `reports/unity_dumps/*.json`
- `reports/unity_dumps/minibot_runtime_trace.jsonl`
