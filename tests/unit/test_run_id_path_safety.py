from __future__ import annotations

import pytest

from korean_social_simulator.config.models import RuntimeSection


@pytest.mark.parametrize(
    "run_id",
    ["../escape", "nested/run", "nested\\run", ".hidden", "bad run id"],
)
def test_runtime_run_id_rejects_unsafe_paths(run_id: str) -> None:
    with pytest.raises(ValueError, match="run_id"):
        RuntimeSection(
            run_id=run_id,
            seed=42,
            output_dir="outputs",
            dry_run=True,
            max_turns=5,
            max_participants=10,
        )


def test_runtime_run_id_accepts_stable_slug() -> None:
    section = RuntimeSection(
        run_id="product_reaction_run_001",
        seed=42,
        output_dir="outputs",
        dry_run=True,
        max_turns=5,
        max_participants=10,
    )
    assert section.run_id == "product_reaction_run_001"
