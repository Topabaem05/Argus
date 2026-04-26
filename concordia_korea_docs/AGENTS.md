# AGENTS.md

## Project Overview

Korean Social Simulation Lab is a greenfield Python project for running synthetic, auditable social simulations using:

- `gdm-concordia` as the generative agent-based simulation engine.
- `nvidia/Nemotron-Personas-Korea` as the structured synthetic Korean persona source.
- Optional PageIndex MCP/RAG as a document-grounding layer for product documents, policies, rules, reports, scenario evidence, and prior simulation logs.
- Optional later SFT/LoRA fine-tuning only as a post-MVP optimization step, never as the default architecture.

The project helps researchers, product teams, game designers, community operators, and policy/communications teams test hypotheses about product reaction, marketing risk, community conflict, rumor response, service operation, policy acceptance, organization dynamics, and NPC social worlds before running real-user studies.

This project must not be used to infer real individuals' political orientation, target protected groups, manipulate voters, create fake grassroots activity, or optimize divisive political persuasion.

## Repository Map

Expected structure:

```txt
.
├── AGENTS.md
├── README.md
├── pyproject.toml
├── uv.lock
├── configs/
│   ├── local.example.yaml
│   ├── scenarios.example.yaml
│   └── safety.example.yaml
├── data/
│   ├── raw/                  # ignored; downloaded datasets or local parquet files
│   ├── cache/                # ignored; HF/Polars/DuckDB cache
│   └── samples/              # small non-sensitive test fixtures only
├── docs/
│   ├── architecture.md
│   ├── coding-style.md
│   └── adr/
├── examples/
│   ├── run_product_reaction.yaml
│   ├── run_rumor_response.yaml
│   └── run_game_npc_world.yaml
├── outputs/                  # ignored; generated simulation logs and reports
├── src/
│   └── korean_social_simulator/
│       ├── __init__.py
│       ├── cli.py
│       ├── config/
│       ├── data/
│       ├── personas/
│       ├── agents/
│       ├── scenarios/
│       ├── simulation/
│       ├── rag/
│       ├── evaluation/
│       ├── safety/
│       ├── storage/
│       └── reporting/
├── specs/
│   └── korean-social-simulation/
└── tests/
    ├── unit/
    ├── integration/
    ├── smoke/
    └── golden/
```

## Agent Operating Rules

- Do not implement unrelated features.
- Do not rewrite unrelated modules.
- Do not remove tests unless the active spec explicitly requires it.
- Do not fake test results.
- Do not claim success unless verification was actually performed.
- Prefer small, reviewable changes.
- Preserve public interfaces unless the spec says otherwise.
- Ask for clarification only if a requirement blocks implementation.
- If ambiguity exists, choose the safest minimal interpretation and document the assumption.
- Never add political persuasion, voter targeting, or protected-class targeting features.
- Never treat synthetic personas as real people or real population forecasts.
- Never log secrets, API keys, raw LLM prompts containing credentials, or private user documents.
- Always add or update tests for new behavior.
- Always update docs when behavior changes.
- Always report exact commands run and exact results.

## Development Commands

Use `uv` as the default Python project manager.

```bash
# Install dependencies
uv sync

# Run CLI help
uv run kssim --help

# Run one smoke scenario
uv run kssim run --config examples/run_product_reaction.yaml --dry-run

# Run tests
uv run pytest

# Run unit tests only
uv run pytest tests/unit

# Run lint
uv run ruff check .

# Format
uv run ruff format .

# Type check
uv run mypy src

# Run all local checks
uv run pytest && uv run ruff check . && uv run mypy src
```

If a command is unavailable because the project has not yet been bootstrapped, the implementing agent must report that clearly and create the missing project infrastructure only when the current task requires it.

## Code Quality Rules

- Python 3.11+ required.
- Type hints are required for all public functions, methods, dataclasses, Pydantic models, and protocol interfaces.
- Use clear module boundaries: data loading, persona sampling, agent building, scenario building, simulation execution, RAG, evaluation, safety, storage, and reporting must remain separate.
- No hidden network calls. Network use must be explicit through dataset loading, LLM calls, PageIndex MCP calls, or configured document sources.
- Explicit error handling required. No broad `except Exception` unless errors are re-raised or converted into a typed project error.
- Deterministic tests required. Use seeds and fixtures.
- No global mutable state unless justified and covered by tests.
- No credentials in source code, tests, fixtures, docs, or committed config files.
- Large datasets and generated logs must not be committed.
- Public APIs must be stable once defined in the spec.

## Safety Rules

- Allowed: synthetic persona experiments for product reaction, usability, community moderation, conflict prevention, rumor response, service operations, educational communication, healthcare communication, financial literacy, policy communication, and game/NPC worlds.
- Disallowed: targeted political persuasion, voter manipulation, protected-class exploitation, identity inference on real users, fake grassroots generation, covert influence operations, malware/social-engineering optimization, and automated harassment.
- Political or sensitive scenarios must be framed as safety, conflict mitigation, misinformation resistance, or communication clarity experiments.
- Outputs must include uncertainty notes and must not be presented as real-world prediction without external validation.

## Completion Criteria

Done when:

- All acceptance criteria are satisfied.
- All required tests pass.
- New behavior is covered by tests.
- Existing behavior is not broken.
- No unrelated files are modified.
- Documentation is updated.
- Safety constraints are preserved.
- The agent reports exact commands run and their results.
