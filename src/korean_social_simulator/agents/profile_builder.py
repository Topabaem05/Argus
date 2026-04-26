"""Agent profile builder converts PersonaRecords into Concordia-ready profiles."""

from __future__ import annotations

from korean_social_simulator.config.models import SafetyPolicy
from korean_social_simulator.errors import AgentProfileError, SafetyViolationError
from korean_social_simulator.models import AgentProfile, PersonaRecord, PopulationSample
from korean_social_simulator.safety.validator import PROHIBITED_OBJECTIVE_PATTERNS


def _find_unsafe_pattern(text: str) -> str | None:
    normalized_text = text.lower()

    for pattern in PROHIBITED_OBJECTIVE_PATTERNS:
        if pattern in normalized_text:
            return pattern

    return None


def _check_record_safety(record: PersonaRecord, policy: SafetyPolicy) -> None:
    if not policy.block_unsafe:
        return

    unsafe_pattern = _find_unsafe_pattern(record.persona)
    if unsafe_pattern is not None:
        raise SafetyViolationError(
            f"Persona record '{record.uuid}' contains prohibited pattern '{unsafe_pattern}'."
        )


def _check_profile_safety(profile: AgentProfile, policy: SafetyPolicy) -> None:
    if not policy.block_unsafe:
        return

    combined = profile.background + " " + " ".join(profile.goals)
    unsafe_pattern = _find_unsafe_pattern(combined)

    if unsafe_pattern is not None:
        raise SafetyViolationError(
            f"Agent profile '{profile.agent_id}' contains prohibited pattern '{unsafe_pattern}'."
        )


def _render_background(record: PersonaRecord) -> str:
    parts = [f"이름: {record.persona}", f"나이: {record.age}세"]

    if record.sex:
        parts.append(f"성별: {record.sex}")
    parts.append(f"직업: {record.occupation}")
    parts.append(f"지역: {record.province} {record.district}")

    if record.professional_persona:
        parts.append(f"전문 프로필: {record.professional_persona}")
    if record.family_persona:
        parts.append(f"가족 사항: {record.family_persona}")
    if record.cultural_background:
        parts.append(f"문화적 배경: {record.cultural_background}")
    if record.hobbies_and_interests:
        parts.append(f"관심사/취미: {record.hobbies_and_interests}")

    return "\n".join(parts)


def _render_memory_seeds(record: PersonaRecord) -> list[str]:
    seeds = [
        f"본인은 {record.persona}이며, {record.occupation}으로 일하고 있다.",
        f"{record.province} {record.district}에 거주 중이다.",
    ]
    if record.skills_and_expertise:
        seeds.append(f"전문 기술: {record.skills_and_expertise}")
    return seeds


def _behavior_rules(language: str) -> list[str]:
    rules = [
        "이것은 합성 페르소나 시뮬레이션입니다. 실존 인물이 아닙니다.",
        "시뮬레이션 상황에서 자연스럽게 반응하세요.",
        "실제 개인 정보를 생성하거나 추론하지 마세요.",
        "정치적 설득이나 표적화된 조작에 참여하지 마세요.",
    ]
    if language == "ko":
        rules.insert(1, "시나리오에서 지정하지 않는 한 한국어로 대화하세요.")
    return rules


def build_agent_profiles(
    sample: PopulationSample,
    language: str = "ko",
    safety_policy: SafetyPolicy | None = None,
) -> list[AgentProfile]:
    """Convert a population sample into agent profiles.

    Each PersonaRecord becomes one AgentProfile with rendered background,
    memory seeds, goals, and behavior rules.

    Raises:
        AgentProfileError: If a record is missing required data.
        SafetyViolationError: If a profile contains unsafe patterns.
    """
    if not sample.records:
        raise AgentProfileError("Population sample has no persona records.")

    policy = safety_policy or SafetyPolicy()
    profiles: list[AgentProfile] = []

    for record in sample.records:
        agent_id = f"agent-{record.uuid}"
        display_name = record.persona.split(",")[0].strip() if record.persona else record.uuid

        background = _render_background(record)
        memory_seeds = _render_memory_seeds(record)
        goals = ["주어진 시나리오에서 자신의 관점으로 반응한다."]
        rules = _behavior_rules(language)

        profile = AgentProfile(
            agent_id=agent_id,
            persona_uuid=record.uuid,
            display_name=display_name,
            language=language,
            background=background,
            memory_seeds=memory_seeds,
            goals=goals,
            behavior_rules=rules,
            safety_notes=[
                "이것은 합성 페르소나입니다.",
                "실제 사용자 행동을 예측하지 않습니다.",
            ],
        )

        _check_record_safety(record, policy)
        _check_profile_safety(profile, policy)
        profiles.append(profile)

    return profiles
