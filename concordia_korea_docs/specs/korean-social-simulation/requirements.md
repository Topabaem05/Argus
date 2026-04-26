# Requirements

## Core Functional Requirements

### Requirement 1: Load Persona Source

#### User Story

As a simulation operator, I want the system to load synthetic Korean persona records from either a Hugging Face dataset or a local fixture, so that simulations can be developed offline and run against the real dataset later.

#### Acceptance Criteria

##### Scenario 1: Load local fixture
GIVEN a valid local fixture file with required persona fields  
WHEN the operator runs the loader in fixture mode  
THEN the system returns validated persona records without network access.

##### Scenario 2: Missing required field
GIVEN a fixture row missing `uuid`, `persona`, `age`, `occupation`, `district`, or `province`  
WHEN the system validates the row  
THEN it raises a `PersonaSchemaError` identifying the missing field.

##### Scenario 3: HF dataset unavailable
GIVEN the operator requests Hugging Face mode and network or cache access fails  
WHEN the loader attempts to load the dataset  
THEN it raises `DatasetLoadError` and does not silently fall back to a different dataset.

### Requirement 2: Deterministic Persona Sampling

#### User Story

As a researcher, I want deterministic persona sampling with filters and seeds, so that experiments are reproducible.

#### Acceptance Criteria

##### Scenario 1: Same seed, same sample
GIVEN the same fixture, filters, sample size, and seed  
WHEN sampling runs twice  
THEN the selected persona UUIDs are identical and ordered identically.

##### Scenario 2: Insufficient rows
GIVEN a filter that matches fewer rows than requested  
WHEN sampling is requested  
THEN the system raises `SamplingError` unless `allow_smaller_sample` is explicitly true.

##### Scenario 3: Invalid age range
GIVEN a sampling config with `min_age > max_age`  
WHEN config validation runs  
THEN the system raises `ConfigurationError` before loading data.

### Requirement 3: Convert Personas to Agent Profiles

#### User Story

As a simulation designer, I want persona rows converted into structured agent profiles, so that Concordia agents have consistent background, memory, goals, and behavior rules.

#### Acceptance Criteria

##### Scenario 1: Valid profile generation
GIVEN a valid persona record  
WHEN the profile builder runs  
THEN it outputs an `AgentProfile` with `agent_id`, `display_name`, `background`, `memory_seeds`, `goals`, `behavior_rules`, and `language`.

##### Scenario 2: Korean language behavior
GIVEN the scenario language is `ko`  
WHEN the profile is rendered  
THEN the behavior rules instruct the agent to speak Korean unless the scenario overrides it.

##### Scenario 3: Unsafe label rejected
GIVEN a profile template asks the system to infer real political affiliation or protected-class vulnerability  
WHEN safety validation runs  
THEN the system blocks the profile with `SafetyViolationError`.

### Requirement 4: Compile Scenario Templates

#### User Story

As a scenario author, I want YAML scenario templates compiled into typed simulation plans, so that scenarios are reusable and testable.

#### Acceptance Criteria

##### Scenario 1: Supported scenario family
GIVEN a scenario family of `product_reaction`, `pricing_reaction`, `viral_marketing_risk`, `rumor_crisis_response`, `conflict_mediation`, `policy_notice_acceptance`, `community_operation`, `organization_negotiation`, or `game_npc_social_world`  
WHEN compilation runs  
THEN the system produces a valid `SimulationPlan`.

##### Scenario 2: Unknown scenario family
GIVEN an unknown scenario family  
WHEN compilation runs  
THEN the system raises `ScenarioValidationError` listing supported families.

##### Scenario 3: Missing metrics
GIVEN a scenario with no metrics configured  
WHEN compilation runs  
THEN default metrics for the selected scenario family are applied and recorded in the plan.

### Requirement 5: Optional RAG Grounding

#### User Story

As a scenario designer, I want optional PageIndex MCP/RAG grounding, so that scenarios can reference product documents, policies, reports, and prior logs without making RAG mandatory.

#### Acceptance Criteria

##### Scenario 1: RAG disabled
GIVEN `rag.enabled=false`  
WHEN the scenario compiles  
THEN no PageIndex client is initialized and the simulation proceeds without retrieved context.

