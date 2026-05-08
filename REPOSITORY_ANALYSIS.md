# Repository Analysis: Topabaem05/Argus

## Inspection Date

2026-04-28

## Summary

Argus presents itself as **Korean Social Simulation Lab**, a Python project for synthetic Korean social simulation using synthetic personas, Concordia-style agent profiles, optional PageIndex MCP/RAG, safety guardrails, JSONL logs, metrics, and Markdown reports.

The repository now has a wired, verified offline MVP for deterministic synthetic Korean social simulation. The CLI orchestrates implementation modules, the example scenario family matches the registry, and the dry-run path writes complete auditable artifacts.

## Observed Repository Structure

```txt
src/korean_social_simulator/
  cli.py
  errors.py
  models.py
  agents/profile_builder.py
  config/loader.py
  config/models.py
  data/loader.py
  data/huggingface_loader.py
  evaluation/metrics.py
  personas/sampler.py
  rag/pageindex_mcp.py
  reporting/markdown.py
  safety/validator.py
  scenarios/compiler.py
  scenarios/registry.py
  simulation/concordia_adapter.py
  simulation/dry_run.py
  simulation/nvidia_nim.py
  storage/run_store.py
examples/run_product_reaction.yaml
pyproject.toml
README.md
```

## Main Entry Point

`pyproject.toml` declares:

```txt
kssim = "korean_social_simulator.cli:app"
```

Expected commands exist:

- `validate-config`
- `sample`
- `compile-scenario`
- `run`
- `evaluate`
- `report`

Current behavior: these commands delegate to the pipeline orchestrator and execute real repository behavior.

## Core Module Findings

### Configuration

`config.models` uses Pydantic v2 models with strict validation. `config.loader` handles YAML loading, environment overrides, business rules, and live-mode secret validation.

Risks:

- Environment overrides such as `KSSIM_HF_CACHE_DIR` and `KSSIM_PAGEINDEX_API_KEY` must map only to declared model fields.
- `extra="forbid"` is valuable and should be kept after schema alignment.

### Persona Loading

`data.loader` validates local JSONL fixtures. `data.huggingface_loader` supports optional Hugging Face loading through `datasets` with `trust_remote_code=False`.

Risks:

- No single dispatcher is clearly wired to `DatasetConfig`.
- Fixture mode must remain the default test path and must not require network access.

### Sampling

`personas.sampler` supports deterministic filtering and sampling.

Risks:

- It is not wired into the CLI flow.
- Tests must prove determinism for the same seed and filters.

### Agent Profiles

`agents.profile_builder` converts personas into Korean-language `AgentProfile` objects.

Risks:

- Safety pattern checks are mostly keyword-based.
- Korean prohibited phrases need explicit tests.

### Scenarios

`scenarios.registry` defines supported scenario families such as `product_market`. `scenarios.compiler` validates families and creates `SimulationPlan`.

Risks:

- Pre-stabilization mismatch: `examples/run_product_reaction.yaml` used `product_reaction`, while the registry supported `product_market`. Current implementation uses `product_market`.
- Pre-stabilization compiler risk: `compile_scenario()` hard-coded dry-run behavior. Current implementation receives effective runtime mode.

### Simulation

`simulation.dry_run` creates structural placeholder events without network calls. `simulation.concordia_adapter` and `simulation.nvidia_nim` define optional live paths.

Risks:

- Live adapter events must not be discarded.
- Missing Concordia must produce honest partial/failed status.
- Optional dependencies must be declared and tested as optional.

### Evaluation

`evaluation.metrics` computes deterministic structural and placeholder metrics.

Risks:

- Reports must label placeholder metrics as synthetic and non-predictive.

### Storage

`storage.run_store` writes managed artifacts such as `events.jsonl`, `run_metadata.json`, `metrics.json`, `metrics.csv`, and `report.md`.

Risks:

- The storage layer is driven by the CLI pipeline and writes the expected artifact tree.
- Existing run protection must be tested.

### Reporting

`reporting.markdown` renders deterministic reports with limitations.

Risks:

- The report path must be wired to CLI.
- Golden tests should ensure limitation text remains present.

### Safety

`safety.validator` blocks known prohibited patterns.

Risks:

- Korean equivalents and semantic bypasses are not fully covered.
- Fail-closed behavior must be tested.

## Stabilization Target

A clean checkout must support this offline flow:

```bash
uv sync --extra dev
uv run kssim validate-config --config examples/run_product_reaction.yaml
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
uv run pytest
uv run ruff check .
uv run mypy src
```

Expected dry-run artifacts:

```txt
outputs/product_reaction_run_001/
  run_metadata.json
  sample.json
  profiles.json
  plan.json
  events.jsonl
  metrics.json
  metrics.csv
  report.md
```

## Stabilization Gaps Addressed

1. CLI wiring.
2. Example family mismatch.
3. Config schema/env override alignment.
4. Runtime dry-run propagation.
5. Optional adapter contracts.
6. Stronger Korean/English safety tests.
7. Complete artifact persistence.
8. End-to-end smoke tests.
9. Golden report tests.
10. Truthful README status.
