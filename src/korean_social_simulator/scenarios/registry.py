"""Scenario family registry for supported simulation types."""

from __future__ import annotations

SUPPORTED_FAMILIES = frozenset(
    {
        "product_reaction",
        "pricing_reaction",
        "viral_marketing_risk",
        "rumor_crisis_response",
        "conflict_mediation",
        "policy_notice_acceptance",
        "community_operation",
        "organization_negotiation",
        "game_npc_social_world",
    }
)

FAMILY_DEFAULT_METRICS: dict[str, list[str]] = {
    "product_reaction": ["trust_score", "conversion_intent", "backlash_rate"],
    "pricing_reaction": ["trust_score", "conversion_intent", "backlash_rate"],
    "viral_marketing_risk": ["trust_score", "confusion_rate", "share_intent"],
    "rumor_crisis_response": ["trust_score", "confusion_rate", "backlash_rate", "consensus_score"],
    "conflict_mediation": ["conflict_intensity", "consensus_score", "trust_score"],
    "policy_notice_acceptance": ["trust_score", "confusion_rate", "policy_acceptance"],
    "community_operation": ["conflict_intensity", "consensus_score", "dropout_intent"],
    "organization_negotiation": ["consensus_score", "conflict_intensity", "trust_score"],
    "game_npc_social_world": ["conflict_intensity", "consensus_score"],
}


def is_supported_family(family: str) -> bool:
    """Check whether a scenario family name is supported."""
    return family in SUPPORTED_FAMILIES


def get_default_metrics(family: str) -> list[str]:
    """Return default metrics for a supported scenario family."""
    if family not in SUPPORTED_FAMILIES:
        return []
    return list(FAMILY_DEFAULT_METRICS.get(family, []))


def list_supported_families() -> list[str]:
    """Return sorted list of all supported scenario families."""
    return sorted(SUPPORTED_FAMILIES)
