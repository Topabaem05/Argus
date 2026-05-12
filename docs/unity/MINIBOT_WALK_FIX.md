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

## 7. Validation

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
