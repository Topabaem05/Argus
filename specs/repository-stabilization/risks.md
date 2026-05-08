# Risk Register

| Risk ID | Risk | Impact | Likelihood | Mitigation | Detection |
|---|---|---:|---:|---|---|
| R-001 | CLI commands remain placeholders | High | High | Wire CLI and add CLI tests | Smoke test asserts artifacts |
| R-002 | Example family unsupported | High | High | Use supported family or tested alias | Compiler test |
| R-003 | Env override adds forbidden fields | High | Medium | Align schema/overrides | Config env tests |
| R-004 | Dry-run flag ignored | Medium | High | Propagate runtime mode | Compiler test |
| R-005 | Live events discarded | High | Medium | Execution result includes events | Mock adapter test |
| R-006 | Optional dependency becomes required | Medium | Medium | Extras and marked tests | Clean install smoke |
| R-007 | Korean unsafe phrases bypass safety | High | High | Add Korean patterns and fixtures | Safety tests |
| R-008 | Reports imply prediction | High | Medium | Mandatory limitation text | Golden report test |
| R-009 | Secrets leak to artifacts | High | Medium | Redact/omit secrets | Artifact secret scan |
| R-010 | Run artifacts overwritten | High | Medium | Overwrite protection | RunStore tests |
| R-011 | Invalid JSONL written | High | Low | Serialize through models | JSONL parse test |
| R-012 | HF tests flaky | Medium | Medium | Fixture default; mark live | Test markers |
| R-013 | AI agent rewrites unrelated files | High | Medium | AGENTS rules and task scope | Diff review |
| R-014 | AI agent fakes tests | High | Medium | Require exact commands | Completion report review |
| R-015 | Over-engineering slows MVP | Medium | Medium | One task at a time | Task review |
| R-016 | Safety weakened to pass tests | High | Low | Safety non-negotiable | Blocked fixtures |
| R-017 | Path traversal in run_id | High | Low | Validate run IDs | Path safety tests |
| R-018 | README overstates maturity | Medium | High | Truthful status section | Manual docs review |
| R-019 | Metrics misread as real sentiment | Medium | Medium | Placeholder labeling | Report tests |
| R-020 | Concordia upstream changes | Medium | Medium | Adapter isolation | Missing dependency test |
## AI Agent Failure Modes

### Modifies unrelated files
Mitigation: task-to-file mapping and minimal diffs. Detection: diff review.

### Ignores requirements
Mitigation: every task maps to requirement IDs. Detection: completion report review.

### Invents unsupported APIs
Mitigation: inspect existing code first. Detection: import/type tests.

### Removes tests
Mitigation: forbidden in `AGENTS.md`. Detection: diff and test count review.

### Claims tests passed without running them
Mitigation: exact command reporting. Detection: reject vague completion reports.

### Over-engineers beyond spec
Mitigation: one task at a time. Detection: scope review.

### Weakens safety
Mitigation: safety rules are non-negotiable. Detection: blocked scenario tests.

### Makes live services mandatory
Mitigation: offline smoke test without API keys. Detection: clean environment run.
