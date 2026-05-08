# ADR 0002: Development Workflow

## Status

Accepted

## Context

The repository will be maintained by humans and AI coding agents. Without strict workflow rules, agents may over-edit, fake tests, or weaken safety rules.

## Decision

Use:

- `uv` for command execution,
- Ruff for format/lint,
- Mypy strict mode,
- Pytest with markers,
- `AGENTS.md` as permanent agent rules,
- `specs/repository-stabilization/tasks.md` as the task queue.

Implementation must proceed one task at a time.

## Consequences

Easier:

- repeatable commands,
- small patches,
- auditable completion reports.

Harder:

- more upfront tests and documentation,
- stricter typing.

## Alternatives Considered

- pip-only: rejected because repository docs already use `uv`.
- no strict typing: rejected because schema/adapters need safety.
- free-form agent work: rejected because it increases rewrite risk.

## Validation

The workflow is valid when `uv run pytest`, `uv run ruff check .`, and `uv run mypy src` pass and every task has a completion report.
