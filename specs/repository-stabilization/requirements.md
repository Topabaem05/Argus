# Requirements

## Requirement RS-001: CLI Commands Execute Real Pipeline Behavior

### User Story

As a developer, I want each documented `kssim` command to execute real repository behavior, so that the CLI can be trusted.

### Acceptance Criteria

#### Scenario 1: Config validation succeeds
GIVEN a valid YAML config exists  
WHEN `kssim validate-config` runs  
THEN exit code is 0 and no artifacts are created.

#### Scenario 2: Sample writes artifact
GIVEN fixture-mode config exists  
WHEN `kssim sample` runs  
THEN a deterministic sample JSON is written.

#### Scenario 3: Compile writes plan
GIVEN supported scenario family is configured  
WHEN `kssim compile-scenario` runs  
THEN a serialized `SimulationPlan` is written.

#### Scenario 4: Run writes artifacts
GIVEN valid fixture config exists  
WHEN `kssim run --dry-run` runs  
THEN events, metrics, metadata, profiles, sample, plan, and report artifacts exist.

#### Scenario 5: Evaluate writes metrics
GIVEN events.jsonl exists  
WHEN `kssim evaluate` runs  
THEN metrics JSON/CSV are written.

#### Scenario 6: Report writes Markdown
GIVEN run directory exists  
WHEN `kssim report` runs  
THEN report contains summary, metrics, event examples, limitations.

## Requirement RS-002: Example Config Matches Scenario Registry

### User Story

As a developer, I want examples to use supported families, so documented commands work.

### Acceptance Criteria

#### Scenario 1: Product example compiles
GIVEN `examples/run_product_reaction.yaml` exists  
WHEN scenario compile runs  
THEN `scenario.family` is supported.

#### Scenario 2: Unknown family fails
GIVEN family is `unknown_family`  
WHEN compile runs  
THEN `ScenarioValidationError` lists supported families.

#### Scenario 3: Default metrics apply
GIVEN metrics list is empty  
WHEN compile runs  
THEN family default metrics are attached.

## Requirement RS-003: Strict Configuration Schema

### User Story

As a maintainer, I want invalid config rejected before simulation starts.

### Acceptance Criteria

#### Scenario 1: Extra fields rejected
GIVEN YAML contains undeclared field  
WHEN config loads  
THEN `ConfigurationError` is raised.

#### Scenario 2: Env overrides declared
GIVEN override env vars are set  
WHEN config loads  
THEN overrides map only to declared fields.

#### Scenario 3: Runtime limits enforced
GIVEN sample size exceeds max participants  
WHEN config loads  
THEN `ConfigurationError` is raised.

#### Scenario 4: Dry-run no secret
GIVEN dry-run true and no API key  
WHEN config loads  
THEN validation succeeds.

#### Scenario 5: Live mode needs secret
GIVEN dry-run false and no credential  
WHEN config loads  
THEN `ConfigurationError` is raised.

## Requirement RS-004: Persona Loading

### User Story

As a researcher, I want fixture and optional HF persona loading.

### Acceptance Criteria

#### Scenario 1: Fixture success
GIVEN valid JSONL fixture exists  
WHEN fixture loader runs  
THEN `PersonaRecord` list is returned.

#### Scenario 2: Missing fixture
GIVEN fixture path missing  
WHEN loader runs  
THEN `DatasetLoadError` is raised.

#### Scenario 3: Invalid JSON
GIVEN fixture line is invalid JSON  
WHEN loader runs  
THEN line number appears in error.

#### Scenario 4: Missing field
GIVEN row lacks required field  
WHEN loader runs  
THEN `PersonaSchemaError` is raised.

#### Scenario 5: HF dependency missing
GIVEN HF mode without `datasets`  
WHEN loader runs  
THEN actionable `DatasetLoadError` is raised.

## Requirement RS-005: Deterministic Sampling

### User Story

As a simulation author, I want reproducible filtered samples.

### Acceptance Criteria

#### Scenario 1: Same seed same sample
GIVEN same personas and seed  
WHEN sampling runs twice  
THEN selected UUIDs match.

