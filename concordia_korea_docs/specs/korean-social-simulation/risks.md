# Risk Register

| Risk ID | Risk | Impact | Likelihood | Mitigation | Detection |
|---|---|---:|---:|---|---|
| R-001 | Synthetic simulation results are mistaken for real-world prediction | High | High | Reports must include limitations and real-user validation recommendations | Report golden tests and manual review |
| R-002 | Tool is used for political persuasion targeting or voter manipulation | High | Medium | Safety validator blocks prohibited objectives; docs state non-goals | Safety regression tests |
| R-003 | Real-user profiling or protected-group exploitation is added later | High | Medium | Permanent AGENTS.md safety rules; prohibited scenario tests | PR review and safety tests |
| R-004 | AI coding agent invents unsupported Concordia APIs | Medium | Medium | Adapter boundary; require inspection and tests against actual installed package | Integration tests and type checks |
| R-005 | PageIndex MCP assumptions differ from actual API | Medium | Medium | Keep RAG behind protocol; use mocked tests and optional live tests | Mock/live comparison tests |
| R-006 | Large persona dataset filtering is slow or memory-heavy | Medium | Medium | Use HF Datasets, Polars, or DuckDB; cache filtered samples | Performance smoke tests |
| R-007 | LLM nondeterminism breaks tests | Medium | High | Default tests use dry-run and mock LLM; live tests opt-in | CI test stability |
| R-008 | Secrets leak into logs or reports | High | Medium | Secret redaction and logging rules | Unit tests for redaction and manual review |
| R-009 | Raw private RAG documents leak into reports | High | Medium | Store citation metadata and summaries by default; private text redaction | Report tests and safety review |
| R-010 | Generated logs become too large | Medium | Medium | Stream JSONL, max turns, max participants, output retention policy | Run metadata and file-size checks |
| R-011 | Scenario templates become inconsistent | Medium | Medium | Scenario registry and schema validation | Compiler tests |
| R-012 | Metrics are vague or untestable | Medium | Medium | Define metric formulas and golden logs | Golden metric tests |
| R-013 | Fine-tuning is prematurely added to MVP | Medium | Medium | ADR states fine-tuning is post-MVP; tasks exclude training loop | Dependency review and task review |
| R-014 | Dataset schema changes upstream | Medium | Low | Schema validation and fixture tests; explicit version metadata | Loader tests and live HF marker |
| R-015 | Dependency updates break runtime | Medium | Medium | Pin versions after bootstrap; lockfile review | `uv sync`, tests, CI |
| R-016 | Reports over-select sensational examples | Medium | Medium | Reporting rules require balanced examples and limitations | Manual review and golden tests |
| R-017 | Storage overwrites prior runs | Medium | Low | Overwrite false by default; run directory checks | Storage tests |
| R-018 | Agent modifies unrelated files | Medium | Medium | AGENTS.md rules and completion report file list | Code review |
| R-019 | Agent removes tests to pass build | High | Low | Forbidden action; test count review | PR diff and CI |
| R-020 | Agent claims tests passed without running them | High | Medium | Completion report must list exact commands and results | Manual review |

## Technical Risks

- Concordia API details may require adapter changes.
- Dataset loading may need efficient filtering for 1M rows.
- Optional RAG may introduce timeout and authentication complexity.
- LLM provider differences may affect behavior consistency.

## Product Risks

- Users may overtrust synthetic outputs.
- Users may ask for manipulative use cases.
- Scenario categories may become too broad without strong templates.

## Performance Risks

- High agent counts and turn counts multiply LLM calls.
- Long RAG contexts may inflate token cost.
- Event logs may grow quickly.

## Security Risks

- API key leakage.
- Prompt/document leakage.
- Unsafe scenario objectives.
- MCP output treated as trusted when it should be validated.

## Data Risks

- Upstream dataset schema changes.
- Synthetic persona bias or representational artifacts.
- Misinterpretation of synthetic personas as real people.
- Storing generated sensitive content in logs.

## Dependency Risks

- Concordia version changes.
- Hugging Face dataset access changes.
- PageIndex MCP API changes.
- LLM provider SDK changes.

## Testing Risks

- Tests become too dependent on live services.
- Golden tests become stale.
- Safety tests miss new harmful phrasings.

## Maintenance Risks

- Scenario registry grows without documentation.
- Metrics become inconsistent across scenario families.
- Config compatibility breaks without migration notes.

## AI Agent Failure Modes

- Agent modifies unrelated files.
- Agent ignores requirements.
- Agent invents unsupported APIs.
- Agent removes tests.
- Agent claims tests passed without running them.
- Agent over-engineers beyond the spec.
- Agent adds fine-tuning dependencies before MVP.
- Agent hardcodes API keys or model names.
- Agent changes safety rules to satisfy a harmful request.
- Agent treats optional RAG as mandatory.

Mitigations:

- Strict `AGENTS.md`.
- Small task phases.
- Required acceptance checks.
- Required completion report.
- Offline tests and safety regression tests.
- ADRs documenting architectural boundaries.
