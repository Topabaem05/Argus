# AGENTS.md — tests/

Pytest layout mirroring `src/`. Offline by default; live/external tests isolated by markers.

## STRUCTURE
```
unit/           # Pure functions + schemas (no network, no markers)
  bridge/       # bridge module unit tests (AckTracker, physics, replay, event adapter)
  bridge_schema/# envelope + event schema validation
  simulation/   # dry_run, behavior_planner, interaction, nvidia_nim redaction
  config/ data/ personas/ rag/ safety/ storage/ reporting/ evaluation/ scenarios/ agents/
integration/    # Multi-module + CLI + FastAPI TestClient (marked @pytest.mark.integration)
  bridge/       # bridge health, websocket, replay, agent inspection, simulation stream
smoke/          # Full offline dry-run end-to-end
golden/         # expected_report.md + test_golden.py (normalized timestamps/paths)
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Add a unit test | `tests/unit/<module>/test_<behavior>.py` |
| Add integration test | `tests/integration/...` + `@pytest.mark.integration` |
| Update golden output | `tests/golden/reports/expected_report.md` (only when format intentionally changes) |
| Test a CLI command | `tests/integration/test_cli_pipeline_commands.py` |
| Test bridge WS | `tests/integration/bridge/test_observer_websocket.py` (TestClient) |

## CONVENTIONS
- Inject clocks/stubs, never real time: `ManualClock` pattern in `test_ack_tracker.py`.
- Bridge integration tests use `fastapi.testclient.TestClient` against `create_app(load_bridge_config("configs/bridge.example.yaml"))`.
- Determinism: seeded samplers, fixed timestamps via injection, normalized paths in golden.
- `@pytest.mark.parametrize` for variant cases (see `test_errors.py`, `test_run_id_path_safety.py`).
- mypy does not check `tests/` (excluded in `pyproject.toml [tool.mypy]`); type hints still encouraged.

## ANTI-PATTERNS
- Do not make network calls in unmarked tests. Live tests need `live_hf`/`live_llm`/`live_pageindex` markers.
- Do not commit `outputs/` run artifacts as fixtures; use `tests/golden/reports/expected_report.md` only.
- Do not weaken assertions to make tests pass; fix the code or update golden intentionally.
- Do not use real API keys in tests; mock the clients.
