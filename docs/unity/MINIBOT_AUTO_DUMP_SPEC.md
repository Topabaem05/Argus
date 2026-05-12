# MiniBot Auto Dump Spec

## Purpose

MiniBot walking issues should be diagnosable from files, not only from visual inspection.

`MiniBotAutoDump` writes scene, prefab, animator, physics, bridge, and runtime trace state whenever `SimulationBootstrap` initializes. It also refreshes bridge/scene/physics dumps when `agent.spawn`, `agent.move`, or `physics.result` is applied.

## Output Location

```txt
reports/unity_dumps/
  scene_dump.json
  prefab_dump.json
  animator_dump.json
  physics_dump.json
  bridge_event_dump.json
  minibot_runtime_trace.jsonl
  gait_dump.json
  minibot_gait_trace.jsonl
  video_gait_review.json
```

## Scene Dump

```json
{
  "scene_name": "EmbodiedDebate",
  "minibot_count": 4,
  "bridge_receiver_count": 1,
  "ground_has_collider": true,
  "spawn_points": [
    {
      "name": "SpawnPoint_A",
      "position": [0, 0.1, 0]
    }
  ],
  "camera_count": 1,
  "warnings": []
}
```

Warnings include missing ground colliders, missing cameras, and incorrect BridgeReceiver counts.

## Prefab Dump

```json
{
  "prefab_name": "Idle",
  "has_rigidbody": true,
  "has_collider": true,
  "has_animator": true,
  "has_motor": true,
  "has_bridge_adapter": true,
  "rigidbody": {
    "use_gravity": false,
    "is_kinematic": true,
    "interpolation": "Interpolate",
    "constraints": "None"
  },
  "collider": {
    "type": "CapsuleCollider",
    "height_valid": true,
    "center_valid": true
  },
  "warnings": []
}
```

In current Argus, `has_bridge_adapter` means `AgentSpawnHandler` and `AgentMoveHandler` are initialized, not that the model prefab itself owns networking.

## Animator Dump

```json
{
  "animator_controller": "MiniBotLocomotion",
  "parameters": {
    "Speed": "Float",
    "Turn": "Float"
  },
  "states": ["Idle", "Walk"],
  "walk_clip_loop": true,
  "apply_root_motion": false,
  "warnings": []
}
```

Key warnings:

- Missing Animator Controller.
- Missing `Speed`.
- Missing `Turn`.

## Physics Dump

```json
{
  "agent_count": 1,
  "active_move_count": 1,
  "agents": [
    {
      "agent_id": "persona_001",
      "position": [0, 0, 0],
      "has_rigidbody": true,
      "has_collider": true,
      "rigidbody_is_kinematic": true,
      "rigidbody_use_gravity": false
    }
  ],
  "warnings": []
}
```

Use this to catch transform/Rigidbody ownership problems.

## Bridge Event Dump

```json
{
  "bridge_connected": true,
  "last_event_type": "agent.move",
  "agent_id": "persona_001",
  "target_position": [2.1, 0.0, -1.5],
  "speed": 0.45,
  "sequence_id": 128,
  "event_applied_to_minibot": true,
  "warnings": []
}
```

Warnings are emitted if an `agent.move` event is missing `agent_id` or `target_position`.

## Runtime Trace

```jsonl
{"frame":120,"agent_id":"persona_001","speed":0.42,"animator_speed":0.42,"turn":0.1,"distance_to_target":1.8,"state":"Walk"}
{"frame":180,"agent_id":"persona_001","speed":0.08,"animator_speed":0.08,"turn":0.0,"distance_to_target":0.05,"state":"Idle"}
```

Interpretation:

| Pattern | Meaning |
| --- | --- |
| `distance_to_target` decreases and `animator_speed > 0` | Movement and animation are connected. |
| `distance_to_target` decreases and `animator_speed == 0` | Animator parameter path is broken. |
| `distance_to_target` does not decrease | Movement target is not being applied or path is blocked. |
| `speed > 0` but character visually slides | Clip cadence or retargeting mismatch. |

## Gait Dump

RunAround capture also writes gait-specific files for the showcase movement path:

```json
{
  "gait_system": "MiniBotWalkAnimator",
  "movement_source": "timeline_showcase_speed_limited",
  "meters_per_walk_cycle": 0.75,
  "normal_walk_speed_range_mps": [0.4, 0.65],
  "fast_walk_speed_range_mps": [0.65, 0.85],
  "run_requires_run_clip": true,
  "warnings": []
}
```

`minibot_gait_trace.jsonl` records the visible, speed-limited movement:

```jsonl
{"frame":120,"agent_id":"B01","actual_speed_mps":0.58,"walk_speed_limit_mps":0.59,"distance_delta":0.019,"allowed_step_meters":0.020,"actual_step_meters":0.019,"meters_per_cycle":0.75,"cycle_rate_hz":0.77,"stride_warning":""}
```

Interpretation:

| Pattern | Meaning |
| --- | --- |
| `actual_speed_mps <= walk_speed_limit_mps` | Speed cap is controlling visible movement. |
| `cycle_rate_hz <= 2.0` | Foot cadence is still in a walk-like range. |
| `stride_warning == timeline_target_exceeded_speed_limit` | Timeline target is moving faster than the MiniBot can naturally walk. |
| `stride_warning == too_fast_for_walk` | The bot is moving or cycling too quickly for a walk clip. |

## Validation

After any MiniBot movement change:

```bash
git diff --check
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate \
  -runTests \
  -testPlatform EditMode \
  -testResults /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/minibot-auto-dump-editmode-results.xml \
  -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/minibot-auto-dump-editmode.log
```

For video/capture QA, also run:

```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate \
  -executeMethod ArgusUnity.Editor.MiniBotScenarioBuilder.CaptureRunAroundVideo \
  -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/minibot-auto-dump-capture.log
```
