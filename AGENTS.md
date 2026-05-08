# AGENTS.md

Compact instructions for AI coding agents working in this repository.

## Architecture & Context
- **Project Purpose**: Argus is a Python-based Korean Social Simulation Lab (`korean_social_simulator`). 
- **Offline First**: The core simulation (MVP) must run entirely offline via deterministic dry-runs. Hugging Face, LLMs, and Concordia are **optional** adapters. Do not make live services mandatory.
- **Safety Policy**: Argus must not be used for political persuasion, real-person profiling, or harassment.
- **Data Boundaries**: Pydantic v2 models are the strict schema boundary.
- **Entrypoints**: The Typer CLI at `src/korean_social_simulator/cli.py` is the primary entrypoint.

## Setup & Dependencies
The project uses `uv` with `hatchling`. Do not use `pip` directly. Do not add dependencies without updating `pyproject.toml` and running `uv sync`.

```bash
# Setup default dev environment
uv sync --extra dev

# Setup with all optional integrations
uv sync --extra all
```

## Quality & Verification Flow
Strict command order for verifying changes. Fix linting errors before type checking.

```bash
uv run ruff format .
uv run ruff check . --fix
uv run mypy src
```

## Testing Quirks & Commands
- **No Network in Unit Tests**: Standard unit tests must work completely offline without API keys.
- **Markers**: Live/external tests are isolated using strict pytest markers: `live_hf`, `live_llm`, `live_pageindex`, `integration`, `smoke`, `golden`, `slow`.
- **Golden Tests**: Must normalize timestamps and machine-specific paths for deterministic output matching.

```bash
# Run only fast, offline tests
uv run pytest -m "not live_hf and not live_llm and not integration and not live_pageindex"

# Run all tests
uv run pytest
```

## CLI Execution Examples
Exact commands for running the simulation offline via dry-run:

```bash
uv run kssim validate-config --config examples/run_product_reaction.yaml
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
```

## Repo-Specific Rules
- Add or update tests for every new behavior.
- Use custom exceptions from `src/korean_social_simulator/errors.py`.
- Do not commit secrets (`.env` files are ignored).
- Keep tests deterministic.
- If modifying core configurations, CLI usage, or dependencies, update `README.md`.
