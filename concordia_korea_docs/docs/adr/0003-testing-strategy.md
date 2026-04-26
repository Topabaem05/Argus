# ADR 0003: Testing Strategy

## Status

Proposed

## Context

LLM-based simulations are probabilistic and can be expensive. The project must prevent fake completion, unsafe scenario drift, broken metrics, and non-reproducible outputs. AI coding agents need exact verification commands and deterministic tests.

## Decision

Use a layered testing strategy:

1. Unit tests for deterministic modules.
2. Integration tests with local fixtures and mocked LLM/RAG providers.
3. Smoke tests for CLI and dry-run execution.
4. Golden tests for profile rendering, scenario compilation, metrics, and reports.
5. Safety regression tests for disallowed scenarios.
6. Optional live tests behind explicit markers for HF dataset, LLM provider, and PageIndex MCP.

## Consequences

What becomes easier:

- Most tests run without network or paid APIs.
- Safety rules remain enforceable.
- AI agents can verify each task locally.
- Golden tests detect unintended behavior changes.

What becomes harder:

- Mock adapters must be maintained.
- Golden outputs must be updated carefully when intended behavior changes.
- Live tests cannot be the default CI path.

Tradeoffs accepted:

- Deterministic tests are prioritized over full live realism.
- Live LLM quality is validated separately from core correctness.
- Some simulation outputs are evaluated structurally instead of semantically in MVP.

## Alternatives Considered

- Only manual testing: rejected because it enables fake completion.
- Live LLM tests in every run: rejected due to cost, latency, and nondeterminism.
- No golden tests: rejected because reports and prompts need stable reviewable outputs.
- Full end-to-end production tests first: rejected as too heavy for early development.

## Validation

This decision is working if:

- `uv run pytest` passes offline.
- Safety regression tests block prohibited scenarios.
- Golden tests catch unintended prompt/report changes.
- Live tests are opt-in with markers.
- Completion reports list exact commands and results.
