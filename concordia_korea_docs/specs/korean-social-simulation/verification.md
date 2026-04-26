# Verification Plan

## Verification Overview

The MVP must be verifiable offline using fixtures and mocked providers. Live Hugging Face, LLM, and PageIndex tests are allowed but must be opt-in and skipped by default. No implementation can be marked complete unless commands were actually run or explicitly reported as unavailable with reasons.

## Required Commands

```bash
uv sync
uv run kssim --help
uv run pytest
uv run ruff check .
uv run ruff format --check .
uv run mypy src
```

End-to-end dry-run command after MVP implementation:

```bash
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
```

Report command after MVP implementation:

```bash
uv run kssim report --input outputs/product_reaction/run_001 --output outputs/product_reaction/run_001/report.md
```

## Unit Tests

Required unit test areas:

- Config validation:
  - required fields
  - environment overrides
  - secret redaction
  - invalid limits
- Persona loading:
  - valid fixture
  - missing field
  - invalid type
- Sampling:
  - deterministic seed
  - insufficient rows
  - invalid filters
- Agent profile builder:
  - required output fields
  - Korean behavior rules
  - golden rendering
- Scenario compiler:
  - supported families
  - unknown family rejection
  - default metrics
- RAG layer:
  - disabled mode
  - optional unavailable mode
  - required unavailable mode
- Safety validator:
  - allowed prevention scenario
  - political targeting blocked
  - real-user inference blocked
  - protected-group exploitation blocked
- Storage:
  - output path layout
  - overwrite protection
  - valid JSONL
- Metrics:
  - supported metrics
  - unsupported metric unavailable status
  - deterministic golden output
- Reporting:
  - complete report
  - partial report
  - no unsupported real-world prediction claim

## Integration Tests

Required integration tests:

- Fixture personas -> sample -> profiles -> scenario plan.
- Scenario plan -> safety validator -> dry-run simulation.
- Mocked PageIndex retrieval -> scenario compilation.
- Mocked Concordia/LLM adapter -> event log writer.
- Event log -> metrics -> markdown report.

## Smoke Tests

Minimum smoke tests:

```bash
uv run kssim --help
uv run kssim validate-config --config examples/run_product_reaction.yaml
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
```

Expected smoke result:

- Exit code 0.
- Run directory created under a temporary output path.
- `events.jsonl`, `metrics.json`, and `report.md` exist.
- Report includes safety limitations.

## Regression Tests

Behavior that must not break:

- Same seed produces the same sampled persona UUIDs.
- Safety guard blocks political persuasion targeting.
- RAG disabled mode does not initialize network clients.
- Dry-run mode does not call LLM providers.
- Generated reports never claim real-world prediction certainty.
- Existing golden metrics remain stable unless intentionally updated.

## Golden Tests

Golden inputs/outputs:

```txt
tests/golden/
├── agent_profiles/
│   ├── input_persona.json
│   └── expected_profile.json
├── scenario_plans/
│   ├── product_reaction.yaml
│   └── expected_plan.json
├── metrics/
│   ├── events.jsonl
│   └── expected_metrics.json
└── reports/
    ├── run_artifacts/
    └── expected_report.md
```

Golden update rule:

- Do not update golden files just to make tests pass.
- Golden updates require a changelog note explaining the intended behavior change.

## Manual Review Checklist

- Does the implementation match requirements?
- Are all scenario families either supported or explicitly rejected?
- Are edge cases handled?
- Are errors typed and explicit?
- Are logs useful and secret-safe?
- Are generated reports conservative about claims?
- Are political manipulation and real-user profiling blocked?
- Is RAG truly optional?
- Is fine-tuning absent from the MVP execution path?
- Is documentation updated?
- Are unrelated files untouched?
- Did the agent run the exact required commands?

## Completion Report Format

```md
# Completion Report

## Summary
[What was implemented]

## Files Changed
[List files]

## Requirements Satisfied
[List requirement IDs]

## Commands Run
[Exact commands and results]

## Tests Added
[List tests]

## Known Limitations
[List limitations]

## Follow-up Work
[List follow-up tasks]
```

## Live Test Policy

Live tests must be skipped by default and run only when explicitly requested:

```bash
uv run pytest -m live_hf
uv run pytest -m live_llm
uv run pytest -m live_pageindex
```

Live test results must never be required for offline MVP completion.
