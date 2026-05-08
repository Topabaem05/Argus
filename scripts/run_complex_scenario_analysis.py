from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any, cast

import yaml

from korean_social_simulator.models import PersonaRecord
from korean_social_simulator.pipeline import run_command

REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_ROOT = Path("outputs/complex_scenarios")
COSMETICS_CONFIG = Path("examples/complex_cosmetics_20s.yaml")
WORKER_LAW_CONFIG = Path("examples/complex_worker_law.yaml")
MIN_PERSONAS_PER_SCENARIO = 3000


@dataclass(frozen=True)
class CampaignVariant:
    variant_id: str
    name: str
    product: str
    message: str
    song_preference: str
    real_world_analog: str


@dataclass(frozen=True)
class WorkerLawComponent:
    component_id: str
    name: str
    affected_groups: tuple[str, ...]
    real_world_analog: str


CAMPAIGNS = (
    CampaignVariant(
        variant_id="commute_skinfit_poster",
        name="Commute Skinfit SPF Cushion",
        product="tone-up sunscreen cushion",
        message="2-minute commute routine, affordable refill, creator review proof",
        song_preference="lo-fi R&B or soft K-pop hook for routine/tutorial cuts",
        real_world_analog="Gen Z beauty outreach through TikTok creators and educational skincare formats.",
    ),
    CampaignVariant(
        variant_id="idol_glow_challenge",
        name="Idol Glow Lip Challenge",
        product="glossy lip tint and cheek glow set",
        message="selfie template, short dance challenge, limited pink visual system",
        song_preference="high-BPM K-pop dance hook under 15 seconds",
        real_world_analog="2023 Barbie-style selfie templates, brand partnerships, and user remixes.",
    ),
    CampaignVariant(
        variant_id="dermatologist_barrier_serum",
        name="Barrier Proof Serum",
        product="low-irritation barrier serum",
        message="ingredient callouts, dermatologist proof, sensitive-skin reassurance",
        song_preference="minimal ambient bed with spoken proof points",
        real_world_analog="Skincare myth-busting and expert-led short video formats.",
    ),
    CampaignVariant(
        variant_id="refillable_vegan_lip_tint",
        name="Refillable Vegan Tint",
        product="refillable vegan lip tint",
        message="campus styling, color swatches, lower waste, daily carry",
        song_preference="retro synth-pop for swatch transitions",
        real_world_analog="Mission-led beauty brands and affordable luxury positioning.",
    ),
)

WORKER_LAW_COMPONENTS = (
    WorkerLawComponent(
        component_id="rest_period_enforcement",
        name="Rest-period and overtime audit enforcement",
        affected_groups=("nurse", "developer", "marketer", "teacher", "public official"),
        real_world_analog="Korean working-time rules include weekly limits and rest-period debates.",
    ),
    WorkerLawComponent(
        component_id="platform_worker_coverage",
        name="Platform and freelance worker coverage",
        affected_groups=("freelancer", "job seeker", "student", "self-employed"),
        real_world_analog="Public labor-reform debate includes protection for new employment types.",
    ),
    WorkerLawComponent(
        component_id="small_business_transition",
        name="Small-business transition support",
        affected_groups=("self-employed", "manager", "doctor"),
        real_world_analog="Working-hour reductions commonly create scheduling and compliance pressure.",
    ),
    WorkerLawComponent(
        component_id="worker_stop_right",
        name="Right to stop unsafe work without retaliation",
        affected_groups=("nurse", "doctor", "teacher", "worker", "developer"),
        real_world_analog="Workplace safety accountability expanded under Korean serious-accident policy debates.",
    ),
)

