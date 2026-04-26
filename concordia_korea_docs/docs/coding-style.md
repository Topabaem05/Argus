# Coding Style

## Language and Framework Rules

- Use Python 3.11+.
- Use `uv` for dependency management and command execution.
- Use `src/` layout.
- Use `pydantic` or dataclasses for typed schemas. Prefer Pydantic for external config and input validation.
- Use `typer` for CLI commands.
- Use `pytest` for tests.
- Use `ruff` for linting and formatting.
- Use `mypy` for static type checks.
- Use dependency injection for LLM, RAG, storage, and dataset adapters.

## Naming Conventions

- Package: `korean_social_simulator`.
- Files and modules: `snake_case.py`.
- Classes: `PascalCase`.
- Functions and variables: `snake_case`.
- Constants: `UPPER_SNAKE_CASE`.
- Exceptions: suffix with `Error`.
- Tests: `test_<module>_<behavior>.py`.
- Fixtures: `fixture_<purpose>.json` or `fixture_<purpose>.jsonl`.
- Scenario configs: `<scenario_family>_<variant>.yaml`.

## Type Rules

- All public functions must have full type annotations.
- All public dataclasses/Pydantic models must define field types and validation constraints.
- Use `Literal` for finite state/status values.
- Use `Protocol` for provider interfaces.
- Do not use `Any` unless documented with a reason.
- Avoid returning raw dicts from internal APIs; use typed models.

## Error Handling

- Use typed project errors.
- Error messages must explain what failed, why it matters, and how to fix it where possible.
- Do not swallow exceptions silently.
- Do not expose secrets in exceptions.
- Avoid broad catch-all handlers except at CLI boundaries, where errors are converted into user-facing messages and non-zero exit codes.
- Recovery behavior must be explicit:
  - fail fast
  - skip with warning
  - retry
  - partial result
  - blocked by safety policy

## Logging

- Use structured logging where practical.
- Log levels:
  - `DEBUG`: developer-only details; never secrets.
  - `INFO`: run lifecycle and high-level progress.
  - `WARNING`: recoverable degradation such as optional RAG unavailable.
  - `ERROR`: failed run or failed required dependency.
  - `CRITICAL`: unsafe state or corrupted output.
- Never log:
  - API keys
  - access tokens
  - private documents
  - raw full prompts when they may contain secrets
  - personal information from real users
- Every run must record a run ID, config hash, seed, scenario ID, and safety policy version.

## Testing Style

### Unit Tests

Required for:

- Config validation.
- Persona schema validation.
- Sampler determinism.
- Agent profile rendering.
- Scenario validation.
- Safety policy blocking.
- Metric calculations.
- Storage write/read behavior.

### Integration Tests

Required for:

- Local fixture dataset to full simulation dry-run.
- Optional mocked PageIndex MCP retrieval.
- Concordia adapter with mock LLM.
- Report generation from event logs.

### Smoke Tests

Required for:

- `kssim --help`.
- `kssim run --config examples/run_product_reaction.yaml --dry-run`.
- `kssim report --input <golden-log> --output <temp-report>`.

### Regression Tests

Required for:

- Safety guard forbidden scenarios.
- Deterministic sampling with a fixed seed.
- Golden report output.
- Backward-compatible config parsing.

### Golden Tests

Use golden tests for stable outputs:

- Rendered agent profiles.
- Compiled scenario plans.
- Evaluation metrics for fixed logs.
- Markdown report summaries.

## Dependency Rules

- Add dependencies only when they directly support a requirement.
- Prefer mature libraries with active maintenance.
- Pin versions after initial implementation stabilizes.
- Do not add large frameworks for simple tasks.
- Do not add a database unless local file storage becomes insufficient.
- RAG dependencies must remain optional extras.
- Fine-tuning dependencies must not be part of the MVP default install.

## Formatting and Linting

Commands:

```bash
uv run ruff format .
uv run ruff check .
uv run mypy src
uv run pytest
```

Rules:

- Keep functions short and focused.
- Prefer pure functions for validation, sampling, metrics, and rendering.
- Avoid hidden side effects.
- Use explicit file encodings.
- Use path objects instead of string path concatenation.

## Documentation Rules

- Update `README.md` when user-facing behavior changes.
- Update `docs/architecture.md` when module boundaries or data flow change.
- Update ADRs when major decisions change.
- Update active spec files when requirements, tasks, or verification change.
- Public classes and functions require concise docstrings explaining purpose, inputs, outputs, and failure behavior.
- Comments must explain why, not restate what the code does.
