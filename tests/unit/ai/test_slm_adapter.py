from __future__ import annotations

from korean_social_simulator.ai.low_vram import LOW_VRAM_PLANS, select_low_vram_plan
from korean_social_simulator.ai.model_profiles import get_model_profile
from korean_social_simulator.ai.slm_adapter import SLMRuntimeAdapter


def test_fallback_is_deterministic() -> None:
    prompt = "기분: 52/100, 호감도: 12, 업무: 서류 전달"
    first = SLMRuntimeAdapter().generate(prompt)
    second = SLMRuntimeAdapter().generate(prompt)
    assert first == second


def test_parser_recovers_json_from_markdown_and_clamps_values() -> None:
    adapter = SLMRuntimeAdapter()
    response = adapter._parse_json_response(
        "설명입니다.\n```json\n"
        '{"action":"complain","dialogue":"또 저예요?","efficiency":3.5,'
        '"mood_change":-99,"side_action":"gossip"}\n```'
    )
    assert response.action == "complain"
    assert response.dialogue == "또 저예요?"
    assert response.efficiency == 1.0
    assert response.mood_change == -10
    assert response.side_action == "gossip"


def test_parser_rejects_unknown_enum_values() -> None:
    adapter = SLMRuntimeAdapter()
    response = adapter._parse_json_response(
        '{"action":"invalid_action","dialogue":"...","efficiency":"bad",'
        '"mood_change":"bad","side_action":"invalid_side"}'
    )
    assert response.action == "accept"
    assert response.efficiency == 0.5
    assert response.mood_change == 0
    assert response.side_action is None


def test_low_mood_fallback_can_refuse() -> None:
    response = SLMRuntimeAdapter().generate("현재 기분: 10/100, 거부 임계 상태")
    assert response.action == "refuse"
    assert response.efficiency == 0.0


def test_high_mood_is_not_misread_as_single_digit_mood() -> None:
    response = SLMRuntimeAdapter().generate("현재 기분: 100/100, 호감도: 80")
    assert response.action != "refuse"
    assert response.efficiency > 0.0


def test_model_profile_lookup_uses_low_vram_edge_default() -> None:
    profile = get_model_profile("edge")
    assert profile.model_id == "Qwen/Qwen3.5-2B"
    assert profile.max_parallel_decisions == 1
    assert get_model_profile("vram_2gb").key == "micro"


def test_low_vram_plan_selection_boundaries() -> None:
    assert select_low_vram_plan(None).key == "cpu"
    assert select_low_vram_plan(2.0).key == "vram_2gb"
    assert select_low_vram_plan(3.0).key == "vram_3gb"
    assert select_low_vram_plan(4.0).key == "vram_4gb"


def test_llama_command_disables_vision_and_limits_slots() -> None:
    command = LOW_VRAM_PLANS["vram_2gb"].llama_server_args(lora_path="minibot.gguf")
    assert "--no-mmproj" in command
    assert command[command.index("--parallel") + 1] == "1"
    assert command[command.index("--gpu-layers") + 1] == "auto"
    assert command[command.index("--lora") + 1] == "minibot.gguf"
