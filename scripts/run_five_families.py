from __future__ import annotations

import json
from pathlib import Path

from korean_social_simulator.agents.profile_builder import build_agent_profiles
from korean_social_simulator.config.loader import load_config
from korean_social_simulator.config.models import SamplingConfig, ScenarioConfig, SafetyPolicy
from korean_social_simulator.data.loader import load_personas_fixture
from korean_social_simulator.errors import KoreanSocialSimulationError
from korean_social_simulator.evaluation.metrics import evaluate_run
from korean_social_simulator.personas.sampler import sample_population
from korean_social_simulator.safety.validator import validate_safety
from korean_social_simulator.scenarios.compiler import compile_scenario
from korean_social_simulator.simulation.dry_run import run_dry_run
from korean_social_simulator.simulation.nvidia_nim import run_nvidia_nim_simulation
from korean_social_simulator.storage.run_store import RunStore

FIXTURE_PATH = Path("data/samples/personas_fixture.jsonl")
OUTPUT_ROOT = Path("outputs")

SCENARIOS = {
    "community_conflict": {
        "family": "community_conflict",
        "subs": [
            {
                "id": "comm_rule_change",
                "title": "Community Rule Change Reaction",
                "hypothesis": "When a community introduces a ban on political writing, short sudden notices cause the most backlash from 20-30s, while detailed FAQs with rationale are accepted by 40+ users. Administrator live Q&A reduces conflict intensity most effectively across all ages.",
            },
            {
                "id": "comm_comment_conflict",
                "title": "Comment Section Conflict Escalation",
                "hypothesis": "Anonymous comment sections escalate conflict 3x faster than real-name systems. 20s users are most aggressive under anonymity, 30-40s self-moderate even when anonymous, and 50+ avoid conflict entirely regardless of anonymity level.",
            },
            {
                "id": "comm_mediation",
                "title": "Mediation Effectiveness Test",
                "hypothesis": "A neutral mediator stepping in after 3 heated exchanges reduces toxicity by 60%, but only if the mediator acknowledges both sides before ruling. Authoritarian moderation ('stop this now') increases exit intent among younger users.",
            },
            {
                "id": "comm_exit_risk",
                "title": "Member Exit and Departure Risk",
                "hypothesis": "When a beloved community feature is removed, 20s users threaten to leave publicly but rarely follow through, while 40+ users silently reduce engagement by 40% without announcement. The real churn risk is invisible, not vocal.",
            },
        ],
    },
    "media_information_ecosystem": {
        "family": "media_information_ecosystem",
        "subs": [
            {
                "id": "media_news_frame",
                "title": "News Framing Acceptance by Age",
                "hypothesis": "When the same event is framed as 'economic opportunity' vs 'social risk', 20-30s respond to opportunity frames, 40-50s to risk frames, and 60+ trust neither frame — they rely on personal networks for information validation.",
            },
            {
                "id": "media_fact_check",
                "title": "Fact-Check Reception",
                "hypothesis": "When a popular but false claim is corrected by a fact-check, 20s ignore the correction if it conflicts with existing beliefs, 30s update their views but only from trusted institutions, and 50+ accept any official correction regardless of source.",
            },
            {
                "id": "media_rumor_correction",
                "title": "Rumor and Misinformation Correction",
                "hypothesis": "Correcting a viral rumor after 48 hours is 70% less effective than correcting within 2 hours. Younger users spread corrections faster, but older users are more likely to share the correction with their social circle once convinced.",
            },
            {
                "id": "media_source_trust",
                "title": "Source Credibility Perception",
                "hypothesis": "力学 professionals (doctors, engineers) trust academic/scientific sources most. Self-employed workers trust community word-of-mouth. Civil servants trust government publications. Students trust influencer/peer content most.",
            },
        ],
    },
    "customer_support_service": {
        "family": "customer_support_service",
        "subs": [
            {
                "id": "cs_refund_notice",
                "title": "Refund Policy Change Reaction",
                "hypothesis": "When refund window changes from 30 days to 7 days, 20s students complain loudly on social media but continue using the service. 30-40s professionals quietly switch to competitors. 50+ users don't notice the change.",
            },
            {
                "id": "cs_outage_notice",
                "title": "Service Outage Communication",
                "hypothesis": "An outage notice with specific time-to-fix estimate and transparent cause explanation reduces anger by 50% compared to vague 'we're working on it' messages. 30s tech workers are most demanding of technical details.",
            },
            {
                "id": "cs_apology",
                "title": "Corporate Apology Effectiveness",
                "hypothesis": "A sincere apology that names specific failures and concrete fixes rebuilds trust more than a generic apology with compensation. 40+ users value accountability language ('we failed to...'), while 20s value action timeline ('fixed by Friday').",
            },
            {
                "id": "cs_compensation",
                "title": "Compensation Offer Reactions",
                "hypothesis": "Offering a 3-month free extension as compensation for a data breach satisfies 20-30s but angers 40+ professionals who want privacy guarantees and legal accountability. Cash compensation is preferred by 50+ over service credits.",
            },
        ],
    },
    "finance_consumer_protection": {
        "family": "finance_consumer_protection",
        "subs": [
            {
                "id": "fin_fee_notice",
                "title": "Fee Increase Communication",
                "hypothesis": "When a bank raises monthly fees from 2,000 to 5,000 KRW, providing a detailed cost-breakdown explanation reduces complaint rates by 40%. 60+ customers are most sensitive to fee changes regardless of explanation quality.",
            },
            {
                "id": "fin_product_understanding",
                "title": "Financial Product Comprehension",
                "hypothesis": "Complex investment products with 10+ page terms confuse 70% of20 users across all education levels. Simplified one-page summaries with visual risk indicators improve comprehension 3x, especially for 50+ and students.",
            },
            {
                "id": "fin_risk_disclosure",
                "title": "Investment Risk Disclosure Impact",
                "hypothesis": "Explicit risk warnings ('you may lose all principal') reduce investment intent by 30% among 20-30s but have zero impact on 50+ investors who have prior investment experience. The young underweight risk, the old overweight past experience.",
            },
            {
                "id": "fin_scam_vulnerability",
                "title": "Fraud and Scam Message Vulnerability",
                "hypothesis": "SMS phishing messages claiming 'urgent bank security alert' fool 60+ users at a 4x higher rate than 20-30s. But 30s office workers are most vulnerable to fake invoice/receipt scams. Scam vulnerability is type-specific, not age-universal.",
            },
        ],
    },
    "healthcare_wellbeing": {
        "family": "healthcare_wellbeing",
        "subs": [
            {
                "id": "health_checkup",
                "title": "Health Checkup Campaign Reception",
                "hypothesis": "Free health checkup campaigns with fear-based messaging ('1 in 3 have undiagnosed hypertension') work on 50+ but repel 20-30s who prefer gain-framing ('detect early, live better'). 40s respond equally to both frames.",
            },
            {
                "id": "health_medication",
                "title": "Medication Adherence Communication",
                "hypothesis": "Simplified medication instructions with pictograms and color-coded schedules improve adherence by 35% for 60+ users. 30-40s professionals prefer app-based reminders.生效 20s respond best to gamified adherence tracking.",
            },
            {
                "id": "health_appointment",
                "title": "Hospital Appointment Explanation Clarity",
                "hypothesis": "When hospital pre-visit instructions are24 jargon-heavy ('NPO after midnight, bring prior imaging'), 60+ patients misunderstand at 2x the rate of younger patients. Simplified Korean-language instructions reduce no-show rates by 25%.",
            },
            {
                "id": "health_anxiety",
                "title": "Anxiety-Reducing Health Messaging",
                "hypothesis": "Health messages that acknowledge anxiety first ('It's normal to feel worried about test results') before providing information reduce procedure cancellation rates by 40%, especially effective for 30-50s patients facing serious diagnoses.",
            },
        ],
    },
}