##### Scenario 2: RAG optional and unavailable
GIVEN `rag.enabled=true` and `rag.required=false`  
WHEN retrieval fails  
THEN the system records a warning and continues with an explicit `rag_unavailable` note.

##### Scenario 3: RAG required and unavailable
GIVEN `rag.enabled=true` and `rag.required=true`  
WHEN retrieval fails  
THEN the system fails before simulation with `RetrievalError`.

### Requirement 6: Safety Validation

#### User Story

As a project owner, I want safety validation before simulation execution, so that the tool is not used for manipulation, real-person profiling, or unsafe targeting.

#### Acceptance Criteria

##### Scenario 1: Allowed conflict-prevention scenario
GIVEN a scenario that tests whether a community FAQ reduces misunderstanding  
WHEN safety validation runs  
THEN the scenario is allowed.

##### Scenario 2: Disallowed political targeting
GIVEN a scenario asking which political subgroup is easiest to persuade  
WHEN safety validation runs  
THEN the scenario is blocked with `SafetyViolationError`.

##### Scenario 3: Real-user inference request
GIVEN a scenario asking to infer real users' ideology from behavior logs  
WHEN safety validation runs  
THEN the scenario is blocked.

### Requirement 7: Run Simulation

#### User Story

As an operator, I want to run a dry-run or mocked-LLM simulation, so that the pipeline can be verified without paid API calls.

#### Acceptance Criteria

##### Scenario 1: Dry-run execution
GIVEN a valid compiled plan and `dry_run=true`  
WHEN the simulation runner executes  
THEN it writes structural events without calling an LLM provider.

##### Scenario 2: Turn limit enforced
GIVEN `max_turns=3`  
WHEN simulation runs  
THEN no more than three turns are executed.

##### Scenario 3: Simulation failure
GIVEN the LLM adapter returns an error during turn execution  
WHEN simulation runs  
THEN the system writes partial logs if safe and marks the run status as `partial` or `failed`.

### Requirement 8: Collect Logs and Metrics

#### User Story

As an analyst, I want structured logs and metrics, so that simulation outputs can be audited and compared.

#### Acceptance Criteria

##### Scenario 1: JSONL event log
GIVEN a completed run  
WHEN storage writes events  
THEN `events.jsonl` exists and each line contains `run_id`, `turn`, `event_type`, `timestamp`, and `payload`.

##### Scenario 2: Metrics output
GIVEN a completed or partial run  
WHEN evaluation runs  
THEN `metrics.json` and `metrics.csv` are written with configured metric names and values.

##### Scenario 3: Unsupported metric
GIVEN a scenario requests an unsupported metric  
WHEN evaluation runs  
THEN the metric is recorded as unavailable with an explicit error instead of crashing the full report.

### Requirement 9: Generate Report

#### User Story

As a human reviewer, I want a markdown report, so that I can inspect hypotheses, metrics, examples, limitations, and next validation steps.

#### Acceptance Criteria

##### Scenario 1: Report generated
GIVEN a completed run with metrics  
WHEN report generation runs  
THEN `report.md` includes summary, scenario, sample description, metrics, selected examples, safety notes, limitations, and recommended follow-up validation.

##### Scenario 2: Partial run report
GIVEN a partial run  
WHEN report generation runs  
THEN the report states the partial status and lists missing outputs.

##### Scenario 3: No unsupported prediction claim
GIVEN any report  
WHEN reviewing report text  
THEN it does not state that synthetic results prove real-world population behavior.

## Input Requirements

### Requirement 10: Validate Configuration

#### User Story

As an operator, I want invalid config files rejected early, so that runtime failures are minimized.

#### Acceptance Criteria

##### Scenario 1: Required config missing
GIVEN a config missing `scenario.id`  
WHEN config validation runs  
THEN `ConfigurationError` identifies `scenario.id`.

##### Scenario 2: Secret in config file
GIVEN a committed example config containing a literal API key value  
WHEN static validation runs  
THEN the check fails and instructs the user to use environment variables.

## Output Requirements

### Requirement 11: Stable Output Layout

#### User Story

As a developer, I want predictable output paths, so that tests and downstream tools can locate artifacts.

#### Acceptance Criteria

##### Scenario 1: New run output
GIVEN scenario ID `product_reaction` and run ID `run_001`  
WHEN the run completes  
THEN outputs are stored under `outputs/product_reaction/run_001/`.

