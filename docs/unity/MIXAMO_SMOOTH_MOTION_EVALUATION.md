# Mixamo Smooth Motion Integration Evaluation

## Scope

Implemented the Unity-side motion runtime requested by `ARGUS_MIXAMO_SMOOTH_MOTION_PLAN.md` for `unity/EmbodiedDebate`.

The implementation keeps Argus deterministic/offline and treats Mixamo clips as visual assets. Navigation-critical movement is still code controlled.

## Implemented

- `MotionIntent`, `MotionEmotion`, `MotionGesture`, `MotionAction`, and `MinibotMotionState`
- `SmoothRigidbodyMotor`
  - `Rigidbody.MovePosition`
  - `Rigidbody.MoveRotation`
  - acceleration/deceleration smoothing
  - world-bounds clamp
  - simple obstacle spherecast/slide behavior
- `MinibotAnimatorDriver`
  - feeds smoothed local velocity into `MoveX`, `MoveZ`, `Speed`, `AngularSpeed`
  - feeds semantic `Emotion`, `Gesture`, `Action`, `GestureWeight`
  - disables root motion on the Animator
- `PersonaMotionMapper`
  - maps bridge behavior/dialogue/physics semantics into motion intents
  - normal walk speed is kept in the MiniBot range instead of human-scale running
- `BridgeMotionAdapter`
  - maps `agent.move`, `agent.behavior`, `agent.dialogue`, `agent.animation`, `agent.emotion`, and `physics.result`
- `StuckDetector` and `MicroBehaviorScheduler`
  - detects blocked movement conditions
  - provides deterministic idle/recovery intents
- `MinibotMotionDebugHud`
  - shows speed, target distance, state, emotion, gesture, action, and obstacle state
- Unity spawn path now attaches `MinibotMotionController` to bridge-spawned minibots.
- `AgentMoveHandler` now delegates to `MinibotMotionController` when present, instead of using transform-driven bridge movement.
- `MiniBotAutoDump` now reports motion runtime state in `physics_dump.json`.
- Local Mixamo import path:

```txt
unity/EmbodiedDebate/Assets/Project/Resources/Animations/Mixamo/Raw/
```

That path is ignored by Git to avoid redistributing raw third-party FBX files without an explicit license decision.

## Animator Parameters Supported

The runtime driver writes these parameters when they exist on the active controller:

```txt
MoveX
MoveZ
Speed
AngularSpeed
Grounded
IsMoving
IsStuck
Emotion
Gesture
Action
GestureWeight
ImpactTrigger
DodgeTrigger
FallTrigger
GetUpTrigger
StepBackTrigger
```

## Minimum Clip Mapping

The semantic motion library maps the first supported Mixamo set:

| Slot | Clip |
|---|---|
| IdleNeutral | Standing Idle |
| IdleBreathing | Breathing Idle |
| IdleThinking | Thinking-2 |
| WalkForward | Walking-3 |
| RunForward | Running-2 |
| WalkBackward | Walking Backward |
| StrafeLeft | Left Strafe Walking |
| StrafeRight | Right Strafe Walking |
| TurnLeft | Left Turn |
| TurnRight | Right Turn |
| Stop | Stop Walking |
| Talk | Talking |
| TalkAlt | Talking-2 |
| Nod | Thoughtful Head Nod |
| ShakeNo | Shaking Head No |
| LookAround | Look Around |
| StepBack | Step Backward |
| Hit | Zombie Reaction Hit |
| Fall | Falling Flat Impact |
| GetUp | Getting Up |

## Evaluation

The implementation satisfies the architectural part of the plan: bridge events are no longer required to directly teleport or directly select raw clip files. They become semantic motion intents, then the motor and Animator driver apply motion and visual state.

The raw FBX import was performed locally from:

```txt
/Users/guribbong/Downloads/motion
```

The copied Unity import folder contains 39 FBX files locally, but they are intentionally not committed.

Generated capture:

```txt
/Users/guribbong/code/Argus/tmp/mini_bot_run_working.mp4
```

Capture validation:

```txt
frames: 240
size: 1280x720
fps: 30
duration: 8.000000 seconds
frame hashes:
  0000 = 5ecb95455097a9bc398412a2339447af
  0080 = d60c8e00e89463421c4432cf914f7cd0
  0160 = 40d3ca6cd0649af166309358614f7e39
  0239 = 76daa3dcad7302996dac9fb8a17fdeec
```

Unity test validation:

```txt
EditMode: 100/100 passed
PlayMode: 5/5 passed
```

## Known Limitations

- The committed repository does not include raw Mixamo FBX files.
- The active controller still needs final artist tuning once the local FBX import has generated stable `.meta` GUIDs.
- Upper-body Avatar Mask and full 2D Freeform Directional Blend Tree generation are prepared by runtime parameters and importer settings, but the final controller asset should be tuned in Unity with the visible model.
- The first-pass motor uses a simple spherecast slide and stuck recovery; it is not a full NavMesh pathfinder.

## Validation Commands

```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate \
  -runTests \
  -testPlatform EditMode \
  -testResults /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/mixamo-motion-editmode-results.xml \
  -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/mixamo-motion-editmode.log
```

```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate \
  -executeMethod ArgusUnity.Editor.MiniBotScenarioBuilder.CaptureRunAroundVideo \
  -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/mixamo-motion-capture.log
```
