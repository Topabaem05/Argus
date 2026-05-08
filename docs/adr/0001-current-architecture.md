# ADR 0001: Current Modular Architecture and Stabilization Target

## Status

Accepted

## Context

Before stabilization, Argus had useful modules for config, loading, sampling, profiles, scenarios, safety, simulation, storage, evaluation, and reporting, but the CLI was not fully wired, examples were inconsistent with the registry, optional adapters were incomplete, and documentation overstated current completion.

## Decision

Preserve the modular architecture and stabilize it through a clear pipeline:

```txt
config -> persona loading -> sampling -> profiles -> scenario -> safety -> simulation -> storage -> evaluation -> report
```

The first target is deterministic offline dry-run. Live Hugging Face, Concordia, and LLM behavior remain optional adapter paths. PageIndex/RAG remains mocked/compiler-level until a live provider is wired into the CLI pipeline.

## Consequences

What becomes easier:

- smaller patches,
- easier unit tests,
- reliable offline MVP,
- safer AI-agent maintenance.

What becomes harder:

- CLI must coordinate modules correctly,
- optional adapters need explicit contracts,
- README must distinguish stable and experimental behavior.

## Alternatives Considered

- Monolithic rewrite: rejected because it reduces testability.
- Live LLM first: rejected because it is nondeterministic and credential-dependent.
- Split packages now: rejected because the repo is not stable enough.

## Validation

This works when every CLI command executes real behavior and offline smoke tests generate the expected artifact tree.