SOURCE_NOTES = [
    {
        "label": "MOEL Labor Standards",
        "url": "https://www.moel.go.kr/english/policy/laborStandards.do",
        "used_for": "Working-hours and rest-period context.",
    },
    {
        "label": "MOEL Labor Reform",
        "url": "https://www.moel.go.kr/english/policy/laborReform.do",
        "used_for": "Worker health, working-hour reform, and new employment-type protection context.",
    },
    {
        "label": "Marketing Dive Gen Z Beauty",
        "url": "https://www.marketingdive.com/news/beauty-marketing-gen-z-elf-cosmetics-estee-lauder-loreal-2023/651167/",
        "used_for": "Beauty-brand creator and Gen Z outreach analogs.",
    },
    {
        "label": "Axios Barbie Marketing Mania",
        "url": "https://www.axios.com/2023/07/06/barbie-movie-marketing-power",
        "used_for": "2023 viral campaign scale and partnership analog.",
    },
    {
        "label": "Creative Boom Barbie Selfie Generator",
        "url": "https://www.creativeboom.com/insight/five-of-the-best-barbie-inspired-marketing-campaigns/",
        "used_for": "Selfie-template and user-generated poster analog.",
    },
]


def run_complex_analysis(
    output_root: Path = DEFAULT_OUTPUT_ROOT,
    cosmetics_config: Path = COSMETICS_CONFIG,
    worker_law_config: Path = WORKER_LAW_CONFIG,
) -> dict[str, object]:
    output_root = Path(output_root)
    output_root.mkdir(parents=True, exist_ok=True)
    generated_config_dir = output_root / "_configs"
    generated_config_dir.mkdir(parents=True, exist_ok=True)

    cosmetics_config_path = _runtime_config(cosmetics_config, output_root, generated_config_dir)
    worker_config_path = _runtime_config(worker_law_config, output_root, generated_config_dir)

    cosmetics_result = run_command(cosmetics_config_path, dry_run=True)
    worker_result = run_command(worker_config_path, dry_run=True)

    cosmetics_sample = _load_sample(output_root / "complex_cosmetics_20s" / "sample.json")
    worker_sample = _load_sample(output_root / "complex_worker_law" / "sample.json")

    cosmetics_analysis = _analyze_cosmetics(cosmetics_sample)
    worker_analysis = _analyze_worker_law(worker_sample)
    result: dict[str, object] = {
        "run_ids": {
            "cosmetics": cosmetics_result.run_id,
            "worker_law": worker_result.run_id,
        },
        "pipeline_status": {
            "cosmetics": cosmetics_result.status,
            "worker_law": worker_result.status,
        },
        "cosmetics": cosmetics_analysis,
        "worker_law": worker_analysis,
        "source_notes": SOURCE_NOTES,
        "limitations": [
            "Synthetic fixture personas are not representative market or labor data.",
            "Scores are deterministic heuristics for scenario comparison, not behavioral prediction.",
            "Real-world analogs ground scenario design; they do not validate the synthetic results.",
        ],
    }

    (output_root / "complex_scenario_analysis.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    (output_root / "complex_scenario_analysis.md").write_text(
        _render_markdown(result),
        encoding="utf-8",
    )
    return result


def _runtime_config(source_config: Path, output_root: Path, generated_config_dir: Path) -> Path:
    config_path = REPO_ROOT / source_config
    payload = yaml.safe_load(config_path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"Config must be a mapping: {config_path}")
    runtime = payload.get("runtime")
    if not isinstance(runtime, dict):
        raise ValueError(f"Config runtime must be a mapping: {config_path}")
    runtime["output_dir"] = str(output_root)
    runtime["overwrite"] = True
    target = generated_config_dir / source_config.name
    target.write_text(
        yaml.safe_dump(payload, allow_unicode=True, sort_keys=False), encoding="utf-8"
    )
    return target


def _load_sample(path: Path) -> list[PersonaRecord]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    records = payload.get("records")
    if not isinstance(records, list):
        raise ValueError(f"Sample records missing from {path}")
    return [PersonaRecord.model_validate(record) for record in records]