def run_sub_scenario(family_key: str, sub: dict, profiles: list, sample) -> dict:
    family = SCENARIOS[family_key]["family"]
    sconfig = ScenarioConfig(
        id=sub["id"],
        family=family,
        title=sub["title"],
        hypothesis=sub["hypothesis"],
        language="ko",
        participant_count=len(profiles),
        max_turns=1,
        metrics=[],
    )
    plan = compile_scenario(sconfig, run_id=sub["id"], plan_id=f"{sub['id']}-plan")
    try:
        validate_safety(plan, profiles, SafetyPolicy())
    except KoreanSocialSimulationError:
        return {"status": "blocked", "error": "Safety blocked"}

    events = run_nvidia_nim_simulation(plan, profiles)
    agent_responses = [e for e in events if e.event_type == "agent_action"]

    response_data = []
    for e in agent_responses:
        actor = e.actor_id or ""
        display = e.payload.get("display_name", "")
        resp = e.payload.get("response", "")
        persona_age = "unknown"
        persona_occ = ""
        for rec in sample.records:
            if rec.uuid in actor:
                persona_age = f"{rec.age // 10 * 10}s"
                persona_occ = rec.occupation
                break
        response_data.append(
            {
                "display": display,
                "age_group": persona_age,
                "occupation": persona_occ,
                "response": resp,
            }
        )

    return {
        "status": "success",
        "sub_id": sub["id"],
        "title": sub["title"],
        "hypothesis": sub["hypothesis"],
        "family": family,
        "responses": response_data,
    }


