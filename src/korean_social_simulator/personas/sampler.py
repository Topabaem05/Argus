"""Deterministic persona sampler for population selection."""

from __future__ import annotations

import random as rand_mod

from korean_social_simulator.config.models import SamplingConfig, SamplingFilters
from korean_social_simulator.errors import SamplingError
from korean_social_simulator.models import PersonaRecord, PopulationSample


def _matches_filters(record: PersonaRecord, filters: SamplingFilters) -> bool:
    if filters.country is not None and record.country != filters.country:
        return False
    if filters.province is not None and record.province != filters.province:
        return False
    if filters.district is not None and record.district != filters.district:
        return False
    if filters.occupation is not None and record.occupation != filters.occupation:
        return False
    if filters.age_range is not None:
        age_range = filters.age_range
        if record.age < age_range.min or record.age > age_range.max:
            return False
    return True


def sample_population(
    personas: list[PersonaRecord],
    config: SamplingConfig,
) -> PopulationSample:
    """Apply filters and deterministic sampling to a persona collection.

    Sampling is deterministic when a seed is provided.

    Raises:
        SamplingError: If filters produce fewer rows than requested and
            `allow_smaller_sample` is False.
    """
    if not personas:
        raise SamplingError("No persona records provided for sampling.")

    filters = config.filters
    filtered = [p for p in personas if _matches_filters(p, filters)]
    requested = config.sample_size

    if len(filtered) < requested and not config.allow_smaller_sample:
        raise SamplingError(
            f"Only {len(filtered)} personas match filters, but {requested} were requested. "
            "Set allow_smaller_sample=true to accept a smaller sample."
        )

    rng = rand_mod.Random(config.seed)
    indices = list(range(len(filtered)))
    rng.shuffle(indices)

    selected_count = min(requested, len(filtered))
    selected_indices = sorted(indices[:selected_count])
    selected_records = [filtered[i] for i in selected_indices]

    filters_dict: dict[str, object] = {}
    if filters.country is not None:
        filters_dict["country"] = filters.country
    if filters.province is not None:
        filters_dict["province"] = filters.province
    if filters.district is not None:
        filters_dict["district"] = filters.district
    if filters.occupation is not None:
        filters_dict["occupation"] = filters.occupation
    if filters.age_range is not None:
        filters_dict["age_range"] = {
            "min": filters.age_range.min,
            "max": filters.age_range.max,
        }

    return PopulationSample(
        sample_id=f"sample_{config.seed}",
        seed=config.seed,
        filters=filters_dict,
        records=selected_records,
        source="sampler",
    )
