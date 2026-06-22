# AGENTS.md — simulation/

Offline-first simulation engine. `dry_run.py` is the default no-network path; the others are optional adapters.

## STRUCTURE
```
dry_run.py          # run_dry_run(): structural placeholder events per turn, no LLM/network
interaction.py      # InteractionContext + build_interaction_context(): input parsing, attachment validation, memory proposals (383 lines)
behavior_planner.py # build_behavior_intents(): deterministic behavior intents per agent/turn
concordia_adapter.py# run_simulation(): optional Concordia live adapter (lazy import)
nvidia_nim.py       # Optional NVIDIA NIM LLM client (lazy import, cast at boundary)
__init__.py
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Change turn event shape | `dry_run.py` `run_dry_run` + `_interaction_prelude_events` |
| Add attachment kind | `interaction.py` `_IMAGE/_VIDEO/_TEXT/_DOCUMENT_EXTENSIONS` frozensets + `AttachmentKind` Literal in `models.py` |
| Add behavior intent | `behavior_planner.py` `build_behavior_intents` |
| Wire a live LLM | `nvidia_nim.py` (lazy `import openai`, `cast()` at boundary) |
| Wire Concordia | `concordia_adapter.py` `run_simulation` (lazy `import gdm_concordia`) |

## CONVENTIONS
- `run_dry_run` raises `ValueError` if `plan.max_turns < 1`; all other failures bubble as typed errors.
- `InteractionContext` is a frozen dataclass; constructed via `build_interaction_context` (validates + normalizes input).
- Tokenization regex `_TOKEN_RE` covers ASCII + Hangul; extend when adding scripts.
- Stance is a `Literal["supports","opposes","mixed","uncertain"]`; `_STANCE_VALUES` tuple mirrors it.
- Memory proposals are opt-in (`PersonaMemoryUpdateConfig`); never applied by default.

## ANTI-PATTERNS
- Do not import `openai`, `gdm_concordia`, or `dotenv` at module top. Lazy import inside functions; raise `SimulationError` with install hint on `ImportError`.
- Do not call network from `dry_run.py`; it must stay offline-deterministic for unit tests.
- Do not apply persona memory proposals without explicit config opt-in.
