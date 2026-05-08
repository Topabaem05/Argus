"""Command orchestration for the Argus simulation pipeline."""

from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.agents.profile_builder import build_agent_profiles
from korean_social_simulator.config.loader import load_bridge_config, load_config
from korean_social_simulator.config.models import BridgeConfig, RuntimeConfig
from korean_social_simulator.data.loader import load_personas
from korean_social_simulator.errors import EvaluationError, SimulationError, StorageError
from korean_social_simulator.evaluation.metrics import evaluate_run
from korean_social_simulator.models import (
    AgentProfile,
    AttachmentInput,
    IndividualEvaluation,
    MetricsResult,
    PersonaSelectionResult,
    PopulationSample,
    SimulationEvent,
    SimulationExecution,
    SimulationInputSummary,
    SimulationPlan,
    SimulationResult,
)
from korean_social_simulator.personas.sampler import sample_population
from korean_social_simulator.reporting.markdown import render_report
from korean_social_simulator.safety.validator import validate_safety
from korean_social_simulator.scenarios.compiler import compile_scenario
from korean_social_simulator.simulation.concordia_adapter import run_simulation
from korean_social_simulator.simulation.dry_run import run_dry_run
from korean_social_simulator.simulation.interaction import (
    InteractionContext,
    build_interaction_context,
    write_persona_memory_proposals,
)
from korean_social_simulator.storage.run_store import RunStore

_STRUCTURAL_METRICS = ("event_count", "turn_count", "agent_count")


def validate_config_command(config_path: str | Path) -> RuntimeConfig:
    """Load and validate a runtime config without creating artifacts."""
    return load_config(config_path)


def bridge_validate_config_command(config_path: str | Path) -> BridgeConfig:
    """Load and validate a bridge config without starting the server."""
    return load_bridge_config(config_path)


def bridge_serve_command(config_path: str | Path) -> BridgeConfig:
    """Start the local Unity bridge server."""
    config = bridge_validate_config_command(config_path)
    try:
        import uvicorn

        from korean_social_simulator.bridge.server import create_app
    except ImportError as exc:
        raise SimulationError(
            "Bridge server dependencies are not installed. Install with: uv sync --extra bridge"
        ) from exc

    uvicorn.run(
        create_app(config),
        host=config.server.host,
        port=config.server.port,
        log_level=config.log_level.lower(),
    )
    return config


def bridge_export_replay_command(events_path: str | Path, output_path: str | Path) -> int:
    """Export Argus events into bridge replay JSONL."""
    output = Path(output_path)
    if output.exists():
        raise StorageError(f"Bridge replay output already exists: {output}")

    from korean_social_simulator.bridge import ReplayStore, adapt_events

    events = _read_events(Path(events_path))
    envelopes = adapt_events(events)
    ReplayStore(output).write_events_batch(envelopes)
    return len(envelopes)


def sample_command(config_path: str | Path, output_path: str | Path) -> PopulationSample:
    """Load personas, sample them deterministically, and write a JSON artifact."""
    config = load_config(config_path)
    sample = _build_sample(config)
    _write_json(Path(output_path), sample.model_dump(mode="json"))
    return sample


def compile_scenario_command(config_path: str | Path, output_path: str | Path) -> SimulationPlan:
    """Compile the configured scenario and write a JSON plan artifact."""
    config = load_config(config_path)
    plan = _compile_plan(config)
    _write_json(Path(output_path), plan.model_dump(mode="json"))
    return plan


def bridge_prepare_dry_run_stream(
    config_path: str | Path,
    *,
    dry_run: bool = True,
    max_turns_override: int | None = None,
    chat_text_override: str | None = None,
    attachments_override: list[AttachmentInput] | None = None,
) -> tuple[list[SimulationEvent], list[AgentProfile], SimulationPlan, RuntimeConfig]:
    """Build dry-run simulation events for streaming to the Unity bridge (no artifacts).

    Applies the same safety validation as ``run_command`` dry-run preparation.
    """
    config = load_config(config_path, dry_run_override=True if dry_run else None)
    effective_dry_run = dry_run or config.runtime.dry_run
    if effective_dry_run != config.runtime.dry_run:
        config = config.model_copy(
            update={"runtime": config.runtime.model_copy(update={"dry_run": effective_dry_run})},
        )

    if max_turns_override is not None:
        config = config.model_copy(
            update={
                "runtime": config.runtime.model_copy(update={"max_turns": max_turns_override}),
            },
        )
    if chat_text_override is not None or attachments_override is not None:
        config = config.model_copy(
            update={
                "input": config.input.model_copy(
                    update={
                        "chat_text": chat_text_override
                        if chat_text_override is not None
                        else config.input.chat_text,
                        "attachments": attachments_override
                        if attachments_override is not None
                        else config.input.attachments,
                    }
                )
            }
        )
    config = config.model_copy(
        update={
            "persona_selection": config.persona_selection.model_copy(
                update={"enabled": True},
            )
        }
    )

    sample = _build_sample(config)
    profiles = build_agent_profiles(
        sample,
        language=config.scenario.language,
        safety_policy=config.safety,
    )
    plan = _compile_plan(config)
    validate_safety(plan, profiles, config.safety)
    context = _build_interaction_context(config, profiles)
    selected_profiles = _selected_profiles(
        profiles,
        context.selections,
        selection_enabled=config.persona_selection.enabled,
    )
    events = run_dry_run(plan, selected_profiles, interaction_context=context)
    return events, selected_profiles, plan, config


