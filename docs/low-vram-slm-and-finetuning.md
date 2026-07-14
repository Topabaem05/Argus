# Argus Minibot Low-VRAM SLM and Fine-tuning Guide

> Target: AI company game employee decisions and short Korean dialogue  
> Runtime: llama.cpp OpenAI-compatible server  
> Deployment target: CPU-only and 2–4 GB VRAM PCs  
> Training target: separate CUDA machine using QLoRA

## 1. Design decision

The game must not require a general-purpose 7B+ model on every player PC. A minibot only
needs to map a compact game state to one constrained decision object:

```json
{
  "action": "accept|reluctant_accept|refuse|complain",
  "dialogue": "short Korean line",
  "efficiency": 0.0,
  "mood_change": 0,
  "side_action": null
}
```

The low-end strategy is therefore:

1. Keep economy, authority, scoring, task progress, rumor propagation, and state mutation in
   deterministic Python code.
2. Use the SLM only for employee intent and dialogue.
3. Load a text-only quantized model; do not load the Qwen3.5 vision projector.
4. Use one llama.cpp slot, a 1.5K–2K context, small batches, and quantized KV cache.
5. Fine-tune 0.6B/1.7B text models on the game schema when the base model is inconsistent.
6. Deploy the small LoRA adapter on top of a quantized base instead of shipping another full
   merged model.
7. Preserve deterministic fallback behavior so a failed model process never blocks a round.

The VRAM values below are conservative runtime plans rather than universal guarantees. Driver
allocation, display usage, backend, quantization file, context length, and GPU architecture can
change the actual memory requirement.

## 2. Model tiers

| Plan | Inference model | Runtime alias | Primary target | Context | Slots | Notes |
|---|---|---|---:|---:|---:|---|
| `cpu` | Qwen3.5 0.8B Q4 | `argus-minibot-0.8b` | no usable GPU | 1536 | 1 | GPU layers disabled |
| `vram_2gb` | Qwen3.5 0.8B Q4 | `argus-minibot-0.8b` | 2 GB | 2048 | 1 | Q4 KV, no vision projector |
| `vram_3gb` | Qwen3.5 2B Q4 | `argus-minibot-2b` | 3 GB | 1536 | 1 | auto-fit and partial offload allowed |
| `vram_4gb` | Qwen3.5 2B Q4 | `argus-minibot-2b` | 4 GB | 2048 | 1 | Q8 KV; lower to Q4 if display VRAM is tight |
| `vram_6gb` | Qwen3.5 4B Q4 | `argus-minibot-4b` | 6 GB+ | 3072 | 1 | quality tier; not a 2–4 GB default |

Model sources:

- `Qwen/Qwen3.5-0.8B` and `unsloth/Qwen3.5-0.8B-GGUF`
- `Qwen/Qwen3.5-2B` and `unsloth/Qwen3.5-2B-GGUF`
- `Qwen/Qwen3.5-4B` and `unsloth/Qwen3.5-4B-GGUF`

All selected Qwen releases are Apache-2.0 according to their model metadata. The runtime uses
`--no-mmproj` because the game prompt is text-only. This prevents the multimodal projector from
consuming memory without adding game value.

## 3. Start the correct local runtime

### 3.1 Inspect the generated command

```bash
python scripts/launch_low_vram_slm.py
```

The script reads the smallest visible NVIDIA GPU through `nvidia-smi`. Without NVIDIA, it selects
the CPU plan. It prints the command without executing it.

Explicit plans:

```bash
python scripts/launch_low_vram_slm.py --plan vram_2gb
python scripts/launch_low_vram_slm.py --plan vram_3gb
python scripts/launch_low_vram_slm.py --plan vram_4gb
```

Override detection:

```bash
python scripts/launch_low_vram_slm.py --vram-gb 3.5
```

### 3.2 Execute llama.cpp

```bash
python scripts/launch_low_vram_slm.py --plan vram_2gb --execute
```

The generated server arguments include:

- `--hf-repo <GGUF repo>:Q4_K_M`
- `--no-mmproj`
- `--parallel 1`
- `--ctx-size 1536|2048`
- bounded `--batch-size` and `--ubatch-size`
- `--cache-type-k` and `--cache-type-v`
- `--gpu-layers auto`
- `--fit on`, `--fit-target`, and `--fit-ctx`

The `--fit` path allows llama.cpp to reduce offload or context allocation instead of failing
immediately when available VRAM is lower than expected.

### 3.3 Point Argus at the server

