#!/usr/bin/env python3
"""Keep four bridge clients active while generating concurrent minibot SLM decisions.

The gate intentionally requires the bridge health payload to report at least four connected
clients. The current single-socket ClientRegistry will therefore keep this stage locked until
session-scoped multiplayer routing is implemented.
"""

from __future__ import annotations

import argparse
import asyncio
import json
import statistics
import time
from datetime import UTC, datetime
from pathlib import Path

import httpx
import websockets

from korean_social_simulator.qa.gates import GateReport, write_gate_report


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--bridge-ws", default="ws://127.0.0.1:8000/ws/unity")
    parser.add_argument("--bridge-health", default="http://127.0.0.1:8000/health")
    parser.add_argument("--slm-base-url", default="http://127.0.0.1:8080/v1")
    parser.add_argument("--model", default="argus-minibot-2b")
    parser.add_argument("--clients", type=int, default=4)
    parser.add_argument("--requests-per-client", type=int, default=20)
    parser.add_argument("--max-p95-ms", type=float, default=3500.0)
    parser.add_argument(
        "--report",
        type=Path,
        default=Path("reports/qa/four_client_slm_load.json"),
    )
    return parser.parse_args()


def _percentile(values: list[float], fraction: float) -> float:
    ordered = sorted(values)
    if not ordered:
        return 0.0
    index = min(len(ordered) - 1, round((len(ordered) - 1) * fraction))
    return ordered[index]


def _envelope(client_index: int, sequence: int, message_type: str, payload: dict[str, object]) -> str:
    return json.dumps(
        {
            "schema_version": "1.0.0",
            "message_id": f"qa-{client_index}-{sequence}",
            "correlation_id": None,
            "session_id": f"qa-player-{client_index}",
            "sequence": sequence,
            "sent_at_ms": int(time.time() * 1000),
            "type": message_type,
            "payload": payload,
        }
    )


async def _client_worker(
    index: int,
    args: argparse.Namespace,
    http: httpx.AsyncClient,
    latencies: list[float],
    errors: list[str],
) -> bool:
    try:
        async with websockets.connect(args.bridge_ws, open_timeout=10, close_timeout=5) as socket:
            await socket.send(_envelope(index, 0, "unity.ready", {"client": index}))
            ready = json.loads(await asyncio.wait_for(socket.recv(), timeout=10))
            if ready.get("type") != "bridge.ready":
                errors.append(f"client {index}: expected bridge.ready, got {ready.get('type')}")
                return False
            for request_index in range(args.requests_per_client):
                await socket.send(
                    _envelope(
                        index,
                        request_index + 1,
                        "observer.camera_state",
                        {"position": [index, 10, request_index], "rotation": [45, 0, 0]},
                    )
                )
                started = time.perf_counter()
                response = await http.post(
                    "/chat/completions",
                    json={
                        "model": args.model,
                        "temperature": 0.0,
                        "max_tokens": 96,
                        "messages": [
                            {"role": "system", "content": "JSON 객체 하나만 출력하세요."},
                            {
                                "role": "user",
                                "content": (
                                    f"직원=QA{index};기분: {45 + index}/100;호감도: {index}/100;"
                                    "명령=assign_task;JSON 하나만 출력"
                                ),
                            },
                        ],
                    },
                )
                latencies.append((time.perf_counter() - started) * 1000.0)
                if response.status_code != 200:
                    errors.append(f"client {index}: SLM HTTP {response.status_code}")
                else:
                    response_payload = response.json()
                    if not response_payload.get("choices"):
                        errors.append(f"client {index}: missing choices")
            await asyncio.sleep(0.5)
            return True
    except Exception as exc:
        errors.append(f"client {index}: {type(exc).__name__}: {exc}")
        return False


async def _run(args: argparse.Namespace) -> tuple[list[bool], list[float], list[str], dict[str, object]]:
    latencies: list[float] = []
    errors: list[str] = []
    async with httpx.AsyncClient(base_url=args.slm_base_url, timeout=30.0) as slm_http:
        results = await asyncio.gather(
            *[
                _client_worker(index, args, slm_http, latencies, errors)
                for index in range(args.clients)
            ]
        )
    health: dict[str, object] = {}
    try:
        async with httpx.AsyncClient(timeout=10.0) as health_http:
            response = await health_http.get(args.bridge_health)
            response.raise_for_status()
            health = response.json()
    except Exception as exc:
        errors.append(f"bridge health: {type(exc).__name__}: {exc}")
    return results, latencies, errors, health


def main() -> int:
    args = parse_args()
    started = datetime.now(UTC).isoformat()
    results, latencies, errors, health = asyncio.run(_run(args))
    unity_health = dict(health.get("unity", {}))
    connected_clients = int(unity_health.get("connected_clients", 0) or 0)
    expected_calls = args.clients * args.requests_per_client
    p95_ms = _percentile(latencies, 0.95)
    criteria = {
        "four_clients_completed_handshake": len(results) == args.clients and all(results),
        "bridge_reports_four_connected_clients": connected_clients >= args.clients,
        "all_slm_requests_completed": len(latencies) == expected_calls,
        "zero_bridge_or_slm_errors": not errors,
        "slm_p95_within_limit": 0 < p95_ms <= args.max_p95_ms,
    }
    status = "passed" if all(criteria.values()) else "failed"
    report = GateReport(
        gate_id="four_client_slm_load",
        status=status,
        metrics={
            "clients_requested": args.clients,
            "clients_completed": sum(results),
            "bridge_connected_clients": connected_clients,
            "slm_requests_expected": expected_calls,
            "slm_requests_completed": len(latencies),
            "latency_mean_ms": statistics.fmean(latencies) if latencies else None,
            "latency_p95_ms": p95_ms,
            "error_count": len(errors),
        },
        criteria=criteria,
        evidence=[args.bridge_ws, args.bridge_health, args.slm_base_url],
        notes=errors[:50],
        started_at=started,
        finished_at=datetime.now(UTC).isoformat(),
    )
    write_gate_report(args.report, report)
    print(json.dumps(report.model_dump(mode="json"), ensure_ascii=False, indent=2))
    return 0 if status == "passed" else 2


if __name__ == "__main__":
    raise SystemExit(main())
