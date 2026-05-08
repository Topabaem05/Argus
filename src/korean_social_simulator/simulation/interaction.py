"""Offline-first interaction helpers for user input, selection, and memory proposals."""

from __future__ import annotations

import json
import random
import re
import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Literal

from korean_social_simulator.config.models import (
    AttachmentPolicyConfig,
    PersonaMemoryUpdateConfig,
)
from korean_social_simulator.errors import StorageError
from korean_social_simulator.models import (
    AgentProfile,
    AttachmentInput,
    AttachmentKind,
    AttachmentValidation,
    IndividualEvaluation,
    PersonaMemoryProposal,
    PersonaSelectionResult,
    SimulationEvent,
    SimulationInputSummary,
)

_TOKEN_RE = re.compile(r"[A-Za-z0-9가-힣]+")
_IMAGE_EXTENSIONS = frozenset({".jpg", ".jpeg", ".png", ".gif", ".webp"})
_VIDEO_EXTENSIONS = frozenset({".mp4", ".mov", ".m4v", ".webm"})
_TEXT_EXTENSIONS = frozenset({".txt", ".md", ".csv", ".json", ".yaml", ".yml"})
_DOCUMENT_EXTENSIONS = frozenset({".pdf", ".doc", ".docx", ".ppt", ".pptx"})
_Stance = Literal["supports", "opposes", "mixed", "uncertain"]
_STANCE_VALUES: tuple[_Stance, ...] = ("supports", "opposes", "mixed", "uncertain")


@dataclass(frozen=True)
class InteractionContext:
    """Prepared input context for a simulation run."""

    summary: SimulationInputSummary
    selections: list[PersonaSelectionResult]
    evaluations: list[IndividualEvaluation]


def build_input_summary(
    *,
    chat_text: str,
    attachments: list[AttachmentInput],
    policy: AttachmentPolicyConfig,
    base_dir: Path | None = None,
) -> SimulationInputSummary:
    """Validate request attachments and build a sanitized input summary."""
    validation = validate_attachments(attachments, policy, base_dir=base_dir)
    accepted = [item for item in validation if item.accepted]
    rejected = [item for item in validation if not item.accepted]
    topic_summary = _topic_summary(chat_text, accepted)
    return SimulationInputSummary(
        chat_text=chat_text,
        attachments=validation,
        accepted_attachment_count=len(accepted),
        rejected_attachment_count=len(rejected),
        topic_summary=topic_summary,
    )


def validate_attachments(
    attachments: list[AttachmentInput],
    policy: AttachmentPolicyConfig,
    *,
    base_dir: Path | None = None,
) -> list[AttachmentValidation]:
    """Validate attachment metadata without executing or parsing file content."""
    if not attachments:
        return []

    root = (base_dir or Path.cwd()).resolve()
    allowed = set(policy.allowed_extensions)
    results: list[AttachmentValidation] = []

    for index, attachment in enumerate(attachments):
        path = Path(attachment.path)
        filename = path.name
        extension = path.suffix.lower()
        size = attachment.size_bytes
        accepted = True
        reason = "accepted"

        if index >= policy.max_attachments:
            accepted = False
            reason = "max attachment count exceeded"
        elif not filename or filename in {".", ".."}:
            accepted = False
            reason = "filename is invalid"
        elif extension not in allowed:
            accepted = False
            reason = f"unsupported extension: {extension or '(none)'}"
        else:
            resolved = path if path.is_absolute() else root / path
            if resolved.exists():
                try:
                    size = resolved.stat().st_size
                except OSError:
                    accepted = False
                    reason = "file size could not be read"
            elif size is None:
                accepted = False
                reason = "file not found and size_bytes was not provided"

        if size is not None and size > policy.max_attachment_bytes:
            accepted = False
            reason = "attachment exceeds max_attachment_bytes"

        results.append(
            AttachmentValidation(
                path=attachment.path,
                filename=filename,
                extension=extension,
                kind=_kind_for_extension(extension),
                accepted=accepted,
                reason=reason,
                size_bytes=size,
            )
        )

    return results


