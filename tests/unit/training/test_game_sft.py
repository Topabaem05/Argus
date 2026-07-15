from __future__ import annotations

import json

from korean_social_simulator.training.game_sft import (
    generate_game_sft_examples,
    split_game_sft_examples,
)


def test_generation_is_deterministic() -> None:
    first = generate_game_sft_examples(count=120, seed=7)
    second = generate_game_sft_examples(count=120, seed=7)
    assert [item.to_record() for item in first] == [item.to_record() for item in second]


def test_hash_splits_are_disjoint_and_complete() -> None:
    examples = generate_game_sft_examples(count=500, seed=42)
    splits = split_game_sft_examples(examples)
    ids = {name: {item.example_id for item in items} for name, items in splits.items()}
    assert sum(len(values) for values in ids.values()) == 500
    assert ids["train"].isdisjoint(ids["validation"])
    assert ids["train"].isdisjoint(ids["test"])
    assert ids["validation"].isdisjoint(ids["test"])
    assert all(ids.values())


def test_assistant_outputs_follow_runtime_schema() -> None:
    examples = generate_game_sft_examples(count=300, seed=11)
    allowed_actions = {"accept", "reluctant_accept", "refuse", "complain"}
    allowed_side_actions = {None, "gossip", "consider_quit"}
    source_actions: set[str] = set()
    for example in examples:
        output = json.loads(example.messages[-1]["content"])
        source_actions.add(str(example.metadata["action"]))
        assert output == example.expected
        assert output["action"] in allowed_actions
        assert output["side_action"] in allowed_side_actions
        assert 0.0 <= float(output["efficiency"]) <= 1.0
        assert -10 <= int(output["mood_change"]) <= 10
        assert isinstance(output["dialogue"], str) and output["dialogue"]
    assert source_actions == {
        "assign_task",
        "praise",
        "scold",
        "snack",
        "raise",
        "bonus",
        "party",
        "fire",
        "gossip",
        "scout",
    }


def test_fire_and_loyal_scout_have_game_specific_responses() -> None:
    examples = generate_game_sft_examples(count=1000, seed=19)
    fire = next(item for item in examples if item.metadata["action"] == "fire")
    loyal_scout = next(
        item
        for item in examples
        if item.metadata["action"] == "scout" and int(item.metadata["loyalty"]) >= 35
    )
    assert fire.expected["action"] == "complain"
    assert fire.expected["side_action"] == "consider_quit"
    assert loyal_scout.expected["action"] == "refuse"