def _analyze_cosmetics(records: list[PersonaRecord]) -> dict[str, object]:
    persona_rows: list[dict[str, object]] = []
    variant_totals = {variant.variant_id: 0.0 for variant in CAMPAIGNS}
    age_band_totals: dict[str, dict[str, float]] = {}

    for record in records:
        scores = {
            variant.variant_id: round(_campaign_score(record, variant), 3) for variant in CAMPAIGNS
        }
        best_variant_id = max(scores, key=lambda variant_id: scores[variant_id])
        for variant_id, score in scores.items():
            variant_totals[variant_id] += score
        band = _age_band(record.age)
        age_band_totals.setdefault(band, {variant.variant_id: 0.0 for variant in CAMPAIGNS})
        for variant_id, score in scores.items():
            age_band_totals[band][variant_id] += score
        persona_rows.append(
            {
                "persona_uuid": record.uuid,
                "age": record.age,
                "age_band": band,
                "gender": record.sex,
                "occupation": record.occupation,
                "best_variant": best_variant_id,
                "scores": scores,
                "reason": _cosmetics_reason(record, best_variant_id),
            }
        )

    count = max(len(records), 1)
    ranked_variants = sorted(
        (
            {
                "variant_id": variant.variant_id,
                "name": variant.name,
                "product": variant.product,
                "message": variant.message,
                "song_preference": variant.song_preference,
                "real_world_analog": variant.real_world_analog,
                "average_score": round(variant_totals[variant.variant_id] / count, 3),
                "top_choice_count": sum(
                    1 for row in persona_rows if row["best_variant"] == variant.variant_id
                ),
            }
            for variant in CAMPAIGNS
        ),
        key=lambda item: (
            -cast(float, item["average_score"]),
            cast(str, item["variant_id"]),
        ),
    )

    twenty_rows = [row for row in persona_rows if row["age_band"] == "20s"]
    twenty_top = _top_choice(twenty_rows)
    all_top = str(ranked_variants[0]["variant_id"])
    return {
        "objective": "Pick cosmetic-commercial products and messages for consumers in their 20s.",
        "persona_count": len(records),
        "minimum_personas_required": MIN_PERSONAS_PER_SCENARIO,
        "ranked_variants": ranked_variants,
        "twenties_top_variant": twenty_top or all_top,
        "twenties_persona_count": len(twenty_rows),
        "persona_results": persona_rows,
        "age_band_summary": _summarize_age_bands(age_band_totals, records),
        "derived_result": _cosmetics_conclusion(twenty_top or all_top),
    }


def _analyze_worker_law(records: list[PersonaRecord]) -> dict[str, object]:
    persona_rows: list[dict[str, object]] = []
    for record in records:
        support = round(_law_support_score(record), 3)
        burden = round(_law_burden_score(record), 3)
        net = round(support - burden, 3)
        if net >= 0.25:
            stance = "likes"
        elif net <= -0.1:
            stance = "dislikes"
        else:
            stance = "mixed"
        persona_rows.append(
            {
                "persona_uuid": record.uuid,
                "age": record.age,
                "age_band": _age_band(record.age),
                "gender": record.sex,
                "occupation": record.occupation,
                "support_score": support,
                "burden_score": burden,
                "net_score": net,
                "stance": stance,
                "suffers_short_term": burden >= 0.45,
                "reason": _worker_reason(record, support, burden, stance),
            }
        )

    return {
        "objective": "Identify who suffers short-term burden, who likes/dislikes worker law, by age and occupation.",
        "persona_count": len(records),
        "minimum_personas_required": MIN_PERSONAS_PER_SCENARIO,
        "law_components": [
            {
                "component_id": component.component_id,
                "name": component.name,
                "affected_groups": list(component.affected_groups),
                "real_world_analog": component.real_world_analog,
            }
            for component in WORKER_LAW_COMPONENTS
        ],
        "persona_results": persona_rows,
        "stance_by_age": _group_counts(persona_rows, "age_band", "stance"),
        "stance_by_occupation": _group_counts(persona_rows, "occupation", "stance"),
        "short_term_burden": [row for row in persona_rows if bool(row["suffers_short_term"])],
        "derived_result": _worker_conclusion(persona_rows),
    }


