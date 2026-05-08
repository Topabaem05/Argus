from __future__ import annotations

import importlib.util
from pathlib import Path


def _load_run_multi_scenario_module():
    script_path = Path(__file__).resolve().parents[2] / "scripts" / "run_multi_scenario.py"
    spec = importlib.util.spec_from_file_location("run_multi_scenario", script_path)
    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load run_multi_scenario.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_multi_scenario_run_id_slug_removes_path_separators() -> None:
    module = _load_run_multi_scenario_module()

    slug = module._slugify_run_id("New K-pop Ballad / Dance Track Target Demographic")

    assert "/" not in slug
    assert "\\" not in slug
    assert slug == "new_k-pop_ballad_dance_track_target_demo"
