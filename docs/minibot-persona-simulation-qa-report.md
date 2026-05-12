# Argus Unity Mini-Bot Persona Simulation QA Report

Date: 2026-05-12

Source plan: `/Users/guribbong/Downloads/argus_unity_minibot_phase_plan_sdd.html`

## Scope

This QA pass follows the SDD phase method for the contract-first mini-bot path:

- Phase 0: baseline audit and reproducibility gate.
- Phase 1: bridge contract upgrade.
- Phase 2: persona evaluation to behavior planner.
- Phase 4: Unity routing and mini-bot behavior application.
- Phase 10 partial: release-gate checks available in this checkout.

Phases not claimed complete in this pass: ML-Agents training/inference package, full beginner Unity UI, full multi-background authored scenes, camera mode implementation, and 5-scenario performance profiling.

## Implementation Corrections

Removed or replaced placeholder behavior found during QA:

- Replaced duplicated hard-coded background lists with `src/korean_social_simulator/bridge/environment_catalog.py`.
- Replaced schema-only `agent.behavior` handling with `MiniBotBehaviorHandler`, which parses behavior intents and queues actual mini-bot movement through `AgentMoveHandler`.
- Extended emotion display so nested behavior emotions update the visible Unity emotion indicator.
- Removed the unused random bridge motion-frame generator from `simulation_stream.py`.
- Replaced the hard-coded physics demo pair with a physics request derived from the actual `conflict.update` participants.
- Redacted private persona fields from generic Unity bridge payloads; bridge replay no longer exposes `persona_uuid`, memory seeds, behavior rules, secrets, or prompt-like fields.

## QA Results

### Phase 0 - Baseline and Reproducibility

Artifact: `examples/run_minibot_persona_binding_smoke.yaml`

Checks:

- Ran the smoke config twice with separate output roots under `/tmp/argus-minibot-qa`.
- Normalized event timestamps and compared both event streams.
- Exported bridge replay from the first run.
- Scanned bridge replay for private-field leakage.

Result:

- Normalized event streams matched exactly.
- Event count: 58.
- Bridge replay count: 58.
- First bridge type: `environment.load`.
- Behavior event count: 8.
- Privacy forbidden hits: 0.

### Phase 1 - Bridge Contract

Implemented and tested message types:

- `agent.behavior`
- `agent.animation`
- `environment.load`
- `ui.status`
- `simulation.summary`

Relevant checks:

- Pydantic validation tests for behavior and environment payloads.
- Event adapter tests for new explicit bridge payloads.
- FastAPI `/schema/version` includes new message types and supported background IDs.
- `/simulation/start` accepts `scenario_text`, `background_id`, `persona_count_override`, attachment metadata, `ui_session_id`, and `dry_run`.
- Invalid background IDs fail validation.

### Phase 2 - Persona Evaluation to Behavior

Implemented deterministic stance mapping:

- `supports` -> `speak`, happy emotion.
- `opposes` -> `argue`, angry emotion.
- `mixed` -> `ask`, confused emotion.
- `uncertain` -> `observe`, neutral emotion.
- Missing evaluation -> safe `observe` fallback.

QA:

- Unit tests cover one intent per selected agent, high-confidence opposed behavior, and missing-evaluation fallback.
- Dry-run tests confirm each selected agent emits `agent.behavior`.
- Replay export includes behavior messages.

### Phase 4 - Unity Routing and Behavior Application

Implemented runtime path:

- `BridgeEnvelope.IsKnownBridgeMessageType()` recognizes new public messages.
- `SimulationSceneOrchestrator` routes `environment.load`, `simulation.summary`, `ui.status`, `agent.behavior`, and `agent.animation`.
- `MiniBotBehaviorHandler` parses behavior intents and queues movement through `AgentMoveHandler`.
- `BridgeReceiver` can post scenario text, persona count, background ID, chat text, and attachment metadata to the bridge.
- Behavior-carried emotion updates `EmotionIndicatorManager`.

Unity validation:

- EditMode: 89 passed, 0 failed, 0 skipped.
- PlayMode: 5 passed, 0 failed, 0 skipped.
- `OpposedPersonaBehaviorMovesMiniBotAndShowsEmotion` proves an `agent.behavior` event for an opposed persona queues mini-bot movement and creates the visible emotion indicator.

### Release-Gate Checks

Commands run:

```bash
uv run ruff format .
uv run ruff check . --fix
uv run mypy src
uv run pytest -m "not live_hf and not live_llm and not integration and not live_pageindex"
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate -runTests -testPlatform EditMode -testResults /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/argus-behavior-handler-editmode-results.xml -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/argus-behavior-handler-editmode-2.log
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate -runTests -testPlatform PlayMode -testResults /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/argus-behavior-handler-playmode-results.xml -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/argus-behavior-handler-playmode.log
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/guribbong/code/Argus/unity/EmbodiedDebate -runTests -testPlatform PlayMode -testResults /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/argus-minibot-application-playmode-results.xml -logFile /Users/guribbong/code/Argus/unity/EmbodiedDebate/tmp/argus-minibot-application-playmode-6.log
```

Results:

- Ruff format/check: passed.
- Mypy: passed, 59 source files.
- Offline pytest gate: 271 passed, 6 live/external tests deselected.
- Unity EditMode: 89 passed.
- Unity PlayMode: 5 passed, including the mini-bot behavior application test.

## Residual Risks

- `environment.load` is catalog-backed on the Python side, but Unity does not yet load authored scenes/prefabs per background.
- ML-Agents remains intentionally unimplemented in this pass; the scripted motor fallback is the active working path.
- Full product QA from Phase 10 still needs multi-background visual capture, 4/8/20 persona runtime checks, FPS profiling, and replay-vs-live state comparison.