def _campaign_score(record: PersonaRecord, variant: CampaignVariant) -> float:
    text = _persona_text(record)
    score = 0.35
    if 20 <= record.age <= 29:
        score += 0.25
    elif 30 <= record.age <= 39:
        score += 0.12
    elif record.age >= 50:
        score -= 0.08

    if variant.variant_id == "commute_skinfit_poster":
        score += _contains_any(text, ("sns", "마케팅", "쇼핑", "요가", "필라테스", "러닝")) * 0.22
        score += _contains_any(text, ("취업준비생", "대학생", "간호사", "개발자")) * 0.12
    elif variant.variant_id == "idol_glow_challenge":
        score += _contains_any(text, ("sns", "유튜브", "쇼핑", "콘텐츠", "마케팅")) * 0.28
        score -= 0.1 if record.age >= 40 else 0.0
    elif variant.variant_id == "dermatologist_barrier_serum":
        score += _contains_any(text, ("의사", "간호사", "러닝", "요가", "공무원", "교사")) * 0.22
        score += 0.08 if record.age >= 30 else 0.0
    elif variant.variant_id == "refillable_vegan_lip_tint":
        score += _contains_any(text, ("디자이너", "대학생", "전시회", "인테리어", "창업")) * 0.24
        score += 0.08 if record.sex == "여성" else 0.0

    return max(0.0, min(score, 1.0))


def _law_support_score(record: PersonaRecord) -> float:
    text = _persona_text(record)
    score = 0.25
    score += _contains_any(text, ("간호사", "취업준비생", "대학생", "교사", "개발자")) * 0.28
    score += _contains_any(text, ("프리랜서", "디자이너", "자영업자")) * 0.18
    score += _contains_any(text, ("공무원", "변호사")) * 0.12
    score += 0.12 if record.age < 35 else 0.0
    score += 0.06 if record.age >= 50 else 0.0
    return max(0.0, min(score, 1.0))


def _law_burden_score(record: PersonaRecord) -> float:
    text = _persona_text(record)
    score = 0.12
    score += _contains_any(text, ("자영업자", "카페 사장", "마케팅 과장")) * 0.42
    score += _contains_any(text, ("의사", "간호사", "공무원", "변호사")) * 0.2
    score += _contains_any(text, ("소프트웨어 엔지니어", "백엔드 개발자", "연구원")) * 0.12
    score += 0.06 if 40 <= record.age <= 59 else 0.0
    return max(0.0, min(score, 1.0))


def _cosmetics_reason(record: PersonaRecord, variant_id: str) -> str:
    if variant_id == "commute_skinfit_poster":
        return "Routine-first, affordable creator proof fits busy daily media habits."
    if variant_id == "idol_glow_challenge":
        return "Short-form social participation and high-energy music fit sharing behavior."
    if variant_id == "dermatologist_barrier_serum":
        return "Expert proof and low-irritation framing fit trust and health concerns."
    if record.occupation in {"대학생", "UI/UX 디자이너"}:
        return "Swatches, identity styling, and lower-waste cues fit campus/design identity."
    return "Style and mission cues produce stronger interest than pure celebrity gloss."


def _worker_reason(
    record: PersonaRecord,
    support: float,
    burden: float,
    stance: str,
) -> str:
    if burden >= 0.45 and stance != "likes":
        return (
            "Short-term scheduling, staffing, or compliance cost dominates direct worker benefit."
        )
    if support >= 0.55:
        return "Rest, safety, and transparency protections map directly to life-stage or job risk."
    if stance == "mixed":
        return "Accepts worker-protection goal but worries about implementation cost or rigidity."
    return "Sees limited direct benefit and higher administrative or fiscal burden."


