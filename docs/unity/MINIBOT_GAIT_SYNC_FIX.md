# MiniBot Gait Sync Fix

## Problem

MiniBot legs animate and the body moves, but the foot cadence can drift from the distance traveled on the floor. This is gait mismatch, not a missing animation state.

## Affected Path

```txt
MiniBotRunAroundScenario
-> MinibotMovementController
-> MiniBotWalkAnimator
```

The bridge path still receives target-position movement commands. The RunAround capture path samples authored timeline targets and now speed-limits the actual transform movement before sampling the walk pose.

## Fix Rules

1. `MinibotMovementController.ApplyKinematicPose()` applies `maxSpeedMetersPerSecond` to the actual planar step.
2. `MiniBotRunAroundScenario.RegisterInteraction()` expands approach/disperse durations from planar distance.
3. Normal walk stays near `0.40-0.65 m/s`; fast walk stays below `0.85 m/s`.
4. `MiniBotWalkAnimator.metersPerWalkCycle` starts at `0.75` for the current small MiniBot scale.
5. `MiniBotWalkAnimator.minimumWalkCycleSeconds` starts at `1.0` so a 30 FPS walk cycle cannot visually restart before 30 rendered frames.
6. When `MiniBotRunAroundScenario` externally samples a distance-synced pose, that sampled pose is authoritative for the frame; `LateUpdate()` must not advance and apply the walk cycle a second time.
7. During approach/disperse, body facing follows locomotion direction. Partner gaze is applied when the bot reaches chat/react state.
8. `MinibotMovementController` treats actual applied movement as authoritative for body heading, so an accidental `look_at_partner` facing command cannot force a visible moving bot into a crab-walk pose.
9. Gait evidence is written to `reports/unity_dumps/minibot_gait_trace.jsonl`.

## Unity Reference Rules

This runtime keeps the MiniBot root controlled by script, not animation root motion:

- Use kinematic `Rigidbody.MovePosition()` and `Rigidbody.MoveRotation()` for the root object because Unity applies Rigidbody interpolation between rendered frames.
- Use Animator parameters only as visual representation of actual movement; `Speed` and turn values must be derived from applied delta, not requested targets.
- Keep root motion disabled for the current showcase path. Unity's Root Motion settings are useful for imported clips, but Argus currently uses distance-synced in-place animation so that backend movement remains deterministic.
- Do not delay root yaw while a visible planar step is being applied. If rotation easing is needed, apply it to an upper/visual layer or to idle/chat turn-in-place only; delaying the root while translating creates sideways walking.

Reference docs:

- [Unity Rigidbody.MovePosition](https://docs.unity.cn/2021.2/Documentation/ScriptReference/Rigidbody.MovePosition.html)
- [Unity Rigidbody.MoveRotation](https://docs.unity3d.com/ja/current/ScriptReference/Rigidbody.MoveRotation.html)
- [Unity Animator.SetFloat](https://docs.unity.cn/ScriptReference/Animator.SetFloat.html)
- [Unity Root Motion](https://docs.unity.cn/Manual/RootMotion.html)

## Acceptance Criteria

- Walk movement stays under `0.85 m/s` unless a run clip is active.
- Runtime trace shows `actual_step_meters` no greater than the configured allowed step, except initial placement.
- `cycle_rate_hz` remains below `2.0` during normal walking.
- `visual_cycle_rate_hz` remains at or below `1.0` for the BVH walk cycle unless the configured minimum cycle duration is changed.
- `externally_sampled_pose` is `true` for RunAround capture frames driven by `SampleDistanceSyncedPose()`, confirming the frame is not double-advanced by `LateUpdate()`.
- `heading_alignment_degrees` stays below `35` for visible walking steps over `0.012m` so a walking bot does not face sideways relative to its travel direction.
- Video review shows no obvious foot sliding over a five-second walking segment.
