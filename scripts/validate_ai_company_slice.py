"""Static repository validation for the AI company Unity/Python vertical slice.

This does not replace opening the project in Unity. It catches missing files, package drift,
Unity metadata mistakes and a regression where commands execute before the SLM decision.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UNITY_ROOT = ROOT / "unity" / "EmbodiedDebate"
GAME_SCRIPTS = UNITY_ROOT / "Assets" / "Project" / "Scripts" / "Game"

REQUIRED_PATHS = (
    ROOT / "src" / "korean_social_simulator" / "ai" / "model_profiles.py",
    ROOT / "src" / "korean_social_simulator" / "ai" / "slm_adapter.py",
    ROOT / "src" / "korean_social_simulator" / "game" / "runner.py",
    ROOT / "tests" / "unit" / "ai" / "test_slm_adapter.py",
    UNITY_ROOT / "Assets" / "Editor" / "BuildGameScene.cs",
    UNITY_ROOT / "Assets" / "Editor" / "CompanyGameVerticalSliceValidator.cs",
    GAME_SCRIPTS / "CompanyMiniBotActor.cs",
    GAME_SCRIPTS / "CompanyMiniBotMotionDriver.cs",
    GAME_SCRIPTS / "CompanyPlayerMiniBotController.cs",
    GAME_SCRIPTS / "CompanyEmployeeAgentController.cs",
    GAME_SCRIPTS / "CompanyCameraController.cs",
    GAME_SCRIPTS / "CompanyGameCoordinator.cs",
    GAME_SCRIPTS / "CompanyGameHud.cs",
    GAME_SCRIPTS / "CompanyMiniBotIndicator.cs",
    UNITY_ROOT / "Assets" / "Project" / "Models" / "GameAssets" / "office" / "Floor_01.glb",
    UNITY_ROOT / "Assets" / "Project" / "Models" / "GameAssets" / "city" / "Terrain01_Art.glb",
    UNITY_ROOT / "Assets" / "Project" / "Robots" / "Prefabs" / "FallbackRobot.prefab",
)

REQUIRED_PACKAGES = (
    "com.unity.cloud.gltfast",
    "com.unity.animation.rigging",
    "com.unity.nuget.newtonsoft-json",
    "com.unity.render-pipelines.universal",
    "com.unity.test-framework",
)

GUID_PATTERN = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)


def main() -> int:
    errors: list[str] = []
    validate_paths(errors)
    validate_manifest(errors)
    validate_unity_metadata(errors)
    validate_scene_builder(errors)
    validate_slm_authority(errors)

    if errors:
        print("AI company vertical slice validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("AI company vertical slice static validation passed.")
    return 0


def validate_paths(errors: list[str]) -> None:
    for path in REQUIRED_PATHS:
        if not path.exists():
            errors.append(f"missing required path: {path.relative_to(ROOT)}")


def validate_manifest(errors: list[str]) -> None:
    manifest_path = UNITY_ROOT / "Packages" / "manifest.json"
    if not manifest_path.exists():
        errors.append("missing Unity Packages/manifest.json")
        return

    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        errors.append(f"invalid Unity manifest: {exc}")
        return

    dependencies = manifest.get("dependencies", {})
    for package in REQUIRED_PACKAGES:
        if package not in dependencies:
            errors.append(f"Unity package missing: {package}")


def validate_unity_metadata(errors: list[str]) -> None:
    source_files = [
        UNITY_ROOT / "Assets" / "Editor" / "CompanyGameVerticalSliceValidator.cs",
        *sorted(GAME_SCRIPTS.glob("*.cs")),
    ]
    seen_guids: dict[str, Path] = {}
    for source in source_files:
        meta = source.with_suffix(source.suffix + ".meta")
        if not meta.exists():
            errors.append(f"Unity metadata missing: {meta.relative_to(ROOT)}")
            continue
        content = meta.read_text(encoding="utf-8")
        match = GUID_PATTERN.search(content)
        if match is None:
            errors.append(f"Unity metadata has no valid GUID: {meta.relative_to(ROOT)}")
            continue
        guid = match.group(1)
        if guid in seen_guids:
            errors.append(
                "duplicate Unity GUID: "
                f"{meta.relative_to(ROOT)} and {seen_guids[guid].relative_to(ROOT)}"
            )
        seen_guids[guid] = meta


def validate_scene_builder(errors: list[str]) -> None:
    path = UNITY_ROOT / "Assets" / "Editor" / "BuildGameScene.cs"
    if not path.exists():
        return
    content = path.read_text(encoding="utf-8")
    required_markers = (
        "CompanyMiniBotActor",
        "CompanyPlayerMiniBotController",
        "CompanyEmployeeAgentController",
        "CompanyGameCoordinator",
        "CompanyGameHud",
        '"emp-{globalEmployeeIndex:000}"',
        '"player-{playerId}"',
    )
    for marker in required_markers:
        if marker not in content:
            errors.append(f"BuildGameScene missing marker: {marker}")
    for company_name in ("AlphaCorp", "BetaTech", "GammaLogic", "DeltaFoods"):
        if company_name not in content:
            errors.append(f"BuildGameScene missing company: {company_name}")


def validate_slm_authority(errors: list[str]) -> None:
    adapter_path = ROOT / "src" / "korean_social_simulator" / "ai" / "slm_adapter.py"
    runner_path = ROOT / "src" / "korean_social_simulator" / "game" / "runner.py"
    if not adapter_path.exists() or not runner_path.exists():
        return

    adapter = adapter_path.read_text(encoding="utf-8")
    for marker in ("hashlib.sha256", '"llamacpp"', "fallback_on_error", "_decode_first_json_object"):
        if marker not in adapter:
            errors.append(f"SLM adapter missing safety marker: {marker}")

    runner = runner_path.read_text(encoding="utf-8")
    decision_index = runner.find("response = slm.generate")
    mutation_index = runner.find("result = cmd_sys.execute")
    if decision_index < 0 or mutation_index < 0:
        errors.append("game runner is missing the SLM decision or command mutation")
    elif decision_index > mutation_index:
        errors.append("game runner mutates command state before the SLM decision")
    if 'if response.action == "refuse"' not in runner:
        errors.append("game runner does not block mutation on SLM refusal")


if __name__ == "__main__":
    raise SystemExit(main())
