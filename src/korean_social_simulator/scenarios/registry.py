"""Scenario family registry for supported simulation types."""

from __future__ import annotations

SUPPORTED_FAMILIES = frozenset(
    {
        "product_market",
        "content_culture",
        "marketing_viral",
        "community_conflict",
        "policy_public_opinion",
        "organization_workplace",
        "education_learning",
        "healthcare_wellbeing",
        "finance_consumer_protection",
        "urban_local_life",
        "crisis_risk_communication",
        "media_information_ecosystem",
        "game_npc_social_world",
        "ai_agent_evaluation",
        "social_norms_behavior",
        "customer_support_service",
    }
)

FAMILY_DEFAULT_METRICS: dict[str, list[str]] = {
    "product_market": [
        "interest_score",
        "purchase_intent",
        "trial_intent",
        "trust_score",
        "price_sensitivity_score",
        "feature_clarity_score",
        "churn_risk",
        "recommendation_intent",
    ],
    "content_culture": [
        "emotional_resonance",
        "identity_alignment",
        "historical_sensitivity",
        "share_intent",
        "fan_conversion_intent",
        "controversy_risk",
        "generation_gap_score",
        "replay_or_reread_intent",
        "interpretation_diversity",
    ],
    "marketing_viral": [
        "share_rate",
        "meme_potential",
        "virality_score",
        "backlash_risk",
        "ad_fatigue_score",
        "message_clarity",
        "brand_lift",
        "organic_spread_score",
        "negative_comment_risk",
    ],
    "community_conflict": [
        "conflict_intensity",
        "trust_in_moderator",
        "rule_acceptance",
        "exit_intent",
        "toxicity_risk",
        "misunderstanding_rate",
        "mediation_effectiveness",
        "polarization_score",
        "counter_narrative_score",
    ],
    "policy_public_opinion": [
        "comprehension_score",
        "acceptance_rate",
        "rejection_reasons",
        "fairness_perception",
        "fiscal_concern_score",
        "trust_score",
        "implementation_feasibility",
        "social_equity_score",
    ],
    "organization_workplace": [
        "satisfaction_score",
        "engagement_score",
        "conflict_intensity",
        "consensus_score",
        "turnover_intent",
        "collaboration_quality",
        "leadership_trust",
        "decision_acceptance",
    ],
    "education_learning": [
        "comprehension_score",
        "engagement_score",
        "knowledge_retention",
        "motivation_score",
        "accessibility_score",
        "dropout_risk",
        "peer_collaboration",
        "practical_applicability",
    ],
    "healthcare_wellbeing": [
        "trust_score",
        "compliance_score",
        "accessibility_score",
        "privacy_concern_score",
        "health_literacy",
        "treatment_satisfaction",
        "cost_concern_score",
        "preventive_behavior_intent",
    ],
    "finance_consumer_protection": [
        "trust_score",
        "transparency_score",
        "comprehension_score",
        "fee_sensitivity",
        "switching_intent",
        "complaint_intent",
        "regulatory_confidence",
        "fraud_awareness",
    ],
    "urban_local_life": [
        "satisfaction_score",
        "accessibility_score",
        "community_belonging",
        "safety_perception",
        "noise_tolerance",
        "development_support",
        "preservation_concern",
        "mobility_satisfaction",
    ],
    "crisis_risk_communication": [
        "trust_score",
        "compliance_score",
        "message_clarity",
        "anxiety_level",
        "behavioral_change_intent",
        "information_seeking",
        "rumor_spread_risk",
        "institutional_trust",
    ],
    "media_information_ecosystem": [
        "trust_score",
        "source_credibility",
        "bias_perception",
        "fact_check_intent",
        "echo_chamber_risk",
        "information_overload",
        "media_literacy",
        "polarization_risk",
    ],
    "game_npc_social_world": [
        "story_progression",
        "npc_interaction_depth",
        "quest_completion",
        "emotional_engagement",
        "character_attachment",
        "world_immersion",
        "dialogue_quality",
        "replay_intent",
    ],
    "ai_agent_evaluation": [
        "trust_score",
        "usefulness_score",
        "accuracy_perception",
        "privacy_concern_score",
        "autonomy_preference",
        "error_tolerance",
        "human_ai_collaboration",
        "adoption_intent",
    ],
    "social_norms_behavior": [
        "norm_acceptance",
        "behavioral_conformity",
        "social_pressure",
        "generational_difference",
        "urban_rural_gap",
        "moral_alignment",
        "deviance_tolerance",
        "norm_change_openness",
    ],
    "customer_support_service": [
        "satisfaction_score",
        "resolution_speed",
        "agent_empathy",
        "escalation_rate",
        "repeat_contact_rate",
        "channel_preference",
        "self_service_effectiveness",
        "brand_loyalty_impact",
    ],
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