def _cosmetics_conclusion(top_variant_id: str) -> str:
    campaign = next(variant for variant in CAMPAIGNS if variant.variant_id == top_variant_id)
    return (
        f"For 20s cosmetics, prioritize {campaign.product}: {campaign.message}. "
        f"Music fit: {campaign.song_preference}. Use 2023 viral mechanics as optional sharing layer, "
        "but keep proof and routine utility central."
    )


def _worker_conclusion(rows: list[dict[str, object]]) -> str:
    burdened = [row for row in rows if bool(row["suffers_short_term"])]
    likes = [row for row in rows if row["stance"] == "likes"]
    dislikes = [row for row in rows if row["stance"] == "dislikes"]
    burden_summary = ", ".join(
        f"{occupation}({count})" for occupation, count in _top_counts(burdened, "occupation", 5)
    )
    return (
        f"Likely supporters: {len(likes)} personas, concentrated in younger workers and worker-facing "
        f"public-service roles. Likely opponents: {len(dislikes)} personas. Short-term sufferers: "
        f"{burden_summary or 'none in synthetic population'} due to staffing, deadline, or compliance pressure."
    )


def _summarize_age_bands(
    age_band_totals: dict[str, dict[str, float]],
    records: list[PersonaRecord],
) -> dict[str, object]:
    counts: dict[str, int] = {}
    for record in records:
        counts[_age_band(record.age)] = counts.get(_age_band(record.age), 0) + 1
    summary: dict[str, object] = {}
    for band, totals in sorted(age_band_totals.items()):
        count = max(counts.get(band, 0), 1)
        averages = {variant_id: round(total / count, 3) for variant_id, total in totals.items()}
        summary[band] = {
            "count": counts.get(band, 0),
            "top_variant": max(averages, key=lambda variant_id: averages[variant_id]),
            "averages": averages,
        }
    return summary


def _top_choice(rows: list[dict[str, object]]) -> str | None:
    counts: dict[str, int] = {}
    for row in rows:
        variant_id = str(row["best_variant"])
        counts[variant_id] = counts.get(variant_id, 0) + 1
    if not counts:
        return None
    return sorted(counts.items(), key=lambda item: (-item[1], item[0]))[0][0]


def _group_counts(
    rows: list[dict[str, object]],
    group_key: str,
    value_key: str,
) -> dict[str, dict[str, int]]:
    grouped: dict[str, dict[str, int]] = {}
    for row in rows:
        group = str(row[group_key])
        value = str(row[value_key])
        grouped.setdefault(group, {})
        grouped[group][value] = grouped[group].get(value, 0) + 1
    return dict(sorted(grouped.items()))


def _top_counts(rows: list[dict[str, object]], key: str, limit: int) -> list[tuple[str, int]]:
    counts: dict[str, int] = {}
    for row in rows:
        value = str(row[key])
        counts[value] = counts.get(value, 0) + 1
    return sorted(counts.items(), key=lambda item: (-item[1], item[0]))[:limit]


