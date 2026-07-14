from __future__ import annotations

from dataclasses import dataclass

from korean_social_simulator.ai.prompt_builder import GamePromptBuilder
from korean_social_simulator.ai.slm_adapter import SLMResponse, SLMRuntimeAdapter
from korean_social_simulator.game.commands import PlayerCommandSystem
from korean_social_simulator.game.runner import GameRunner
from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.social.memory import AgentMemorySystem


@dataclass
class _FakeSLM:
    response: SLMResponse

    def generate(self, prompt: str, system_prompt: str = "") -> SLMResponse:
        return self.response


def _run_auto_command(response: SLMResponse) -> GameStateManager:
    manager = GameStateManager.new_game([("p1", "알파상사")], max_rounds=1)
    memory = AgentMemorySystem()
    runner = GameRunner(manager=manager)
    runner._auto_commands(  # noqa: SLF001 - verifies the command authority boundary directly
        PlayerCommandSystem(manager),
        memory,
        GamePromptBuilder(memory),
        _FakeSLM(response),  # type: ignore[arg-type]
        round_num=1,
    )
    return manager


def test_refusal_blocks_command_mutation_but_applies_model_mood_change() -> None:
    manager = _run_auto_command(
        SLMResponse(
            action="refuse",
            dialogue="오늘은 못 하겠습니다.",
            efficiency=0.0,
            mood_change=-3,
            side_action=None,
        )
    )
    company = manager.company("p1")
    assert company.funds == 10000
    assert company.active_employees()[0].mood == 47


def test_acceptance_applies_command_after_model_decision() -> None:
    manager = _run_auto_command(
        SLMResponse(
            action="accept",
            dialogue="알겠습니다.",
            efficiency=0.9,
            mood_change=2,
            side_action=None,
        )
    )
    company = manager.company("p1")
    assert company.funds == 9800
    assert company.active_employees()[0].mood == 67
