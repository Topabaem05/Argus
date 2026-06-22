# AGENTS.md — config/

Typed configuration schema boundary. Two model families: `RuntimeConfig` (simulation) and `BridgeConfig` (Unity bridge).

## STRUCTURE
```
models.py   # Pydantic models: RuntimeConfig, BridgeConfig, SamplingConfig, SafetyPolicy, etc. (368 lines)
loader.py   # load_config()/load_bridge_config(): YAML -> ruamel.yaml -> dict -> model, env merge (243 lines)
__init__.py
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Add a config field | `models.py` model + `configs/*.example.yaml` + `examples/*.yaml` |
| Add a validator | `models.py` `@field_validator` / `@model_validator(mode="after")` |
| Change YAML parsing | `loader.py` (`_load_yaml_file`, `_coerce` helpers) |
| Add env-backed secret | `loader.py` lazy `python-dotenv` import + `cast()` |

## CONVENTIONS
- `extra="forbid"` on every model. Unknown keys raise `ConfigurationError`.
- Patterns enforced by validators: `_RUN_ID_PATTERN`, `_BRIDGE_SCHEMA_VERSION_PATTERN` (semver), `_LOCAL_BRIDGE_HOSTS` (localhost-only).
- `Literal` for finite option sets (`mode`, `log_level`, `out_of_order_policy`, `write_policy`).
- Range constraints via `Field(ge=..., le=...)` (age, temperature, sample_size, ports).
- Env merge for secrets only (LLM API keys); never for structural config.
- `loader.py` uses `cast(dict[str, object], ...)` at the YAML parse boundary; not elsewhere.

## ANTI-PATTERNS
- Do not add optional/unknown fields; `extra="forbid"` will reject them.
- Do not bypass validators by constructing models from raw dicts in callers; go through `load_config`/`load_bridge_config`.
- Do not put secrets in YAML; use env vars + `.env` (gitignored).
