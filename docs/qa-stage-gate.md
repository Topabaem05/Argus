# AI Company Hardware, Unity, and Multiplayer QA Gate

This document defines the tests that must pass before Argus proceeds from the current AI-company vertical slice to authoritative multiplayer and production polish.

The policy is machine-readable at `qa/stage1-gates.json`. Results are aggregated by `scripts/qa/evaluate_stage_gate.py`. Missing evidence is not treated as success.

## Gate rule

The next stage remains locked unless all six reports exist and contain:

- `status: passed`;
- a matching `gate_id`;
- at least one criterion;
- every criterion set to `true`.

The aggregate result is written to:

```text
reports/qa/stage-gate-summary.json
```

Only a complete pass creates:

```text
reports/qa/NEXT_STAGE_UNLOCKED.md
```

## Required self-hosted runners

| Label | Required hardware/software | Purpose |
|---|---|---|
| `argus-cuda-train` | Linux, CUDA GPU, enough VRAM for Qwen3-0.6B NF4 QLoRA | Real training smoke |
| `argus-gpu-2gb` | Physical NVIDIA GPU with 1.5-2.6 GB visible VRAM | OOM verification |
| `argus-gpu-4gb` | Physical NVIDIA GPU with 3.5-4.8 GB visible VRAM | P95 and load verification |
| `argus-unity` | macOS runner with Unity 2022.3 LTS and valid license | Compile and Play Mode |

Virtual memory caps on a larger GPU do not qualify as the 2 GB or 4 GB result. Driver allocation and actual hardware behavior must be measured.

## Running the complete workflow

Open GitHub Actions and run **AI Company Hardware and Unity QA** manually. Supply:

- `llama_server`: path to the current llama.cpp `llama-server` binary;
- `adapter_lora`: path to the converted minibot LoRA GGUF on the 4 GB runner;
- `unity_editor`: path to Unity 2022.3 Editor;
- `bridge_config`: bridge YAML path in the repository checkout;
- latency thresholds.

The workflow always uploads evidence, even if a test command fails. The final `stage-gate` job downloads all reports and makes the only unlock decision.

---

## 1. Real CUDA QLoRA smoke

### Command

```bash
python scripts/build_minibot_sft_dataset.py \
  --output-dir data/minibot_sft \
  --count 500 \
  --seed 42

python scripts/qa/run_qlora_smoke.py \
  --model Qwen/Qwen3-0.6B \
  --max-steps 5
```

### What it proves

- CUDA is available;
- the model loads with NF4 and double quantization;
- forward, backward, optimizer, evaluation, and checkpoint paths execute;
- adapter files are loadable artifacts rather than an empty output directory;
- train/eval losses are finite;
- peak allocated and reserved VRAM are recorded.

### Pass criteria

- process exit code 0;
- at least five optimization steps;
- `adapter_config.json` plus adapter weights exist;
- finite train loss;
- finite validation loss;
- measured peak VRAM is positive and below the configured training-runner ceiling.

This is a smoke test, not a claim that five steps produce a useful model. Full training and held-out comparison are separate gates.

---

## 2. Base versus adapter accuracy

Both models must be evaluated sequentially on the same physical 4 GB runner, test split, context, quantization, temperature, and request limit.

```bash
python scripts/qa/compare_base_adapter.py \
  --base-report reports/qa/base/vram_4gb_latency-policy-eval.json \
  --adapter-report reports/qa/adapter/vram_4gb_latency-policy-eval.json
```

### Default pass criteria

- adapter action accuracy at least 0.85;
- adapter improves base action accuracy by at least 0.03;
- adapter generation failures do not exceed base failures;
- adapter P95 is no more than 1.25x the base P95.

Do not compare results from different GPUs or different test splits.

---

## 3. Actual 2 GB GPU OOM test

```bash
python scripts/qa/probe_llama_runtime.py \
  --gate-id vram_2gb_oom \
  --plan vram_2gb \
  --llama-server /path/to/llama-server \
  --limit 100 \
  --report reports/qa/vram_2gb_oom.json
```

The probe launches the real server, polls `/health`, samples `nvidia-smi`, runs held-out policy decisions, terminates the process, and scans logs for CUDA allocation failures.

### Pass criteria

- detected physical VRAM is 1.5-2.6 GB;
- server reaches health-ready state;
- no OOM marker appears in the server log;
- evaluation completes;
- zero generation failures.

The report records baseline, peak, and delta GPU memory.

---

## 4. Actual 4 GB P95 test

```bash
python scripts/qa/probe_llama_runtime.py \
  --gate-id vram_4gb_latency \
  --plan vram_4gb \
  --llama-server /path/to/llama-server \
  --limit 200 \
  --max-p95-ms 2500 \
  --report reports/qa/vram_4gb_latency.json
```

### Pass criteria

- detected physical VRAM is 3.5-4.8 GB;
- no OOM;
- all requested evaluations complete;
- zero generation failures;
- measured P95 is at or below the configured ceiling.

Report the GPU model and driver separately in runner inventory. P95 without hardware identity is not reusable evidence.

---

## 5. Unity compile and Play Mode

The workflow calls:

```bash
python scripts/qa/run_unity_qa.py \
  --unity /Applications/Unity/Hub/Editor/2022.3.x/Unity.app/Contents/MacOS/Unity
```

The script performs two independent batch invocations:

1. open/compile the project and run `ArgusUnity.Editor.BuildGameScene.Build`;
2. execute Play Mode tests and write NUnit XML.

Play Mode assertions verify:

- four CEO minibots;
- twelve employee minibots;
- unique actor IDs;
- root selection colliders;
- command ownership rules for own/rival employees;
- bridge, HUD, camera, local player controller, and employee controllers.

### Pass criteria

- scene build exit code 0;
- Play Mode exit code 0;
- NUnit XML parses;
- at least three tests execute;
- zero test failures;
- no `error CS` compiler marker.

A successful Python test suite does not substitute for this gate.

---

## 6. Four clients plus SLM load

```bash
python scripts/qa/four_client_slm_load_test.py \
  --clients 4 \
  --requests-per-client 20 \
  --max-p95-ms 3500
```

Each logical Unity client:

- opens an independent WebSocket;
- completes `unity.ready` / `bridge.ready` handshake;
- remains active while sending observer telemetry;
- issues concurrent structured SLM decisions.

### Pass criteria

- all four handshakes complete;
- bridge health reports `unity.connected_clients >= 4`;
- all 80 SLM requests complete;
- zero bridge/SLM errors;
- concurrent P95 stays within the configured limit.

### Known current blocker

`ClientRegistry` currently stores one `_websocket`, one `_session_id`, and one readiness state. The bridge health payload also lacks `connected_clients`. Therefore this gate is expected to fail until the multiplayer registry is refactored. That failure is intentional evidence, not a flaky-test exception.

---

## Gate aggregation

Manual aggregation:

```bash
python scripts/qa/evaluate_stage_gate.py \
  --policy qa/stage1-gates.json \
  --report-dir reports/qa
```

Exit codes:

- `0`: every required gate passed, next stage unlocked;
- `2`: missing, blocked, skipped, invalid, or failed evidence.

## Work unlocked after all gates pass

Only after `next_stage_unlocked=true` may the following be moved from backlog to implementation:

1. session-scoped four-client routing;
2. revisioned full-state snapshots and reconnect recovery;
3. command idempotency;
4. local CEO slot binding;
5. production animation import;
6. final uGUI/UI Toolkit replacement;
7. 20-30 minute multiplayer economy/rumor balance soak.

Passing the gate authorizes the next stage. It does not automatically claim those tasks are complete.
