#!/usr/bin/env python3
"""Build deterministic train/validation/test JSONL for minibot SFT."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path

from korean_social_simulator.training.game_sft import (
    generate_game_sft_examples,
    split_game_sft_examples,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", type=Path, default=Path("data/minibot_sft"))
    parser.add_argument("--count", type=int, default=4000)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--overwrite", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    args.output_dir.mkdir(parents=True, exist_ok=True)
    outputs = {
        "train": args.output_dir / "train.jsonl",
        "validation": args.output_dir / "validation.jsonl",
        "test": args.output_dir / "test.jsonl",
    }
    existing = [path for path in outputs.values() if path.exists()]
    if existing and not args.overwrite:
        names = ", ".join(str(path) for path in existing)
        raise SystemExit(f"Refusing to overwrite existing files: {names}; pass --overwrite")

    examples = generate_game_sft_examples(count=args.count, seed=args.seed)
    splits = split_game_sft_examples(examples)
    for split, path in outputs.items():
        with path.open("w", encoding="utf-8") as handle:
            for example in splits[split]:
                handle.write(json.dumps(example.to_record(), ensure_ascii=False) + "\n")

    actions = Counter(str(example.metadata["action"]) for example in examples)
    outcomes = Counter(str(example.expected["action"]) for example in examples)
    manifest = {
        "version": 1,
        "seed": args.seed,
        "count": len(examples),
        "splits": {name: len(items) for name, items in splits.items()},
        "action_distribution": dict(sorted(actions.items())),
        "outcome_distribution": dict(sorted(outcomes.items())),
        "files": {name: str(path.name) for name, path in outputs.items()},
    }
    manifest_path = args.output_dir / "manifest.json"
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(manifest, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