```bash
export ARGUS_SLM_PROVIDER=llamacpp
export ARGUS_SLM_MODEL=argus-minibot-2b
export ARGUS_SLM_BASE_URL=http://127.0.0.1:8080/v1
python -m korean_social_simulator.game.runner
```

For 2 GB:

```bash
export ARGUS_SLM_MODEL=argus-minibot-0.8b
```

The runtime alias must match the launcher's `--alias`. The default `GameRunner` alias is now
`argus-minibot-2b`.

## 4. When fine-tuning is justified

Do not fine-tune merely because a model sounds terse. Fine-tune only when held-out tests show one
or more of these failures:

- invalid JSON or extra prose around the JSON;
- wrong action under mood/loyalty threshold cases;
- accepting a command that the rule-derived target says should be refused;
- ignoring task/stat suitability;
- failing loyal-employee gossip refusal or loyal-company scout refusal;
- using invalid `side_action` values;
- unstable Korean dialogue that changes the intended action;
- action accuracy below the release gate.

Do not move authoritative game rules into the learned model. The server must still validate
ownership, active employment, round number, funds, and legal command targets.

## 5. Build the game-domain dataset

The generator is deterministic and teacher-free. It produces all command categories, balanced
mood/loyalty bands, personality effects, task suitability, memory variants, and exact JSON outputs.

```bash
python scripts/build_minibot_sft_dataset.py \
  --output-dir data/minibot_sft \
  --count 4000 \
  --seed 42
```

Outputs:

```text
data/minibot_sft/
├── train.jsonl
├── validation.jsonl
├── test.jsonl
└── manifest.json
```

The split is derived from a stable SHA-256 example ID:

- train: approximately 80%
- validation: approximately 10%
- test: approximately 10%

Never randomly re-split after examining test failures. Add new examples, regenerate with the same
algorithm/version, and keep the test set untouched until the next declared dataset version.

## 6. Fine-tune with QLoRA

### 6.1 Why Qwen3 text models are the training bases

Qwen3.5 0.8B/2B GGUF releases are the preferred low-memory inference targets. The checked-in
training path defaults to `Qwen/Qwen3-0.6B` or `Qwen/Qwen3-1.7B` because they are text-only
`AutoModelForCausalLM` models and have a simpler, reproducible QLoRA toolchain.

The game-specific adapter teaches the constrained policy and Korean response format; it is not
intended to create a broadly smarter model.

### 6.2 Install training dependencies

Do this on the training machine, not every player PC:

```bash
python -m venv .venv-finetune
source .venv-finetune/bin/activate
python -m pip install --upgrade pip
python -m pip install -e '.[finetune]'
```

The training script requires CUDA. Low-VRAM support in this document refers to inference. Training
still needs enough GPU memory for the selected base model, optimizer states, activations, and CUDA
workspace.

### 6.3 Train the 0.6B adapter

```bash
python scripts/train_minibot_qlora.py \
  --model Qwen/Qwen3-0.6B \
  --train-file data/minibot_sft/train.jsonl \
  --validation-file data/minibot_sft/validation.jsonl \
  --output-dir outputs/argus-minibot-0.6b-lora \
  --max-length 768 \
  --batch-size 1 \
  --gradient-accumulation 16 \
  --epochs 2 \
  --learning-rate 1e-4 \
  --lora-r 16 \
  --lora-alpha 32
```

### 6.4 Train the 1.7B adapter

```bash
python scripts/train_minibot_qlora.py \
  --model Qwen/Qwen3-1.7B \
  --output-dir outputs/argus-minibot-1.7b-lora \
  --batch-size 1 \
  --gradient-accumulation 16 \
  --max-length 768 \
  --epochs 2
```

Implemented training controls:

- 4-bit NF4 base weights;
- double quantization;
- BF16 when supported, otherwise FP16;
- `prepare_model_for_kbit_training`;
- gradient checkpointing;
- LoRA on all linear layers;
- assistant-only loss;
- sequence packing;
- paged 8-bit AdamW;
- deterministic model/data seeds;
- validation loss checkpoint selection;
- resumable checkpoints;
- no external experiment tracker by default.

## 7. Evaluate before export

Start an OpenAI-compatible endpoint with the candidate model or adapter, then run:

```bash
python scripts/evaluate_minibot_policy.py \
  --provider llamacpp \
  --model argus-minibot-0.8b \
  --base-url http://127.0.0.1:8080/v1 \
  --test-file data/minibot_sft/test.jsonl \
  --limit 300 \
  --minimum-action-accuracy 0.85
```

