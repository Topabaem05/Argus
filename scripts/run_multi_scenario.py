from __future__ import annotations

from pathlib import Path

from korean_social_simulator.agents.profile_builder import build_agent_profiles
from korean_social_simulator.config.models import (
    SafetyPolicy,
    SamplingConfig,
    ScenarioConfig,
)
from korean_social_simulator.errors import KoreanSocialSimulationError
from korean_social_simulator.evaluation.metrics import evaluate_run
from korean_social_simulator.reporting.markdown import render_report
from korean_social_simulator.safety.validator import validate_safety
from korean_social_simulator.scenarios.compiler import compile_scenario
from korean_social_simulator.simulation.dry_run import run_dry_run
from korean_social_simulator.simulation.nvidia_nim import run_nvidia_nim_simulation
from korean_social_simulator.storage.run_store import RunStore

FIXTURE_PATH = Path("data/samples/personas_fixture.jsonl")
OUTPUT_ROOT = Path("outputs")


def run_scenario(
    scenario_title: str,
    hypothesis: str,
    family: str,
    metric_names: list[str],
    max_turns: int = 1,
) -> dict:
    from korean_social_simulator.data.loader import load_personas_fixture
    from korean_social_simulator.personas.sampler import sample_population

    slug = scenario_title.replace(" ", "_").replace("-", "_").lower()[:40]
    run_id = f"sim_{slug}"

    personas = load_personas_fixture(FIXTURE_PATH)

    config = SamplingConfig(sample_size=14, seed=42, allow_smaller_sample=True)
    sample = sample_population(personas, config)
    profiles = build_agent_profiles(sample, language="ko")

    sconfig = ScenarioConfig(
        id=f"{run_id}_v1",
        family=family,
        title=scenario_title,
        hypothesis=hypothesis,
        language="ko",
        participant_count=len(profiles),
        max_turns=max_turns,
        metrics=metric_names,
    )

    plan = compile_scenario(sconfig, run_id=run_id, plan_id=f"{run_id}-plan")

    try:
        validate_safety(plan, profiles, SafetyPolicy())
    except KoreanSocialSimulationError:
        return {"status": "blocked", "error": "Safety validation blocked this scenario"}

    events = run_nvidia_nim_simulation(plan, profiles)
    agent_responses = [e for e in events if e.event_type == "agent_action"]

    dry_events = run_dry_run(plan, profiles)
    metrics_result = evaluate_run(dry_events, metric_names)

    output_dir = OUTPUT_ROOT / run_id
    store = RunStore(run_dir=output_dir, overwrite=True)
    store.write_events_batch(dry_events)
    store.write_metrics(metrics_result.metrics)

    report = render_report(
        run_id=run_id,
        status="success",
        metrics=metrics_result.metrics,
        events=dry_events,
        scenario_title=scenario_title,
        scenario_hypothesis=hypothesis,
    )
    (output_dir / "report.md").write_text(report, encoding="utf-8")

    from collections import defaultdict

    age_groups: dict[str, list[str]] = defaultdict(list)
    occ_groups: dict[str, list[str]] = defaultdict(list)

    for rec in sample.records:
        decade = f"{rec.age // 10 * 10}s"
        occupation = rec.occupation
        for e in agent_responses:
            actor = e.actor_id or ""
            payload_str = str(e.payload)
            if rec.uuid in actor or (
                rec.occupation and rec.occupation.replace(" ", "") in payload_str
            ):
                age_groups[decade].append(e.payload.get("response", ""))
                occ_groups[occupation].append(e.payload.get("response", ""))

    return {
        "status": "success",
        "run_id": run_id,
        "responses": len(agent_responses),
        "age_distribution": {k: len(v) for k, v in age_groups.items()},
        "age_groups": dict(age_groups),
        "occ_groups": dict(occ_groups),
        "agent_responses": [
            {
                "actor_id": e.actor_id,
                "display": e.payload.get("display_name", ""),
                "response": e.payload.get("response", ""),
            }
            for e in agent_responses
        ],
        "metrics": metrics_result.metrics,
    }


