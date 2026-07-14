#!/usr/bin/env python3
"""Compile/rebuild the Unity project and execute AI-company Play Mode tests."""

from __future__ import annotations

import argparse
import json
import subprocess
import xml.etree.ElementTree as ET
from datetime import UTC, datetime
from pathlib import Path

from korean_social_simulator.qa.gates import GateReport, write_gate_report


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--unity", required=True, help="Unity Editor executable")
    parser.add_argument(
        "--project-path",
        type=Path,
        default=Path("unity/EmbodiedDebate"),
    )
    parser.add_argument("--report-dir", type=Path, default=Path("reports/qa/unity"))
    parser.add_argument(
        "--gate-report",
        type=Path,
        default=Path("reports/qa/unity_compile_playmode.json"),
    )
    return parser.parse_args()


def _run(command: list[str], log_path: Path) -> subprocess.CompletedProcess[str]:
    completed = subprocess.run(command, capture_output=True, text=True, check=False)
    log_path.write_text(completed.stdout + "\n" + completed.stderr, encoding="utf-8")
    return completed


def main() -> int:
    args = parse_args()
    started = datetime.now(UTC).isoformat()
    args.report_dir.mkdir(parents=True, exist_ok=True)
    project = args.project_path.resolve()
    build_log = args.report_dir / "unity-build-scene.log"
    play_log = args.report_dir / "unity-playmode.log"
    results_xml = args.report_dir / "playmode-results.xml"

    build = _run(
        [
            args.unity,
            "-batchmode",
            "-nographics",
            "-quit",
            "-projectPath",
            str(project),
            "-executeMethod",
            "ArgusUnity.Editor.BuildGameScene.Build",
            "-logFile",
            "-",
        ],
        build_log,
    )
    play = _run(
        [
            args.unity,
            "-batchmode",
            "-nographics",
            "-projectPath",
            str(project),
            "-runTests",
            "-testPlatform",
            "PlayMode",
            "-testResults",
            str(results_xml.resolve()),
            "-logFile",
            "-",
            "-quit",
        ],
        play_log,
    )

    total = passed = failed = skipped = 0
    xml_parse_ok = False
    if results_xml.is_file():
        try:
            root = ET.parse(results_xml).getroot()
            total = int(root.attrib.get("total", root.attrib.get("testcasecount", "0")))
            passed = int(root.attrib.get("passed", "0"))
            failed = int(root.attrib.get("failed", "0"))
            skipped = int(root.attrib.get("skipped", root.attrib.get("inconclusive", "0")))
            xml_parse_ok = True
        except (ET.ParseError, ValueError):
            xml_parse_ok = False

    compiler_errors = sum(
        text.lower().count("error cs")
        for text in (
            build_log.read_text(encoding="utf-8", errors="replace"),
            play_log.read_text(encoding="utf-8", errors="replace"),
        )
    )
    criteria = {
        "scene_build_exit_zero": build.returncode == 0,
        "playmode_exit_zero": play.returncode == 0,
        "test_results_parsed": xml_parse_ok,
        "playmode_tests_executed": total >= 3,
        "zero_test_failures": failed == 0,
        "zero_compiler_errors": compiler_errors == 0,
    }
    status = "passed" if all(criteria.values()) else "failed"
    report = GateReport(
        gate_id="unity_compile_playmode",
        status=status,
        metrics={
            "build_return_code": build.returncode,
            "playmode_return_code": play.returncode,
            "tests_total": total,
            "tests_passed": passed,
            "tests_failed": failed,
            "tests_skipped": skipped,
            "compiler_error_markers": compiler_errors,
        },
        criteria=criteria,
        evidence=[str(build_log), str(play_log), str(results_xml)],
        started_at=started,
        finished_at=datetime.now(UTC).isoformat(),
    )
    write_gate_report(args.gate_report, report)
    print(json.dumps(report.model_dump(mode="json"), ensure_ascii=False, indent=2))
    return 0 if status == "passed" else 2


if __name__ == "__main__":
    raise SystemExit(main())
