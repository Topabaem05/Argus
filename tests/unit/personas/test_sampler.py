"""Unit tests for deterministic persona sampler."""

from __future__ import annotations

import pytest

from korean_social_simulator.config.models import (
    AgeRangeFilter,
    SamplingConfig,
    SamplingFilters,
)
from korean_social_simulator.errors import SamplingError
from korean_social_simulator.models import PersonaRecord, PopulationSample
from korean_social_simulator.personas.sampler import sample_population


def _make_persona(
    uuid: str,
    age: int = 30,
    province: str = "서울특별시",
    occupation: str = "개발자",
    country: str = "South Korea",
) -> PersonaRecord:
    return PersonaRecord(
        uuid=uuid,
        persona=f"테스트 페르소나 {uuid}",
        age=age,
        occupation=occupation,
        district="강남구",
        province=province,
        country=country,
    )


def _make_config(
    sample_size: int = 5,
    seed: int = 42,
    allow_smaller: bool = False,
    filters: SamplingFilters | None = None,
) -> SamplingConfig:
    return SamplingConfig(
        sample_size=sample_size,
        seed=seed,
        allow_smaller_sample=allow_smaller,
        filters=filters or SamplingFilters(),
    )


def test_same_seed_same_sample() -> None:
    """Same seed produces identical UUID order across runs."""
    personas = [_make_persona(f"p-{i:03d}", age=25 + i) for i in range(20)]
    config = _make_config(sample_size=5, seed=42)

    sample1 = sample_population(personas, config)
    sample2 = sample_population(personas, config)

    uuids1 = [r.uuid for r in sample1.records]
    uuids2 = [r.uuid for r in sample2.records]
    assert uuids1 == uuids2


def test_insufficient_rows_raises() -> None:
    """Filter producing fewer rows than requested raises SamplingError."""
    personas = [_make_persona(f"p-{i:03d}") for i in range(5)]
    filters = SamplingFilters(country="North Korea")
    config = _make_config(sample_size=10, filters=filters)

    with pytest.raises(SamplingError, match="0 personas match"):
        sample_population(personas, config)


def test_insufficient_rows_allowed_with_flag() -> None:
    """allow_smaller_sample returns what is available without error."""
    personas = [_make_persona(f"p-{i:03d}") for i in range(5)]
    filters = SamplingFilters(country="North Korea")
    config = _make_config(sample_size=10, filters=filters, allow_smaller=True)

    sample = sample_population(personas, config)
    assert len(sample.records) == 0


def test_age_range_filter() -> None:
    """Age range filter selects only matching ages."""
    personas = [_make_persona(f"p-{i:03d}", age=20 + i) for i in range(30)]
    filters = SamplingFilters(age_range=AgeRangeFilter(min=30, max=39))
    config = _make_config(sample_size=5, seed=99, filters=filters)

    sample = sample_population(personas, config)
    assert len(sample.records) == 5
    for record in sample.records:
        assert 30 <= record.age <= 39


def test_province_filter() -> None:
    """Province filter selects only matching province."""
    personas = [
        _make_persona("p-seoul-01", province="서울특별시"),
        _make_persona("p-busan-01", province="부산광역시"),
        _make_persona("p-seoul-02", province="서울특별시"),
        _make_persona("p-busan-02", province="부산광역시"),
        _make_persona("p-seoul-03", province="서울특별시"),
    ]
    filters = SamplingFilters(province="서울특별시")
    config = _make_config(sample_size=3, seed=123, filters=filters)

    sample = sample_population(personas, config)
    assert len(sample.records) == 3
    for record in sample.records:
        assert record.province == "서울특별시"


def test_sample_population_metadata() -> None:
    """Sample includes seed, filters, andsource metadata."""
    personas = [_make_persona(f"p-{i:03d}") for i in range(10)]
    filters = SamplingFilters(country="South Korea")
    config = _make_config(sample_size=3, seed=77, filters=filters)

    sample = sample_population(personas, config)
    assert isinstance(sample, PopulationSample)
    assert sample.seed == 77
    assert "country" in sample.filters


def test_empty_persona_list_raises() -> None:
    """Empty persona list raises SamplingError."""
    config = _make_config(sample_size=5)
    with pytest.raises(SamplingError, match="No persona records"):
        sample_population([], config)


def test_complex_filters() -> None:
    """Multiple filters work together."""
    personas = [
        _make_persona("p-001", age=25, province="서울특별시", occupation="개발자"),
        _make_persona("p-002", age=35, province="부산광역시", occupation="개발자"),
        _make_persona("p-003", age=30, province="서울특별시", occupation="디자이너"),
        _make_persona("p-004", age=28, province="서울특별시", occupation="개발자"),
        _make_persona("p-005", age=40, province="서울특별시", occupation="개발자"),
    ]
    filters = SamplingFilters(
        province="서울특별시",
        occupation="개발자",
        age_range=AgeRangeFilter(min=20, max=35),
    )
    config = _make_config(sample_size=2, seed=5, filters=filters)

    sample = sample_population(personas, config)
    assert len(sample.records) == 2
    for record in sample.records:
        assert record.province == "서울특별시"
        assert record.occupation == "개발자"
        assert 20 <= record.age <= 35
