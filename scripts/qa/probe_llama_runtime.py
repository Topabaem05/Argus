#!/usr/bin/env python3
"""Launch llama.cpp on target hardware, evaluate it, and record OOM/latency evidence."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.request
from datetime import UTC, datetime
from pathlib import Path

from korean_social_simulator.ai.low_vram import LOW_VRAM_PLANS, detect_nvidia_vram_gb
from korean_social_simulator.qa.gates import GateReport, write_gate_report


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--gate-id", choices=("vram_2gb_oom", "vram_4gb_latency"), required=True)
    parser.add_argument("--plan", choices=tuple(LOW_VRAM_PLANS), required=True)
    parser.add_argument("--llama-server", default="llama-server")
    parser.add_argument("--lora")
    parser.add_argument("--port", type=int, default=8080)
    parser.add_argument("--test-file", type=Path, default=Path("data/minibot_sft/test.jsonl"))
    parser.add_argument("--limit", type=int, default=100)
    parser.add_argument("--max-p95-ms", type=float, default=0.0)
    parser.add_argument("--startup-timeout", type=float, default=180.0)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--log", type=Path, default=Path("reports/qa/llama-server.log"))
    return parser.parse_args()


def _gpu_used_mb() -> int | None:
    command = [
        "nvidia-smi",
        "--query-gpu=memory.used",
        "--format=csv,noheader,nounits",
    ]
    try:
        completed = subprocess.run(command, capture_output=True, text=True, timeout=5, check=True)
    except (FileNotFoundError, subprocess.CalledProcessError, subprocess.TimeoutExpired):
        return None
    values: list[int] = []
    for line in completed.stdout.splitlines():
        try:
            values.append(int(line.strip()))
        except ValueError:
            continue
    return max(values) if values else None


def _wait_for_health(url: str, process: subprocess.Popen[str], timeout: float) -> bool:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if process.poll() is not None:
            return False
        try:
            with urllib.request.urlopen(url, timeout=2) as response:
                if response.status == 200:
                    return True
        except (urllib.error.URLError, TimeoutError):
            pass
        time.sleep(1)
    return False


def main() -> int:
    args = parse_args()
    started_at = datetime.now(UTC).isoformat()
    plan = LOW_VRAM_PLANS[args.plan]
    detected_vram_gb = detect_nvidia_vram_gb()
    args.log.parent.mkdir(parents=True, exist_ok=True)
    command = plan.llama_server_args(
        binary=args.llama_server,
        port=args.port,
        lora_path=args.lora,
    )
    before_mb = _gpu_used_mb()
    peak_mb = before_mb or 0
    stop_sampling = threading.Event()

    def sample_memory() -> None:
        nonlocal peak_mb
        while not stop_sampling.wait(0.25):
            current = _gpu_used_mb()
            if current is not None:
                peak_mb = max(peak_mb, current)

    with args.log.open("w", encoding="utf-8") as log_handle:
        try:
            process = subprocess.Popen(
                command,
                stdout=log_handle,
                stderr=subprocess.STDOUT,
                text=True,
            )
        except OSError as exc:
            report = GateReport(
                gate_id=args.gate_id,
                status="blocked",
                metrics={"detected_vram_gb": detected_vram_gb},
                criteria={"llama_server_started": False},
                notes=[str(exc)],
                started_at=started_at,
                finished_at=datetime.now(UTC).isoformat(),
            )
            write_gate_report(args.report, report)
            return 2

        sampler = threading.Thread(target=sample_memory, daemon=True)
        sampler.start()
        health_url = f"http://127.0.0.1:{args.port}/health"
        startup_ok = _wait_for_health(health_url, process, args.startup_timeout)
        eval_report = args.report.with_name(f"{args.gate_id}-policy-eval.json")
        eval_return_code = -1
        if startup_ok:
            evaluation = subprocess.run(
                [
                    sys.executable,
                    "scripts/evaluate_minibot_policy.py",
                    "--provider",
                    "llamacpp",
                    "--model",
                    plan.profile.runtime_model,
                    "--base-url",
                    f"http://127.0.0.1:{args.port}/v1",
                    "--test-file",
                    str(args.test_file),
                    "--limit",
                    str(args.limit),
                    "--output",
                    str(eval_report),
                    "--minimum-action-accuracy",
                    "0.0",
                ],
                capture_output=True,
                text=True,
                check=False,
            )
            eval_return_code = evaluation.returncode
        process.terminate()
        try:
            process.wait(timeout=20)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=10)
        stop_sampling.set()
        sampler.join(timeout=2)

    log_text = args.log.read_text(encoding="utf-8", errors="replace")
    lower_log = log_text.lower()
    oom_detected = any(
        marker in lower_log
        for marker in ("out of memory", "cuda error 2", "cuda_error_out_of_memory", "failed to allocate")
    )
    evaluation_payload: dict[str, object] = {}
    if eval_report.is_file():
        evaluation_payload = json.loads(eval_report.read_text(encoding="utf-8"))
    latency = dict(evaluation_payload.get("latency_ms", {}))
    p95_ms = float(latency.get("p95") or 0.0)
    action_accuracy = float(evaluation_payload.get("action_accuracy") or 0.0)
    generation_failures = int(evaluation_payload.get("generation_failures") or 0)
    vram_matches_runner = detected_vram_gb is not None and (
        (args.plan == "vram_2gb" and 1.5 <= detected_vram_gb <= 2.6)
        or (args.plan == "vram_4gb" and 3.5 <= detected_vram_gb <= 4.8)
    )
    criteria = {
        "target_vram_runner_detected": vram_matches_runner,
        "llama_server_started": startup_ok,
        "no_oom_detected": not oom_detected,
        "evaluation_completed": eval_return_code == 0 and bool(evaluation_payload),
        "no_generation_failures": generation_failures == 0,
    }
    if args.max_p95_ms > 0:
        criteria["p95_within_limit"] = 0 < p95_ms <= args.max_p95_ms
    status = "passed" if all(criteria.values()) else "failed"
    report = GateReport(
        gate_id=args.gate_id,
        status=status,
        metrics={
            "detected_vram_gb": detected_vram_gb,
            "gpu_memory_before_mb": before_mb,
            "gpu_memory_peak_mb": peak_mb,
            "gpu_memory_delta_mb": peak_mb - (before_mb or 0),
            "action_accuracy": action_accuracy,
            "p95_ms": p95_ms,
            "generation_failures": generation_failures,
            "server_return_code": process.returncode,
        },
        criteria=criteria,
        evidence=[str(args.log), str(eval_report), " ".join(command)],
        notes=["Actual target hardware is required; virtual VRAM caps are not accepted."],
        started_at=started_at,
        finished_at=datetime.now(UTC).isoformat(),
    )
    write_gate_report(args.report, report)
    print(json.dumps(report.model_dump(mode="json"), ensure_ascii=False, indent=2))
    return 0 if status == "passed" else 2


if __name__ == "__main__":
    raise SystemExit(main())