def print_results(label: str, result: dict):
    print()
    print("=" * 70)
    print(f"  {label}")
    print("=" * 70)
    if result.get("status") != "success":
        print(f"  FAILED: {result.get('error', 'unknown')}")
        return
    print(f"  Responses: {result['responses']}")
    age_dist = result.get("age_distribution", {})
    print(f"  Age groups: {dict(sorted(age_dist.items()))}")
    print()

    agent_resps = result.get("agent_responses", [])
    pencodings = {}
    for ar in agent_resps:
        enc = ar.get("display", "unknown")
        if enc not in pencodings:
            pencodings[enc] = []
        pencodings[enc].append(ar.get("response", ""))

    for name, responses in list(pencodings.items())[:8]:
        for r in responses[:1]:
            print(f"  --- {name} ---")
            for line in r.replace(". ", ".\n  ").split("\n")[:3]:
                print(f"  {line.strip()}")
            print()


result_1 = run_scenario(
    scenario_title="New K-pop Ballad / Dance Track Target Demographic",
    hypothesis="A new K-pop mid-tempo ballad with retro synth elements will resonate most strongly with 20-30s office workers and students who seek emotional catharsis, while an aggressive EDM dance track will split demographically between late teens and early 30s clubgoers.",
    family="product_reaction",
    metric_names=[
        "trust_score",
        "confusion_rate",
        "conversion_intent",
        "event_count",
        "turn_count",
        "agent_count",
    ],
)
print_results("SCENARIO 1: K-pop Song Demographics", result_1)


result_2 = run_scenario(
    scenario_title="Universal Basic Income (UBI) Policy Reception",
    hypothesis="A UBI proposal of 500,000 KRW per month will be received positively by self-employed workers and students but met with skepticism by civil servants and professionals in stable careers. Younger respondents will focus on freedom, older on fiscal responsibility.",
    family="policy_notice_acceptance",
    metric_names=[
        "comprehension_score",
        "acceptance_rate",
        "rejection_reasons",
        "event_count",
        "turn_count",
        "agent_count",
    ],
)
print_results("SCENARIO 2: Policy Reception (UBI)", result_2)


result_3 = run_scenario(
    scenario_title="YouTube Shorts vs Long-form Video Engagement",
    hypothesis="A 60-second YouTube Shorts format with fast cuts and trending audio will hook viewers under 30 within 2 seconds, while older viewers (40+) prefer 10+ minute deep-dive formats with calm pacing. Mid-career professionals fall in between, preferring 3-5 minute concise explainers.",
    family="viral_marketing_risk",
    metric_names=[
        "sentiment_score",
        "spreading_prob",
        "engagement_rate",
        "event_count",
        "turn_count",
        "agent_count",
    ],
)
print_results("SCENARIO 3: YouTube Video Engagement", result_3)


result_4 = run_scenario(
    scenario_title="AI Coding Assistant - Occupation x Age Reception",
    hypothesis="Software engineers and students (20-30s) will enthusiastically embrace AI coding tools as productivity enhancers, while experienced professionals in their 40-50s (doctors, lawyers, civil servants) will express concern about reliability, accountability, and skill erosion. Retired personas will be indifferent.",
    family="product_reaction",
    metric_names=[
        "trust_score",
        "confusion_rate",
        "conversion_intent",
        "backlash_rate",
        "event_count",
        "turn_count",
        "agent_count",
    ],
)
print_results("SCENARIO 4: AI Coding Tool by Occupation", result_4)


all_results = [r for r in [result_1, result_2, result_3, result_4] if r.get("status") == "success"]
print(f"  Successful simulations: {len(all_results)}/4")
for r in all_results:
    resp_count = r.get("responses", 0)
    print(
        f"  {r.get('run_id', '?'):50s}  {resp_count:3d} responses  metrics: {len(r.get('metrics', {}))}"
    )
