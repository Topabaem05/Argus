from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.config.models import (
    AttachmentPolicyConfig,
    PersonaMemoryUpdateConfig,
)
from korean_social_simulator.models import AgentProfile, AttachmentInput, SimulationEvent
from korean_social_simulator.simulation.dry_run import run_dry_run
from korean_social_simulator.simulation.interaction import (
    build_input_summary,
    build_interaction_context,
    select_personas_for_input,
    write_persona_memory_proposals,
)
from tests.unit.simulation.test_dry_run import _make_plan


def _profiles(count: int = 3) -> list[AgentProfile]:
    return [
        AgentProfile(
            agent_id=f"agent-{idx}",
            persona_uuid=f"persona-{idx}",
            display_name=f"Persona {idx}",
            language="ko",
            background=f"AI privacy file organizer user profile {idx}",
            memory_seeds=["privacy", "file management"],
            goals=["Evaluate user-submitted scenario."],
            behavior_rules=["Synthetic only."],
            safety_notes=["Not a prediction."],
        )
        for idx in range(count)
    ]


def test_attachment_validation_accepts_metadata_and_rejects_unsupported_extension(
    tmp_path: Path,
) -> None:
    accepted_file = tmp_path / "brief.txt"
    accepted_file.write_text("privacy feature brief", encoding="utf-8")
    summary = build_input_summary(
        chat_text="privacy-first file organizer",
        attachments=[
            AttachmentInput(path=str(accepted_file)),
            AttachmentInput(path="installer.exe", size_bytes=10),
        ],
        policy=AttachmentPolicyConfig(max_attachments=5),
        base_dir=tmp_path,
    )

    assert summary.accepted_attachment_count == 1
    assert summary.rejected_attachment_count == 1
    assert summary.attachments[0].kind == "text"
    assert summary.attachments[1].reason.startswith("unsupported extension")


def test_persona_selection_is_deterministic_and_capped() -> None:
    summary = build_input_summary(
        chat_text="AI privacy file organizer",
        attachments=[],
        policy=AttachmentPolicyConfig(),
    )

    first = select_personas_for_input(_profiles(25), summary, max_personas=20, seed=7)
    second = select_personas_for_input(_profiles(25), summary, max_personas=20, seed=7)

    assert len(first) == 20
    assert [item.agent_id for item in first] == [item.agent_id for item in second]
    assert first[0].reason.startswith("Matched topic terms")
    assert 0.0 <= first[0].confidence <= 1.0


def test_interaction_context_adds_schema_valid_discussion_events() -> None:
    profiles = _profiles(2)
    context = build_interaction_context(
        profiles=profiles,
        chat_text="AI privacy file organizer",
        attachments=[],
        policy=AttachmentPolicyConfig(),
        max_personas=2,
        seed=42,
    )
    plan = _make_plan(max_turns=1, agent_count=2)

    events = run_dry_run(plan, profiles, interaction_context=context)

    phases = [event.payload.get("phase") for event in events]
    assert "input_summary" in phases
    assert "persona_selection" in phases
    assert "individual_evaluation" in phases
    assert "discussion_turn" in phases
    assert "relationship_update" in phases


def test_persona_memory_proposals_write_backup_and_rollback(tmp_path: Path) -> None:
    dataset = tmp_path / "personas.jsonl"
    dataset.write_text('{"uuid":"p1"}\n', encoding="utf-8")
    events = [
        SimulationEvent(
            run_id="run-1",
            turn=1,
            event_type="system",
            payload={"phase": "turn_start"},
        )
    ]

    summary = write_persona_memory_proposals(
        run_dir=tmp_path / "run-1",
        source_dataset_path=dataset,
        profiles=_profiles(1),
        events=events,
        config=PersonaMemoryUpdateConfig(enabled=True),
    )

    assert summary["enabled"] is True
    assert summary["applied"] is False
    assert Path(str(summary["backup_path"])).read_text(encoding="utf-8") == '{"uuid":"p1"}\n'
    proposals = json.loads(Path(str(summary["proposal_path"])).read_text(encoding="utf-8"))
    assert proposals[0]["safe_to_apply"] is False