def select_personas_for_input(
    profiles: list[AgentProfile],
    input_summary: SimulationInputSummary,
    *,
    max_personas: int,
    seed: int,
) -> list[PersonaSelectionResult]:
    """Select up to ``max_personas`` profiles with deterministic reasons."""
    if not profiles:
        return []

    query_terms = _terms(
        " ".join(
            [
                input_summary.chat_text,
                input_summary.topic_summary,
                " ".join(a.filename for a in input_summary.attachments if a.accepted),
            ]
        )
    )
    rng = random.Random(seed)
    scored: list[tuple[float, float, AgentProfile, list[str]]] = []

    for profile in profiles:
        profile_terms = _terms(
            " ".join(
                [
                    profile.display_name,
                    profile.background,
                    " ".join(profile.memory_seeds),
                    " ".join(profile.goals),
                ]
            )
        )
        matches = sorted(query_terms & profile_terms)
        score = float(len(matches))
        if profile.safety_notes:
            score += 0.1
        # Stable jitter breaks ties without hiding deterministic behavior.
        scored.append((score, rng.random(), profile, matches))

    scored.sort(key=lambda item: (-item[0], item[1], item[2].agent_id))
    selected = scored[: min(max_personas, len(scored))]

    results: list[PersonaSelectionResult] = []
    for score, _jitter, profile, matches in selected:
        confidence = min(0.95, 0.35 + (score * 0.12))
        if matches:
            reason = f"Matched topic terms: {', '.join(matches[:5])}."
        else:
            reason = "Selected as a deterministic diversity candidate for the simulation."
        results.append(
            PersonaSelectionResult(
                agent_id=profile.agent_id,
                persona_uuid=profile.persona_uuid,
                display_name=profile.display_name,
                reason=reason,
                confidence=round(confidence, 3),
                matched_terms=matches[:10],
                safety_notes=profile.safety_notes,
            )
        )
    return results


def build_individual_evaluations(
    profiles: list[AgentProfile],
    input_summary: SimulationInputSummary,
    *,
    seed: int,
) -> list[IndividualEvaluation]:
    """Create schema-valid mock evaluations for deterministic offline runs."""
    rng = random.Random(seed)
    evaluations: list[IndividualEvaluation] = []
    topic = input_summary.topic_summary or "the submitted scenario"

    for profile in profiles:
        stance = _STANCE_VALUES[(sum(ord(ch) for ch in profile.agent_id) + rng.randrange(4)) % 4]
        confidence = 0.45 + ((len(profile.memory_seeds) % 4) * 0.1)
        evaluations.append(
            IndividualEvaluation(
                agent_id=profile.agent_id,
                stance=stance,
                confidence=round(min(confidence, 0.85), 3),
                rationale=(
                    f"{profile.display_name} responds to {topic} from synthetic background "
                    "and stated behavior rules."
                ),
                uncertainty="Dry-run output; not a real-world prediction.",
            )
        )

    return evaluations


def build_interaction_context(
    *,
    profiles: list[AgentProfile],
    chat_text: str,
    attachments: list[AttachmentInput],
    policy: AttachmentPolicyConfig,
    max_personas: int,
    seed: int,
    base_dir: Path | None = None,
) -> InteractionContext:
    """Build all deterministic interaction context for one run."""
    summary = build_input_summary(
        chat_text=chat_text,
        attachments=attachments,
        policy=policy,
        base_dir=base_dir,
    )
    selections = select_personas_for_input(
        profiles,
        summary,
        max_personas=max_personas,
        seed=seed,
    )
    selected_ids = {selection.agent_id for selection in selections}
    selected_profiles = [profile for profile in profiles if profile.agent_id in selected_ids]
    evaluations = build_individual_evaluations(
        selected_profiles,
        summary,
        seed=seed,
    )
    return InteractionContext(summary=summary, selections=selections, evaluations=evaluations)