##### Scenario 2: Existing output path
GIVEN the output directory already exists and overwrite is false  
WHEN the run starts  
THEN the system raises `StorageError` before writing partial files.

## Configuration Requirements

### Requirement 12: Environment Overrides

#### User Story

As an operator, I want environment variables to override local config for secrets and runtime choices, so that configs can be committed safely.

#### Acceptance Criteria

##### Scenario 1: LLM API key from environment
GIVEN `KSSIM_LLM_API_KEY` is set  
WHEN config loads  
THEN the runtime config uses the environment value and redacts it in logs.

##### Scenario 2: Missing required secret
GIVEN live LLM mode is requested and no API key is available  
WHEN config validation runs  
THEN the system raises `ConfigurationError` before simulation.

## Error Handling Requirements

### Requirement 13: Typed Failures

#### User Story

As a developer, I want typed project errors, so that failure modes are testable and user messages are clear.

#### Acceptance Criteria

##### Scenario 1: Dataset error type
GIVEN a dataset path does not exist  
WHEN loading starts  
THEN a `DatasetLoadError` is raised.

##### Scenario 2: Safety error type
GIVEN a prohibited scenario objective  
WHEN safety validation runs  
THEN a `SafetyViolationError` is raised.

## Testing Requirements

### Requirement 14: Offline Verification

#### User Story

As an AI coding agent, I want verification commands to pass offline, so that implementation can be checked without external services.

#### Acceptance Criteria

##### Scenario 1: Offline tests
GIVEN no LLM key and no PageIndex key  
WHEN `uv run pytest` runs  
THEN default tests pass using fixtures and mocks.

##### Scenario 2: Live tests excluded by default
GIVEN live integration tests exist  
WHEN default pytest runs  
THEN live tests are skipped unless explicitly selected by marker.

## Performance Requirements

### Requirement 15: Bounded Runtime and Memory

#### User Story

As an operator, I want bounded simulation runtime and memory usage, so that failed scenarios do not run indefinitely.

#### Acceptance Criteria

##### Scenario 1: Max participants
GIVEN a config requests more participants than `runtime.max_participants`  
WHEN config validation runs  
THEN it fails with `ConfigurationError`.

##### Scenario 2: Max turns
GIVEN a scenario exceeds configured max turns  
WHEN simulation runs  
THEN the runner stops at the limit and records `turn_limit_reached`.

## Security Requirements

### Requirement 16: Secret and Privacy Protection

#### User Story

As a project owner, I want secrets and sensitive content protected, so that logs and reports are safe to review.

#### Acceptance Criteria

##### Scenario 1: Secret redaction
GIVEN an API key is configured  
WHEN config is logged  
THEN the key is redacted.

##### Scenario 2: No raw private docs in reports
GIVEN RAG returns document text marked private  
WHEN report generation runs  
THEN the report includes citation metadata or summary only, not raw private text, unless explicitly configured for local-only output.

## Compatibility Requirements

### Requirement 17: Optional Dependencies

#### User Story

As a developer, I want optional RAG and live LLM dependencies isolated, so that the core project runs in dry-run mode with minimal setup.

#### Acceptance Criteria

##### Scenario 1: RAG package unavailable
GIVEN PageIndex dependencies are not installed  
WHEN RAG is disabled  
THEN the core simulation still runs.

##### Scenario 2: RAG enabled without dependency
GIVEN PageIndex dependencies are unavailable and RAG is enabled  
WHEN config validation runs  
THEN a clear `ConfigurationError` explains the missing optional dependency.

## Documentation Requirements

### Requirement 18: Agent-Ready Documentation

#### User Story

As a future AI coding agent, I want strict project documentation, so that I can implement tasks safely and verifiably.

#### Acceptance Criteria

##### Scenario 1: Required docs exist
GIVEN the repository is initialized  
WHEN documentation is checked  
THEN `AGENTS.md`, `README.md`, `docs/architecture.md`, `docs/coding-style.md`, ADRs, and spec files exist.

##### Scenario 2: Completion report required
GIVEN an implementation task is completed  
WHEN the agent reports completion  
THEN the report includes files changed, requirements satisfied, commands run, test results, limitations, and next task.
