# MiniBot Expanded Animation Library Plan

> For agentic workers: REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

## Role

Act as a senior Unity gameplay programmer extending the mini-bot animation set without destabilizing the current locomotion Blend Tree, Rigidbody movement authority, or wall-stutter fixes.

## Goal

Import and document the next batch of user-provided FBX clips, then wire only the validated clips into the Unity mini-bot animation system. Walking and side-turn locomotion must remain smooth and code-driven; new action clips should be layered or state-driven only where they do not reset locomotion unexpectedly.

## Current Baseline

- Scene: `unity/EmbodiedDebate/Assets/Project/Scenes/MiniBotHideAndSeekDesign.unity`
- Builder: `unity/EmbodiedDebate/Assets/Editor/HideAndSeekDesignBuilder.cs`
- Rigidbody movement authority: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotHideAndSeekScenario.cs`
- Animator parameter authority: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/AgentLocomotionDriver.cs`
- Animator Controller: `unity/EmbodiedDebate/Assets/Project/Resources/Animations/Controllers/MiniBotLocomotion.controller`
- Current locomotion parameters: `Speed` and `Turn`
- Current locomotion assets:
  - `Idle`: `Assets/Project/Resources/UserModels/Idle.fbx`
  - `Walk_InPlace`: `Assets/Project/Resources/Animations/Locomotion/Walking-2.fbx`
  - `TurnLeft_Briefcase`: `Assets/Project/Resources/Animations/FBXTurns/Left Turn W_Briefcase.fbx`
  - `TurnRight_Briefcase`: `Assets/Project/Resources/Animations/FBXTurns/Right Turn W_Briefcase_Mirrored.fbx`

## Preview Acceptance Update

The generated preview video showed these clips are not production-safe:

- `Running.fbx`: rotates a full circle in the air.
- `Slow Run.fbx`: sways left and right.
- `Happy Right Turn-2.fbx`: rotates in the air.
- `Happy Right Turn.fbx`: operates in place and does not visually rotate.

Keep those clips imported for diagnostics, but quarantine them from `MiniBotLocomotion.controller`.
Use the visually accepted briefcase turns for production side-turn pose blending.

## New Source Assets

| Source file | Proposed role | Proposed resource destination | Initial clip name | Validation notes |
| --- | --- | --- | --- | --- |
| `/Users/guribbong/Downloads/Running.fbx` | Quarantined run diagnostic | `Assets/Project/Resources/Animations/Locomotion/Running.fbx` | `Run` | Preview failed: full-circle/in-air motion. |
| `/Users/guribbong/Downloads/Slow Run.fbx` | Quarantined slow-run diagnostic | `Assets/Project/Resources/Animations/Locomotion/Slow Run.fbx` | `SlowRun` | Preview failed: sways left and right. |
| `/Users/guribbong/Downloads/Running To Turn.fbx` | Quarantined run-turn candidate | `Assets/Project/Resources/Animations/Locomotion/Running To Turn.fbx` | `RunToTurn` | Keep out of continuous locomotion until a separate transition pass validates it. |
| `/Users/guribbong/Downloads/Left Turn W_Briefcase.fbx` | Production left-turn pose | `Assets/Project/Resources/Animations/FBXTurns/Left Turn W_Briefcase.fbx` | `TurnLeft_Briefcase` | Preview accepted. |
| `/Users/guribbong/Downloads/Left Turn W_Briefcase-2.fbx` | Preview alternate and mirrored right-turn source | `Assets/Project/Resources/Animations/FBXTurns/Left Turn W_Briefcase-2.fbx` and `Right Turn W_Briefcase_Mirrored.fbx` | `TurnLeft_Briefcase_Alt`, `TurnRight_Briefcase` | Original preview accepted; mirrored copy used for production right-turn pose. |
| `/Users/guribbong/Downloads/Thinking.fbx` | Non-locomotion emote | `Assets/Project/Resources/Animations/Actions/Thinking.fbx` | `Thinking` | Should not affect root movement; use upper-body layer if possible. |
| `/Users/guribbong/Downloads/Angry.fbx` | Non-locomotion emote | `Assets/Project/Resources/Animations/Actions/Angry.fbx` | `Angry` | Trigger from bridge emotion/action state, not from locomotion parameters. |
| `/Users/guribbong/Downloads/Male Laying Pose.fbx` | Pose/action state | `Assets/Project/Resources/Animations/Actions/Male Laying Pose.fbx` | `LayingPose` | Treat as full-body action; it should temporarily override locomotion. |
| `/Users/guribbong/Downloads/Standing Torch Light Torch.fbx` | Pose/prop action candidate | `Assets/Project/Resources/Animations/Actions/Standing Torch Light Torch.fbx` | `StandingTorch` | Validate prop dependencies; do not rely on redistribution rights for torch assets. |

