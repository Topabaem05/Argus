"""CLI entrypoint for Korean Social Simulation Lab."""

from __future__ import annotations

import typer

app = typer.Typer(
    name="kssim",
    help="Korean Social Simulation Lab - synthetic social simulation pipeline.",
    no_args_is_help=True,
)


@app.command()
def validate_config(
    config: str = typer.Option(..., "--config", help="Path to config YAML file"),
) -> None:
    """Validate a configuration file."""
    typer.echo(f"Validating config: {config}")


@app.command()
def sample(
    config: str = typer.Option(..., "--config", help="Path to config YAML file"),
    output: str = typer.Option(..., "--output", help="Output path for sampled personas"),
) -> None:
    """Sample personas from a configured source."""
    typer.echo(f"Sampling personas: config={config}, output={output}")


@app.command()
def compile_scenario(
    config: str = typer.Option(..., "--config", help="Path to config YAML file"),
    output: str = typer.Option(..., "--output", help="Output path for compiled scenario plan"),
) -> None:
    """Compile a scenario template into a simulation plan."""
    typer.echo(f"Compiling scenario: config={config}, output={output}")


@app.command()
def run(
    config: str = typer.Option(..., "--config", help="Path to config YAML file"),
    dry_run: bool = typer.Option(False, "--dry-run", help="Run simulation without LLM calls"),
) -> None:
    """Run a configured simulation."""
    mode = "dry-run" if dry_run else "live"
    typer.echo(f"Running simulation: config={config}, mode={mode}")


@app.command()
def evaluate(
    events: str = typer.Option(..., "--events", help="Path to events.jsonl"),
    config: str = typer.Option(..., "--config", help="Path to scenario config"),
) -> None:
    """Evaluate metrics from event logs."""
    typer.echo(f"Evaluating metrics: events={events}, config={config}")


@app.command()
def report(
    input: str = typer.Option(..., "--input", help="Path to run directory or events.jsonl"),
    output: str = typer.Option(..., "--output", help="Path for generated report.md"),
) -> None:
    """Generate a markdown report from run artifacts."""
    typer.echo(f"Generating report: input={input}, output={output}")


def main() -> None:
    """Entrypoint for kssim CLI."""
    app()


if __name__ == "__main__":
    main()
