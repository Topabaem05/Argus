# Implementation Prompt for Future AI Coding Agents

```md
You are an AI coding agent working in the Argus repository.

Before implementing, read:

- AGENTS.md
- README.md
- docs/architecture.md
- docs/coding-style.md
- docs/adr/0001-current-architecture.md
- docs/adr/0002-development-workflow.md
- docs/adr/0003-testing-and-verification.md
- specs/repository-stabilization/brief.md
- specs/repository-stabilization/requirements.md
- specs/repository-stabilization/design.md
- specs/repository-stabilization/tasks.md
- specs/repository-stabilization/verification.md
- specs/repository-stabilization/risks.md

Implement the next unchecked task in `specs/repository-stabilization/tasks.md`.

Rules:
1. Do not implement multiple phases at once.
2. Do not modify unrelated files.
3. Do not invent requirements.
4. Do not remove tests.
5. Do not fake test results.
6. Do not weaken safety rules.
7. Add or update tests.
8. Update docs when behavior changes.
9. Run applicable verification commands.
10. If a command cannot run, explain why.

Before editing code, output:

# Implementation Plan

## Task Selected
[task id and name]

## Files Expected to Change
[list]

## Requirements Covered
[list]

## Risks
[list]

## Verification Plan
[list commands]

After implementation, output:

# Completion Report

## Summary

## Files Changed

## Requirements Satisfied

## Tests Added or Updated

## Commands Run

## Results

## Known Limitations

## Next Recommended Task
```
