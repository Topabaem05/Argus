# ADR 001: MVP Architecture - Modular Pipeline

## Status

Accepted

## Context

The Korean Social Simulation Lab needs a Python pipeline that loads synthetic personas, samples them deterministically, builds agent profiles, compiles scenarios, validates safety, runs simulations, and produces auditable logs and reports. The MVP must work offline with fixtures and no mandatory network access.

## Decision

We chose a modular pipeline architecture with explicit, non-overlapping modules:

1. **config/** - YAML-based configuration with Pydantic validation and environment variable overrides
2. **data/** - Persona source adapters (fixture and Hugging Face)
3. **personas/** - Deterministic sampling with filters and seeds
4. **agents/** - Agent profile builder with Korean language support
5. **scenarios/** - 9 supported scenario families with a registry and compiler
6. **safety/** - Pattern-based safety validator blocking prohibited use cases
7. **rag/** - Optional retriever protocol with no-op and mock PageIndex MCP adapters
8. **simulation/** - Dry-run and Concordia adapter boundaries
9. **storage/** - JSONL event log writer with overwrite protection
10. **evaluation/** - Counting-based placeholder metric evaluators
11. **reporting/** - Markdown report generator with safety limitations

## Rationale

- **Module boundaries** prevent coupling between concerns (e.g., persona loading doesn't depend on Concordia)
- **Deterministic seed-based** sampling ensures reproducibility
- **Typed Pydantic models** enable validation at boundaries without runtime surprises
- **Optional RAG** with no-op default keeps the core lightweight
- **Stubbed CLI** allows API development to proceed independently of command-line wiring

## Consequences

- Each module can be tested independently with fixtures and mocks
- New scenario families require only registry updates
- Concordia dependency is optional — the pipeline works offline without it
- CLI commands are stubs in MVP; full CLI-wiring is post-MVP work
- Golden test infrastructure is expected but not yet populated in MVP

## Alternatives Considered

### Monolithic Main Script
Would have been simpler but unscalable for 9 scenario families and multiple data sources.

### Full CLI-first Implementation
Too much surface area for MVP validation; the Python API approach lets us verify the pipeline logic independently.

### Synchronous Network-only Operation
Rejected because offline development and CI/CD require fixture mode.