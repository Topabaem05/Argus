"""Smoke test for CLI --help command."""

from __future__ import annotations

import subprocess
import sys


def test_kssim_help_exit_code() -> None:
    """Verify that `kssim --help` exits with code 0 and lists expected commands."""
    result = subprocess.run(
        [sys.executable, "-m", "korean_social_simulator.cli", "--help"],
        capture_output=True,
        text=True,
    )
    assert result.returncode == 0, f"Expected exit code 0, got {result.returncode}"
    assert "validate-config" in result.stdout
    assert "sample" in result.stdout
    assert "compile-scenario" in result.stdout
    assert "run" in result.stdout
    assert "evaluate" in result.stdout
    assert "report" in result.stdout
