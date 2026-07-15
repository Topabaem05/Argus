"""Machine-readable QA reports and the stage-unlock decision.

Hardware-dependent checks write one JSON report per gate. The aggregate gate never infers
success from a workflow job name: every required report must exist, declare ``passed``, and
contain only true criterion values. Missing, skipped, or blocked evidence keeps the next stage
locked.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

GateStatus = Literal["passed", "failed", "blocked", "skipped"]
MetricValue = bool | int | float | str | None


class GateReport(BaseModel):
    """Result emitted by one executable QA check."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str = "1.0"
    gate_id: str = Field(min_length=1)
    status: GateStatus
    metrics: dict[str, MetricValue] = Field(default_factory=dict)
    criteria: dict[str, bool] = Field(default_factory=dict)
    evidence: list[str] = Field(default_factory=list)
    notes: list[str] = Field(default_factory=list)
    started_at: str | None = None
    finished_at: str | None = None

    @model_validator(mode="after")
    def _passed_report_requires_true_criteria(self) -> GateReport:
        if self.status == "passed" and (not self.criteria or not all(self.criteria.values())):
            raise ValueError("A passed gate must contain at least one criterion and all must be true")
        return self


class GateRequirement(BaseModel):
    """One required QA report in the stage policy."""

    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1)
    report_file: str = Field(min_length=1)
    description: str = ""


class NextStage(BaseModel):
    """Work that becomes eligible only after every required gate passes."""

    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1)
    title: str = Field(min_length=1)
    tasks: list[str] = Field(min_length=1)


class GatePolicy(BaseModel):
    """QA policy checked by CI and self-hosted runners."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str = "1.0"
    stage_id: str = Field(min_length=1)
    required_gates: list[GateRequirement] = Field(min_length=1)
    next_stage: NextStage

    @model_validator(mode="after")
    def _gate_ids_are_unique(self) -> GatePolicy:
        ids = [gate.id for gate in self.required_gates]
        files = [gate.report_file for gate in self.required_gates]
        if len(ids) != len(set(ids)):
            raise ValueError("Gate ids must be unique")
        if len(files) != len(set(files)):
            raise ValueError("Gate report files must be unique")
        return self


class StageGateSummary(BaseModel):
    """Aggregate evidence used to unlock or block the next implementation stage."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str = "1.0"
    stage_id: str
    passed: bool
    next_stage_unlocked: bool
    required_gate_count: int
    passed_gate_count: int
    reports: dict[str, GateReport]
    missing_reports: list[str]
    blocked_reasons: list[str]
    next_stage: NextStage


def load_gate_policy(path: str | Path) -> GatePolicy:
    """Load and validate a QA stage policy."""

    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    return GatePolicy.model_validate(payload)


def load_gate_report(path: str | Path) -> GateReport:
    """Load and validate one gate result."""

    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    return GateReport.model_validate(payload)


def write_gate_report(path: str | Path, report: GateReport) -> None:
    """Persist a normalized gate result."""

    destination = Path(path)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(
        json.dumps(report.model_dump(mode="json"), ensure_ascii=False, indent=2, sort_keys=True)
        + "\n",
        encoding="utf-8",
    )


def aggregate_stage_gate(
    policy: GatePolicy,
    report_dir: str | Path,
) -> StageGateSummary:
    """Aggregate all required reports and decide whether the next stage is unlocked."""

    root = Path(report_dir)
    reports: dict[str, GateReport] = {}
    missing: list[str] = []
    blocked_reasons: list[str] = []

    for requirement in policy.required_gates:
        report_path = root / requirement.report_file
        if not report_path.is_file():
            missing.append(requirement.id)
            blocked_reasons.append(f"{requirement.id}: missing report {requirement.report_file}")
            continue
        try:
            report = load_gate_report(report_path)
        except (OSError, json.JSONDecodeError, ValueError) as exc:
            blocked_reasons.append(f"{requirement.id}: invalid report: {exc}")
            continue
        if report.gate_id != requirement.id:
            blocked_reasons.append(
                f"{requirement.id}: report gate_id mismatch ({report.gate_id!r})"
            )
            continue
        reports[requirement.id] = report
        if report.status != "passed":
            blocked_reasons.append(f"{requirement.id}: status={report.status}")
        elif not report.criteria or not all(report.criteria.values()):
            blocked_reasons.append(f"{requirement.id}: one or more criteria failed")

    passed_count = sum(
        1
        for report in reports.values()
        if report.status == "passed" and report.criteria and all(report.criteria.values())
    )
    required_count = len(policy.required_gates)
    passed = passed_count == required_count and not missing and not blocked_reasons
    return StageGateSummary(
        stage_id=policy.stage_id,
        passed=passed,
        next_stage_unlocked=passed,
        required_gate_count=required_count,
        passed_gate_count=passed_count,
        reports=reports,
        missing_reports=missing,
        blocked_reasons=blocked_reasons,
        next_stage=policy.next_stage,
    )
