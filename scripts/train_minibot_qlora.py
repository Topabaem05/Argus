#!/usr/bin/env python3
"""QLoRA SFT for a compact text-only Qwen3 minibot policy model.

The script supports both full training and a bounded hardware-QA smoke run. A QA report records
CUDA capability, peak allocated/reserved VRAM, train/eval loss, produced adapter files, and the
actual number of optimization steps. Low-VRAM limits in this project apply to inference; QLoRA
training is expected to run on a separate CUDA machine.
"""

from __future__ import annotations

import argparse
import json
import math
import time
from pathlib import Path
from typing import Any


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", default="Qwen/Qwen3-0.6B")
    parser.add_argument("--train-file", type=Path, default=Path("data/minibot_sft/train.jsonl"))
    parser.add_argument(
        "--validation-file",
        type=Path,
        default=Path("data/minibot_sft/validation.jsonl"),
    )
    parser.add_argument("--output-dir", type=Path, default=Path("outputs/minibot-qwen3-lora"))
    parser.add_argument("--qa-report", type=Path)
    parser.add_argument("--max-length", type=int, default=768)
    parser.add_argument("--epochs", type=float, default=2.0)
    parser.add_argument("--max-steps", type=int, default=-1)
    parser.add_argument("--max-train-samples", type=int)
    parser.add_argument("--max-validation-samples", type=int)
    parser.add_argument("--learning-rate", type=float, default=1e-4)
    parser.add_argument("--batch-size", type=int, default=1)
    parser.add_argument("--gradient-accumulation", type=int, default=16)
    parser.add_argument("--lora-r", type=int, default=16)
    parser.add_argument("--lora-alpha", type=int, default=32)
    parser.add_argument("--lora-dropout", type=float, default=0.05)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--save-steps", type=int, default=100)
    parser.add_argument("--eval-steps", type=int, default=100)
    parser.add_argument("--logging-steps", type=int, default=10)
    parser.add_argument("--resume-from-checkpoint")
    parser.add_argument("--trust-remote-code", action="store_true")
    parser.add_argument(
        "--assistant-only-loss",
        action=argparse.BooleanOptionalAction,
        default=True,
    )
    parser.add_argument("--packing", action=argparse.BooleanOptionalAction, default=True)
    return parser.parse_args()


def _require_files(*paths: Path) -> None:
    missing = [str(path) for path in paths if not path.is_file()]
    if missing:
        raise SystemExit(
            "Missing dataset files: "
            + ", ".join(missing)
            + ". Run scripts/build_minibot_sft_dataset.py first."
        )


def _adapter_files(output_dir: Path) -> list[str]:
    candidates = [
        output_dir / "adapter_config.json",
        output_dir / "adapter_model.safetensors",
        output_dir / "adapter_model.bin",
    ]
    return [str(path) for path in candidates if path.is_file()]


def _finite(value: object) -> bool:
    try:
        return math.isfinite(float(value))
    except (TypeError, ValueError):
        return False