#### Scenario 2: Filters applied
GIVEN filters are configured  
WHEN sampling runs  
THEN all selected personas match filters.

#### Scenario 3: Insufficient rows fail
GIVEN not enough matches and allow smaller false  
WHEN sampling runs  
THEN `SamplingError` is raised.

#### Scenario 4: Smaller sample allowed
GIVEN not enough matches and allow smaller true  
WHEN sampling runs  
THEN all matching rows returned.

## Requirement RS-006: Agent Profiles

### User Story

As a runner, I want consistent agent profiles.

### Acceptance Criteria

#### Scenario 1: One profile per persona
GIVEN sample has N records  
WHEN profiles build  
THEN N profiles are returned.

#### Scenario 2: Korean rule
GIVEN language is ko  
WHEN profiles build  
THEN Korean behavior rule is included.

#### Scenario 3: Empty sample fails
GIVEN sample has no records  
WHEN profiles build  
THEN `AgentProfileError` is raised.

#### Scenario 4: Unsafe content blocked
GIVEN persona has prohibited content  
WHEN profiles build  
THEN `SafetyViolationError` is raised.

## Requirement RS-007: Scenario Compilation

### User Story

As a developer, I want typed executable plans.

### Acceptance Criteria

#### Scenario 1: Supported family compiles
GIVEN family is supported  
WHEN compile runs  
THEN `SimulationPlan` is returned.

#### Scenario 2: Dry-run propagated
GIVEN runtime mode is set  
WHEN compile runs  
THEN plan dry-run matches effective runtime.

#### Scenario 3: Available RAG attached
GIVEN retrieval status is available  
WHEN compile runs  
THEN query appears in plan metadata.

#### Scenario 4: Optional RAG unavailable
GIVEN RAG optional and unavailable  
WHEN compile runs  
THEN compile continues with warning.

## Requirement RS-008: Safety Blocks Prohibited Use

### User Story

As a project owner, I want unsafe simulations blocked.

### Acceptance Criteria

#### Scenario 1: Allowed product passes
GIVEN benign product scenario  
WHEN safety validates  
THEN allowed decision returned.

#### Scenario 2: Political persuasion blocked
GIVEN scenario asks political persuasion  
WHEN safety validates  
THEN `SafetyViolationError` is raised.

#### Scenario 3: Real-person profiling blocked
GIVEN scenario profiles real users  
WHEN safety validates  
THEN `SafetyViolationError` is raised.

#### Scenario 4: Protected targeting blocked
GIVEN scenario targets protected group  
WHEN safety validates  
THEN `SafetyViolationError` is raised.

#### Scenario 5: Korean phrases blocked
GIVEN Korean prohibited phrase appears  
WHEN safety validates  
THEN `SafetyViolationError` is raised.

## Requirement RS-009: Dry-Run Is Offline

### User Story

As a developer, I want dry-run to require no network or secrets.

### Acceptance Criteria

#### Scenario 1: No external call
GIVEN no network and no API keys  
WHEN dry-run executes  
THEN run succeeds locally.

#### Scenario 2: Predictable event count
GIVEN A agents and T turns  
WHEN dry-run executes  
THEN `T * (A + 2) + 1` events are emitted if structural design is preserved.

#### Scenario 3: Invalid turns fail
GIVEN max_turns < 1  
WHEN dry-run executes  
THEN clear error is raised.

#### Scenario 4: Events identify run
GIVEN valid plan exists  
WHEN events emitted  
THEN every event has run ID and turn.

## Requirement RS-010: Optional Live Adapters

### User Story

As a maintainer, I want live adapters isolated from offline MVP.

### Acceptance Criteria

#### Scenario 1: No NVIDIA without key
GIVEN NVIDIA_API_KEY absent  
WHEN availability checked  
THEN NIM is not called.

#### Scenario 2: Missing Concordia honest
GIVEN Concordia missing  
WHEN live mode requested  
THEN partial/failed result with explicit error.