def analyze_sub(name: str, result: dict):
    print(f"\n  {'─' * 50}")
    print(f"  {name} | {result.get('title', '?')}")
    print(f"  {'─' * 50}")
    resp_data = result.get("responses", [])
    if not resp_data:
        print("  No NIM responses.")
        return
    print(f"  Responses: {len(resp_data)}")
    for rd in resp_data:
        age = rd.get("age_group", "?")
        occ = rd.get("occupation", "")
        display = rd.get("display", "?")
        resp = rd.get("response", "")[:200]
        print(f"\n  [{age} {occ}] {display}")
        for line in resp.replace(". ", ".\n    ").split("\n")[:3]:
            print(f"    {line.strip()}")


def main():
    print("=" * 60)
    print("  Multi-Family Simulation — 5 Families, 20 Sub-Scenarios")
    print("=" * 60)

    personas = load_personas_fixture(FIXTURE_PATH)
    config = SamplingConfig(sample_size=5, seed=123, allow_smaller_sample=True)
    sample = sample_population(personas, config)
    profiles = build_agent_profiles(sample, language="ko")

    print(
        f"\n  Profiles: {len(profiles)} ({', '.join(f'{r.age // 10 * 10}s' for r in sample.records)})"
    )
    print(f"  Occupations: {', '.join(r.occupation for r in sample.records)}")

    all_results = {}
    skipped = set()

    for family_key in [
        "community_conflict",
        "media_information_ecosystem",
        "customer_support_service",
        "finance_consumer_protection",
        "healthcare_wellbeing",
    ]:
        all_results[family_key] = []
        print(f"\n{'=' * 60}")
        print(f"  FAMILY: {family_key}")
        print(f"{'=' * 60}")

        for sub in SCENARIOS[family_key]["subs"]:
            sub_key = sub["id"]
            if sub_key in skipped:
                continue
            print(f"\n  >>> Running: {sub['title']}")
            try:
                result = run_sub_scenario(family_key, sub, profiles, sample)
            except Exception as e:
                print(f"  FAILED: {e}")
                result = {"status": "failed", "error": str(e)}
            all_results[family_key].append(result)
            analyze_sub(family_key, result)

    print(f"\n{'=' * 60}")
    print("  Summary")
    print(f"{'=' * 60}")
    total_success = 0
    total_responses = 0

    for family_key, results in all_results.items():
        good_subs = [r for r in results if r.get("status") == "success"]
        failed = len(results) - len(good_subs)
        responses = sum(len(r.get("responses", [])) for r in good_subs)
        total_success += len(good_subs)
        total_responses += responses
        print(
            f"  {family_key:40s} {len(good_subs)}/4 passed  {responses} NIM responses  {failed} failed"
        )

    print(f"\n  TOTAL: {total_success} sub-scenarios, {total_responses} NIM calls")


if __name__ == "__main__":
    main()
