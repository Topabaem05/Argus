# Korean Social Simulation Lab

Korean Social Simulation Lab is a specification-driven Python project for building synthetic Korean social simulations with Concordia, Nemotron-Personas-Korea, optional PageIndex MCP/RAG, and strict safety/evaluation guardrails. It is designed to test hypotheses about product reaction, marketing risk, community dynamics, rumor response, service operation, policy communication, organization change, and game NPC social worlds before real-user validation.

## Problem Statement

Teams often need to understand how different user or community archetypes may react to products, messages, rules, crises, or social situations. Real user studies are expensive, slow, and ethically sensitive. Simple LLM roleplay is too unstructured and hard to evaluate. This project provides a controlled, reproducible simulation pipeline where synthetic Korean personas become Concordia agents, interact in configured scenarios, and produce auditable logs and metrics.

The system is not a real-world forecasting engine. It is a hypothesis-generation and risk-analysis tool.

## Target Users

- AI builders designing multi-agent simulations.
- Product teams testing messaging, pricing, onboarding, and usability hypotheses.
- Community operators testing rules, moderation, and conflict prevention strategies.
- Policy and communication teams testing public notice clarity and trust risks.
- Game designers building Korean social-world NPC simulations.
- Researchers studying synthetic social simulation methodology.

## Key Features

- Load and sample Korean synthetic personas from `nvidia/Nemotron-Personas-Korea`.
- Convert persona rows into structured Concordia agent profiles.
- Run scenario categories including:
  - Product and price reaction.
  - Marketing and viral spread risk.
  - Rumor and crisis response.
  - Conflict and mediation.
  - Policy and notice acceptance.
  - Community operation.
  - Organization and negotiation.
  - Game NPC and social-world simulation.
- Optionally ground scenarios with PageIndex MCP/RAG for product docs, policies, reports, historical notes, and prior logs.
- Collect JSONL logs and aggregate metrics.
- Generate markdown/CSV reports.
- Enforce safety rules against political manipulation, real-person profiling, protected-group targeting, and fake influence campaigns.

## High-Level Architecture

```mermaid
flowchart LR
    A[Nemotron-Personas-Korea] --> B[Persona Sampler]
    B --> C[Agent Profile Builder]
    D[Optional PageIndex MCP/RAG] -. documents .-> E[Scenario Builder]
    C --> F[Concordia Simulation Runner]
    E --> F
    F --> G[Logs and Metrics]
    G --> H[Evaluation Engine]
    H --> I[Reports]
    G -. optional later .-> J[SFT/LoRA Optimization Dataset]
```

## Installation

```bash
git clone <repository-url>
cd korean-social-simulation-lab
uv sync
```

Expected Python version: 3.11 or newer.

## Usage

Dry-run a scenario without LLM calls:

```bash
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
```

Run a configured simulation:

```bash
uv run kssim run --config examples/run_product_reaction.yaml
```

Generate a report from existing logs:

```bash
uv run kssim report --input outputs/product_reaction/run_001/events.jsonl --output outputs/product_reaction/run_001/report.md
```

## Development Workflow

1. Read `AGENTS.md`.
2. Read the active feature spec under `specs/korean-social-simulation/`.
3. Select the next unchecked task in `tasks.md`.
4. Implement only that task.
5. Add or update tests.
6. Run required verification commands.
7. Report exact commands and results.

## Testing

```bash
uv run pytest
uv run ruff check .
uv run mypy src
```

Test categories:

- Unit tests for data models, validation, samplers, adapters, metrics, and safety guards.
- Integration tests for dataset sampling, scenario compilation, optional RAG adapters, and log writing.
- Smoke tests for end-to-end dry-run simulations.
- Golden tests for deterministic reports and metric outputs.

## Configuration

Configuration is YAML-first with environment variable overrides.

Expected files:

```txt
configs/local.example.yaml
configs/scenarios.example.yaml
configs/safety.example.yaml
```

Expected environment variables:

```bash
KSSIM_LLM_PROVIDER=<provider-name>
KSSIM_LLM_MODEL=<model-name>
KSSIM_LLM_API_KEY=<secret>
KSSIM_HF_CACHE_DIR=<optional-local-cache-dir>
KSSIM_PAGEINDEX_API_KEY=<optional-secret>
KSSIM_OUTPUT_DIR=outputs
```

Secrets must be provided through environment variables or a local ignored `.env` file, never committed.

## Project Status

Specification phase. No implementation should be assumed complete until tasks are implemented and verification passes.

## Roadmap

1. Bootstrap package, CLI, config loader, and typed models.
2. Add Nemotron persona dataset loader and deterministic sampler.
3. Add persona-to-agent profile conversion.
4. Add scenario templates and safety guardrails.
5. Add Concordia simulation runner adapter.
6. Add JSONL log storage and evaluation metrics.
7. Add optional PageIndex MCP/RAG integration.
8. Add reporting and golden tests.
9. Add optional later SFT/LoRA dataset export.

## References

- Concordia: https://github.com/google-deepmind/concordia
- Nemotron-Personas-Korea: https://huggingface.co/datasets/nvidia/Nemotron-Personas-Korea
- PageIndex: https://github.com/VectifyAI/PageIndex
- PageIndex MCP: https://docs.pageindex.ai/mcp

## License

TBD. Recommended project license: Apache-2.0 for project code, while respecting third-party licenses and dataset attribution requirements.