def write_persona_memory_proposals(
    *,
    run_dir: Path,
    source_dataset_path: Path,
    profiles: list[AgentProfile],
    events: list[SimulationEvent],
    config: PersonaMemoryUpdateConfig,
) -> dict[str, object]:
    """Write opt-in persona-memory proposal artifacts with backup metadata."""
    if not config.enabled:
        return {"enabled": False, "applied": False, "proposal_count": 0}

    target_dir = run_dir / config.output_dir
    backup_dir = target_dir / "backups"
    proposal_path = target_dir / "proposals.json"
    rollback_path = target_dir / "rollback.json"
    diff_path = target_dir / "diff.json"

    try:
        backup_dir.mkdir(parents=True, exist_ok=True)
        target_dir.mkdir(parents=True, exist_ok=True)
        backup_path = backup_dir / source_dataset_path.name
        if source_dataset_path.exists():
            shutil.copy2(source_dataset_path, backup_path)
        else:
            backup_path.write_text("", encoding="utf-8")

        proposals = _memory_proposals(profiles, events)
        proposal_payload = [proposal.model_dump(mode="json") for proposal in proposals]
        proposal_path.write_text(
            json.dumps(proposal_payload, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )
        diff_path.write_text(
            json.dumps(
                {
                    "applied": False,
                    "apply_confirmed": config.apply_confirmed,
                    "changed_persona_uuids": [proposal.persona_uuid for proposal in proposals],
                },
                ensure_ascii=False,
                indent=2,
                sort_keys=True,
            )
            + "\n",
            encoding="utf-8",
        )
        rollback_path.write_text(
            json.dumps(
                {
                    "source_dataset_path": str(source_dataset_path),
                    "backup_path": str(backup_path),
                    "restore_command": f"cp {backup_path} {source_dataset_path}",
                },
                ensure_ascii=False,
                indent=2,
                sort_keys=True,
            )
            + "\n",
            encoding="utf-8",
        )
    except OSError as exc:
        raise StorageError(f"Failed to write persona memory proposal artifacts: {exc}") from exc

    return {
        "enabled": True,
        "applied": False,
        "proposal_count": len(proposals),
        "backup_path": str(backup_path),
        "proposal_path": str(proposal_path),
        "diff_path": str(diff_path),
        "rollback_path": str(rollback_path),
    }


def _memory_proposals(
    profiles: list[AgentProfile],
    events: list[SimulationEvent],
) -> list[PersonaMemoryProposal]:
    event_ids = [
        f"{event.run_id}:{event.turn}:{event.event_type}:{event.actor_id or 'system'}"
        for event in events[:5]
    ]
    proposals: list[PersonaMemoryProposal] = []
    for profile in profiles:
        proposals.append(
            PersonaMemoryProposal(
                persona_uuid=profile.persona_uuid,
                agent_id=profile.agent_id,
                proposed_memory=(
                    "Simulated run produced a scenario-specific stance trace. "
                    "Store only if a human explicitly approves the synthetic update."
                ),
                reason="Opt-in proposal generated from dry-run interaction artifacts.",
                evidence_event_ids=event_ids,
                safe_to_apply=False,
            )
        )
    return proposals


def _topic_summary(chat_text: str, attachments: list[AttachmentValidation]) -> str:
    terms = sorted(_terms(chat_text))
    attachment_bits = [item.kind for item in attachments if item.accepted]
    if terms:
        return " ".join(terms[:12])
    if attachment_bits:
        return f"{len(attachments)} accepted attachment(s): {', '.join(attachment_bits)}"
    return "general user-submitted simulation request"


def _terms(text: str) -> set[str]:
    return {token.lower() for token in _TOKEN_RE.findall(text) if len(token) >= 2}


def _kind_for_extension(extension: str) -> AttachmentKind:
    if extension in _IMAGE_EXTENSIONS:
        return "image"
    if extension in _VIDEO_EXTENSIONS:
        return "video"
    if extension in _TEXT_EXTENSIONS:
        return "text"
    if extension in _DOCUMENT_EXTENSIONS:
        return "document"
    return "unknown"
