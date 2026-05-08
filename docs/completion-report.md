# Completion Report

## Summary

Implemented the repository-stabilization spec for the deterministic offline MVP. The CLI now executes real pipeline behavior for validation, sampling, scenario compilation, dry-run execution, evaluation, and report generation. The example config uses the supported `product_market` scenario family, strict config/env overrides are schema-aligned, run IDs are path-safe, dry-run mode propagates into plans, live adapter events are preserved, Korean and English safety phrases are blocked, and the full artifact tree is persisted.

## Files Changed

- Added root stabilization docs: `AGENTS.md`, `IMPLEMENTATION_PROMPT.md`, `REPOSITORY_ANALYSIS.md`, `SCORECARD_TO_10.md`, `DOCUMENTATION_SELF_REVIEW.md`, `FILE_INDEX.md`.
- Added repository-stabilization specs under `specs/repository-stabilization/`.
- Added ADRs under `docs/adr/`.
- Updated README, architecture, coding style, examples, config models/loader, CLI, pipeline orchestration, persona loading, scenario compiler, safety validator, simulation adapter contract, run storage, scripts, tests, and golden fixtures.

## Requirements Satisfied

- RS-001: CLI commands execute real pipeline behavior.
- RS-002: Example config uses supported scenario family.
- RS-003: Strict config schema and declared env overrides.
- RS-004: Persona source dispatcher for fixture/HF modes.
- RS-005: Existing deterministic sampling tests preserved.
- RS-006: Profile generation and safety tests preserved.
- RS-007: Scenario compilation propagates dry-run mode and records optional RAG warnings.
- RS-008: English and Korean prohibited phrases fail closed.
- RS-009: Dry-run remains offline and deterministic.
- RS-010: Live adapter returns events/status instead of discarding events.
- RS-011: Run store persists stable artifacts with overwrite protection.
- RS-012: Metrics include deterministic counts and placeholder values.
- RS-013: Reports include metrics, examples, safety, warnings, errors, limitations.
- RS-014: Verification commands were run and recorded.
- RS-015: Docs now describe stable offline MVP and optional live paths.
- RS-016: Python version, bounded example, path safety, and secret scan verified.

## Commands Run

```bash
uv sync --extra dev
# exit 0; resolved 172 packages

uv run pytest
# exit 0; 165 passed

uv run ruff check .
# exit 0; All checks passed

uv run ruff format --check .
# exit 0; 70 files already formatted

uv run mypy src
# exit 0; Success: no issues found in 35 source files

uv run kssim --help
# exit 0; listed validate-config, sample, compile-scenario, run, evaluate, report

uv build --out-dir /tmp/argus-build-check
# exit 0; built source distribution and wheel

python3 -m venv /tmp/argus-base-install-check
/tmp/argus-base-install-check/bin/python -m pip install /tmp/argus-build-check/korean_social_simulator-0.1.0-py3-none-any.whl
/tmp/argus-base-install-check/bin/kssim --help
KSSIM_OUTPUT_DIR=/tmp/argus-base-install-check-run /tmp/argus-base-install-check/bin/kssim run --config examples/run_product_reaction.yaml --dry-run
# exit 0; base wheel install works without optional extras and writes all 8 artifacts

env -u NVIDIA_API_KEY -u KSSIM_LLM_API_KEY uv run python scripts/run_human_acts.py
# exit 0; falls back to offline dry-run and writes full artifact tree

env -u NVIDIA_API_KEY -u KSSIM_LLM_API_KEY uv run python scripts/run_multi_scenario.py
# exit 0; 4/4 simulations complete offline with live adapter reported partial

env -u NVIDIA_API_KEY -u KSSIM_LLM_API_KEY uv run python scripts/run_five_families.py
# exit 0; 20/20 sub-scenarios complete offline with live adapter reported partial

rm -rf outputs/product_reaction_run_001
uv run kssim validate-config --config examples/run_product_reaction.yaml
uv run kssim sample --config examples/run_product_reaction.yaml --output outputs/product_reaction_run_001/sample.json
uv run kssim compile-scenario --config examples/run_product_reaction.yaml --output outputs/product_reaction_run_001/plan.json
KSSIM_LLM_API_KEY=sk-argus-secret-test KSSIM_PAGEINDEX_API_KEY=pg-argus-secret-test uv run kssim run --config examples/run_product_reaction.yaml --dry-run
uv run kssim evaluate --events outputs/product_reaction_run_001/events.jsonl --config examples/run_product_reaction.yaml
uv run kssim report --input outputs/product_reaction_run_001 --output outputs/product_reaction_run_001/report.md
# exit 0; run status success; 10 personas sampled; 7 metrics written

python3 - <<'PY'
# parsed JSON artifacts, parsed every JSONL event line, checked limitations text,
# and scanned artifacts for fake secret values.
PY
# exit 0; 8 artifacts, 61 events, status success, family product_market, secret scan passed

uv run python qa_verify_scenario_families.py
# exit 0; registry matches expected 16 families; unknown family raises ScenarioValidationError; QA pipeline writes all 8 run artifacts

uv sync --extra hf --dry-run
uv sync --extra llm --dry-run
uv sync --extra concordia --dry-run
uv sync --extra all --dry-run
# exit 0; optional extras resolve without applying environment changes

uv run pytest -q
# exit 0; 165 passed in 0.46s

uv run ruff check .
# exit 0; All checks passed

uv run ruff format --check .
# exit 0; 70 files already formatted

uv run mypy src
# exit 0; Success: no issues found in 35 source files

uv run python - <<'PY'
# parsed and secret-scanned outputs/product_reaction_run_001 and outputs/human_acts_run_001.
PY
# exit 0; product_market 61 events, content_culture 29 events, reports include limitations, fake secrets absent

uv run kssim run --config examples/run_product_reaction.yaml --dry-run
# exit 1 with existing events.jsonl; prints recovery hint for runtime.overwrite/new run_id/removing run dir
```

## Tests Added

- CLI integration tests for real artifact-writing commands, typed errors, and overwrite hints.
- CLI unsafe-run regression test for no artifact writes after safety failure.
- Persona source dispatcher tests.
- Config env override and run ID safety tests.
- Dry-run propagation test.
- Optional RAG warning tests.
- Korean/intervention safety tests.
- Live adapter event retention test.
- Live adapter system-only partial-status test.
- Live adapter secret redaction tests.
- Script run ID slug regression test.
- Golden report renderer comparison.

## Known Limitations

- Hugging Face, mocked PageIndex/RAG, Concordia, and NVIDIA NIM remain optional/experimental adapter paths.
- The `rag` extra is reserved; live PageIndex wiring remains future work.
- Dry-run metrics are deterministic structural or placeholder values, not predictive measurements.
- Reports are for synthetic hypothesis generation and require external validation.

## Follow-up Work

- Add live-service tests under explicit markers when credentials and services are available.
- Replace placeholder metrics with validated domain metrics only after offline MVP behavior remains stable.
