# Repository Stabilization Brief

## Feature Name

Repository Stabilization

## Summary

Stabilize Argus from a modular but partially wired MVP skeleton into a truthful, deterministic, offline-capable Korean Social Simulation Lab.

## User Problem

Before stabilization, the repository had useful modules, but the CLI was not fully wired, examples were inconsistent with the scenario registry, optional integrations were not cleanly separated, and verification was insufficient.

## Goals

1. Make every documented CLI command execute real behavior.
2. Fix example/registry mismatch.
3. Preserve deterministic offline operation.
4. Persist complete artifacts.
5. Keep live adapters optional.
6. Strengthen safety behavior.
7. Add tests that prevent fake completion.
8. Update documentation truthfully.

## Non-Goals

- No real-world prediction claims.
- No required live LLM calls.
- No required Concordia runtime for offline MVP.
- No web UI.
- No political persuasion, voter targeting, protected-group targeting, or real-person profiling.
- No real user data.

## Primary User Flow

1. Install with `uv sync --extra dev`.
2. Validate config.
3. Run `kssim run --config examples/run_product_reaction.yaml --dry-run`.
4. Inspect artifacts under `outputs/<run_id>/`.
5. Run tests, lint, and type checks.

## Expected Output

```txt
run_metadata.json
sample.json
profiles.json
plan.json
events.jsonl
metrics.json
metrics.csv
report.md
```

## Main Risks

- CLI regresses to placeholder behavior.
- Safety is bypassed.
- Reports overclaim.
- Live adapters break offline flow.
- Tests are claimed but not run.

## Completion Definition

Complete when all requirements in `requirements.md` pass, offline smoke test generates artifacts, tests/lint/type checks pass, and docs match actual behavior.
