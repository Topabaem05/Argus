# Mixamo Motion System

Argus minibots use a Unity-side motion layer so bridge/persona events select varied Mixamo motions without letting animation drive navigation. The backend still sends deterministic intent, target, evaluation, and physics events; Unity owns movement smoothing, clip selection, Animator parameters, and stuck recovery.

## Import Settings

Place local FBX files in:

```txt
unity/EmbodiedDebate/Assets/Project/Resources/Animations/Mixamo/Raw/
```

That directory is intentionally ignored by Git. Do not commit raw Mixamo files unless redistribution is explicitly allowed.

`MixamoMotionImportPostprocessor` applies these settings for files in that folder:

- Rig: Humanoid, animation import enabled, no cameras/lights.
- Root motion for navigation-critical clips: baked into pose so code/physics drive position.
- Loop enabled for idle and continuous locomotion clips such as `Standing Idle`, `Breathing Idle`, `Walking-3`, `Running-2`, strafe, backward, and arc-backward walk clips.
- One-shot actions such as `Push`, `Picking Up`, `Falling Flat Impact`, `Getting Up`, and `Zombie Reaction Hit` stay non-looping.

## Local Controller Generation

The diverse Mixamo controller is generated locally from real FBX clips. It is not committed because it references ignored Mixamo assets.

Run this after placing the FBX files in `/Users/guribbong/Downloads/motion` or setting `ARGUS_MIXAMO_SOURCE_DIR`:

```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate \
  -executeMethod ArgusUnity.Editor.MixamoMotionControllerBuilder.BuildLocalDiverseMixamoSetup
```

This creates:

```txt
Assets/Project/Resources/Animations/Mixamo/Generated/
  MiniBotDiverseMixamo.controller
  MiniBotUpperBody.mask
```

`AgentLocomotionDriver` and `MinibotAnimatorDriver` prefer the generated controller resource when it exists, then fall back to the committed `MiniBotLocomotion.controller`. The generated controller contains named states for every `MotionCatalog` clip on Base, Upper Body Overlay, and Emotion Overlay layers, plus a `LocomotionBlendTree` using `MoveX` and `MoveZ`. The current local asset set has `Stop Walking.fbx`, not `Step Walking.fbx`, so the catalog uses the real stop clip instead of pretending a missing step-walk clip exists.

`MiniBotRunAround` also uses this generated controller for the video capture path. The capture still keeps `MinibotMovementController` as the root movement authority because that controller has the anti-crab-walk facing guard, but the visible animation is driven through `MinibotAnimatorDriver` and deterministic `MotionSelectionPolicy` selections. The legacy `MiniBotWalkAnimator` is not added by the RunAround builder.

## Runtime Data Flow

```txt
Argus bridge event / Persona state / Local AI state
        -> MotionIntent
        -> PersonaMotionMapper
        -> MotionSelectionPolicy
        -> MinibotAnimatorDriver
        -> Animator Controller + overlay layers
        -> Smooth visual motion
```

Movement remains separate:

```txt
SmoothRigidbodyMotor = real movement, Rigidbody.MovePosition/MoveRotation, obstacle probing
MinibotAnimatorDriver = visual parameters, triggers, optional state crossfades
```

For the RunAround video, movement is externally sampled and speed-limited by `MinibotMovementController` instead of `SmoothRigidbodyMotor`; `MinibotAnimatorDriver.SetExternalKinematicState()` receives the actual kinematic velocity and turn value so Mixamo clips follow the rendered root motion without owning navigation.

## Animator Structure

Expected controller structure:

```txt
Base Layer
  Idle / Idle Blend
  Locomotion 2D Blend Tree
    Walking-3
    Running-2
    Walking Backward
    Left Strafe Walking
    Right Strafe Walking
  Turn In Place
    Left Turn
    Left Turn-2
    Right Turn
    Right Turn-180turn
  Recovery
    Step Backward
    Dodging
    Falling Flat Impact
    Getting Up
  Interaction Full Body
    Picking Up
    Push
    Pull Heavy Object

Upper Body Overlay Layer
  Talking
  Talking-2
  Yelling
  Waving
  Thoughtful Head Nod
  Hard Head Nod
  Shaking Head No
  Button Pushing
  Clapping

Emotion Overlay Layer
  Excited
  Surprised
  Sad Idle
  Look Around
```

The driver writes parameters only when they exist, so older controllers still run:

```txt
Speed, MoveX, MoveZ, Turn, AngularError, AngularSpeed,
IsMoving, Grounded, IsGrounded, IsTalking, IsStuck,
Emotion, Gesture, Action, MotionIntent, OverlayWeight,
GestureWeight, RecoveryState,
InteractionTrigger, TalkTrigger, EmotionTrigger, RecoveryTrigger,
DodgeTrigger, FallTrigger, GetUpTrigger, ImpactTrigger, StepBackTrigger
```

If states are named exactly like the Mixamo clip names, `MinibotAnimatorDriver` crossfades into them and raises overlay layer weights for talk/emotion clips. If a state is absent, the selected clip remains visible as a selected/debug value but is not reported as the currently applied clip. Locomotion still falls back to parameter-driven blend trees/triggers.

## Motion Intent Table

