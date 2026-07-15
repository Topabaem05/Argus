#!/usr/bin/env python3
"""Convert a trained PEFT adapter into a llama.cpp GGUF LoRA file."""

from __future__ import annotations

import argparse
import shlex
import subprocess
import sys
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--llama-cpp-dir", type=Path, required=True)
    parser.add_argument("--adapter-dir", type=Path, required=True)
    parser.add_argument("--base-model-id", default="Qwen/Qwen3-0.6B")
    parser.add_argument("--output", type=Path, default=Path("outputs/argus-minibot-lora-f16.gguf"))
    parser.add_argument("--outtype", choices=("f16", "bf16", "q8_0", "auto"), default="f16")
    parser.add_argument("--execute", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    converter = args.llama_cpp_dir / "convert_lora_to_gguf.py"
    if not converter.is_file():
        raise SystemExit(f"llama.cpp LoRA converter not found: {converter}")
    required = (args.adapter_dir / "adapter_config.json",)
    if not all(path.is_file() for path in required):
        raise SystemExit(f"Not a PEFT adapter directory: {args.adapter_dir}")
    if not any(
        (args.adapter_dir / filename).is_file()
        for filename in ("adapter_model.safetensors", "adapter_model.bin")
    ):
        raise SystemExit(f"Adapter weights are missing from {args.adapter_dir}")

    args.output.parent.mkdir(parents=True, exist_ok=True)
    command = [
        sys.executable,
        str(converter),
        "--outfile",
        str(args.output),
        "--outtype",
        args.outtype,
        "--base-model-id",
        args.base_model_id,
        str(args.adapter_dir),
    ]
    print(shlex.join(command))
    if not args.execute:
        print("Dry run only. Add --execute after reviewing the command.")
        return 0
    completed = subprocess.run(command, check=False)
    return completed.returncode


if __name__ == "__main__":
    raise SystemExit(main())
