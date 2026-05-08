# Implementation Tasks

## Phase 1: Foundation

- [x] Task 1.1: Document baseline
  - Files to create or modify:
    - `REPOSITORY_ANALYSIS.md, docs/architecture.md`
  - Requirements covered:
    - RS-015
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 1.2: Fix example scenario family mismatch
  - Files to create or modify:
    - `examples/run_product_reaction.yaml, tests/unit/test_scenario_compiler.py`
  - Requirements covered:
    - RS-002
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 1.3: Align env overrides with config schema
  - Files to create or modify:
    - `config/models.py, config/loader.py, tests/unit/test_config_loader.py`
  - Requirements covered:
    - RS-003
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

## Phase 2: Core Interfaces

- [x] Task 2.1: Add persona source dispatcher
  - Files to create or modify:
    - `data/__init__.py, data/loader.py, tests/unit/test_persona_source_dispatcher.py`
  - Requirements covered:
    - RS-004
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 2.2: Define simulation execution contract
  - Files to create or modify:
    - `models.py, simulation/concordia_adapter.py, tests/unit/test_simulation_execution_model.py`
  - Requirements covered:
    - RS-009, RS-010
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 2.3: Propagate dry-run mode through scenario compiler
  - Files to create or modify:
    - `scenarios/compiler.py, tests/unit/test_scenario_compiler.py`
  - Requirements covered:
    - RS-007
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

## Phase 3: CLI Wiring

- [x] Task 3.1: Implement validate-config command
  - Files to create or modify:
    - `cli.py, tests/integration/test_cli_validate_config.py`
  - Requirements covered:
    - RS-001, RS-003
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 3.2: Implement sample command
  - Files to create or modify:
    - `cli.py, pipeline.py, tests/integration/test_cli_sample.py`
  - Requirements covered:
    - RS-001, RS-004, RS-005
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 3.3: Implement compile-scenario command
  - Files to create or modify:
    - `cli.py, pipeline.py, tests/integration/test_cli_compile_scenario.py`
  - Requirements covered:
    - RS-001, RS-002, RS-007
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 3.4: Implement run --dry-run end-to-end
  - Files to create or modify:
    - `cli.py, pipeline.py, tests/smoke/test_smoke_example_dry_run.py`
  - Requirements covered:
    - RS-001, RS-004-RS-013
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 3.5: Implement evaluate command
  - Files to create or modify:
    - `cli.py, tests/integration/test_cli_evaluate.py`
  - Requirements covered:
    - RS-001, RS-012
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 3.6: Implement report command
  - Files to create or modify:
    - `cli.py, tests/integration/test_cli_report.py, tests/golden/test_report_golden.py`
  - Requirements covered:
    - RS-001, RS-013
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

## Phase 4: Safety and Errors

- [x] Task 4.1: Centralize CLI error handling
  - Files to create or modify:
    - `cli.py, tests/integration/test_cli_errors.py`
  - Requirements covered:
    - RS-001, RS-003, RS-008
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 4.2: Strengthen Korean and English safety patterns
  - Files to create or modify:
    - `safety/validator.py, agents/profile_builder.py, tests/unit/test_safety_validator.py`
  - Requirements covered:
    - RS-008
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 4.3: Validate run IDs and output paths
  - Files to create or modify:
    - `config/models.py, storage/run_store.py, tests/unit/test_run_id_path_safety.py`
  - Requirements covered:
    - RS-011, RS-016
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

## Phase 5: Testing

- [x] Task 5.1: Add unit tests for foundation modules
  - Files to create or modify:
    - `tests/unit/*`
  - Requirements covered:
    - RS-003-RS-007
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 5.2: Add unit tests for runtime modules
  - Files to create or modify:
    - `tests/unit/*`
  - Requirements covered:
    - RS-008-RS-013
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 5.3: Add integration and smoke tests
  - Files to create or modify:
    - `tests/integration/*, tests/smoke/*`
  - Requirements covered:
    - RS-001, RS-014
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 5.4: Add golden report tests
  - Files to create or modify:
    - `tests/golden/*`
  - Requirements covered:
    - RS-013, RS-014
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

## Phase 6: Optional Adapters

- [x] Task 6.1: Add optional dependency extras
  - Files to create or modify:
    - `pyproject.toml, README.md, docs/architecture.md`
  - Requirements covered:
    - RS-010, RS-015
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 6.2: Add mocked live adapter tests
  - Files to create or modify:
    - `tests/unit/test_live_adapter_contract.py`
  - Requirements covered:
    - RS-010
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

## Phase 7: Documentation and Final Verification

- [x] Task 7.1: Update README truthfulness
  - Files to create or modify:
    - `README.md`
  - Requirements covered:
    - RS-015
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 7.2: Add/update AGENTS.md
  - Files to create or modify:
    - `AGENTS.md`
  - Requirements covered:
    - RS-014, RS-015
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.

- [x] Task 7.3: Run final verification and report results
  - Files to create or modify:
    - `docs/completion-report.md`
  - Requirements covered:
    - RS-014
  - Acceptance checks:
    - [x] Behavior is implemented only for this task scope.
    - [x] Tests are added or updated.
    - [x] Documentation is updated if behavior changes.
    - [x] Relevant verification commands are run or blocked reason is recorded.
  - Notes:
    - Prefer the smallest safe change. Do not rewrite unrelated modules.