## Architecture Decision

Keep one movement authority:

- Hide-and-seek agents stay Rigidbody-owned in `FixedUpdate`.
- Bridge-spawn visual agents stay transform-owned in `Update`, with spawned Rigidbodies kinematic.
- `AgentLocomotionDriver` remains the single owner of `Speed` and `Turn`.
- `Animator.applyRootMotion` remains off.

Animation expansion should split into two surfaces:

1. Locomotion layer:
   - Use a Blend Tree for continuous motion.
   - Add running only after loop settings and foot timing are validated.
   - Avoid `Any State -> RunTurn` style transitions for normal movement.

2. Action/emote layer:
   - Use explicit action state parameters or an override layer.
   - Keep action clips independent of `Speed` and `Turn`.
   - Do not let actions move the Rigidbody or root transform.

## Proposed Animator Parameters

Keep current:

```text
Speed : Float
Turn  : Float
```

Optional future action parameters:

```text
ActionState : Int
ActionWeight : Float
```

Do not add triggers for locomotion. If action clips need one-shot playback, prefer a state driver that can be reset by code and tested deterministically.

## Locomotion Blend Tree Expansion

Current Blend Tree should remain valid:

```text
Blend Type: 2D Freeform Cartesian
X: Turn
Y: Speed
```

Validated production placement:

| Motion | Turn | Speed |
| --- | ---: | ---: |
| Idle | 0 | 0 |
| Walk_InPlace | 0 | 1.0 |
| TurnLeft_Briefcase | -1 | 1.0 |
| TurnRight_Briefcase | 1 | 1.0 |

`SlowRun`, `Run`, `TurnLeft_Happy`, and `TurnRight_Happy` must stay out of the production Blend Tree until a later preview proves they are grounded and directionally correct.

## Data Flow

Rigidbody movement produces actual world displacement:

```text
Rigidbody position delta
-> AgentLocomotionDriver.LateUpdate()
-> actual planar Speed
-> actual yaw-rate Turn
-> Animator.SetFloat(..., dampTime, deltaTime)
-> LocomotionBlendTree
```

Action/emote flow should be separate:

```text
bridge event or scenario action intent
-> action state driver
-> ActionState / ActionWeight
-> action layer or action state
-> return to locomotion when action completes
```

## Task 1: Import Assets Safely

Files:

- Modify: `unity/EmbodiedDebate/Assets/Editor/HideAndSeekDesignBuilder.cs`
- Add files under: `unity/EmbodiedDebate/Assets/Project/Resources/Animations/Locomotion/`
- Add files under: `unity/EmbodiedDebate/Assets/Project/Resources/Animations/Actions/`
- Add files under: `unity/EmbodiedDebate/Assets/Project/Resources/Animations/FBXTurns/`

Steps:

- [x] Copy the nine source FBXs from `/Users/guribbong/Downloads` into the resource folders above.
- [x] Import all as Humanoid animation clips using the mini-bot avatar when valid.
- [x] Disable root motion on imported clips.
- [x] Enable loop time and loop pose only on continuous locomotion clips.
- [x] Keep action/pose clips non-looping unless preview proves they should loop.
- [x] Log imported clip name, avatar validity, human validity, loop status, and root-motion lock for each asset.

Failure behavior:

- Missing source FBX: log warning and skip that clip.
- Invalid avatar: log warning and do not overwrite the controller.
- Clip import succeeds but loop validation fails: keep the asset imported but skip Blend Tree wiring.

## Task 2: Preview and Classify Clips

Files:

- Add or extend EditMode/editor validation around `HideAndSeekDesignBuilder`.
- Optional output: `tmp/minibot-animation-import-report.md`

Steps:

- [ ] Generate a small report listing each clip's length, loop flag, root transform usage, and clip names found by Unity.
- [ ] Preview `Running.fbx`, `Slow Run.fbx`, and `Running To Turn.fbx` against `Walking-2.fbx`.
- [ ] Determine whether `Left Turn W_Briefcase-2.fbx` is actually a left-turn variant, a mirrored right-turn, or just an alternate take.
- [ ] Check whether briefcase/torch clips include prop bones or visible offsets that make them unsuitable for the mini-bot.

Decision rule:

- Continuous locomotion clips can go into the Blend Tree.
- One-shot motion clips should become action states or remain unused until a transition driver exists.
- Prop-contaminated clips should stay documented but not active.

## Task 3: Add Running Without Breaking Walking

Files:

