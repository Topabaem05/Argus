# Implementation Tasks

## Phase 1: Foundation

- [ ] Task 1.1: Bootstrap Python project
  - Files to create or modify:
    - `pyproject.toml`
    - `src/korean_social_simulator/__init__.py`
    - `tests/__init__.py`
  - Requirements covered:
    - Requirement 14
    - Requirement 18
  - Acceptance checks:
    - [ ] `uv sync` succeeds.
    - [ ] `uv run pytest` discovers tests.
    - [ ] `uv run ruff check .` runs.
    - [ ] `uv run mypy src` runs.
  - Notes:
    - Do not add optional RAG or fine-tuning dependencies to the base install.

- [ ] Task 1.2: Add CLI skeleton
  - Files to create or modify:
    - `src/korean_social_simulator/cli.py`
    - `tests/smoke/test_cli_help.py`
  - Requirements covered:
    - Requirement 14
  - Acceptance checks:
    - [ ] `uv run kssim --help` exits with code 0.
    - [ ] Help text lists `validate-config`, `sample`, `compile-scenario`, `run`, `evaluate`, and `report`.
  - Notes:
    - CLI can be stubbed but must not claim unsupported behavior is implemented.

- [ ] Task 1.3: Add example configs
  - Files to create or modify:
    - `configs/local.example.yaml`
    - `configs/scenarios.example.yaml`
    - `configs/safety.example.yaml`
    - `examples/run_product_reaction.yaml`
  - Requirements covered:
    - Requirement 10
    - Requirement 12
    - Requirement 18
  - Acceptance checks:
    - [ ] Example configs contain no secrets.
    - [ ] Example configs use fixture mode by default.
    - [ ] Example scenario includes safety notes.

## Phase 2: Core Models and Interfaces

- [ ] Task 2.1: Define typed config models
  - Files to create or modify:
    - `src/korean_social_simulator/config/models.py`
    - `src/korean_social_simulator/config/loader.py`
    - `tests/unit/config/test_config_validation.py`
  - Requirements covered:
    - Requirement 10
    - Requirement 12
    - Requirement 15
  - Acceptance checks:
    - [ ] Missing required fields raise `ConfigurationError`.
    - [ ] Environment variables override config.
    - [ ] Secrets are redacted in serialized config.
    - [ ] Invalid age and participant limits fail fast.

- [ ] Task 2.2: Define project errors
  - Files to create or modify:
    - `src/korean_social_simulator/errors.py`
    - `tests/unit/test_errors.py`
  - Requirements covered:
    - Requirement 13
  - Acceptance checks:
    - [ ] All named project errors exist.
    - [ ] CLI boundary can map errors to non-zero exit codes.

- [ ] Task 2.3: Define data models
  - Files to create or modify:
    - `src/korean_social_simulator/models.py`
    - `tests/unit/test_models.py`
  - Requirements covered:
    - Requirement 1
    - Requirement 3
    - Requirement 8
  - Acceptance checks:
    - [ ] Persona, sample, agent, scenario, event, metric, and result models validate valid fixtures.
    - [ ] Invalid status values fail validation.

## Phase 3: Persona Pipeline

- [ ] Task 3.1: Implement local fixture persona loader
  - Files to create or modify:
    - `src/korean_social_simulator/data/loader.py`
    - `data/samples/personas_fixture.jsonl`
    - `tests/unit/data/test_fixture_loader.py`
  - Requirements covered:
    - Requirement 1
  - Acceptance checks:
    - [ ] Fixture mode loads without network.
    - [ ] Missing required fields raise `PersonaSchemaError`.

- [ ] Task 3.2: Add optional Hugging Face dataset loader interface
  - Files to create or modify:
    - `src/korean_social_simulator/data/huggingface_loader.py`
    - `tests/unit/data/test_huggingface_loader_mocked.py`
  - Requirements covered:
    - Requirement 1
    - Requirement 17
  - Acceptance checks:
    - [ ] Loader can be mocked without network.
    - [ ] Dataset load failures raise `DatasetLoadError`.
    - [ ] Live HF test is marked and skipped by default.

- [ ] Task 3.3: Implement deterministic sampler
  - Files to create or modify:
    - `src/korean_social_simulator/personas/sampler.py`
    - `tests/unit/personas/test_sampler.py`
  - Requirements covered:
    - Requirement 2
  - Acceptance checks:
    - [ ] Same seed produces same UUID order.
    - [ ] Insufficient rows fail unless explicitly allowed.
    - [ ] Sampling metadata is recorded.

## Phase 4: Agent, Scenario, and Safety

- [ ] Task 4.1: Implement agent profile builder
  - Files to create or modify:
    - `src/korean_social_simulator/agents/profile_builder.py`
    - `tests/golden/agent_profiles/`
    - `tests/unit/agents/test_profile_builder.py`
  - Requirements covered:
    - Requirement 3
  - Acceptance checks:
    - [ ] Valid profile includes all required fields.
    - [ ] Korean behavior rules are included for Korean scenarios.
    - [ ] Golden profile output is stable.

- [ ] Task 4.2: Implement scenario registry and compiler
  - Files to create or modify:
    - `src/korean_social_simulator/scenarios/registry.py`
    - `src/korean_social_simulator/scenarios/compiler.py`
    - `tests/unit/scenarios/test_compiler.py`
  - Requirements covered:
    - Requirement 4
  - Acceptance checks:
    - [ ] Supported families compile.
    - [ ] Unknown family raises `ScenarioValidationError`.
    - [ ] Default metrics are applied.

