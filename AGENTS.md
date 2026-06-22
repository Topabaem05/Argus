# AGENTS.md

**Generated:** 2026-06-22
**Commit:** 3cb024e0
**Branch:** main

## OVERVIEW
Argus (`korean-social-simulator`) is a deterministic, auditable Korean social simulation lab. YAML config in, JSONL/Markdown artifacts out. Python 3.11+, Pydantic v2 (strict), Typer CLI, `uv`+hatchling. Live adapters (HF, LLM, Concordia, FastAPI bridge) are optional extras; the offline dry-run is the default path.

## STRUCTURE
```
src/korean_social_simulator/
  cli.py            # Typer entrypoint, thin wrapper over pipeline.*_command
  pipeline.py        # 587 lines: command orchestration (validate/sample/compile/run/eval/report + bridge)
  models.py          # Core Pydantic schemas: PersonaRecord, AgentProfile, SimulationPlan/Result
  errors.py          # Typed exception hierarchy (KoreanSocialSimulationError base)
  redaction.py       # Secret/path scrubbing
  config/            # RuntimeConfig, BridgeConfig, validators, YAML loader
  data/              # Persona loader (fixture JSONL) + optional HF adapter
  personas/          # Deterministic sampler (seeded)
  agents/            # Profile builder from PersonaRecord
  scenarios/         # 16 scenario families registry + compiler -> SimulationPlan
  simulation/        # dry_run (offline) + concordia_adapter + nvidia_nim + interaction/behavior_planner
  rag/               # NoopRetriever + PageIndex MCP (optional)
  safety/            # Bilingual EN/KO prohibited-phrase validator (fails closed)
  storage/           # RunStore: events.jsonl, metrics.json, report.md, run_metadata.json
  evaluation/        # MetricsResult over events
  reporting/         # Markdown renderer (golden-tested)
  bridge/            # FastAPI/WebSocket Unity visualization bridge (15 modules) - see subdir AGENTS.md
  bridge_schema/     # Versioned Pydantic envelope protocol - see subdir AGENTS.md
tests/               # unit / integration / smoke / golden - see subdir AGENTS.md
configs/             # *.example.yaml for bridge, scenarios, safety, local, robot-assets
examples/            # Runnable scenario YAMLs
specs/               # ai-unity-mujoco-bridge, korean-social-simulation, repository-stabilization
docs/                # architecture.md, coding-style.md, adr/, design specs
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Add/modify a CLI command | `cli.py` + matching `pipeline.<name>_command` | CLI is a thin error-handling wrapper; logic lives in `pipeline.py` |
| Change config schema | `config/models.py` + `config/loader.py` | `extra="forbid"` everywhere; validators enforce run_id pattern, semver, localhost-only hosts |
| Add scenario family | `scenarios/registry.py` (`SUPPORTED_FAMILIES`) + `FAMILY_DEFAULT_METRICS` | Compiler refuses unsupported families |
| Add prohibited content | `safety/validator.py` `PROHIBITED_OBJECTIVE_PATTERNS` | Bilingual EN+KO; safety violations fail closed |
| New artifact file | `storage/run_store.py` constants `_EVENTS_FILE_NAME` etc. | Run dir = `outputs/<run_id>/` |
| Unity bridge message | `bridge_schema/envelope.py` (`_PAYLOAD_MODELS`) + `bridge/server.py` (`_SUPPORTED_MESSAGE_TYPES`) | Both lists must stay in sync |
| Debug a dry run | `simulation/dry_run.py` `run_dry_run` | No network; structural events per turn |
| Persona input parsing | `simulation/interaction.py` | Attachment validation, tokenization, memory proposals |

## CONVENTIONS
- `from __future__ import annotations` at top of every module.
- Pydantic models: `model_config = ConfigDict(extra="forbid")` unless documented. `Literal` for finite statuses.
- Errors: raise typed `KoreanSocialSimulationError` subclasses from `errors.py`; wrap lower-level with `from exc`. Never bare `except Exception` without re-raise.
- Optional adapters import lazily inside functions (e.g. `import uvicorn` in `bridge_serve_command`) and raise `SimulationError` with install hint on `ImportError`.
- `cast()` and `# type: ignore[import-not-found]` allowed only at adapter boundaries (HF datasets, OpenAI, dotenv). Never in core models/config.
- Frozen dataclasses for value objects (`TrackedMessage`, `InteractionContext`).
- Module `__all__` exports in bridge/ and bridge_schema/.

## ANTI-PATTERNS (THIS PROJECT)
- Do not make live services (HF, LLM, Concordia, bridge FastAPI) mandatory for the offline dry-run path.
- Do not add dependencies without updating `pyproject.toml` and running `uv sync`. Never use `pip` directly.
- Do not use `as any` / `# type: ignore` / `@ts-ignore` to suppress mypy in core (only at lazy-import adapter boundaries).
- Do not log API keys, tokens, `.env` contents, credentials, or real user data.
- Do not treat synthetic personas as real people or population forecasts.
- Do not add political persuasion, voter targeting, or protected-class targeting features.
- Do not weaken safety validation to make a test pass; safety fails closed when blocking is enabled.
- Do not commit `outputs/`, `.env`, large datasets, or `unity/EmbodiedDebate/Library/`.

## UNIQUE STYLES
- CLI commands split: `cli.py` defines Typer commands + error handling; `pipeline.py` holds `*_command` functions with all logic. CLI catches `KoreanSocialSimulationError` and maps to prefixed exit-1 message via `_ERROR_PREFIXES`.
- Bridge message protocol: versioned envelope (`BridgeEnvelope`, semver `schema_version`) with `_PAYLOAD_MODELS` dispatch table mapping `type` -> Pydantic payload class. `bridge/server.py` mirrors `_SUPPORTED_MESSAGE_TYPES`.
- Config loaded from YAML via `ruamel.yaml`; env vars merged for secrets (LLM keys) via lazy `python-dotenv`.
- Physics coordinator wraps a backend with timeout+fallback; `FallbackPhysicsBackend` is the default (no external physics service required).

## COMMANDS
```bash
# Setup
uv sync --extra dev            # dev only
uv sync --extra all            # all optional integrations

# Verify (strict order: format -> lint -> type)
uv run ruff format .
uv run ruff check . --fix
uv run mypy src

# Test
uv run pytest -m "not live_hf and not live_llm and not integration and not live_pageindex"  # fast offline
uv run pytest                                                  # all

# CLI
uv run kssim validate-config --config examples/run_product_reaction.yaml
uv run kssim run --config examples/run_product_reaction.yaml --dry-run
uv run kssim bridge serve --config configs/bridge.example.yaml
```

## NOTES
- `unity/` and `tmp/` are noise (Unity Library build cache + animation frame dumps). Do not document or edit.
- `concordia_korea_docs/AGENTS.md` is a legacy spec doc (greenfield plan); root AGENTS.md supersedes for current conventions.
- Golden test (`tests/golden/`) normalizes timestamps and absolute paths; update `expected_report.md` only when report format intentionally changes.
- Bridge config requires localhost-only hosts (`127.0.0.1`, `localhost`, `::1`); enforced by `_LOCAL_BRIDGE_HOSTS` validator.
- `mypy strict` excludes `tests/`; tests are not type-checked.
- Optional extras: `hf`, `llm`, `concordia`, `bridge`, `polars`, `rag`. `all` = union.
