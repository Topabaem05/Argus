# Existing Text Simulation Analysis

## Purpose

This document records the current Argus simulation event and artifact contract before implementing the Unity Bridge with Deterministic Physics. The bridge must adapt these outputs without changing their source semantics.

> **Note:** MuJoCo integration has been removed. The bridge now uses deterministic local fallback as the sole physics backend.

## Verification Run

Command run:

```bash
uv run kssim run --config /tmp/argus_bridge_contract_config.XXXXXX.yaml --dry-run
```

Result:

```txt
Run complete: bridge_contract_analysis_001 (success)
```

The temporary config was copied from `examples/run_product_reaction.yaml` and changed only to:

- `runtime.run_id: bridge_contract_analysis_001`
- `runtime.output_dir: /tmp/argus_bridge_contract_output`
- `runtime.overwrite: true`

Generated run directory:

```txt
/tmp/argus_bridge_contract_output/bridge_contract_analysis_001/
```

## Artifact Contract

The current pipeline writes these files for a dry-run:

```txt
events.jsonl
metrics.csv
metrics.json
plan.json
profiles.json
report.md
run_metadata.json
sample.json
```

Observed artifact sizes:

| Artifact | Size bytes |
|---|---:|
| `events.jsonl` | 14901 |
| `sample.json` | 7111 |
| `profiles.json` | 14565 |
| `plan.json` | 855 |
| `metrics.json` | 286 |
| `metrics.csv` | 132 |
| `report.md` | 2246 |
| `run_metadata.json` | 1973 |

`run_metadata.json` includes stable artifact path keys for events, metrics, plan, profiles, report, metadata, and sample artifacts.

## Event Model

Current source model: `SimulationEvent` in `src/korean_social_simulator/models.py`.

Fields:

| Field | Observed | Notes |
|---|---|---|
| `run_id` | yes | Run-level identifier. |
| `turn` | yes | Integer turn, `>= 0`. |
| `event_type` | yes | Literal event type. |
| `actor_id` | nullable | Present for agent observations. |
| `timestamp` | yes | ISO datetime string. |
| `payload` | yes | Event-specific dictionary. |

Declared event types:

```txt
observation
agent_action
gm_decision
metric_hook
safety_block
system
```

Observed dry-run event types:

| Event type | Count |
|---|---:|
| `system` | 10 |
| `observation` | 50 |
| `metric_hook` | 1 |

Observed phases:

| Phase | Count |
|---|---:|
| `turn_start` | 5 |
| `observation` | 50 |
| `turn_end` | 5 |
| `turn_limit_reached` | 1 |

Observed turns: `1, 2, 3, 4, 5`.

Observed actor count: `10`.

## Observed Payload Shapes

### `system`

Payload keys:

```txt
dry_run
observation_count
phase
plan_id
```

Bridge implication:

- `turn_start` and `turn_end` can become `simulation.event` or `simulation.snapshot` metadata.
- They should not become Unity movement, dialogue, emotion, or conflict events.

### `observation`

Payload keys:

```txt
display_name
dry_run
language
phase
```

Observed example:

```json
{
  "run_id": "bridge_contract_analysis_001",
  "turn": 1,
  "event_type": "observation",
  "actor_id": "agent-p-003",
  "payload": {
    "phase": "observation",
    "dry_run": true,
    "display_name": "40대 자영업자",
    "language": "ko"
  }
}
```

Bridge implication:

- This can safely seed `agent.spawn` or `simulation.event` because `actor_id`, `display_name`, and `language` are traceable.
- It cannot safely infer dialogue text, target agents, position, emotion, movement style, group, or conflict intensity.
- Missing visual fields should be filled only by explicit bridge defaults, not by invented social semantics.

### `metric_hook`

Payload keys:

```txt
max_turns
phase
```

Bridge implication:

- `turn_limit_reached` can become `simulation.event` or `replay.status`.
- It should not create avatar behavior.

## Agent Profile Contract

`profiles.json` contains:

```txt
agent_id
persona_uuid
display_name
language
background
memory_seeds
goals
behavior_rules
safety_notes
```

Bridge implication:

- `agent_id`, `display_name`, and `language` are safe public fields for Unity labels and default inspection.
- `background`, `memory_seeds`, `goals`, and `behavior_rules` should not be sent to Unity by default because they reveal hidden simulation setup and richer synthetic persona details.
- Agent inspection must use an allowlist.

## Plan Contract

`plan.json` includes:

```txt
run_id
plan_id
agent_count
max_turns
language
dry_run
scenario_spec
```

Bridge implication:

- `run_id` can become `session_id`.
- `plan_id` can become a stable correlation source.
- Scenario title and family can appear in bridge health or replay metadata.
- Scenario hypothesis should not be shown in Unity by default unless explicitly allowed.

## Initial Bridge Mapping

| Source | Safe bridge mapping | Notes |
|---|---|---|
| `system` with `phase == "turn_start"` | `simulation.event` | Marks turn start. |
| `system` with `phase == "turn_end"` | `simulation.event` | Include observation count. |
| `observation` | `agent.spawn` plus/or `simulation.event` | Use `actor_id`, `display_name`, `language`; no inferred position beyond deterministic layout default. |
| `metric_hook` with `phase == "turn_limit_reached"` | `replay.status` or `simulation.event` | Marks completion. |
| `agent_action` | adapter-specific | Not observed in dry-run; requires fixture before mapping. |
| `gm_decision` | adapter-specific | Not observed in dry-run; requires fixture before mapping. |
| `safety_block` | `bridge.error` or `simulation.event` | Not observed in dry-run; must preserve blocked reason without unsafe content. |

## Fields Missing For Full Unity Semantics

The current dry-run output does not provide:

- 3D positions,
- movement targets,
- facing direction,
- dialogue text,
- target agent IDs,
- emotion label or intensity,
- group ID,
- conflict ID,
- conflict stage or intensity,
- physical action intent,
- physics constraints (MuJoCo removed; deterministic local fallback only).

Implementation guidance:

- Add synthetic bridge golden fixtures for dialogue, movement, conflict, and physical events before requiring Argus to emit all of them.
- Keep the adapter strict. Unsupported event shapes should produce `adapter.error`.
- If deterministic layout defaults are used for spawn positions, document them as bridge visualization defaults rather than source simulation facts.

## Safety Notes

- The sample personas are synthetic, but profile details can still be sensitive in a visualization context.
- Unity should receive public display fields by default.
- Hidden prompts, memory seeds, behavior rules, and full backgrounds should be excluded from default bridge messages and selected-agent inspection.

## Next Implementation Guidance

1. Define bridge schema models with explicit source trace fields.
2. Build adapter tests from the observed dry-run output.
3. Add synthetic bridge fixtures for richer Unity-visible events.
4. Keep current Argus dry-run artifacts unchanged.
