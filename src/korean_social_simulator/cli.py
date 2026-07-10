"""CLI entrypoint for Korean Social Simulation Lab."""

from __future__ import annotations

import typer

from korean_social_simulator.errors import (
    AgentProfileError,
    ConfigurationError,
    DatasetLoadError,
    EvaluationError,
    KoreanSocialSimulationError,
    PersonaSchemaError,
    RetrievalError,
    SafetyViolationError,
    SamplingError,
    ScenarioValidationError,
    SimulationError,
    StorageError,
)
from korean_social_simulator.pipeline import (
    bridge_export_replay_command,
    bridge_serve_command,
    bridge_validate_config_command,
    compile_scenario_command,
    evaluate_command,
    game_run_command,
    report_command,
    run_command,
    sample_command,
    validate_config_command,
)

app = typer.Typer(
    name="kssim",
    help="Korean Social Simulation Lab - synthetic social simulation pipeline.",
    no_args_is_help=True,
)
bridge_app = typer.Typer(
    help="Unity bridge utilities.",
    no_args_is_help=True,
)
app.add_typer(bridge_app, name="bridge")
game_app = typer.Typer(
    help="AI company operation game commands.",
    no_args_is_help=True,
)
app.add_typer(game_app, name="game")

_ERROR_PREFIXES: tuple[tuple[type[KoreanSocialSimulationError], str], ...] = (
    (ConfigurationError, "Configuration error"),
    (DatasetLoadError, "Dataset load error"),
    (PersonaSchemaError, "Persona schema error"),
    (SamplingError, "Sampling error"),
    (AgentProfileError, "Agent profile error"),
    (ScenarioValidationError, "Scenario validation error"),
    (SafetyViolationError, "Safety violation"),
    (RetrievalError, "Retrieval error"),
    (StorageError, "Storage error"),
    (SimulationError, "Simulation error"),
    (EvaluationError, "Evaluation error"),
)


def _handle_error(error: KoreanSocialSimulationError) -> None:
    prefix = "Argus error"
    for error_type, candidate_prefix in _ERROR_PREFIXES:
        if isinstance(error, error_type):
            prefix = candidate_prefix
            break
    typer.echo(f"{prefix}: {error}", err=True)
    raise typer.Exit(code=1) from error


@app.command()
def validate_config(
    config: str = typer.Option(..., "--config", help="Path to config YAML file"),
) -> None:
    """Validate a configuration file."""
    try:
        loaded = validate_config_command(config)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Configuration valid: {loaded.runtime.run_id}")


@bridge_app.command("validate-config")
def bridge_validate_config(
    config: str = typer.Option(..., "--config", help="Path to bridge config YAML file"),
) -> None:
    """Validate a bridge configuration file."""
    try:
        loaded = bridge_validate_config_command(config)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(
        f"Bridge configuration valid: {loaded.server.host}:{loaded.server.port} "
        f"({loaded.schema_config.version})"
    )


@bridge_app.command()
def serve(
    config: str = typer.Option(..., "--config", help="Path to bridge config YAML file"),
) -> None:
    """Start the local Unity bridge server."""
    try:
        loaded = bridge_serve_command(config)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Bridge server stopped: {loaded.server.host}:{loaded.server.port}")


@bridge_app.command("export-replay")
def bridge_export_replay(
    events: str = typer.Option(..., "--events", help="Path to Argus events.jsonl"),
    output: str = typer.Option(..., "--output", help="Output path for bridge replay JSONL"),
) -> None:
    """Export Argus event logs into bridge replay JSONL."""
    try:
        count = bridge_export_replay_command(events, output)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Bridge replay written: {output} ({count} envelopes)")


@app.command()
def sample(
    config: str = typer.Option(..., "--config", help="Path to config YAML file"),
    output: str = typer.Option(..., "--output", help="Output path for sampled personas"),
) -> None:
    """Sample personas from a configured source."""
    try:
        sampled = sample_command(config, output)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Sample written: {output} ({len(sampled.records)} personas)")


@app.command()
def compile_scenario(
    config: str = typer.Option(..., "--config", help="Path to config YAML file"),
    output: str = typer.Option(..., "--output", help="Output path for compiled scenario plan"),
) -> None:
    """Compile a scenario template into a simulation plan."""
    try:
        plan = compile_scenario_command(config, output)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Scenario plan written: {output} ({plan.scenario_spec.family})")


@app.command()
def run(
    config: str = typer.Option(..., "--config", help="Path to config YAML file"),
    dry_run: bool = typer.Option(False, "--dry-run", help="Run simulation without LLM calls"),
) -> None:
    """Run a configured simulation."""
    try:
        result = run_command(config, dry_run=dry_run)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Run complete: {result.run_id} ({result.status})")


@app.command()
def evaluate(
    events: str = typer.Option(..., "--events", help="Path to events.jsonl"),
    config: str = typer.Option(..., "--config", help="Path to scenario config"),
) -> None:
    """Evaluate metrics from event logs."""
    try:
        result = evaluate_command(events, config)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Metrics written: {result.run_id} ({len(result.metrics)} metrics)")


@app.command()
def report(
    input: str = typer.Option(..., "--input", help="Path to run directory or events.jsonl"),
    output: str = typer.Option(..., "--output", help="Path for generated report.md"),
) -> None:
    """Generate a markdown report from run artifacts."""
    try:
        report_command(input, output)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Report written: {output}")


@game_app.command("run")
def game_run(
    players: str = typer.Option(
        "p1:알파상사,p2:베타테크,p3:감마로직,p4:델타푸드",
        "--players",
        help="Comma-separated player_id:company_name pairs",
    ),
    rounds: int = typer.Option(5, "--rounds", help="Number of rounds"),
) -> None:
    """Run an AI company operation game simulation."""
    try:
        result = game_run_command(players, rounds)
    except KoreanSocialSimulationError as error:
        _handle_error(error)
    typer.echo(f"Game complete: winner={result.get('winner')} rounds={result.get('rounds_played')}")


@game_app.command("tutorial")
def game_tutorial() -> None:
    """Print the game tutorial."""
    import sys

    from korean_social_simulator.game.tutorial import TutorialSystem

    TutorialSystem(out=sys.stdout).print_tutorial()


def main() -> None:
    """Entrypoint for kssim CLI."""
    app()


if __name__ == "__main__":
    main()