def main() -> int:
    args = parse_args()
    _require_files(args.train_file, args.validation_file)

    try:
        import torch
        from datasets import load_dataset
        from peft import LoraConfig, prepare_model_for_kbit_training
        from transformers import AutoModelForCausalLM, AutoTokenizer, BitsAndBytesConfig
        from trl import SFTConfig, SFTTrainer
    except ImportError as exc:
        raise SystemExit(
            "Fine-tune dependencies are missing. Install: pip install -e '.[finetune]'"
        ) from exc

    if not torch.cuda.is_available():
        raise SystemExit("QLoRA training requires a CUDA GPU; low-VRAM limits apply to inference.")

    torch.cuda.reset_peak_memory_stats()
    device_name = torch.cuda.get_device_name(0)
    total_vram_mb = int(torch.cuda.get_device_properties(0).total_memory / 1024**2)
    use_bf16 = bool(torch.cuda.is_bf16_supported())
    compute_dtype = torch.bfloat16 if use_bf16 else torch.float16
    quantization = BitsAndBytesConfig(
        load_in_4bit=True,
        bnb_4bit_quant_type="nf4",
        bnb_4bit_use_double_quant=True,
        bnb_4bit_compute_dtype=compute_dtype,
    )
    tokenizer = AutoTokenizer.from_pretrained(
        args.model,
        use_fast=True,
        trust_remote_code=args.trust_remote_code,
    )
    if tokenizer.pad_token_id is None:
        tokenizer.pad_token = tokenizer.eos_token

    model = AutoModelForCausalLM.from_pretrained(
        args.model,
        quantization_config=quantization,
        device_map="auto",
        torch_dtype=compute_dtype,
        trust_remote_code=args.trust_remote_code,
    )
    model.config.use_cache = False
    model = prepare_model_for_kbit_training(model, use_gradient_checkpointing=True)

    dataset = load_dataset(
        "json",
        data_files={
            "train": str(args.train_file),
            "validation": str(args.validation_file),
        },
    )
    if args.max_train_samples is not None:
        limit = min(args.max_train_samples, len(dataset["train"]))
        dataset["train"] = dataset["train"].select(range(limit))
    if args.max_validation_samples is not None:
        limit = min(args.max_validation_samples, len(dataset["validation"]))
        dataset["validation"] = dataset["validation"].select(range(limit))

    lora = LoraConfig(
        r=args.lora_r,
        lora_alpha=args.lora_alpha,
        lora_dropout=args.lora_dropout,
        target_modules="all-linear",
        bias="none",
        task_type="CAUSAL_LM",
    )
    training_args = SFTConfig(
        output_dir=str(args.output_dir),
        max_length=args.max_length,
        packing=args.packing,
        assistant_only_loss=args.assistant_only_loss,
        per_device_train_batch_size=args.batch_size,
        per_device_eval_batch_size=1,
        gradient_accumulation_steps=args.gradient_accumulation,
        gradient_checkpointing=True,
        learning_rate=args.learning_rate,
        num_train_epochs=args.epochs,
        max_steps=args.max_steps,
        warmup_ratio=0.05,
        lr_scheduler_type="cosine",
        optim="paged_adamw_8bit",
        bf16=use_bf16,
        fp16=not use_bf16,
        logging_steps=max(1, args.logging_steps),
        eval_strategy="steps",
        eval_steps=max(1, min(args.eval_steps, args.max_steps if args.max_steps > 0 else args.eval_steps)),
        save_strategy="steps",
        save_steps=max(1, min(args.save_steps, args.max_steps if args.max_steps > 0 else args.save_steps)),
        save_total_limit=3,
        load_best_model_at_end=True,
        metric_for_best_model="eval_loss",
        greater_is_better=False,
        seed=args.seed,
        data_seed=args.seed,
        report_to="none",
        remove_unused_columns=False,
    )
    trainer_kwargs: dict[str, Any] = {
        "model": model,
        "args": training_args,
        "train_dataset": dataset["train"],
        "eval_dataset": dataset["validation"],
        "processing_class": tokenizer,
        "peft_config": lora,
    }
    trainer = SFTTrainer(**trainer_kwargs)
    started = time.perf_counter()
    train_result = trainer.train(resume_from_checkpoint=args.resume_from_checkpoint)
    eval_metrics = trainer.evaluate()
    torch.cuda.synchronize()
    runtime_seconds = time.perf_counter() - started
    trainer.save_model(str(args.output_dir))
    tokenizer.save_pretrained(args.output_dir)

    adapter_files = _adapter_files(args.output_dir)
    summary = {
        "base_model": args.model,
        "output_dir": str(args.output_dir),
        "train_samples": len(dataset["train"]),
        "validation_samples": len(dataset["validation"]),
        "metrics": train_result.metrics,
        "evaluation": eval_metrics,
        "runtime_seconds": runtime_seconds,
        "completed_steps": int(getattr(trainer.state, "global_step", 0)),
        "compute_dtype": str(compute_dtype),
        "cuda": {
            "device_name": device_name,
            "total_vram_mb": total_vram_mb,
            "peak_allocated_mb": int(torch.cuda.max_memory_allocated() / 1024**2),
            "peak_reserved_mb": int(torch.cuda.max_memory_reserved() / 1024**2),
        },
        "adapter_files": adapter_files,
        "adapter_created": (args.output_dir / "adapter_config.json").is_file()
        and any(path.endswith((".safetensors", ".bin")) for path in adapter_files),
        "finite_train_loss": _finite(train_result.metrics.get("train_loss")),
        "finite_eval_loss": _finite(eval_metrics.get("eval_loss")),
        "lora": {
            "r": args.lora_r,
            "alpha": args.lora_alpha,
            "dropout": args.lora_dropout,
            "target_modules": "all-linear",
        },
    }
    args.output_dir.mkdir(parents=True, exist_ok=True)
    (args.output_dir / "argus_training_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2, default=str) + "\n",
        encoding="utf-8",
    )
    if args.qa_report is not None:
        args.qa_report.parent.mkdir(parents=True, exist_ok=True)
        args.qa_report.write_text(
            json.dumps(summary, ensure_ascii=False, indent=2, default=str) + "\n",
            encoding="utf-8",
        )
    print(json.dumps(summary, ensure_ascii=False, indent=2, default=str))
    return 0 if summary["adapter_created"] and summary["finite_eval_loss"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
