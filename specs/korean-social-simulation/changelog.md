# Changelog

## Unreleased

### Added

- Initial specification for Korean Social Simulation MVP.
- Scenario families: product reaction, pricing reaction, viral marketing risk, rumor crisis response, conflict mediation, policy notice acceptance, community operation, organization negotiation, and game NPC social world.
- Safety non-goals for political persuasion targeting, real-user profiling, protected-group exploitation, and fake influence operations.
- Optional PageIndex MCP/RAG architecture.
- Post-MVP fine-tuning position as optional optimization only.

### Changed

- None.

### Deprecated

- None.

### Removed

- None.

### Fixed

- None.

### Security

- Added explicit safety and privacy constraints for scenario validation and reporting.

## 2026-04-27 - MVP Implementation Complete — All 7 Phases

### Added

- Implemented the Typer CLI surface in `src/korean_social_simulator/cli.py` with six commands: `validate-config`, `sample`, `compile-scenario`, `run`, `evaluate`, and `report`.
- Implemented configuration and core domain foundations in `config/models.py`, `config/loader.py`, `models.py`, and `errors.py`.
- Implemented persona ingestion and preparation in `data/loader.py`, `data/huggingface_loader.py`, `personas/sampler.py`, and `agents/profile_builder.py`.
- Implemented scenario planning and grounding in `scenarios/registry.py`, `scenarios/compiler.py`, `rag/base.py`, and `rag/pageindex_mcp.py`.
- Implemented execution, safety, storage, evaluation, and reporting in `safety/validator.py`, `simulation/dry_run.py`, `simulation/concordia_adapter.py`, `storage/run_store.py`, `evaluation/metrics.py`, and `reporting/markdown.py`.
- Completed MVP support for all nine scenario families: product reaction, pricing reaction, viral marketing risk, rumor crisis response, conflict mediation, policy notice acceptance, community operation, organization negotiation, and game NPC social world.

### Changed

- Updated the root README and architecture documentation to reflect the implemented MVP module layout and end-to-end workflow.
- Verified the MVP with `uv run pytest` (127 tests passing), `uv run ruff check .` (clean), and `uv run mypy src` (clean on 32 source files).

### Deprecated

- None.

### Removed

- None.

### Fixed

- None.

### Security

- Confirmed prohibited-pattern blocking in scenario validation and agent profile construction for political persuasion, real-person profiling, protected-group targeting, fake influence operations, harassment, and social-engineering use cases.

## 2026-04-27 — Nvidia NIM Live LLM Integration

### Added

- Nvidia NIM LLM adapter in `simulation/nvidia_nim.py` with OpenAI-compatible client wrapper targeting `deepseek-ai/deepseek-v4-pro` at `https://integrate.api.nvidia.com/v1`.
- `run_nvidia_nim_simulation()` produces per-turn Korean-language agent responses using NIM API.
- `.env.example` template with `NVIDIA_API_KEY` configuration.
- `python-dotenv` and `openai` dependencies added to base install.

### Changed

- `simulation/concordia_adapter.py::run_simulation()` now prefers Nvidia NIM when `NVIDIA_API_KEY` is set in environment, falls back to Concordia stub, then returns `partial` when neither is available.
- Integration test updated to mock `is_nvidia_nim_available()` for Concordia-only scenarios.

### Verified

- 135 tests passing, ruff clean, mypy clean (33 source files, +1 new module).