- [ ] Task 4.3: Implement safety validator
  - Files to create or modify:
    - `src/korean_social_simulator/safety/policy.py`
    - `src/korean_social_simulator/safety/validator.py`
    - `tests/unit/safety/test_safety_validator.py`
  - Requirements covered:
    - Requirement 6
    - Requirement 16
  - Acceptance checks:
    - [ ] Allowed conflict-prevention scenarios pass.
    - [ ] Political persuasion targeting is blocked.
    - [ ] Real-user inference is blocked.
    - [ ] Safety violation error includes reason and blocked rule.

## Phase 5: Optional RAG Layer

- [ ] Task 5.1: Define retriever protocol and no-op retriever
  - Files to create or modify:
    - `src/korean_social_simulator/rag/base.py`
    - `src/korean_social_simulator/rag/noop.py`
    - `tests/unit/rag/test_noop_retriever.py`
  - Requirements covered:
    - Requirement 5
    - Requirement 17
  - Acceptance checks:
    - [ ] RAG disabled initializes no external client.
    - [ ] No-op retriever returns skipped status.

- [ ] Task 5.2: Add mocked PageIndex MCP adapter
  - Files to create or modify:
    - `src/korean_social_simulator/rag/pageindex_mcp.py`
    - `tests/unit/rag/test_pageindex_mcp_mocked.py`
  - Requirements covered:
    - Requirement 5
    - Requirement 17
  - Acceptance checks:
    - [ ] Optional retrieval failure records warning.
    - [ ] Required retrieval failure raises `RetrievalError`.
    - [ ] Retrieved context preserves references.

## Phase 6: Simulation, Storage, Evaluation, Reporting

- [ ] Task 6.1: Implement dry-run simulation adapter
  - Files to create or modify:
    - `src/korean_social_simulator/simulation/dry_run.py`
    - `tests/unit/simulation/test_dry_run.py`
  - Requirements covered:
    - Requirement 7
  - Acceptance checks:
    - [ ] Dry-run emits structural events.
    - [ ] No LLM provider is called.
    - [ ] Max turns are enforced.

- [ ] Task 6.2: Implement Concordia adapter boundary with mocked LLM
  - Files to create or modify:
    - `src/korean_social_simulator/simulation/concordia_adapter.py`
    - `tests/integration/test_concordia_adapter_mocked.py`
  - Requirements covered:
    - Requirement 7
  - Acceptance checks:
    - [ ] Adapter can run with a mocked LLM.
    - [ ] LLM errors produce partial or failed result.
    - [ ] No Concordia-specific details leak into unrelated modules.

- [ ] Task 6.3: Implement event storage
  - Files to create or modify:
    - `src/korean_social_simulator/storage/run_store.py`
    - `tests/unit/storage/test_run_store.py`
  - Requirements covered:
    - Requirement 8
    - Requirement 11
  - Acceptance checks:
    - [ ] Event JSONL schema is stable.
    - [ ] Existing output path fails unless overwrite is true.
    - [ ] Run metadata is written.

- [ ] Task 6.4: Implement metric evaluators
  - Files to create or modify:
    - `src/korean_social_simulator/evaluation/metrics.py`
    - `tests/golden/metrics/`
    - `tests/unit/evaluation/test_metrics.py`
  - Requirements covered:
    - Requirement 8
  - Acceptance checks:
    - [ ] Configured metrics write JSON and CSV.
    - [ ] Unsupported metrics are marked unavailable.
    - [ ] Golden metric outputs match fixtures.

- [ ] Task 6.5: Implement markdown report generator
  - Files to create or modify:
    - `src/korean_social_simulator/reporting/markdown.py`
    - `tests/golden/reports/`
    - `tests/unit/reporting/test_markdown_report.py`
  - Requirements covered:
    - Requirement 9
  - Acceptance checks:
    - [ ] Report includes summary, metrics, examples, safety notes, limitations, and follow-up validation.
    - [ ] Partial reports identify missing outputs.
    - [ ] Report does not make unsupported real-world prediction claims.

## Phase 7: End-to-End Verification and Documentation

- [ ] Task 7.1: Add end-to-end dry-run smoke test
  - Files to create or modify:
    - `tests/smoke/test_product_reaction_dry_run.py`
  - Requirements covered:
    - Requirement 14
    - Requirement 18
  - Acceptance checks:
    - [ ] Full dry-run from fixture to report passes offline.
    - [ ] Output files are created in tempdir.

- [ ] Task 7.2: Update documentation after MVP behavior stabilizes
  - Files to create or modify:
    - `README.md`
    - `docs/architecture.md`
    - `specs/korean-social-simulation/changelog.md`
  - Requirements covered:
    - Requirement 18
  - Acceptance checks:
    - [ ] README usage commands match implementation.
    - [ ] Architecture reflects actual module names.
    - [ ] Changelog records implemented MVP docs updates.

- [ ] Task 7.3: Final verification
  - Files to create or modify:
    - None unless fixes are required.
  - Requirements covered:
    - All requirements
  - Acceptance checks:
    - [ ] `uv run pytest` passes.
    - [ ] `uv run ruff check .` passes.
    - [ ] `uv run mypy src` passes.
    - [ ] Completion report lists exact commands and results.
