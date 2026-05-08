# Scorecard to Reach 10/10

| Area | Stabilized State | 10/10 Definition | Evidence |
|---|---|---|---|
| Idea | Non-predictive framing in README and reports | Synthetic hypothesis-generation lab, not real-world predictor | README and report limitations |
| Architecture | Module responsibilities documented and tested | Every module has clear responsibility and tested boundaries | Architecture doc and module tests |
| Runtime | CLI commands execute real behavior | All CLI commands execute real behavior | CLI integration tests and artifacts |
| README alignment | README matches stable offline behavior and optional paths | README exactly matches implemented behavior | Docs review and smoke tests |
| RAG | Mock/noop stable; live CLI wiring explicitly blocked | Mock/noop stable; live PageIndex optional later | RAG provider contract and tests |
| Concordia/LLM | Dry-run stable; live adapters optional and honest | Dry-run stable; live adapters optional and honest | Mocked adapter tests |
| Safety | English and Korean prohibited uses blocked | English and Korean prohibited uses blocked | Safety fixtures and tests |
| Metrics | Placeholder status explicit and non-predictive | Placeholder status explicit, no predictive claims | Golden report tests |
| Storage | Stable artifact tree generated for runs | Stable artifact tree for every run | RunStore and E2E tests |
| Testing | Local gates pass | Unit, integration, smoke, regression, golden, lint, type check pass | Completion report |

## Release Gates

### Gate 1: Truthful Documentation

- README distinguishes stable, experimental, and planned behavior.
- Reports state that outputs are synthetic and non-predictive.
- Optional external services are labeled optional.
- Example configs match actual registry values.

### Gate 2: Offline MVP

The following must work without network or API keys:

```bash
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
```

It must generate:

```txt
events.jsonl
metrics.json
metrics.csv
report.md
run_metadata.json
sample.json
profiles.json
plan.json
```

### Gate 3: Strict Errors

Required typed failures:

- missing config -> `ConfigurationError`
- invalid fixture -> `DatasetLoadError` or `PersonaSchemaError`
- unsupported family -> `ScenarioValidationError`
- insufficient personas -> `SamplingError`
- unsafe scenario -> `SafetyViolationError`
- storage conflict -> `StorageError`

### Gate 4: Test Coverage

Required test categories:

- unit
- integration
- smoke
- regression
- golden
- marked live tests

### Gate 5: AI-Agent Safety

- `AGENTS.md` present.
- One task at a time.
- No fake test results.
- No unrelated rewrites.
- No safety weakening.

## 10/10 Acceptance Statement

Argus reaches 10/10 when it truthfully provides a deterministic, offline-capable, well-tested synthetic Korean social simulation pipeline with strict config validation, typed errors, safety-first defaults, auditable artifacts, truthful reports, and optional external adapters.
