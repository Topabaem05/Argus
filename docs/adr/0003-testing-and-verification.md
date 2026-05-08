# ADR 0003: Testing and Verification Strategy

## Status

Accepted

## Context

Argus can look complete while failing in practice if tests only check function existence or CLI text. The main risk is documented behavior not matching runtime behavior.

## Decision

Use layered tests:

1. Unit tests.
2. Integration tests.
3. Offline smoke tests.
4. Regression tests.
5. Golden tests.
6. Marked live tests.
7. Exact command reporting.

Offline tests must not require network or API keys.

## Consequences

Easier:

- CLI/documentation mismatch is caught,
- artifacts remain stable,
- safety behavior is verified,
- fake completion is harder.

Harder:

- fixture and golden data must be maintained,
- timestamps need normalization.

## Alternatives Considered

- only unit tests: rejected because CLI wiring is the highest risk.
- only manual tests: rejected because not repeatable.
- live tests as primary gate: rejected because slow and flaky.

## Validation

This strategy works when tests fail if the CLI only echoes input, examples use unsupported families, artifacts are missing, or safety-blocked scenarios pass.
