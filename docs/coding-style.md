# Coding Style

Korean Social Simulation Lab follows these coding conventions. All code contributions must comply.

## Language

- Python 3.11+ only.
- `from __future__ import annotations` at the top of every module.
- Type hints required for all public functions, methods, dataclasses, Pydantic models, and protocol interfaces.
- Use Pydantic v2 models with `ConfigDict(extra="forbid")` for all data structures.

## Project Structure

- Source lives under `src/korean_social_simulator/`.
- Tests live under `tests/` with mirror subdirectories (`unit/`, `integration/`, `smoke/`, `golden/`).
- Keep module boundaries clean: config, data, personas, agents, scenarios, safety, rag, simulation, storage, evaluation, and reporting are separate.

## Naming

- Module files: snake_case (`profile_builder.py`).
- Classes: PascalCase (`AgentProfile`, `RunStore`).
- Functions/Methods: snake_case (`build_agent_profiles`, `write_event`).
- Test files: prefix `test_` (`test_sampler.py`).
- Test functions: `test_<noun>_<behavior>` with GIVEN/WHEN/THEN docstrings.
- Private helpers in tests: prefix `_` (`_make_config`, `_make_profiles`).

## Typing

- Public functions always have return type annotations.
- Use `| None` syntax (Python 3.10+, not `Optional[]`).
- Pydantic models use `Field()` with validation constraints (`ge`, `le`, `min_length`).
- Typed exceptions inherit from `KoreanSocialSimulationError`.

## Error Handling

- No bare `except Exception` unless re-raised or converted to a project error.
- Use named project exceptions from `korean_social_simulator.errors`.
- Document `Raises:` in all public function docstrings.
- Safety violations raise `SafetyViolationError`.
- Storage failures raise `StorageError`.

## Documentation

- Every public function has a docstring with `Raises:` section when applicable.
- Models have triple-quoted class docstrings.
- Test docstrings use GIVEN/WHEN/THEN format.

## Testing

- Use pytest with `tmp_path` or `tempfile.TemporaryDirectory` for file tests.
- Deterministic tests: use fixed seeds and fixture data.
- Do not commit test dependencies on live services (`live_hf`, `live_llm`, `live_pageindex` markers).
- Golden tests produce stable outputs; update only with changelog notes.

## Formatting

- Ruff formatter with `quote-style = "double"`, `line-length = 100`.
- Ruff linter with `E`, `W`, `F`, `I`, `B`, `C4`, `UP`, `RUF` rules.
- Mypy strict mode: `strict = true`, no type suppression (`as any`, `@ts-ignore`).

## Safety

- Never log secrets, API keys, raw LLM prompts with credentials, or private user documents.
- Never hardcode credentials in source, tests, fixtures, or config files.
- Configs use environment variable overrides for secrets.

## Git

- No auto-commit. Only commit when explicitly requested.
- Never commit `outputs/`, `.venv/`, `__pycache__/`, or large datasets.
- Example configs must contain no secrets.