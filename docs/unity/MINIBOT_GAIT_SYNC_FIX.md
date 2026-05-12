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
5. Gait evidence is written to `reports/unity_dumps/minibot_gait_trace.jsonl`.

## Acceptance Criteria

- Walk movement stays under `0.85 m/s` unless a run clip is active.
- Runtime trace shows `actual_step_meters` no greater than the configured allowed step, except initial placement.
- `cycle_rate_hz` remains below `2.0` during normal walking.
- Video review shows no obvious foot sliding over a five-second walking segment.
