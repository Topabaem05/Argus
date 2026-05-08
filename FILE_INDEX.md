# Argus Documentation Stabilization Package

This ZIP is a documentation-only SDD package for stabilizing `Topabaem05/Argus` into a 10/10-quality Korean Social Simulation Lab.

## Files

```txt
AGENTS.md
README.md
REPOSITORY_ANALYSIS.md
SCORECARD_TO_10.md
IMPLEMENTATION_PROMPT.md
DOCUMENTATION_SELF_REVIEW.md
docs/
  architecture.md
  coding-style.md
  adr/
    0001-current-architecture.md
    0002-development-workflow.md
    0003-testing-and-verification.md
specs/
  repository-stabilization/
    brief.md
    requirements.md
    design.md
    tasks.md
    verification.md
    risks.md
    changelog.md
```

## How to Use

1. Copy these files into the root of the Argus repository.
2. Read `AGENTS.md` first.
3. Implement one unchecked task at a time from `specs/repository-stabilization/tasks.md`.
4. Run the verification commands in `specs/repository-stabilization/verification.md`.
5. Do not claim 10/10 completion until every gate in `SCORECARD_TO_10.md` is satisfied.

## Scope

This package contains no implementation code. It is specification, architecture, requirements, verification, and AI-agent workflow documentation only.
