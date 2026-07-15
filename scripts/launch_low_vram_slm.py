#!/usr/bin/env python3
"""Print or execute a low-VRAM llama.cpp server command for Argus minibots."""

from __future__ import annotations

import argparse
import shlex
import subprocess
from pathlib import Path

from korean_social_simulator.ai.low_vram import (
    LOW_VRAM_PLANS,
    detect_nvidia_vram_gb,
    select_low_vram_plan,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Select a conservative 0-6 GB VRAM plan and start llama-server.",
    )
    parser.add_argument("--plan", choices=sorted(LOW_VRAM_PLANS))
    parser.add_argument("--vram-gb", type=float, help="Override automatic nvidia-smi detection.")
    parser.add_argument("--binary", default="llama-server")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8080)
    parser.add_argument("--lora", type=Path, help="Optional GGUF LoRA adapter from the fine-tune pipeline.")
    parser.add_argument(
        "--execute",
        action="store_true",
        help="Execute the command. Without this flag the script is a safe dry run.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    detected = args.vram_gb if args.vram_gb is not None else detect_nvidia_vram_gb()
    plan = LOW_VRAM_PLANS[args.plan] if args.plan else select_low_vram_plan(detected)
    lora_path = str(args.lora) if args.lora else None
    command = plan.llama_server_args(
        binary=args.binary,
        host=args.host,
        port=args.port,
        lora_path=lora_path,
    )

    print(f"VRAM detected/selected: {detected if detected is not None else 'CPU only'} GB")
    print(f"Plan: {plan.key} / model profile: {plan.profile_key}")
    print(f"Command: {shlex.join(command)}")
    if not args.execute:
        print("Dry run only. Add --execute after reviewing the command.")
        return 0

    if args.lora and not args.lora.is_file():
        raise SystemExit(f"LoRA adapter does not exist: {args.lora}")
    completed = subprocess.run(command, check=False)
    return completed.returncode


if __name__ == "__main__":
    raise SystemExit(main())
