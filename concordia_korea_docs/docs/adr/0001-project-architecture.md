# ADR 0001: Project Architecture

## Status

Proposed

## Context

The project must combine a structured synthetic Korean persona dataset, an LLM-based social simulation engine, optional document grounding, safety constraints, and measurable outputs. The system must be usable by AI coding agents and human developers without ambiguity.

The main architectural question is whether the project should be built as a fine-tuned model, a monolithic script, or a modular simulation pipeline.

## Decision

Use a modular Python pipeline:

1. Load and sample personas from Nemotron-Personas-Korea.
2. Convert personas into Concordia-compatible agent profiles.
3. Compile scenario templates into simulation plans.
4. Optionally retrieve grounding context through PageIndex MCP/RAG.
5. Run Concordia simulations through an adapter layer.
6. Persist logs and metrics.
7. Generate reports.
8. Keep fine-tuning as a post-MVP optimization path only.

## Consequences

What becomes easier:

- Each module can be tested independently.
- AI agents can implement one task at a time.
- Optional RAG and optional fine-tuning remain decoupled from the MVP.
- Safety validation can happen before simulation execution.
- Scenario categories can be added without rewriting the engine.

What becomes harder:

- More interfaces must be defined up front.
- Integration tests are required to catch boundary mismatches.
- Concordia adapter behavior must be isolated from project-specific logic.

Tradeoffs accepted:

- Slightly more structure than a quick notebook.
- Some initial overhead in typed models and config validation.
- No early optimization through fine-tuning.

## Alternatives Considered

- Monolithic notebook: faster to start, but hard to test, audit, and maintain.
- Fine-tuned simulator model: potentially cheaper at scale, but premature, inflexible, and harder to verify.
- Pure RAG chatbot: useful for document QA, but insufficient for multi-agent social interaction.
- Fully custom agent engine: maximum control, but duplicates Concordia's core purpose.

## Validation

This decision is working if:

- Unit tests cover each module boundary.
- A dry-run scenario can execute without LLM calls.
- A mocked LLM scenario can produce logs and metrics.
- Optional RAG can be disabled without breaking the core flow.
- Safety guard tests block prohibited scenario objectives.
