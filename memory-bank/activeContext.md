# Active Context

## Current Mode

Plan phase for the Unity URP and Mini-bot wall-recovery addendum.

## Current Objective

Prepare the next build pass for the Unity visualization:

- migrate the Unity project to URP,
- replace Built-in `Standard` shader assumptions with URP-compatible shader selection,
- keep desks/furniture pushable when Mini-bots collide with them,
- stop Mini-bots from driving into walls or circling one bad spot,
- implement wall-contact recovery using a 90-degree turn with seeded random fallback.

## Source Materials

- `/Users/guribbong/Downloads/AI_Unity_MuJoCo_Bridge_Documentation.zip`
- Extracted review copy: `/tmp/argus_ai_unity_mujoco_bridge_docs`
- Main plan: `memory-bank/tasks.md`
- Current Argus source tree under `src/korean_social_simulator/`

## Important Repository Facts

- Argus is already a Python package named `korean_social_simulator`.
- Existing event contract is `SimulationEvent` in `src/korean_social_simulator/models.py`.
- Existing run artifacts are written through `RunStore` under the configured output directory.
- Existing CLI entrypoint is `kssim` in `src/korean_social_simulator/cli.py`.
- Existing dry-run mode must remain deterministic and network-free.
- Bridge, Unity, and MuJoCo dependencies must stay optional.

## Constraints To Preserve

- Argus text simulation is authoritative for cognition and social state.
- Unity may observe, pause, resume, step, and inspect allowlisted public state only.
- Unity must not rewrite hidden simulation state.
- MuJoCo is event-level and optional.
- Fallback physics is mandatory for local MVP.
- Third-party robot assets are not downloaded automatically and raw assets are not committed unless legally redistributable.
- Every build task needs tests and exact verification evidence.

## Current Decision State

- `memory-bank/tasks.md` exists and includes 22 requirement coverage rows and 32 QA fields.
- Creative decisions are being captured in `memory-bank/creative/creative-ai-unity-mujoco-bridge.md`.
- Next implementation phase after creative is `build`, starting with Wave 0 baseline/spec import and event contract analysis.
- `memory-bank/tasks.md` now includes a URP and Mini-bot wall-recovery addendum.
- The addendum's source-level build pass is implemented.
- Unity `2022.3.0f1` was found and used for this project.
- URP `14.0.7` is resolved and assigned through `ArgusUniversalRenderPipeline`.
- `MiniBotHideAndSeekDesign.unity` and `MiniBotRunAround.unity` were regenerated.
- Unity EditMode passed with 57 tests. PlayMode runner passed but discovered 0 tests.
