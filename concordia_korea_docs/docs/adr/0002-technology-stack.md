# ADR 0002: Technology Stack

## Status

Proposed

## Context

The project needs reproducible local development, typed data validation, dataset processing, scenario configuration, CLI execution, testability, optional RAG integration, and future extensibility.

## Decision

Use the following stack:

- Python 3.11+.
- `uv` for dependency and command management.
- `gdm-concordia` for generative social simulation.
- Hugging Face `datasets` for loading Nemotron-Personas-Korea.
- Polars or DuckDB for efficient local filtering where needed.
- Pydantic for config and external data validation.
- Typer for CLI.
- PyYAML or ruamel.yaml for YAML config parsing.
- JSONL/CSV/Markdown for outputs.
- Optional PageIndex MCP integration behind a retriever interface.
- Pytest for tests.
- Ruff for linting and formatting.
- Mypy for type checking.

## Consequences

What becomes easier:

- Python ecosystem aligns with Concordia and Hugging Face.
- `uv` provides fast reproducible environments.
- Pydantic improves external input validation.
- Typer creates a clear CLI for batch workflows.
- JSONL makes long simulation logs streamable and auditable.

What becomes harder:

- LLM provider behavior will need careful mocking in tests.
- Optional dependencies need extras or configuration isolation.
- Large dataset handling needs performance care.

Tradeoffs accepted:

- No web UI in MVP.
- No database in MVP.
- Optional RAG is not a hard dependency.
- Fine-tuning stack is out of MVP scope.

## Alternatives Considered

- Poetry instead of uv: stable, but slower and less aligned with fast AI-agent workflows.
- Pandas only: simpler but less suitable for large filtering workloads than Polars/DuckDB.
- SQLite database from day one: useful later, but unnecessary for initial JSONL/CSV outputs.
- Streamlit/Gradio UI: useful later, but CLI-first is easier to test.
- Direct PageIndex dependency everywhere: rejected to keep core simulator usable without RAG.

## Validation

This decision is working if:

- `uv sync` installs the environment.
- CLI commands run in dry-run mode.
- Dataset fixtures can be loaded offline.
- Optional RAG tests can run with mocks.
- `pytest`, `ruff`, and `mypy` are part of the verification workflow.