| Intent | Primary clips |
| --- | --- |
| `Idle` | `Standing Idle`, `Breathing Idle`, `Idle-2`, `Sad Idle`, `Thinking-2`, `Look Around` |
| `WalkForward` | `Walking-3` |
| `WalkBackward` | `Walking Backward`, `Walk Backward Arc Left`, `Walk Backward Arc Right` |
| `StrafeLeft` / `StrafeRight` | `Left Strafe Walking`, `Right Strafe Walking` |
| `Run` / `Charge` | `Running-2`, `Charge` |
| `TurnLeft` / `TurnRight` / `TurnAround` | `Left Turn`, `Left Turn-2`, `Right Turn`, `Right Turn-180turn`, `180 Turn W_ Briefcase` |
| `Talk` / `Explain` | `Talking`, `Talking-2`, `Waving`, nod clips |
| `Yell` / `Agree` / `Disagree` | `Yelling`, `Hard Head Nod`, `Thoughtful Head Nod`, `Shaking Head No` |
| `Excited` / `Sad` / `Surprised` | `Excited`, `Sad Idle`, `Surprised`, `Clapping` |
| `Push` / `Pull` / `PickUp` / `ButtonPush` | `Push`, `Pull Heavy Object`, `Picking Up`, `Button Pushing` |
| `StepBackward` / `Dodge` / `HitReaction` / `Fall` / `GetUp` | `Step Backward`, `Dodging`, `Zombie Reaction Hit`, `Falling Flat Impact`, `Getting Up` |

## Persona Trait Mapping

`PersonaMotionProfile` gives each agent deterministic trait weights:

- High `Energy`: prefers `Running-2`, `Excited`, `Waving`.
- High `Aggression`: prefers `Yelling`, `Hard Head Nod`, `Charge`, `Push`.
- High `Curiosity`: prefers `Look Around`, `Thinking-2`, `Picking Up`.
- High `Anxiety`: prefers `Step Backward`, `Dodging`, `Surprised`.
- High `Friendliness`: prefers `Waving`, `Clapping`, `Talking`; suppresses yelling.
- Low `Confidence`: prefers `Thoughtful Head Nod`, `Shaking Head No`, `Sad Idle`.

`MotionSelectionPolicy` is seeded per minibot, uses per-clip and per-category cooldowns, prevents immediate repeats when alternatives exist, and keeps the last five selected clips in debug state.

## Bridge Event Mapping

- `agent.spawn`: idle profile seeded from agent id.
- `agent.move`: `WalkForward`, `Run`, `Charge`, backward, or strafe intent based on speed/locomotion fields.
- `agent.behavior`: richer behavior payload to movement/action intent.
- `agent.dialogue` / `agent.animation` / `agent.emotion`: talk, explain, agree, disagree, wave, yell, think, or emotion overlay.
- `physics.result`: hit, fall/get-up, dodge, or step-back recovery.
- Local stuck detection: step backward or dodge, then resume instead of pushing into a wall.

## Debug Fields

`MinibotMotionDebugState` exposes:

- current intent
- selected base/overlay/emotion clips
- currently applied base/overlay/emotion clips when an Animator state exists
- current speed
- current turn
- stuck flag
- last recovery time
- last five used clips
- persona motion profile

The runtime dump also writes selected clip names into `reports/unity_dumps/physics_dump.json`.

## How To Add New Clips

1. Add the FBX to the local ignored Mixamo raw folder.
2. Add a `MotionClipId` enum value.
3. Add a `MotionCatalog` entry with clip name, category, loop flag, overlay preference, weight, and supported intents.
4. Run `MixamoMotionControllerBuilder.BuildLocalDiverseMixamoSetup` to regenerate local Animator states using the exact clip name.
5. Add a deterministic EditMode test when the clip changes selection behavior.

## Debugging Checklist

- If the bot slides: check `Speed`, `MoveX`, `MoveZ`, and code-driven speed in `SmoothRigidbodyMotor`.
- If only one idle repeats: check `MotionSelectionPolicy` cooldowns and `LastFiveUsedClips`.
- If talking cancels walking: move the talk state to the upper-body overlay layer and keep locomotion in Base.
- If recovery jitters: verify stuck detection triggers `Step Backward` or `Dodging` and that the motor is not still pushing into the wall.
- If clips do not visibly play: confirm controller state names match Mixamo clip names or wire transitions from the integer/trigger parameters.
- If replay is not deterministic: confirm `PersonaMotionProfile.Seed` is stable for the same `agent_id`.

## Common Problems And Fixes

| Problem | Fix |
| --- | --- |
| Animator state missing | Add a state with the exact clip name or rely on blend parameters/triggers. |
| Generated controller missing | Run `MixamoMotionControllerBuilder.BuildLocalDiverseMixamoSetup`; inspect `reports/unity_dumps/mixamo_controller_build.json`. |
| Raw FBX appears in Git | Keep `Mixamo/Raw/` ignored; commit only code/docs/controller metadata allowed by license. |
| Stuck bot keeps walking in place | Confirm `StuckDetector` thresholds and `MinibotStuckRecovery.LastRecoveryTime`. |
| Same gesture repeats | Inspect `LastFiveUsedClips`; alternatives may be missing from the relevant intent pool. |
| Run looks too fast for minibot scale | Lower bridge speed or keep minibot walk range around `0.45-0.65 m/s`. |
