#!/usr/bin/env python3
"""Run a bounded real CUDA QLoRA training smoke test and emit a stage-gate report."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import UTC, datetime
from pathlib import Path

from korean_social_simulator.qa.gates import GateReport, write_gate_report


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", default="Qwen/Qwen3-0.6B")
    parser.add_argument("--dataset-dir", type=Path, default=Path("data/minibot_sft"))
    parser.add_argument("--output-dir", type=Path, default=Path("outputs/qa-qlora-smoke"))
    parser.add_argument("--report", type=Path, default=Path("reports/qa/cuda_qlora_smoke.json"))
    parser.add_argument("--max-steps", type=int, default=5)
    parser.add_argument("--max-peak-vram-mb", type=int, default=16384)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    started = datetime.now(UTC).isoformat()
    raw_report = args.output_dir / "training-smoke.json"
    command = [
        sys.executable,
        "scripts/train_minibot_qlora.py",
        "--model",
        args.model,
        "--train-file",
        str(args.dataset_dir / "train.jsonl"),
        "--validation-file",
        str(args.dataset_dir / "validation.jsonl"),
        "--output-dir",
        str(args.output_dir),
        "--qa-report",
        str(raw_report),
        "--max-steps",
        str(args.max_steps),
        "--max-train-samples",
        "64",
        "--max-validation-samples",
        "32",
        "--gradient-accumulation",
        "2",
        "--max-length",
        "512",
        "--eval-steps",
        "1",
        "--save-steps",
        "1",
        "--logging-steps",
        "1",
        "--no-packing",
    ]
    completed = subprocess.run(command, capture_output=True, text=True, check=False)
    payload: dict[str, object] = {}
    if raw_report.is_file():
        payload = json.loads(raw_report.read_text(encoding="utf-8"))
    peak_reserved = int(dict(payload.get("cuda", {})).get("peak_reserved_mb", 0))
    criteria = {
        "process_exit_zero": completed.returncode == 0,
        "adapter_created": bool(payload.get("adapter_created", False)),
        "finite_train_loss": bool(payload.get("finite_train_loss", False)),
        "finite_eval_loss": bool(payload.get("finite_eval_loss", False)),
        "completed_requested_steps": int(payload.get("completed_steps", 0)) >= args.max_steps,
        "peak_vram_within_training_budget": 0 < peak_reserved <= args.max_peak_vram_mb,
    }
    status = "passed" if all(criteria.values()) else "failed"
    report = GateReport(
        gate_id="cuda_qlora_smoke",
        status=status,
        metrics={
            "return_code": completed.returncode,
            "completed_steps": int(payload.get("completed_steps", 0)),
            "peak_vram_reserved_mb": peak_reserved,
            "train_loss": dict(payload.get("metrics", {})).get("train_loss"),
            "eval_loss": dict(payload.get("evaluation", {})).get("eval_loss"),
        },
        criteria=criteria,
        evidence=[str(raw_report), str(args.output_dir / "adapter_config.json")],
        notes=[completed.stderr[-2000:]] if completed.stderr else [],
        started_at=started,
        finished_at=datetime.now(UTC).isoformat(),
    )
    write_gate_report(args.report, report)
    print(json.dumps(report.model_dump(mode="json"), ensure_ascii=False, indent=2))
    return 0 if status == "passed" else 2


if __name__ == "__main__":
    raise SystemExit(main())
