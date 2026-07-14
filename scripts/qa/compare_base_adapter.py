#!/usr/bin/env python3
"""Compare held-out base/adapted endpoint reports and emit the accuracy QA gate."""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path

from korean_social_simulator.qa.gates import GateReport, write_gate_report


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-report", type=Path, required=True)
    parser.add_argument("--adapter-report", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=Path("reports/qa/base_adapter_accuracy.json"))
    parser.add_argument("--minimum-adapter-accuracy", type=float, default=0.85)
    parser.add_argument("--minimum-delta", type=float, default=0.03)
    parser.add_argument("--maximum-p95-regression", type=float, default=1.25)
    return parser.parse_args()


def _load(path: Path) -> dict[str, object]:
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    args = parse_args()
    started = datetime.now(UTC).isoformat()
    missing = [str(path) for path in (args.base_report, args.adapter_report) if not path.is_file()]
    if missing:
        report = GateReport(
            gate_id="base_adapter_accuracy",
            status="blocked",
            metrics={},
            criteria={"reports_present": False},
            notes=["Missing reports: " + ", ".join(missing)],
            started_at=started,
            finished_at=datetime.now(UTC).isoformat(),
        )
        write_gate_report(args.output, report)
        return 2

    base = _load(args.base_report)
    adapter = _load(args.adapter_report)
    base_accuracy = float(base.get("action_accuracy") or 0.0)
    adapter_accuracy = float(adapter.get("action_accuracy") or 0.0)
    base_p95 = float(dict(base.get("latency_ms", {})).get("p95") or 0.0)
    adapter_p95 = float(dict(adapter.get("latency_ms", {})).get("p95") or 0.0)
    base_failures = int(base.get("generation_failures") or 0)
    adapter_failures = int(adapter.get("generation_failures") or 0)
    accuracy_delta = adapter_accuracy - base_accuracy
    criteria = {
        "adapter_accuracy_floor": adapter_accuracy >= args.minimum_adapter_accuracy,
        "adapter_improves_base": accuracy_delta >= args.minimum_delta,
        "adapter_generation_failures_not_worse": adapter_failures <= base_failures,
        "adapter_latency_not_excessive": base_p95 > 0
        and adapter_p95 > 0
        and adapter_p95 <= base_p95 * args.maximum_p95_regression,
    }
    status = "passed" if all(criteria.values()) else "failed"
    report = GateReport(
        gate_id="base_adapter_accuracy",
        status=status,
        metrics={
            "base_action_accuracy": base_accuracy,
            "adapter_action_accuracy": adapter_accuracy,
            "accuracy_delta": accuracy_delta,
            "base_p95_ms": base_p95,
            "adapter_p95_ms": adapter_p95,
            "base_generation_failures": base_failures,
            "adapter_generation_failures": adapter_failures,
        },
        criteria=criteria,
        evidence=[str(args.base_report), str(args.adapter_report)],
        started_at=started,
        finished_at=datetime.now(UTC).isoformat(),
    )
    write_gate_report(args.output, report)
    print(json.dumps(report.model_dump(mode="json"), ensure_ascii=False, indent=2))
    return 0 if status == "passed" else 2


if __name__ == "__main__":
    raise SystemExit(main())
