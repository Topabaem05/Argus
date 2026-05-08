# Risks

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

## Risk Register

| Risk ID | Risk | Impact | Likelihood | Mitigation | Detection |
|---|---|---:|---:|---|---|
| R-001 | Existing text simulation event format is different from assumptions | High | Medium | Inspect existing event output before implementation; create adapter analysis doc | Adapter tests fail; missing mapping table |
| R-002 | Unity robot asset is cute but not technically usable | High | Medium | Validate rig, animations, prefab, scale, and import before depending on it | Asset validation checklist fails |
| R-003 | Robot asset license does not allow redistribution | High | High | Do not commit raw asset unless license permits; store import instructions and attribution | Git review; license checklist |
| R-004 | Robot Kyle or fallback asset is not stylistically cute enough | Medium | Medium | Use as technical fallback only; keep cute Sketchfab candidate as primary if valid | Manual visual review |
| R-005 | MuJoCo humanoid physics is too expensive for many agents | High | Medium | Use MuJoCo only for selected physical events; use simplified MJCF bodies | Physics latency metrics |
| R-006 | Unity main thread freezes due to network handling | High | Medium | Queue network messages and process in Update; avoid blocking calls | Unity PlayMode stress test |
| R-007 | WebSocket messages arrive out of order or duplicate | Medium | Medium | Use sequence numbers, ACK tracking, and strict policy | Ack tracker tests; replay tests |
| R-008 | Replay logs become non-deterministic | High | Medium | Use fixed seeds, sorted keys, versioned schemas, golden tests | Golden test diff |
| R-009 | Bridge logs sensitive agent data | High | Medium | Use log allowlist and redact private metadata by default | Log review tests |
| R-010 | Remote clients can connect accidentally | High | Low | Bind localhost by default; require explicit remote flag | Config/security test |
| R-011 | MuJoCo timeout blocks Unity events | Medium | Medium | Run physics asynchronously; fallback on timeout | Timeout integration test |
| R-012 | Unity asset paths change after import | Medium | High | Use config-based prefab path and validation tests | Unity scene/prefab test |
| R-013 | Animator state names differ between assets | Medium | High | Use animation mapping config and fallback states | Animation mapper tests |
| R-014 | AI coding agent overbuilds full game systems | Medium | High | Keep tasks scoped to bridge/visualization/physics; enforce AGENTS.md | Code review; task checklist |
| R-015 | AI coding agent rewrites existing text simulation | High | Medium | Explicitly forbid unrelated rewrites; require minimal diffs | Git diff review |
| R-016 | AI coding agent claims tests passed without running them | High | Medium | Require exact commands and results in completion report | Completion report review |
| R-017 | Unity version mismatch causes package failures | Medium | Medium | Pin target Unity version; document alternatives | Unity batchmode failure |
| R-018 | NativeWebSocket or selected WebSocket package becomes incompatible | Medium | Low | Keep bridge protocol independent; allow package replacement behind interface | Unity compile/test failure |
| R-019 | MuJoCo Unity plug-in integration is harder than expected | Medium | Medium | Defer plug-in path; use Python MuJoCo service for MVP | ADR review; task dependencies |
| R-020 | Physical conflict visuals become too graphic | High | Low | Use non-graphic animation outcomes only; manual safety review | Manual visual review |
| R-021 | JSON payloads are too large for dense populations | Medium | Medium | Use event deltas, limits, and batching after MVP | Payload metrics |
| R-022 | Unity scene object pooling is missing | Medium | Medium | Add pooling if agent count exceeds target | FPS/performance smoke |
| R-023 | Golden fixtures include private or copyrighted text | High | Low | Use synthetic sample dialogue only | Fixture review |
| R-024 | Third-party asset attribution is incomplete | High | Medium | Require attribution file before acceptance | Manual checklist |
| R-025 | MuJoCo model does not match Unity visual robot | Low | High | Accept simplified physical model for MVP; document mapping | Design review |
| R-026 | Observer controls mutate simulation state incorrectly | High | Medium | Restrict controls to pause/resume/step/select; use allowlist | Observer control tests |
| R-027 | Lack of CI for Unity tests weakens verification | Medium | Medium | Document local batchmode commands and require reports | Missing Unity test report |
| R-028 | Physics fallback feels arbitrary to users | Medium | Medium | Mark fallback status and make it deterministic; document limitations | Replay/log inspection |
| R-029 | Asset Store account requirements block automation | Medium | High | Do not automate account-bound downloads; use local import procedure | Setup review |
| R-030 | Multiple agents speaking simultaneously clutter UI | Medium | High | Use bubble duration, stacking, priority, and selected-agent panel | Manual visual review |

## Technical Risks

The main technical risks are process synchronization, Unity main-thread constraints, message ordering, and MuJoCo performance. The MVP must avoid every-frame physics and use event-level physical evaluation.

## Product Risks

The system may look less like Sims than expected if avatar animation is too simple. The MVP should focus on understandable social visualization rather than cinematic animation.

## Performance Risks

Dense population scenarios can overload Unity if every text event becomes a visual update. Use event batching, throttling, prefab pooling, and level-of-detail after the communication loop is stable.

## Security Risks

The bridge processes simulation text and possibly demographic or personal-like agent data. It must bind to localhost by default, validate messages, limit payload size, and redact logs.

## Data Risks

Replay logs can accidentally contain sensitive text or private agent attributes. Golden tests must use synthetic fixtures. Real scenario logs should be stored locally and excluded from public commits unless reviewed.

## Dependency Risks

Unity packages, MuJoCo versions, and third-party robot assets can change. The architecture must keep dependencies behind adapters and document version assumptions.

## Testing Risks

Unity batchmode tests may not run on every developer machine. If Unity tests are not runnable, the completion report must say why and include manual checks performed.

## Maintenance Risks

Message schemas can drift between Python and Unity. Keep canonical schema docs, golden tests, and Unity DTO tests synchronized.

## AI Agent Failure Modes

### Agent modifies unrelated files

Mitigation:

- Require task-specific file list.
- Review git diff.
- Reject changes outside the task unless justified.

### Agent ignores requirements

Mitigation:

- Map each task to requirement IDs.
- Require completion report with requirements satisfied.

### Agent invents unsupported APIs

Mitigation:

- Use official docs or existing repository inspection before using an API.
- Add compile/test verification for Unity and Python.

### Agent removes tests

Mitigation:

- Forbid removing tests unless explicitly required.
- Review test deletions manually.

### Agent claims tests passed without running them

Mitigation:

- Require exact commands and results.
- Ask for test output artifacts when possible.

### Agent over-engineers beyond the spec

Mitigation:

- Implement one unchecked task at a time.
- Prefer minimal interfaces.
- Reject game systems not required for bridge MVP.

### Agent downloads or commits restricted assets

Mitigation:

- Require license review before import.
- Keep raw third-party assets excluded by default.
- Require attribution file.

### Agent rewrites the existing text simulation

Mitigation:

- Treat existing simulation as a black-box provider.
- Only implement adapter code unless a future spec explicitly changes the simulation engine.
