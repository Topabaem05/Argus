# MiniBot Backend Contract

## Bridge Flow

README describes the Unity bridge flow as:

```txt
Unity connects with unity.ready
Client calls /simulation/start
Bridge streams agent.spawn, simulation envelopes, agent.move, physics.result
Unity applies and acknowledges events
```

In Unity, the flow is:

```txt
BridgeReceiver
-> SimulationSceneOrchestrator
-> AgentSpawnHandler
-> AgentMoveHandler
-> AgentLocomotionDriver
```

## `agent.move` Contract

Argus standardizes `agent.move` as a target-position movement command.

```txt
agent.move = "move this MiniBot toward this world-space target"
```

Unity owns how movement happens.

| Field | Meaning |
| --- | --- |
| `agent_id` | MiniBot/agent to move. |
| `target_position` | World-space target position. |
| `speed_mps` | Desired movement speed in meters per second before Unity clamping/scaling. |
| `intent` | Optional semantic intent: `walk`, `approach`, `avoid`, `idle`. |
| `duration_ms` | Optional estimated duration. |
| `emotion` | Optional persona state for embodiment. |
| `sequence` | Envelope sequence ordering. |
| `sent_at_ms` | Event timestamp. |

## Unity Responsibilities

Unity must:

- Match `agent_id` to a spawned MiniBot.
- Ground `target_position` to the Unity floor.
- Smooth movement and turning.
- Stop within a configured arrival distance.
- Compute Animator `Speed` from actual transform delta.
- Compute Animator `Turn` from actual yaw/heading delta.
- Keep `Animator.applyRootMotion` OFF for bridge-driven agents.

## Forbidden Meanings

Do not use `agent.move` as:

```txt
teleport command
animation playback command
raw physics force command
raw velocity command
direct transform snapshot
```

Those meanings make walking QA ambiguous because Unity cannot tell whether a failure is motion, animation, physics, or bridge contract.

## Diagnostics

`MiniBotAutoDump` writes the last bridge event to:

```txt
reports/unity_dumps/bridge_event_dump.json
```

For `agent.move`, check:

- `last_event_type == "agent.move"`
- `agent_id` is not empty
- `target_position` has three coordinates
- `event_applied_to_minibot == true`
- warnings are empty

Then compare with:

```txt
reports/unity_dumps/minibot_runtime_trace.jsonl
```

If `distance_to_target` decreases but `animator_speed` stays at zero, the bridge contract is reaching Unity but the Animator connection is broken.