- Modify: `unity/EmbodiedDebate/Assets/Editor/HideAndSeekDesignBuilder.cs`
- Modify: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/AgentLocomotionDriver.cs` only if normalization needs tuning.

Steps:

- [x] Keep the current `Idle -> LocomotionBlendTree -> Idle` transition structure.
- [x] Quarantine `SlowRun` and `Run` after preview failure.
- [x] Use briefcase turn clips for production side-turn pose blending.
- [x] Keep `Speed` based on actual displacement, not desired speed.
- [x] Tune `referenceMoveSpeed` or add a separate `maxRunReferenceSpeed` only if current speed normalization caps too early.
- [x] Confirm wall-blocked movement still reduces Animator `Speed`.

Validation:

- `Walking-2.fbx` still logs for normal walking.
- Briefcase turn clips log when `Turn` is left/right.
- Agents blocked at walls still reduce Animator `Speed`.

## Task 4: Add Action/Emotion Clips

Files:

- New or modified runtime driver, proposed: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/AgentActionAnimationDriver.cs`
- Existing action mapping reference: `unity/EmbodiedDebate/Assets/Project/Scripts/Robots/AnimationStateMapper.cs`

Steps:

- [ ] Map `Thinking.fbx` to an explicit thinking action.
- [ ] Map `Angry.fbx` to an explicit angry/emotion action.
- [ ] Map `Male Laying Pose.fbx` to a full-body pose state.
- [ ] Keep `Standing Torch Light Torch.fbx` disabled until prop dependency is reviewed.
- [ ] Add logs proving which action source is active.
- [ ] Ensure actions do not write `Speed` or `Turn`.

Failure behavior:

- Missing action clip: log warning and fall back to locomotion/idle.
- Action requested while moving: either ignore action, blend upper-body only, or stop movement based on scenario policy.

## Task 5: Regression Tests

Add or extend EditMode tests:

- [x] Import plan includes all nine source FBX names.
- [x] Locomotion controller still has `Speed` and `Turn`.
- [x] `AgentLocomotionDriver` remains the only writer for locomotion parameters.
- [x] Actual planar speed still ignores Y.
- [x] Wall-blocked velocity still damps movement speed.
- [x] Action clips are not inserted into the locomotion Blend Tree.
- [x] Quarantined run/happy clips are not inserted into the locomotion Blend Tree.

## Validation Commands

Run EditMode:

```bash
"/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath unity/EmbodiedDebate -runTests -testPlatform EditMode -testResults tmp/urp-minibot-editmode-results-expanded-animation.xml -logFile tmp/urp-minibot-editmode-expanded-animation.log
```

Rebuild scene:

```bash
"/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath unity/EmbodiedDebate -executeMethod ArgusUnity.Editor.HideAndSeekDesignBuilder.BuildScene -logFile tmp/hide-and-seek-expanded-animation-build.log
```

Capture Play Mode:

```bash
ARGUS_UNITY_VIDEO_CAPTURE=1 ARGUS_UNITY_VIDEO_DIR=/Users/guribbong/code/Argus/tmp/expanded_animation_frames ARGUS_UNITY_VIDEO_PREFIX=expanded_animation "/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath unity/EmbodiedDebate -executeMethod ArgusUnity.Editor.HideAndSeekDesignBuilder.OpenSceneInPlayMode -logFile tmp/hide-and-seek-expanded-animation-capture.log
```

Expected log checks:

```text
MiniBotLocomotion
Walking-2.fbx
TurnLeft_Briefcase
TurnRight_Briefcase
QUARANTINED
```

No log matches:

```text
CS####
NullReferenceException
MissingComponentException
Compilation failed
Test run failed
```

## Privacy, Licensing, and Asset Notes

- These FBX files are local user-provided assets under `/Users/guribbong/Downloads`.
- Do not assume redistribution rights.
- Keep attribution notes if the source is later known.
- Do not upload or publish the FBXs outside this local repo without explicit permission.

## Open Questions

- Does the mirrored `Left Turn W_Briefcase-2.fbx` production copy visually read as a right turn in the next preview video?
- Should `Running To Turn.fbx` be a continuous turn motion or a one-shot transition?
- Should `Thinking` and `Angry` be upper-body overlays while moving, or full-body actions that pause movement?
- Should `Male Laying Pose` be used in the hide-and-seek scene, or only in scenario/action demos?
- Should `Standing Torch Light Torch` ignore the torch prop, or should the prop be imported and shown?

## Recommended First Implementation Slice

Do this before touching action/emote states:

- [x] Import `Running.fbx` and `Slow Run.fbx`.
- [ ] Validate loop settings and foot timing.
- [x] Remove failed run/happy clips from the existing locomotion Blend Tree.
- [x] Add validated briefcase turns to the existing locomotion Blend Tree.
- [x] Run EditMode and Play Mode capture.
- [ ] Only then evaluate `Running To Turn` and the action clips.