def run_command(config_path: str | Path, dry_run: bool = False) -> SimulationResult:
    """Execute a configured run and persist the full artifact tree."""
    config = load_config(config_path, dry_run_override=True if dry_run else None)
    effective_dry_run = dry_run or config.runtime.dry_run
    if effective_dry_run != config.runtime.dry_run:
        config = config.model_copy(
            update={"runtime": config.runtime.model_copy(update={"dry_run": effective_dry_run})}
        )

    sample = _build_sample(config)
    profiles = build_agent_profiles(
        sample,
        language=config.scenario.language,
        safety_policy=config.safety,
    )
    plan = _compile_plan(config)

    validate_safety(plan, profiles, config.safety)
    context = _build_interaction_context(config, profiles)
    selected_profiles = _selected_profiles(
        profiles,
        context.selections,
        selection_enabled=config.persona_selection.enabled,
    )

    run_dir = _run_dir(config)
    store = RunStore(run_dir=run_dir, overwrite=config.runtime.overwrite)

    _write_json(run_dir / "sample.json", sample.model_dump(mode="json"))
    _write_json(
        run_dir / "profiles.json",
        [profile.model_dump(mode="json") for profile in selected_profiles],
    )
    _write_json(run_dir / "plan.json", plan.model_dump(mode="json"))
    _write_interaction_artifacts(run_dir, context)

    if effective_dry_run:
        execution = SimulationExecution(
            run_id=plan.run_id,
            status="success",
            events=run_dry_run(
                plan,
                selected_profiles,
                interaction_context=context if config.persona_selection.enabled else None,
            ),
            warnings=[],
            errors=[],
        )
    else:
        execution = run_simulation(plan, selected_profiles)

    store.write_events_batch(execution.events)

    metrics_result = _evaluate_events(execution.events, plan)
    store.write_metrics(metrics_result.model_dump(mode="json"))
    store.write_metrics_csv(metrics_result.metrics)

    report_text = _render(config, plan, execution, metrics_result)
    (run_dir / "report.md").write_text(report_text, encoding="utf-8")
    persona_memory_summary = write_persona_memory_proposals(
        run_dir=run_dir,
        source_dataset_path=Path(config.dataset.fixture_path),
        profiles=selected_profiles,
        events=execution.events,
        config=config.persona_memory,
    )

    store.write_metadata(
        _metadata(
            config=config,
            plan=plan,
            profiles=selected_profiles,
            execution=execution,
            metrics_result=metrics_result,
            run_dir=run_dir,
            input_summary=context.summary,
            selections=context.selections,
            evaluations=context.evaluations,
            persona_memory_summary=persona_memory_summary,
        )
    )
    return store.finalize(
        SimulationResult(
            run_id=plan.run_id,
            status=execution.status,
            metrics_path=str(run_dir / "metrics.json"),
            report_path=str(run_dir / "report.md"),
            errors=execution.errors + metrics_result.errors,
            warnings=execution.warnings + metrics_result.unavailable_metrics,
        )
    )


def evaluate_command(events_path: str | Path, config_path: str | Path) -> MetricsResult:
    """Evaluate metrics from an event log and write metrics JSON/CSV beside it."""
    config = load_config(config_path)
    path = Path(events_path)
    events = _read_events(path)
    plan = _compile_plan(config)
    metrics_result = _evaluate_events(events, plan)
    run_dir = path.parent
    _write_json(run_dir / "metrics.json", metrics_result.model_dump(mode="json"))
    _write_metrics_csv(run_dir / "metrics.csv", metrics_result.metrics)
    return metrics_result


