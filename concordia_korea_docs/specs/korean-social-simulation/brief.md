# Korean Social Simulation Feature Brief

## Feature Name

Korean Social Simulation

## Summary

Build the MVP pipeline that samples synthetic Korean personas, converts them into Concordia agent profiles, runs configured social scenarios, collects logs and metrics, and generates reports. Optional PageIndex MCP/RAG may ground scenario facts, but the core simulator must work without RAG. Fine-tuning is explicitly out of MVP scope except for optional later log export.

## User Problem

Users need a controlled way to test hypotheses about how different synthetic Korean personas may react to products, messages, policies, community events, crises, or game-world situations. They need repeatable logs, measurable outputs, and safety controls, not ad hoc LLM roleplay.

## Goals

- Load or fixture-simulate Nemotron-Personas-Korea persona records.
- Filter and sample personas deterministically.
- Convert personas into typed agent profiles.
- Compile scenario templates across the supported scenario families.
- Enforce safety restrictions before simulation.
- Run a dry-run or mocked-LLM simulation loop.
- Persist event logs and metrics.
- Generate a markdown report with limitations and next validation steps.
- Keep RAG optional.
- Keep fine-tuning out of MVP execution path.

## Non-Goals

- No real-person profiling.
- No political persuasion targeting.
- No voter manipulation or fake grassroots generation.
- No claim that synthetic results predict real Korean society.
- No production web UI in MVP.
- No fine-tuning implementation in MVP.
- No database requirement in MVP.
- No hidden network calls.

## Primary User Flow

1. User selects scenario config.
2. System loads config and safety policy.
3. System loads persona source or test fixture.
4. System samples a deterministic synthetic population.
5. System builds Concordia-style agent profiles.
6. System compiles the scenario and optional retrieved document context.
7. System validates safety constraints.
8. System runs a dry-run or LLM-backed simulation.
9. System writes JSONL events and metric files.
10. System generates a report with limitations and recommended real-user validation.

## Expected Output

```txt
outputs/<scenario_id>/<run_id>/
├── run_metadata.json
├── sampled_personas.jsonl
├── agent_profiles.jsonl
├── scenario_plan.json
├── events.jsonl
├── metrics.json
├── metrics.csv
└── report.md
```

## Main Risks

- Overstating synthetic simulation as real-world prediction.
- Unsafe use for political or manipulative targeting.
- Non-deterministic tests due to LLM behavior.
- Large dataset performance issues.
- Optional RAG failures affecting scenario grounding.
- AI coding agents inventing unsupported Concordia or PageIndex APIs.

## Completion Definition

Complete when the MVP can run one dry-run scenario from fixture personas to report generation, all safety tests pass, and verification commands pass without requiring live LLM or PageIndex access.
