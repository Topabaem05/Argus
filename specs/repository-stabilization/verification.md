# Verification Plan

## Overview

Verification proves that the repository is no longer just a skeleton. The primary gate is the deterministic offline MVP. External services must not be required.

## Required Commands

```bash
uv sync --extra dev
uv run pytest
uv run ruff check .
uv run ruff format --check .
uv run mypy src
uv run kssim --help
uv run kssim validate-config --config examples/run_product_reaction.yaml
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
```

If `ruff format --check` is unavailable, run `uv run ruff format .` and report whether files changed.

## Artifact Checks

After dry-run, verify:

```txt
outputs/product_reaction_run_001/run_metadata.json
outputs/product_reaction_run_001/sample.json
outputs/product_reaction_run_001/profiles.json
outputs/product_reaction_run_001/plan.json
outputs/product_reaction_run_001/events.jsonl
outputs/product_reaction_run_001/metrics.json
outputs/product_reaction_run_001/metrics.csv
outputs/product_reaction_run_001/report.md
```

Validation:

- JSON files parse.
- Each JSONL line parses.
- Report includes `Limitations`.
- Metadata includes status and artifact paths.
- Artifacts contain no raw secret values.

## Required Unit Tests

- Config loader: valid, invalid YAML, missing file, extra field, env override, dry-run no secret, live requires secret.
- Fixture loader: success, missing file, empty file, invalid JSON, missing required fields.
- Hugging Face loader: dependency missing, mocked success, `trust_remote_code=False`.
- Sampler: deterministic, filters, insufficient rows, smaller sample allowed.
- Profile builder: one profile per persona, Korean rule, empty sample, unsafe profile blocked.
- Scenario registry/compiler: supported family, unknown family, default metrics, dry-run propagation.
- Safety: allowed product scenario, blocked political persuasion, blocked real-person profiling, blocked protected targeting, Korean blocked phrases.
- Dry-run: event count, run ID, turn values, invalid turns.
- Storage: write events, write metadata, overwrite protection, metrics JSON/CSV.
- Metrics: counts, unknown metrics, empty events.
- Report: required sections, partial/failed status, empty metrics, limitations.

## Integration Tests

- CLI `validate-config` success/failure.
- CLI `sample` writes sample.
- CLI `compile-scenario` writes plan.
- CLI `evaluate` writes metrics.
- CLI `report` writes Markdown.
- Pipeline run writes full artifact tree.

## Smoke Test

```bash
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
```

Must pass without network and without API keys.

## Regression Tests

Add tests for:

1. CLI only echoing.
2. unsupported example family.
3. env override extra field failure.
4. compiler hard-coded dry-run.
5. live events discarded.
6. Korean safety bypass.
7. report missing limitations.
8. storage overwriting existing run.

## Golden Tests

Golden fixtures should cover:

- report Markdown,
- metrics JSON,
- optionally deterministic sample JSON.

Normalize timestamps and temporary paths.

## Manual Review Checklist

- [x] Requirements are satisfied.
- [x] CLI does real work.
- [x] Offline run works without secrets.
- [x] Artifacts are complete.
- [x] Errors are typed and clear.
- [x] Safety fails closed.
- [x] Docs are truthful.
- [x] Optional integrations are optional.
- [x] Tests are deterministic.
- [x] Agent reported exact commands.

## Completion Report Format

```md
# Completion Report

## Summary
[what changed]

## Files Changed
[list]

## Requirements Satisfied
[list]

## Commands Run
[exact commands and results]

## Tests Added
[list]

## Known Limitations
[list]

## Follow-up Work
[list]
```