def report_command(input_path: str | Path, output_path: str | Path) -> str:
    """Render a markdown report from a run directory or event log."""
    source = Path(input_path)
    run_dir = source if source.is_dir() else source.parent
    events_path = run_dir / "events.jsonl" if source.is_dir() else source
    events = _read_events(events_path)
    metadata = _read_json_object(run_dir / "run_metadata.json", required=False)
    metrics_payload = _read_json_object(run_dir / "metrics.json", required=False)
    metrics = _metrics_from_payload(metrics_payload)

    report = render_report(
        run_id=str(metadata.get("run_id") or (events[0].run_id if events else run_dir.name)),
        status=str(metadata.get("status", "success")),
        metrics=metrics,
        events=events,
        scenario_title=str(metadata.get("scenario_title", "")),
        scenario_hypothesis=str(metadata.get("scenario_hypothesis", "")),
        safety_notes=_string_list(metadata.get("safety_notes")),
        errors=_string_list(metadata.get("errors")),
        warnings=_string_list(metadata.get("warnings")),
    )
    Path(output_path).parent.mkdir(parents=True, exist_ok=True)
    Path(output_path).write_text(report, encoding="utf-8")
    return report


def _build_sample(config: RuntimeConfig) -> PopulationSample:
    personas = load_personas(config.dataset)
    return sample_population(personas, config.sampling)


def _compile_plan(config: RuntimeConfig) -> SimulationPlan:
    return compile_scenario(
        config.scenario,
        run_id=config.runtime.run_id,
        plan_id=f"{config.runtime.run_id}-plan",
        dry_run=config.runtime.dry_run,
    )


def _build_interaction_context(
    config: RuntimeConfig,
    profiles: list[AgentProfile],
) -> InteractionContext:
    if not config.persona_selection.enabled:
        summary = build_interaction_context(
            profiles=[],
            chat_text=config.input.chat_text,
            attachments=config.input.attachments,
            policy=config.input.attachment_policy,
            max_personas=config.persona_selection.max_personas,
            seed=config.persona_selection.seed,
            base_dir=Path.cwd(),
        ).summary
        return InteractionContext(summary=summary, selections=[], evaluations=[])
    return build_interaction_context(
        profiles=profiles,
        chat_text=config.input.chat_text,
        attachments=config.input.attachments,
        policy=config.input.attachment_policy,
        max_personas=config.persona_selection.max_personas,
        seed=config.persona_selection.seed,
        base_dir=Path.cwd(),
    )


def _selected_profiles(
    profiles: list[AgentProfile],
    selections: list[PersonaSelectionResult],
    *,
    selection_enabled: bool,
) -> list[AgentProfile]:
    if not selection_enabled:
        return profiles
    if not selections:
        return []
    selected_ids = {selection.agent_id for selection in selections}
    return [profile for profile in profiles if profile.agent_id in selected_ids]


def _write_interaction_artifacts(run_dir: Path, context: InteractionContext) -> None:
    _write_json(run_dir / "input_summary.json", context.summary.model_dump(mode="json"))
    _write_json(
        run_dir / "persona_selection.json",
        [selection.model_dump(mode="json") for selection in context.selections],
    )
    _write_json(
        run_dir / "individual_evaluations.json",
        [evaluation.model_dump(mode="json") for evaluation in context.evaluations],
    )


def _run_dir(config: RuntimeConfig) -> Path:
    return Path(config.runtime.output_dir) / config.runtime.run_id


def _metric_names(plan: SimulationPlan) -> list[str]:
    names: list[str] = []
    for metric_name in [*plan.scenario_spec.metrics, *_STRUCTURAL_METRICS]:
        if metric_name not in names:
            names.append(metric_name)
    return names


def _evaluate_events(events: list[SimulationEvent], plan: SimulationPlan) -> MetricsResult:
    result = evaluate_run(events, _metric_names(plan))
    if result.run_id == "unknown":
        return result.model_copy(update={"run_id": plan.run_id})
    return result


def _render(
    config: RuntimeConfig,
    plan: SimulationPlan,
    execution: SimulationExecution,
    metrics_result: MetricsResult,
) -> str:
    return render_report(
        run_id=plan.run_id,
        status=execution.status,
        metrics=metrics_result.metrics,
        events=execution.events,
        scenario_title=plan.scenario_spec.title,
        scenario_hypothesis=plan.scenario_spec.hypothesis,
        safety_notes=config.scenario.safety_notes,
        errors=execution.errors + metrics_result.errors,
        warnings=execution.warnings + metrics_result.unavailable_metrics,
    )