#### Scenario 3: Events not discarded
GIVEN mock live adapter returns events  
WHEN simulation completes  
THEN events are persisted.

#### Scenario 4: Live tests marked
GIVEN test needs external service  
WHEN tests collected  
THEN live marker is present.

## Requirement RS-011: Stable Run Store

### User Story

As a user, I want auditable run artifacts.

### Acceptance Criteria

#### Scenario 1: Run dir created
GIVEN run starts  
WHEN store initializes  
THEN `outputs/<run_id>` exists.

#### Scenario 2: Existing protected
GIVEN events.jsonl exists and overwrite false  
WHEN store initializes  
THEN `StorageError` is raised.

#### Scenario 3: Overwrite resets
GIVEN overwrite true  
WHEN store initializes  
THEN managed files reset.

#### Scenario 4: JSONL valid
GIVEN events written  
WHEN events read  
THEN each line parses as JSON.

## Requirement RS-012: Explicit Metrics

### User Story

As a report reader, I want metrics not mistaken for predictions.

### Acceptance Criteria

#### Scenario 1: Counts computed
GIVEN events exist  
WHEN evaluate runs  
THEN event, turn, agent counts available.

#### Scenario 2: Unknown unavailable
GIVEN unknown metric requested  
WHEN evaluate runs  
THEN metric listed as unavailable.

#### Scenario 3: Placeholders documented
GIVEN placeholder metric returned  
WHEN report renders  
THEN limitations state synthetic/non-predictive.

## Requirement RS-013: Complete Markdown Reports

### User Story

As a stakeholder, I want honest run reports.

### Acceptance Criteria

#### Scenario 1: Required sections
GIVEN completed run exists  
WHEN report renders  
THEN summary, metrics, events, safety, warnings, errors, limitations appear.

#### Scenario 2: Partial warnings
GIVEN status partial/failed  
WHEN report renders  
THEN warning and error counts appear.

#### Scenario 3: Empty metrics handled
GIVEN no metrics exist  
WHEN report renders  
THEN no-metrics row appears.

#### Scenario 4: Golden stable
GIVEN timestamps normalized  
WHEN golden test runs  
THEN report matches fixture.

## Requirement RS-014: Verification Commands Required

### User Story

As a maintainer, I want exact commands so completion can be checked.

### Acceptance Criteria

#### Scenario 1: Tests pass
GIVEN repo stabilized  
WHEN `uv run pytest` runs  
THEN offline tests pass.

#### Scenario 2: Lint passes
GIVEN repo stabilized  
WHEN `uv run ruff check .` runs  
THEN exit code 0.

#### Scenario 3: Types pass
GIVEN repo stabilized  
WHEN `uv run mypy src` runs  
THEN exit code 0.

#### Scenario 4: Commands reported
GIVEN agent completes task  
WHEN report is written  
THEN exact commands/results are listed.

## Requirement RS-015: Truthful Documentation

### User Story

As a contributor, I want docs to match behavior.

### Acceptance Criteria

#### Scenario 1: README accurate
GIVEN repo incomplete  
WHEN README read  
THEN does not claim production readiness.

#### Scenario 2: Optional labeled
GIVEN feature needs external service  
WHEN docs mention it  
THEN it is labeled optional/experimental/planned.

#### Scenario 3: Docs updated
GIVEN behavior changes  
WHEN change merged  
THEN relevant docs updated.

## Requirement RS-016: Compatibility, Performance, Security

### User Story

As a maintainer, I want stable baselines.

### Acceptance Criteria

#### Scenario 1: Python version clear
GIVEN developer reads repo  
WHEN pyproject/README inspected  
THEN Python 3.11+ stated.

#### Scenario 2: Example bounded
GIVEN example dry-run executes  
WHEN run starts  
THEN participants/turns remain small.

#### Scenario 3: Secrets not written
GIVEN API keys in env  
WHEN run completes  
THEN artifacts contain no raw secrets.

#### Scenario 4: No hidden network
GIVEN --dry-run set  
WHEN run executes  
THEN no HF/PageIndex/LLM/Concordia network is required.
