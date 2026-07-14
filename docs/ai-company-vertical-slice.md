# AI Company Game — Vertical Slice Implementation Guide

> Project: Argus / `korean-social-simulator` + `unity/EmbodiedDebate`  
> Scope: one-session, real-time, 1–4 player company simulation  
> Prototype client binding: local player `p1`  
> Updated: 2026-07-15

## 1. What this slice delivers

This slice turns the existing AI-company backend and asset drop into a reproducible playable scene:

- four company offices in one shared city;
- one CEO minibot per company and three employee minibots per company;
- local WASD control for the `p1` CEO minibot;
- selectable employee minibots with stable server IDs (`emp-000` through `emp-011`);
- command HUD for praise, scold, snack, raise, bonus, party, fire, hire, gossip, scout and task assignment;
- local SLM decisions that can accept, reluctantly accept, complain, refuse or consider quitting;
- task, rumor, economy and phase events projected into minibot motion and HUD feedback;
- runtime-generated selection rings and mood dots, so the scene remains readable before final UI art arrives;
- an editor command that rebuilds and validates the complete scene.

The implementation is a **vertical slice**, not a finished 4-player release. Authoritative server routing, player-slot negotiation, final animation clips, audio and full game-state snapshots remain follow-up work.

## 2. Runtime flow

```text
Player input / employee click
        |
        v
CompanyGameHud + CompanyPlayerMiniBotController
        |
        v
CompanyGameCoordinator
        |
        v
game.player_command (strict bridge payload)
        |
        v
Python PlayerCommandSystem + SLMRuntimeAdapter
        |
        +--> accept / reluctant_accept / complain --> apply command
        |
        +--> refuse -------------------------------> no command mutation
        |
        v
task_update / rumor_event / economy_update / state_sync
        |
        v
CompanyEmployeeAgentController + MotionDriver + HUD
```

### Authority boundary

- Python remains authoritative for rounds, commands, economy, reputation and task outcomes.
- Unity owns presentation, local input, camera and temporary motion previews.
- The SLM is called on decision boundaries, never every frame.
- A model outage falls back to deterministic local behavior so a real-time session does not stall.

## 3. Minibot control architecture

| Component | Responsibility |
|---|---|
| `CompanyMiniBotActor` | Stable actor/company identity, role, mood, loyalty, current task, selection |
| `CompanyPlayerMiniBotController` | Camera-relative CEO movement using `CharacterController` |
| `CompanyEmployeeAgentController` | Employee destination movement and model-decision reactions |
| `CompanyMiniBotMotionDriver` | Animator parameter adapter plus procedural fallback motion |
| `CompanyMiniBotIndicator` | Selection ring and mood dot generated at runtime |
| `CompanyGameCoordinator` | Bridge event projection, command emission, local HUD state |
| `CompanyGameHud` | Minimal generated management HUD and command bar |
| `CompanyCameraController` | Quarter-view pan, zoom, rotate and local-player focus |

### Player and employee identity

Do not infer network identity from Unity object names. `CompanyMiniBotActor.ActorId` is the protocol identity.

| Scene role | ID pattern |
|---|---|
| CEO | `player-p1` … `player-p4` |
| Employee | `emp-000` … `emp-011` |
| Company/player slot | `p1` … `p4` |

`BuildGameScene` assigns employee IDs in the same order as `GameStateManager.new_game`, preventing a visual employee from receiving another employee's state.

## 4. Local SLM choice

The model profile registry is in `src/korean_social_simulator/ai/model_profiles.py`.

| Profile | Model | Intended deployment |
|---|---|---|
| `edge` | `Qwen/Qwen3.5-4B` | Default local dialogue and structured action decisions |
| `balanced` | `Qwen/Qwen3.5-9B` | Better dialogue quality on a stronger single-GPU host |
| `quality` | `Qwen/Qwen3.5-35B-A3B` | Dedicated local MoE inference server |

All three referenced Qwen model repositories declare Apache-2.0. The 4B profile is the default because this game needs short Korean JSON decisions with low latency more than long-form reasoning.

### Supported endpoints

`SLMRuntimeAdapter` supports OpenAI-compatible endpoints:

- `ollama`: default `http://127.0.0.1:11434/v1`
- `vllm`: default `http://127.0.0.1:8000/v1`
- `llamacpp`: default `http://127.0.0.1:8080/v1`
- `nim`: optional remote compatibility path
- `none`: deterministic offline simulation

Install the optional client dependency:

```bash
uv sync --extra llm
```

Run the CLI demo against a local endpoint:

```bash
export ARGUS_SLM_PROVIDER=llamacpp
export ARGUS_SLM_MODEL=qwen3.5-4b
export ARGUS_SLM_BASE_URL=http://127.0.0.1:8080/v1
uv run python -m korean_social_simulator.game.runner
```

The model string must match the name exposed by the selected runtime.

### Decision response contract

```json
{
  "action": "accept | reluctant_accept | refuse | complain",
  "dialogue": "short Korean dialogue",
  "efficiency": 0.0,
  "mood_change": 0,
  "side_action": null
}
```

Runtime validation clamps `efficiency` to `0..1`, `mood_change` to `-10..10`, rejects unknown enum values and limits dialogue length. Markdown fences and explanatory text around the first JSON object are tolerated.

### Real-time inference budget

For 2–5 employees per company, do not ask every employee to reason continuously. Queue inference only for:

