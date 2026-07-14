# AI Company Game — Test and Playtest Plan

> Target session: 20–30 minutes, 1–4 players, 5 business-day rounds  
> Current vertical slice: local player `p1`, 4 companies, 3 employees per company

## 1. Definition of playable

A build is ready for an internal playtest only when all of the following are true:

- Unity opens with no compile errors;
- `Argus > Build + Validate AI Company Vertical Slice` passes;
- the local CEO minibot can move and the camera can pan/zoom/rotate;
- each of the 12 employee minibots is selectable and shows a unique employee ID;
- valid command buttons emit one bridge command and show immediate visual feedback;
- invalid commands are disabled or rejected locally (for example firing another company’s employee);
- a refused SLM decision does not mutate the command result;
- losing the local SLM process does not freeze the round;
- task, rumor, economy and phase events update the HUD without exceptions;
- five rounds complete and a final ranking is produced.

## 2. Automated Python checks

Run before every PR update:

```bash
uv sync --extra dev
uv run ruff check src tests
uv run mypy src
uv run pytest
```

Focused game checks:

```bash
uv run pytest \
  tests/unit/ai/test_slm_adapter.py \
  tests/unit/game \
  tests/unit/social \
  tests/unit/multiplayer \
  tests/unit/bridge_schema
```

Required assertions:

- deterministic fallback returns the same decision for the same prompt across processes;
- malformed/markdown-wrapped model output is recovered or safely downgraded;
- action, side-action, mood and efficiency ranges are validated;
- SLM refusal prevents `PlayerCommandSystem.execute`;
- command IDs remain unique;
- duplicate/out-of-order bridge messages do not apply twice;
- employee IDs created by the game state match Unity actor IDs;
- bankruptcy and final scoring are deterministic for a fixed seed.

## 3. Unity editor smoke test

1. Open `unity/EmbodiedDebate` in Unity 2022.3 LTS.
2. Wait for package/import completion.
3. Run `Argus > Build + Validate AI Company Vertical Slice`.
4. Open `Assets/Project/Scenes/AICompanyGame.unity`.
5. Enter Play Mode with the Python bridge stopped.
6. Confirm `오프라인 미리보기` is visible and the scene remains interactive.
7. Confirm:
   - WASD moves only `player-p1`;
   - Q/E rotates, wheel zooms, middle-drag pans and F focuses p1;
   - clicking all 16 minibots changes the selection ring;
   - employees display mood dots;
   - command buttons enable/disable according to ownership;
   - Escape clears selection;
   - no per-frame errors or missing-reference exceptions appear.

## 4. Connected single-client test

Start the Python bridge and selected local SLM runtime, then enter Play Mode.

Test each message path:

| Path | Trigger | Expected result |
|---|---|---|
| `state_sync` | Start/phase transition | Round and phase chip update |
| `player_command` | Click command button | One command packet with current round and selected employee ID |
| `task_update` | Assign/process a task | Employee task label and work/success/failure motion update |
| `economy_update` | Evening settlement | Funds and settlement toast update |
| `rumor_event` | Spread rumor | Source employees gossip, target employees complain |

Fault cases:

- stop the SLM server during a command;
- stop/restart the Python bridge;
- send an unknown message type;
- send a task update for a nonexistent employee;
- send a duplicate command/event;
- send a stale round number;
- return model prose before/after JSON;
- return out-of-range numeric values.

Expected behavior: no client lockup, no uncontrolled state mutation, and an actionable log entry.

## 5. Four-client test matrix

The current scene binds local movement to `p1`; complete player-slot binding before this test is considered passing.

Run four clients against one session and assign `p1` through `p4`.

| Scenario | Pass condition |
|---|---|
| Simultaneous movement | Each client controls only its assigned CEO |
| Same employee selected by two players | Selection remains local presentation state |
| Simultaneous commands to one employee | Server order/revision rule produces one deterministic state |
| Price-war bid at same timestamp | Server tie-break is deterministic and logged |
| Employee scouted while task is active | One owner, one task resolution, no duplicate salary |
| Rumor fan-out | Each employee processes one rumor revision once |
| Client disconnect/reconnect | Rejoined client receives full authoritative snapshot |
| Late packet | Stale revision is ignored |
| Round transition under latency | No command is applied to the wrong round |
| Host SLM slowdown | Round continues under configured decision deadline/fallback |

## 6. SLM behavior evaluation

Create a fixed evaluation set with at least:

- 5 personality profiles;
- moods at 10, 35, 50, 75 and 95;
- loyalty at -80, -20, 0, 30 and 80;
- all task categories and difficulty levels;
- truthful, uncertain and false rumors;
- command repetition and conflicting recent memories.

Measure:

| Metric | Initial gate |
|---|---|
| Valid structured response | >= 99.5% after runtime repair |
| Decision timeout rate | < 1% on target host |
| P95 command decision latency | Set after target hardware benchmark; must fit UI feedback budget |
| Same-state deterministic fallback | 100% |
| Refusal correctly blocks mutation | 100% |
| Dialogue repetition over one session | < 25% exact duplicate lines |
| Personality consistency | Human rating >= 4/5 |
| Clearly irrational command acceptance | Human rating <= 1/5 frequency target after baseline |

Do not tune solely for entertaining dialogue. The structured action must remain compatible with the deterministic economy and multiplayer rules.

## 7. Gameplay playtest script

### First five minutes

- Can a new player identify their company and CEO without explanation?
- Can they select an employee and issue a command within 30 seconds?
- Do mood, loyalty and current task communicate why a command was accepted or rejected?
- Does the camera preserve orientation while moving between companies?

### Mid-session

- Is there a meaningful choice between welfare spending and task throughput?
- Is gossip readable without requiring log inspection?
- Does scouting feel earned rather than random?
- Are rival actions visible enough to create a party-game reaction?
- Does waiting for model output interrupt play?

### End of session

- Can players explain why the winner won?
- Does the final company value expose its components?
- Did bankruptcy have warning signals?
- Were five rounds too short, too long or appropriate?
- Would players choose a different employee/personality strategy next time?

Record event logs, model decisions, decision latency, command counts, task outcomes and end-of-round state snapshots. Do not record microphone audio or personal chat unless players explicitly consent.

## 8. Performance budget

Baseline target for the prototype client:

- stable 60 FPS at 1080p on the target development machine;
- no regular managed allocations from HUD/actor Update loops;
- no SLM call on Unity main thread;
- pooled VFX and speech bubbles;
- at most one authoritative decision in flight per employee;
- configurable global inference concurrency based on model profile;
- bounded replay/event log size for a 30-minute session.

Profile after final models and Animator Controllers are imported. Procedural fallback motion is not representative of final animation CPU/GPU cost.

## 9. Current blockers before external testing

- full state snapshot schema and revision numbers;
- session-assigned local player binding;
- command result/error envelope and idempotency;
- task destination/anchor protocol;
- verified license/attribution for the existing robot and KayKit-named prototype assets;
- final P0 motion clips and Animator Controller;
- actual Unity Play Mode run on a machine with the required editor version;
- 4-client network soak test.

## 10. Bug report template

```text
Build/commit:
Unity version:
Python version:
SLM provider/model:
Players/round/phase:
Actor ID:
Command/event:
Expected:
Observed:
Reproduction rate:
Client log:
Server log:
Replay/event IDs:
Screenshot/video:
```