def _metadata(
    config: RuntimeConfig,
    plan: SimulationPlan,
    profiles: list[AgentProfile],
    execution: SimulationExecution,
    metrics_result: MetricsResult,
    run_dir: Path,
    input_summary: SimulationInputSummary,
    selections: list[PersonaSelectionResult],
    evaluations: list[IndividualEvaluation],
    persona_memory_summary: dict[str, object],
) -> dict[str, object]:
    artifact_paths = {
        "run_metadata": str(run_dir / "run_metadata.json"),
        "sample": str(run_dir / "sample.json"),
        "profiles": str(run_dir / "profiles.json"),
        "plan": str(run_dir / "plan.json"),
        "events": str(run_dir / "events.jsonl"),
        "metrics_json": str(run_dir / "metrics.json"),
        "metrics_csv": str(run_dir / "metrics.csv"),
        "report": str(run_dir / "report.md"),
        "input_summary": str(run_dir / "input_summary.json"),
        "persona_selection": str(run_dir / "persona_selection.json"),
        "individual_evaluations": str(run_dir / "individual_evaluations.json"),
    }
    return {
        "run_id": plan.run_id,
        "plan_id": plan.plan_id,
        "scenario_id": plan.scenario_spec.scenario_id,
        "scenario_title": plan.scenario_spec.title,
        "scenario_hypothesis": plan.scenario_spec.hypothesis,
        "family": plan.scenario_spec.family,
        "status": execution.status,
        "dry_run": plan.dry_run,
        "sample_size": len(profiles),
        "agent_count": len(profiles),
        "turn_count": plan.max_turns,
        "event_count": len(execution.events),
        "metric_count": len(metrics_result.metrics),
        "metric_names": _metric_names(plan),
        "safety_notes": config.scenario.safety_notes,
        "warnings": execution.warnings + metrics_result.unavailable_metrics,
        "errors": execution.errors + metrics_result.errors,
        "input": input_summary.model_dump(mode="json"),
        "persona_selection": {
            "selected_count": len(selections),
            "max_personas": config.persona_selection.max_personas,
            "selected_agent_ids": [selection.agent_id for selection in selections],
        },
        "individual_evaluation_count": len(evaluations),
        "persona_memory": persona_memory_summary,
        "artifact_paths": artifact_paths,
    }


def _write_json(path: Path, payload: object) -> None:
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )
    except OSError as exc:
        raise StorageError(f"Failed to write JSON artifact {path}: {exc}") from exc


def _write_metrics_csv(path: Path, metrics: dict[str, str | int | float | None]) -> None:
    rows = ["metric,value"]
    rows.extend(f"{metric},{value}" for metric, value in metrics.items())
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("\n".join(rows) + "\n", encoding="utf-8")
    except OSError as exc:
        raise StorageError(f"Failed to write metrics CSV {path}: {exc}") from exc


def _read_events(path: Path) -> list[SimulationEvent]:
    if not path.exists():
        raise EvaluationError(f"Event log not found: {path}")

    events: list[SimulationEvent] = []
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except OSError as exc:
        raise EvaluationError(f"Failed to read event log {path}: {exc}") from exc

    for line_number, line in enumerate(lines, start=1):
        if not line.strip():
            continue
        try:
            events.append(SimulationEvent.model_validate_json(line))
        except ValueError as exc:
            raise EvaluationError(
                f"Invalid event JSON at line {line_number} in {path}: {exc}"
            ) from exc

    return events


def _read_json_object(path: Path, required: bool) -> dict[str, object]:
    if not path.exists():
        if required:
            raise EvaluationError(f"JSON artifact not found: {path}")
        return {}

    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise EvaluationError(f"Failed to read JSON artifact {path}: {exc}") from exc

    if not isinstance(payload, dict):
        raise EvaluationError(f"JSON artifact must be an object: {path}")
    return payload


def _metrics_from_payload(payload: dict[str, object]) -> dict[str, str | int | float | None]:
    metrics = payload.get("metrics", payload)
    if not isinstance(metrics, dict):
        return {}

    allowed_types = (str, int, float)
    result: dict[str, str | int | float | None] = {}
    for key, value in metrics.items():
        if isinstance(key, str) and (value is None or isinstance(value, allowed_types)):
            result[key] = value
    return result


def _string_list(value: object) -> list[str]:
    if not isinstance(value, list):
        return []
    return [item for item in value if isinstance(item, str)]