1. direct player command;
2. rumor received;
3. major random event;
4. task completion/failure reflection;
5. end-of-round social conversation.

Batch or coalesce repeated state changes. A command should carry a state revision; discard a late response when its employee revision no longer matches the current authoritative state.

## 5. Motion specification

### Animator parameters

The driver works without an Animator Controller, but final clips should expose these parameters:

| Type | Name | Use |
|---|---|---|
| Float | `Speed` | `0` idle, `1` walk/carry |
| Bool | `IsCarrying` | Carry overlay/state |
| Bool | `IsWorking` | Desk, typing, cleaning or repair loop |
| Trigger | `Talk` | Neutral dialogue gesture |
| Trigger | `Cheer` | Praise, snack, raise, bonus, party, success |
| Trigger | `Complain` | Low mood, scold, task failure |
| Trigger | `Refuse` | Explicit command rejection |
| Trigger | `Gossip` | Whisper/social interaction |
| Trigger | `Quit` | Leave-company sequence |

### Required clip set

P0 clips needed before the first external playtest:

- idle loop with two variants;
- walk and faster walk/run;
- carry-light and carry-heavy loops;
- desk typing / paperwork;
- pick-up and put-down;
- talk-neutral, cheer, complain, refuse;
- whisper/gossip;
- quit/walk-away.

P1 polish clips:

- cleaning, repair, phone call, sales pitch;
- sick/tired, panic during event, rain umbrella;
- handshake, high-five, argument and team dinner.

All locomotion clips should be in-place. Python/Unity movement owns world displacement; importing root-motion displacement would double movement and desynchronize clients.

## 6. UI direction

The runtime HUD implements the agreed minimal, cute, cartoon direction without waiting for art assets:

- dark slate translucent cards;
- cyan primary accent, warm yellow economy accent, pastel company colors;
- persistent company card, round/phase chip and bridge status;
- bottom command bar that appears as the primary interaction surface;
- bottom-right recent-event toast;
- world-space mood dots and a selected-minibot ring.

Reference layout remains in `docs/game-ui-design-spec.md`. The IMGUI surface is a prototype. Replace it with uGUI or UI Toolkit prefabs only after the information architecture and input flow pass playtests.

## 7. Scene build and validation

Open `unity/EmbodiedDebate` in Unity 2022.3 LTS, then run:

```text
Argus > Build + Validate AI Company Vertical Slice
```

This rebuilds `Assets/Project/Scenes/AICompanyGame.unity` and checks:

- required minibot/office/city/task assets exist;
- 4 CEO actors and 12 employee actors exist;
- IDs are unique;
- exactly one local `p1` controller exists;
- all employees have an agent controller;
- all 16 minibots have motion drivers;
- bridge, coordinator, HUD and camera controller each exist exactly once.

Controls:

| Input | Action |
|---|---|
| WASD | Move local CEO minibot |
| Middle mouse drag | Pan camera |
| Mouse wheel | Zoom |
| Q / E | Rotate quarter-view camera |
| F | Focus local CEO |
| Click minibot | Select employee/actor |
| Escape | Clear selection |

## 8. Installed asset usage and remaining gaps

The current scene uses the repository's already-imported CC0 office and city GLB collection documented in `Assets/Project/Models/GameAssets/README.md`:

- office floors, walls, doors, windows, tables, shelf, lamp and retro computer;
- terrain, path, trees, benches, street lights and exterior structures;
- existing `FallbackRobot.prefab` as the common player/employee minibot body.

Runtime primitives supply the contract board, rumor kiosk, mood dots and selection rings. This keeps the slice functional without adding unreviewed binary art.

High-priority missing art:

- office chair, printer/copier, coffee/snack station, paper/contract props;
- warehouse rack, hand truck, parcel variants and delivery vehicle;
- four readable company signs and route markers;
- final 16-icon command set and nine-slice panels;
- impact/confetti/speech/rumor VFX;
- footsteps, UI feedback, office ambience and event audio.

See `Assets/Project/Models/GameAssets/ASSET_REGISTRY.md` for license and provenance gates.

## 9. Known protocol gaps

1. `GameStateSyncPayload` currently omits companies, employees, loyalty, mood, rankings and active orders. The Unity coordinator accepts an optional future `employees` array, but strict Python schema must be expanded before it can be transmitted.
2. `PlayerCommandPayload` currently omits command `payload`, so Unity does not send custom cost or rumor text. Defaults remain server-side.
3. `TaskUpdatePayload` lacks a world destination/anchor ID. Unity can show task state but cannot authoritatively route an employee to a specific scene anchor yet.
4. The prototype scene binds local input to `p1`. Session handshake must deliver the assigned player slot before enabling the matching CEO controller.
5. Unity bridge command receipt is not yet an idempotent request/response transaction. Add command result/error messages and deduplicate by `command_id`.

## 10. Next implementation order

1. Extend state sync and command-result schemas; add revision numbers.
2. Bind the local CEO controller from multiplayer session assignment.
3. Route task categories to scene anchors and authoritative employee movement intents.
4. Import/retarget the P0 animation set and build one Animator Controller.
5. Replace prototype IMGUI with prefab-based responsive UI after playtest feedback.
6. Add 4-client soak tests and simulate delayed/out-of-order messages.
7. Tune economy and SLM acceptance thresholds from recorded 20–30 minute sessions.
