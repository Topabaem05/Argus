#!/usr/bin/env python3
"""Aggregate required hardware/Unity QA evidence and unlock the next stage only on pass."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from korean_social_simulator.qa.gates import aggregate_stage_gate, load_gate_policy


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--policy", type=Path, default=Path("qa/stage1-gates.json"))
    parser.add_argument("--report-dir", type=Path, default=Path("reports/qa"))
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("reports/qa/stage-gate-summary.json"),
    )
    parser.add_argument(
        "--unlock-marker",
        type=Path,
        default=Path("reports/qa/NEXT_STAGE_UNLOCKED.md"),
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    policy = load_gate_policy(args.policy)
    summary = aggregate_stage_gate(policy, args.report_dir)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(summary.model_dump(mode="json"), ensure_ascii=False, indent=2, sort_keys=True)
        + "\n",
        encoding="utf-8",
    )

    if summary.next_stage_unlocked:
        lines = [
            f"# Next stage unlocked: {summary.next_stage.title}",
            "",
            f"Stage gate `{summary.stage_id}` passed all {summary.required_gate_count} checks.",
            "",
            "## Eligible tasks",
            "",
            *[f"- {task}" for task in summary.next_stage.tasks],
            "",
        ]
        args.unlock_marker.parent.mkdir(parents=True, exist_ok=True)
        args.unlock_marker.write_text("\n".join(lines), encoding="utf-8")
    elif args.unlock_marker.exists():
        args.unlock_marker.unlink()

    print(json.dumps(summary.model_dump(mode="json"), ensure_ascii=False, indent=2))
    return 0 if summary.passed else 2


if __name__ == "__main__":
    raise SystemExit(main())