def _age_band(age: int) -> str:
    decade = max(0, age // 10 * 10)
    return f"{decade}s"


def _persona_text(record: PersonaRecord) -> str:
    values = [
        record.persona,
        record.professional_persona,
        record.family_persona,
        record.cultural_background,
        record.skills_and_expertise,
        record.hobbies_and_interests,
        record.occupation,
    ]
    return " ".join(value for value in values if value).lower()


def _contains_any(text: str, needles: tuple[str, ...]) -> int:
    return int(any(needle.lower() in text for needle in needles))


def _render_markdown(result: dict[str, object]) -> str:
    cosmetics = _require_mapping(result["cosmetics"])
    worker_law = _require_mapping(result["worker_law"])
    cosmetic_variants = _require_list(cosmetics["ranked_variants"])
    worker_rows = _require_list(worker_law["persona_results"])
    stance_by_age = _require_mapping(worker_law["stance_by_age"])
    source_notes = _require_list(result["source_notes"])

    lines = [
        "# Complex Scenario Analysis",
        "",
        "Synthetic Argus dry-run scenarios comparing product, media, age, occupation, and worker-law response.",
        "",
        f"Minimum personas per scenario: {MIN_PERSONAS_PER_SCENARIO}",
        "",
        "## Cosmetics: 20s Commercial Direction",
        "",
        f"Scenario personas executed: {cosmetics['persona_count']}",
        "",
        f"Derived result: {cosmetics['derived_result']}",
        "",
        "| Rank | Product | Avg Score | Top Choices | Song / Sound | Real-world analog |",
        "|---|---|---:|---:|---|---|",
    ]
    for index, item in enumerate(cosmetic_variants, start=1):
        variant = _require_mapping(item)
        lines.append(
            "| "
            f"{index} | {variant['product']} | {variant['average_score']} | "
            f"{variant['top_choice_count']} | {variant['song_preference']} | "
            f"{variant['real_world_analog']} |"
        )

    lines.extend(
        [
            "",
            "## Worker Law: Who Likes, Dislikes, And Suffers Short-Term",
            "",
            f"Scenario personas executed: {worker_law['persona_count']}",
            "",
            f"Derived result: {worker_law['derived_result']}",
            "",
            "### Stance By Age Band",
            "",
            "| Age band | Likes | Mixed | Dislikes |",
            "|---|---:|---:|---:|",
        ]
    )
    for age_band, counts_value in sorted(stance_by_age.items()):
        counts = _require_mapping(counts_value)
        lines.append(
            f"| {age_band} | {counts.get('likes', 0)} | "
            f"{counts.get('mixed', 0)} | {counts.get('dislikes', 0)} |"
        )

    lines.extend(
        [
            "",
            "### Largest Short-Term Burden Groups",
            "",
            "| Occupation | Count |",
            "|---|---:|",
        ]
    )
    burden_rows = [row for row in worker_rows if bool(_require_mapping(row)["suffers_short_term"])]
    for occupation, count in _top_counts(
        [_require_mapping(row) for row in burden_rows], "occupation", 10
    ):
        lines.append(f"| {occupation} | {count} |")

    lines.extend(
        [
            "",
            "### Example Persona Responses",
            "",
            "| Age | Occupation | Stance | Support | Burden | Short-term sufferer | Reason |",
            "|---:|---|---|---:|---:|---|---|",
        ]
    )
    examples = sorted(worker_rows, key=lambda row: int(_require_mapping(row)["age"]))[:20]
    for item in examples:
        row = _require_mapping(item)
        lines.append(
            "| "
            f"{row['age']} | {row['occupation']} | {row['stance']} | "
            f"{row['support_score']} | {row['burden_score']} | "
            f"{row['suffers_short_term']} | {row['reason']} |"
        )

    lines.extend(["", "## Sources Used As Scenario Analogs", ""])
    for item in source_notes:
        source = _require_mapping(item)
        lines.append(f"- [{source['label']}]({source['url']}): {source['used_for']}")

    lines.extend(
        [
            "",
            "## Limitations",
            "",
            "- Synthetic fixture personas are not representative market or labor data.",
            "- Deterministic heuristic scores support comparison only; they are not predictions.",
            "- Real-world analogs ground scenario design, not result validation.",
            "",
        ]
    )
    return "\n".join(lines)


def _require_mapping(value: object) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise TypeError("Expected mapping")
    return value


def _require_list(value: object) -> list[object]:
    if not isinstance(value, list):
        raise TypeError("Expected list")
    return value


def main() -> None:
    result = run_complex_analysis()
    output_root = DEFAULT_OUTPUT_ROOT
    run_ids = cast(dict[str, str], result["run_ids"])
    print("complex_scenario_analysis: success")
    print(f"cosmetics_run_id: {run_ids['cosmetics']}")
    print(f"worker_law_run_id: {run_ids['worker_law']}")
    print(f"analysis_json: {output_root / 'complex_scenario_analysis.json'}")
    print(f"analysis_report: {output_root / 'complex_scenario_analysis.md'}")


if __name__ == "__main__":
    main()
