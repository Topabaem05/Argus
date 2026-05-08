# ADR 0003: Multi-Layer Testing Strategy

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

# ADR 0003: Multi-Layer Testing Strategy

## Status

Proposed

## Context

This project spans a text simulation, Python bridge, WebSocket protocol, Unity rendering, robot assets, replay logging, and MuJoCo physics. A single end-to-end manual test would be fragile and insufficient. The testing strategy must prevent fake completion by AI coding agents and must verify boundaries independently.

## Decision

Use layered tests:

1. Python unit tests for schemas, adapter mapping, routing, and replay.
2. Python integration tests for WebSocket and MuJoCo service behavior.
3. Golden JSONL replay tests for deterministic event contracts.
4. Unity EditMode tests for message DTOs, mapping, and controller logic.
5. Unity PlayMode tests for spawn, movement, dialogue, emotion, animation, and connection lifecycle.
6. Manual visual checklist for robot asset import and simulation readability.
7. Completion reports with exact commands and results.

## Consequences

What becomes easier:

- Failures can be isolated to a subsystem.
- AI coding agents have explicit verification commands.
- Schema changes are caught by golden tests.
- Unity visuals are tested separately from Python logic.
- MuJoCo can be tested without Unity.

What becomes harder:

- Test infrastructure takes more setup.
- Unity batchmode test paths differ by OS and editor version.
- Some visual details still need manual review.
- Asset validation cannot be fully automated until assets are imported.

Tradeoffs accepted:

- Manual review remains required for model aesthetics and scene readability.
- Automated tests prioritize communication correctness, not cinematic quality.
- Golden files require deliberate updates when schemas change.

## Alternatives Considered

- Manual testing only.
  - Rejected because bridge protocols and replay behavior need deterministic validation.
- Unity-only tests.
  - Rejected because Python schemas and MuJoCo behavior need independent tests.
- Python-only tests.
  - Rejected because Unity scene behavior and asset import can fail independently.
- Full real-time performance tests in MVP.
  - Deferred until core communication and visualization pass.

## Validation

This strategy is working when:

- Every requirement maps to at least one automated or manual verification item.
- CI or local verification can run Python tests without Unity.
- Unity test reports are generated.
- Golden fixtures catch schema regressions.
- A completion report includes all commands run and their results.
