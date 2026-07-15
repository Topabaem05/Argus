from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.qa.gates import (
    GatePolicy,
    GateReport,
    aggregate_stage_gate,
    write_gate_report,
)


def _policy() -> GatePolicy:
    return GatePolicy.model_validate(
        {
            "stage_id": "stage-1",
            "required_gates": [
                {"id": "a", "report_file": "a.json"},
                {"id": "b", "report_file": "b.json"},
            ],
            "next_stage": {
                "id": "stage-2",
                "title": "Next",
                "tasks": ["task"],
            },
        }
    )


def test_missing_report_keeps_next_stage_locked(tmp_path: Path) -> None:
    write_gate_report(
        tmp_path / "a.json",
        GateReport(gate_id="a", status="passed", criteria={"ok": True}),
    )
    summary = aggregate_stage_gate(_policy(), tmp_path)
    assert not summary.passed
    assert not summary.next_stage_unlocked
    assert summary.missing_reports == ["b"]


def test_failed_report_keeps_next_stage_locked(tmp_path: Path) -> None:
    write_gate_report(
        tmp_path / "a.json",
        GateReport(gate_id="a", status="passed", criteria={"ok": True}),
    )
    write_gate_report(
        tmp_path / "b.json",
        GateReport(gate_id="b", status="failed", criteria={"ok": False}),
    )
    summary = aggregate_stage_gate(_policy(), tmp_path)
    assert not summary.next_stage_unlocked
    assert "b: status=failed" in summary.blocked_reasons


def test_all_required_reports_unlock_next_stage(tmp_path: Path) -> None:
    for gate_id in ("a", "b"):
        write_gate_report(
            tmp_path / f"{gate_id}.json",
            GateReport(gate_id=gate_id, status="passed", criteria={"ok": True}),
        )
    summary = aggregate_stage_gate(_policy(), tmp_path)
    assert summary.passed
    assert summary.next_stage_unlocked
    assert summary.passed_gate_count == 2


def test_repository_policy_defines_all_six_required_gates() -> None:
    payload = json.loads(Path("qa/stage1-gates.json").read_text(encoding="utf-8"))
    policy = GatePolicy.model_validate(payload)
    assert {gate.id for gate in policy.required_gates} == {
        "cuda_qlora_smoke",
        "base_adapter_accuracy",
        "vram_2gb_oom",
        "vram_4gb_latency",
        "unity_compile_playmode",
        "four_client_slm_load",
    }
