# Coding Style

## Language and Framework Rules

- Python 3.11+.
- `src/` package layout.
- Typer for CLI.
- Pydantic v2 for schemas.
- Pytest for tests.
- Ruff for lint/format.
- Mypy strict mode.

## Naming

- Modules: `snake_case.py`.
- Tests: `test_<behavior>.py`.
- Classes: `PascalCase`.
- Exceptions: `PascalCaseError`.
- Functions: `snake_case`, verb-first.
- Constants: `UPPER_SNAKE_CASE`.
- Spec folders: `kebab-case`.

## Types

- Public functions require full type hints.
- Use `Path` for filesystem paths.
- Use `Literal` for finite statuses.
- Avoid `Any`; isolate at adapter boundaries if unavoidable.
- Pydantic models should use `extra="forbid"` unless documented otherwise.

## Error Handling

- Use custom exceptions from `errors.py`.
- Wrap lower-level exceptions with `from exc`.
- Do not raise bare `Exception` for expected user failures.
- CLI catches typed errors and exits non-zero.
- Safety violations fail closed when blocking is enabled.

## Logging

Use levels:

- `DEBUG`: internal details, never secrets.
- `INFO`: major pipeline milestones.
- `WARNING`: optional dependency unavailable or partial run.
- `ERROR`: failed command or blocked scenario.

Never log API keys, tokens, `.env` contents, credentials, or real user data.

## Testing Style

### Unit

Pure functions and schemas.

### Integration

Multi-module workflows and CLI commands.

### Smoke

Full offline dry-run.

### Regression

One test per fixed bug.

### Golden

Stable reports and artifacts with normalized timestamps.

### Live

External services only with explicit markers.

## Dependency Rules

- Base dependencies support offline MVP.
- `hf` extra for Hugging Face.
- `concordia` extra if Concordia is referenced.
- `rag` extra if live PageIndex is implemented.
- Live LLM providers are optional.
- Do not add dependencies for trivial standard-library tasks.

## Formatting and Linting

```bash
uv run ruff format .
uv run ruff check .
uv run mypy src
```

## Documentation Rules

Update docs when changing:

- CLI,
- config,
- artifact layout,
- scenario registry,
- safety behavior,
- optional dependencies,
- report format,
- test commands.

## Artifact Rules

- UTF-8 text.
- JSONL for events.
- Stable JSON for metadata and metrics.
- No secrets in artifacts.
- Do not overwrite runs unless explicitly allowed.
