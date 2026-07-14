#!/usr/bin/env python3
"""Evaluate a local minibot endpoint on held-out game-domain decisions."""

from __future__ import annotations

import argparse
import json
import statistics
import time
from pathlib import Path
from typing import cast

from korean_social_simulator.ai.slm_adapter import SLMProvider, SLMRuntimeAdapter


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--test-file", type=Path, default=Path("data/minibot_sft/test.jsonl"))
    parser.add_argument(
        "--provider",
        choices=("ollama", "vllm", "llamacpp", "nim", "none"),
        default="llamacpp",
    )
    parser.add_argument("--model", default="argus-minibot-0.8b")
    parser.add_argument("--base-url", default="http://127.0.0.1:8080/v1")
    parser.add_argument("--limit", type=int, default=300)
    parser.add_argument("--output", type=Path, default=Path("reports/minibot_policy_eval.json"))
    parser.add_argument("--minimum-action-accuracy", type=float, default=0.85)
    return parser.parse_args()


def _percentile(values: list[float], fraction: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    index = min(len(ordered) - 1, round((len(ordered) - 1) * fraction))
    return ordered[index]


def main() -> int:
    args = parse_args()
    if not args.test_file.is_file():
        raise SystemExit(
            f"Test file not found: {args.test_file}; build the dataset before evaluation."
        )

    adapter = SLMRuntimeAdapter(
        provider=cast(SLMProvider, args.provider),
        model=args.model,
        base_url=args.base_url,
        temperature=0.0,
        max_tokens=160,
        fallback_on_error=False,
    )
    total = 0
    action_correct = 0
    side_correct = 0
    efficiency_errors: list[float] = []
    mood_errors: list[float] = []
    latencies_ms: list[float] = []
    failures: list[dict[str, object]] = []

    with args.test_file.open(encoding="utf-8") as handle:
        for line in handle:
            if total >= args.limit:
                break
            record = json.loads(line)
            messages = record["messages"]
            expected = record["expected"]
            system_prompt = str(messages[0]["content"])
            user_prompt = str(messages[1]["content"])
            started = time.perf_counter()
            try:
                actual = adapter.generate(user_prompt, system_prompt)
            except Exception as exc:
                failures.append({"id": record.get("id"), "error": f"{type(exc).__name__}: {exc}"})
                total += 1
                continue
            latencies_ms.append((time.perf_counter() - started) * 1000.0)
            action_match = actual.action == expected["action"]
            side_match = actual.side_action == expected["side_action"]
            action_correct += int(action_match)
            side_correct += int(side_match)
            efficiency_errors.append(abs(actual.efficiency - float(expected["efficiency"])))
            mood_errors.append(abs(actual.mood_change - int(expected["mood_change"])))
            if not action_match and len(failures) < 50:
                failures.append(
                    {
                        "id": record.get("id"),
                        "expected_action": expected["action"],
                        "actual_action": actual.action,
                        "dialogue": actual.dialogue,
                    }
                )
            total += 1

    if total == 0:
        raise SystemExit("No evaluation examples were read")
    action_accuracy = action_correct / total
    report = {
        "total": total,
        "action_accuracy": action_accuracy,
        "side_action_accuracy": side_correct / total,
        "efficiency_mae": statistics.fmean(efficiency_errors) if efficiency_errors else None,
        "mood_change_mae": statistics.fmean(mood_errors) if mood_errors else None,
        "latency_ms": {
            "mean": statistics.fmean(latencies_ms) if latencies_ms else None,
            "p50": _percentile(latencies_ms, 0.50),
            "p95": _percentile(latencies_ms, 0.95),
        },
        "generation_failures": sum(1 for item in failures if "error" in item),
        "sample_failures": failures,
        "gate": {
            "minimum_action_accuracy": args.minimum_action_accuracy,
            "passed": action_accuracy >= args.minimum_action_accuracy,
        },
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report["gate"]["passed"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