The report includes:

- action accuracy;
- side-action accuracy;
- efficiency MAE;
- mood-change MAE;
- generation failures;
- mean/P50/P95 latency;
- sample mismatches.

### Release gate

A candidate is not eligible for the default low-VRAM profile unless:

1. action accuracy is at least 0.85 on the untouched synthetic test set;
2. no invalid output reaches state mutation after adapter validation;
3. refusal and command-ownership Python tests still pass;
4. generation failures fall back without freezing a round;
5. P95 latency is acceptable on the actual 2 GB and 4 GB target machines;
6. a human Korean dialogue review finds no systematic personality collapse.

Promote the adapter only when it beats the quantized base model on the same test file and runtime
settings. Report both results; do not compare different context, quantization, or hardware.

## 8. Export LoRA to GGUF

Clone a current llama.cpp checkout and convert the PEFT adapter:

```bash
python scripts/export_minibot_lora_gguf.py \
  --llama-cpp-dir ../llama.cpp \
  --adapter-dir outputs/argus-minibot-0.6b-lora \
  --base-model-id Qwen/Qwen3-0.6B \
  --output outputs/argus-minibot-lora-f16.gguf
```

This is a dry run. Execute after reviewing the command:

```bash
python scripts/export_minibot_lora_gguf.py \
  --llama-cpp-dir ../llama.cpp \
  --adapter-dir outputs/argus-minibot-0.6b-lora \
  --base-model-id Qwen/Qwen3-0.6B \
  --output outputs/argus-minibot-lora-f16.gguf \
  --execute
```

Serve a base GGUF plus the adapter:

```bash
python scripts/launch_low_vram_slm.py \
  --plan vram_2gb \
  --lora outputs/argus-minibot-lora-f16.gguf \
  --execute
```

Base-model and LoRA architecture must match. Do not apply a Qwen3-0.6B adapter to a Qwen3.5-0.8B
base. For a Qwen3.5-specific adapter, train with a compatible multimodal-capable stack and repeat all
quality gates before changing the runtime profile.

## 9. Improving the dataset with real play sessions

Synthetic data establishes coverage but does not prove player-facing quality. Add replay-derived
examples through a reviewed pipeline:

1. Record prompt, raw model response, validated response, command result, employee state before and
   after, and latency.
2. Remove player names, chat identifiers, and free-form personal information.
3. Mark the failure category: JSON, wrong action, wrong side action, poor dialogue, latency, or
   safety.
4. Have a reviewer write the corrected JSON target.
5. Deduplicate by normalized state and command.
6. Keep one company/session entirely in one split to avoid near-duplicate leakage.
7. Weight rare refusal, gossip, scout, firing, and resignation cases during sampling.
8. Add adversarial paraphrases, but preserve the same game state and expected action.
9. Re-run base versus adapter evaluation and existing Python authority tests.

Do not train directly on unchecked model output. That amplifies the exact errors the fine-tune is
supposed to remove.

## 10. Failure recovery

### Out of memory at startup

1. Select a lower plan.
2. Close applications using display VRAM.
3. Lower context to 1536 or 1024.
4. Change K/V cache from Q8 to Q4.
5. Keep `--parallel 1`.
6. Allow `--fit` to reduce GPU offload.
7. Use CPU mode when the GPU cannot hold stable allocations.

### Model returns prose or invalid JSON

1. Keep the runtime parser and enum clamps enabled.
2. Evaluate the failure on the held-out set.
3. Add reviewed examples of the failure class.
4. Retrain; do not weaken the server schema.

### Dialogue quality is weak but actions are correct

Do not automatically move to a larger model. First add diverse, reviewed Korean dialogue targets
while preserving the same action labels. The game outcome depends on action correctness; dialogue
variation is a secondary objective.

## 11. Files introduced by this implementation

```text
src/korean_social_simulator/ai/low_vram.py
src/korean_social_simulator/ai/model_profiles.py
src/korean_social_simulator/training/game_sft.py
scripts/launch_low_vram_slm.py
scripts/build_minibot_sft_dataset.py
scripts/train_minibot_qlora.py
scripts/evaluate_minibot_policy.py
scripts/export_minibot_lora_gguf.py
tests/unit/training/test_game_sft.py
```

The GitHub workflow validates the low-VRAM command, builds a deterministic 1,000-example fixture,
runs Ruff, and runs the AI/game/training/bridge test suite without installing CUDA training
packages.
