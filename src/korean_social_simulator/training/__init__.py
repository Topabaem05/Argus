"""Game-specific SFT data and evaluation helpers for compact minibot models."""

from __future__ import annotations

from korean_social_simulator.training.game_sft import (
    GameSFTExample,
    generate_game_sft_examples,
    split_game_sft_examples,
)

__all__ = ["GameSFTExample", "generate_game_sft_examples", "split_game_sft_examples"]
