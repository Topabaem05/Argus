# Procedural Walk + Minimalist Shader/Lighting

## TL;DR
> **Summary**: Add OpenAI Hide-and-Seek style flat matte URP visuals (SSAO, zero-smoothness materials, bright ambient) and a procedural leg-walking system where the character's head/gaze stays fixed while legs stride direction-aware via IK.
> **Deliverables**: SSAO renderer feature, matte material configurator, ambient lighting setup, ProceduralLegController (raycast + stride + parabolic step), HeadFixConstraint (fixed-yaw head), editor builder to wire them, EditMode + PlayMode tests, QA screenshots.
> **Effort**: Medium
> **Parallel**: YES - 2 waves
> **Critical Path**: Task 1 (SSAO) -> Task 5 (integration) | Task 2 (materials) -> Task 5 | Task 3 (leg controller) + Task 4 (head fix) -> Task 5

## Context
### Original Request
User wants Unity character walking motion matching OpenAI Multi-Agent Hide and Seek research video: (1) minimalist flat matte shader/lighting with SSAO depth, (2) head fixed forward while legs procedurally walk direction-aware (방식 B: procedural animation + IK). User provided detailed algorithm spec covering raycast ground detection, stride-length triggers, parabolic foot interpolation via Lerp + sine wave, and dual-foot synchronization.

### Interview Summary
No interview round needed — user's spec is decision-complete for approach (방식 B chosen explicitly). Three open questions resolved with defaults:
- Q1 "head fixed" -> world-space fixed yaw (head maintains initial forward direction regardless of movement direction). Default; user can override to target-tracking later.
- Q2 "which rig" -> existing Mixamo humanoid rig (Idle.fbx). The production rig already has leg bones; procedural IK layers on top. Building a separate leg-only rig would duplicate the character system.
- Q3 "replace or layer blend tree" -> layer underneath. Existing walk/run blend tree stays for body bob/arm swing; procedural IK overrides foot positions only. This preserves existing locomotion behavior.

### Metis Review (gaps addressed)
- **Contradiction**: User said "capsule agents" favor 방식 B, but production rig is humanoid (Idle.fbx), not capsule. Resolution: humanoid rig has legs -> procedural IK applies directly. Fallback capsule (CuteRobotPrefabInitializer) has no legs -> out of scope; if used, no procedural legs render (graceful no-op).
- **Missing constraint**: animation-rigging package not in manifest.json -> Task 0 adds it.
- **Execution risk**: SSAO renderer feature asset cannot be created via pure C# in EditMode tests reliably (requires AssetDatabase serialization). Resolution: UrpProjectConfigurator extended to add SSAO programmatically + EditMode test verifies feature count, PlayMode QA verifies visual via screenshot.
- **Scope creep**: User did not ask to change bridge protocol or Python sim. Guardrail: no edits outside unity/EmbodiedDebate/Assets/.
- **Missing acceptance criteria**: Every task now has agent-executable acceptance + QA scenario with concrete inputs.

## Work Objectives
### Core Objective
Character walks with head fixed forward and legs procedurally striding direction-aware, rendered in flat matte minimalist URP style with SSAO depth, matching OpenAI Hide-and-Seek video aesthetic.

### Deliverables
1. URP SSAO renderer feature added to ArgusUniversalRenderer
2. Matte material configurator (Smoothness=0, Metallic=0) applied to character + environment
3. Bright ambient lighting configuration
4. ProceduralLegController MonoBehaviour (raycast + stride trigger + parabolic step + dual-foot sync)
5. HeadFixConstraint MonoBehaviour (fixed-yaw head orientation)
6. Editor builder to wire components onto existing rig prefab/scene
7. EditMode tests for leg math + head fix logic
8. PlayMode test for procedural walk integration
9. QA screenshots proving visual match

### Definition of Done (verifiable conditions with commands)
- [ ] `ArgusUniversalRenderer.asset` has `m_RendererFeatures` with SSAO entry (verify: `rg "m_RendererFeatures" ArgusUniversalRenderer.asset` shows non-empty)
- [ ] Character materials have `_Smoothness` = 0 and `_Metallic` = 0 (verify: EditMode test asserts material properties)
- [ ] ProceduralLegController moves feet in parabolic arc when stride distance exceeded (verify: EditMode test)
- [ ] HeadFixConstraint keeps head yaw within 1 degree of initial yaw during movement (verify: EditMode test)
- [ ] PlayMode: character walks across room, head stays forward, feet alternate (verify: PlayMode test + screenshot)
- [ ] All existing EditMode + PlayMode tests still pass (verify: Unity Test Runner exit 0)

### Must Have
- Procedural leg IK using raycast + stride + parabolic interpolation (방식 B per user spec)
- Head yaw fixed to initial forward direction during locomotion
- SSAO renderer feature on URP forward renderer
- Matte materials (Smoothness=0, Metallic=0)
- Bright ambient light filling shadow areas
- Tests proving each behavior

### Must NOT Have (guardrails, scope boundaries)
- No new character models or Mixamo imports
- No changes to Python simulation, bridge protocol, or bridge_schema
- No replacement of existing blend tree (layer on top, do not destroy)
- No edits outside `unity/EmbodiedDebate/Assets/`
- No deletion of existing tests
- No suppression of type errors or lint

## Verification Strategy
> ZERO HUMAN INTERVENTION - all verification is agent-executed.
- Test decision: TDD (RED-GREEN-REFACTOR) using Unity Test Framework (NUnit) EditMode + PlayMode
- QA policy: Every task has agent-executed scenarios. Visual tasks use PlayMode screenshot capture (existing SmokeScreenshot.cs pattern).
- Evidence: `evidence/task-{N}-{slug}.{ext}`

## Execution Strategy
### Parallel Execution Waves
> Wave 1: independent foundation tasks (package, SSAO, materials, ambient, leg math, head math)
> Wave 2: integration + builder + full QA

Wave 1: Tasks 0-4 (independent: package add, SSAO, materials, ambient, leg controller, head controller)
Wave 2: Task 5 (integration builder + scene wiring + PlayMode + screenshots)

### Dependency Matrix (full, all tasks)

| Task | Depends On | Blocks |
|------|-----------|--------|
| 0. Add animation-rigging package | none | 3, 4, 5 |
| 1. SSAO renderer feature | none | 5 |
| 2. Matte material configurator | none | 5 |
| 3. ProceduralLegController | 0 | 5 |
| 4. HeadFixConstraint | 0 | 5 |
| 5. Integration builder + QA | 0,1,2,3,4 | F1-F4 |

## TODOs

- [ ] 0. Add animation-rigging package to manifest.json

  **What to do**: Add `"com.unity.animation.rigging": "1.2.1"` to `unity/EmbodiedDebate/Packages/manifest.json` dependencies. This enables Two-Bone IK Constraint and Multi-Aim Constraint components needed for procedural legs and head fix. Verify the package resolves (it's a built-in Unity package, no registry URL needed beyond version).

  **Must NOT do**: Do not add other packages. Do not modify existing dependency versions. Do not touch Packages/packages-lock.json manually (Unity regenerates it).

  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: 3, 4, 5 | Blocked By: none

  **References** (executor has NO interview context - be exhaustive):
  - File: `unity/EmbodiedDebate/Packages/manifest.json` - current package list, add entry in alphabetical order within dependencies
  - Pattern: existing entries like `"com.unity.render-pipelines.universal": "14.0.7"` - follow same format
  - External: Unity animation.rigging package docs - version 1.2.1 is stable for Unity 2022.3 (check ProjectVersion.txt for exact Unity version)

  **Acceptance Criteria** (agent-executable only):
  - [ ] `manifest.json` contains `"com.unity.animation.rigging"` key
  - [ ] `rg "com.unity.animation.rigging" unity/EmbodiedDebate/Packages/manifest.json` returns the line
  - [ ] No other dependency lines changed (git diff shows only the one added line)

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: Package entry present and valid JSON
    Tool: bash
    Steps: `python3 -c "import json; d=json.load(open('unity/EmbodiedDebate/Packages/manifest.json')); assert 'com.unity.animation.rigging' in d['dependencies']"`
    Expected: exits 0 (valid JSON, key present)
    Evidence: evidence/task-0-rigging-package.txt

  Scenario: No existing dependencies modified
    Tool: bash
    Steps: `git diff unity/EmbodiedDebate/Packages/manifest.json | grep "^[-+]" | grep -v "^+++" | grep -v "^---" | wc -l` should equal 2 (one `-` context marker not counted, or exactly 1 added line)
    Expected: diff shows only added lines, no removed lines
    Evidence: evidence/task-0-rigging-diff.txt
  ```

  **Commit**: YES | Message: `build(unity): add animation-rigging package for procedural IK` | Files: `unity/EmbodiedDebate/Packages/manifest.json`

- [ ] 1. Add SSAO renderer feature to URP forward renderer

  **What to do**: Extend `UrpProjectConfigurator.cs` (`unity/EmbodiedDebate/Assets/Editor/UrpProjectConfigurator.cs`) to add a `ScreenSpaceAmbientOcclusion` (or `UniversalRendererData`'s `m_RendererFeatures` entry with `ScreenSpaceAmbientOcclusion` type) to `ArgusUniversalRenderer.asset`. Add a method `EnsureSsaoRendererFeature()` that: (1) loads `ArgusUniversalRenderer.asset`, (2) checks if an SSAO feature already exists in `m_RendererFeatures`, (3) if not, creates a `ScriptableObject` of type `UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion` (the SSAO renderer feature), configures it (Radius ~0.35, Intensity ~0.8, Radius ~0.35), appends it to `m_RendererFeatures` list via SerializedObject, and saves the asset. Call this from the existing `Configure()` method. Run `Argus/Configure URP Project` menu item to apply.

  **Must NOT do**: Do not replace the existing renderer asset. Do not change shadow or pipeline settings. Do not use deprecated `ForwardRendererData` if `UniversalRendererData` is available.

  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: 5 | Blocked By: none

  **References** (executor has NO interview context):
  - File: `unity/EmbodiedDebate/Assets/Editor/UrpProjectConfigurator.cs` - existing configurator, add method alongside `ConfigurePipelineAsset`
  - File: `unity/EmbodiedDebate/Assets/Project/Settings/ArgusUniversalRenderer.asset` - target asset, currently `m_RendererFeatures: []` (empty)
  - Pattern: `AttachRendererData` method in same file shows how to manipulate SerializedObject on pipeline assets
  - External: URP SSAO docs - `ScreenSpaceAmbientOcclusion` is in `UnityEngine.Rendering.Universal` namespace, added as RendererFeature
  - Type resolution: use `ResolveType` / `ResolveFirstType` pattern already in the file to handle assembly-qualified names

  **Acceptance Criteria** (agent-executable only):
  - [ ] `UrpProjectConfigurator.cs` has an `EnsureSsaoRendererFeature` method
  - [ ] After running configurator, `ArgusUniversalRenderer.asset` has non-empty `m_RendererFeatures`
  - [ ] EditMode test: loading the renderer asset and checking `m_RendererFeatures` array size > 0

  **QA Scenarios** (MANDATORY):
  ```
  Scenario: SSAO feature added to renderer asset
    Tool: bash
    Steps: After running configurator via menu or test, `rg "m_RendererFeatures" unity/EmbodiedDebate/Assets/Project/Settings/ArgusUniversalRenderer.asset` should show a populated list (not `[]`)
    Expected: `m_RendererFeatures:` followed by list entries, not `[]`
    Evidence: evidence/task-1-ssao-asset.txt

  Scenario: Configurator is idempotent
    Tool: bash (Unity EditMode test)
    Steps: Run `EnsureSsaoRendererFeature()` twice in EditMode test, assert feature count stays at 1
    Expected: count == 1 after second call (no duplicate features)
    Evidence: evidence/task-1-ssao-idempotent.txt
  ```

  **Commit**: YES | Message: `feat(unity): add SSAO renderer feature to URP for minimalist depth` | Files: `unity/EmbodiedDebate/Assets/Editor/UrpProjectConfigurator.cs`, `unity/EmbodiedDebate/Assets/Project/Settings/ArgusUniversalRenderer.asset`

- [ ] 2. Create matte material configurator (Smoothness=0, Metallic=0)

  **What to do**: Extend `UrpMaterialFactory.cs` (`unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/UrpMaterialFactory.cs`) with: (1) a `ConfigureMatte(Material)` method that sets `_Smoothness` (or `_Glossiness`/`_SmoothnessTextureChannel` fallback) to 0 and `_Metallic` to 0, disables `_SPECULARHIGHLIGHTS_OFF` keyword off (so no specular), and sets `_SpecColor` to black if present; (2) update `ApplyLit` to call `ConfigureMatte` after setting color. Also create an editor menu item `Argus/Apply Matte Materials to Scene` that iterates all renderers in the active scene and applies `ConfigureMatte` to their materials. Add an ambient color / gradient configuration to `UrpProjectConfigurator` or a new `LightingConfigurator.cs`: set `RenderSettings.ambientMode` to `AmbientMode.Flat` or `AmbientMode.Trilight`, set `RenderSettings.ambientLight` to a bright cool gray (e.g. `new Color(0.82f, 0.85f, 0.9f)`), and set `RenderSettings.ambientIntensity` to ~1.2.

  **Must NOT do**: Do not change material colors (only smoothness/metallic). Do not remove the transparent material path. Do not modify ThirdParty materials.

  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: 5 | Blocked By: none

  **References** (executor has NO interview context):
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/UrpMaterialFactory.cs` - existing factory, `ApplyLit` method at line ~60
  - File: `unity/EmbodiedDebate/Assets/Editor/UrpProjectConfigurator.cs` - pattern for editor menu items (`[MenuItem("Argus/...")]`)
  - Pattern: `ApplyColor` method shows how to handle `_BaseColor` vs `_Color` property fallbacks
  - URP/Lit shader properties: `_Smoothness`, `_Metallic`, `_SpecGlossMap`, `_SpecColor`

  **Acceptance Criteria** (agent-executable only):
  - [ ] `UrpMaterialFactory.ConfigureMatte` exists and sets `_Smoothness`=0, `_Metallic`=0
  - [ ] `ApplyLit` calls `ConfigureMatte`
  - [ ] EditMode test: `CreateLit(Color.red)` produces material with `_Smoothness` == 0f and `_Metallic` == 0f
  - [ ] Lighting configurator sets `RenderSettings.ambientLight` to bright tone

  **QA Scenarios** (MANDATORY):
  ```
  Scenario: Matte material properties
    Tool: bash (Unity EditMode test)
    Steps: In EditMode test, call `UrpMaterialFactory.CreateLit(Color.red)`, assert `material.GetFloat("_Smoothness") == 0f` and `material.GetFloat("_Metallic") == 0f`
    Expected: both assertions pass
    Evidence: evidence/task-2-matte-material.txt

  Scenario: Existing transparent path still works
    Tool: bash (Unity EditMode test)
    Steps: Call `UrpMaterialFactory.CreateTransparent(Color.blue)`, assert material has `_Surface`=1 and renderQueue=Transparent
    Expected: transparent properties unchanged (matte only affects lit path)
    Evidence: evidence/task-2-transparent-regression.txt
  ```

  **Commit**: YES | Message: `feat(unity): add matte material + bright ambient lighting configurator` | Files: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/UrpMaterialFactory.cs`, `unity/EmbodiedDebate/Assets/Editor/UrpProjectConfigurator.cs` (or new `LightingConfigurator.cs`)

- [ ] 3. Implement ProceduralLegController (raycast + stride + parabolic step + dual-foot sync)

  **What to do**: Create `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/ProceduralLegController.cs` implementing the user's 방식 B algorithm:
  1. **Fields**: `leftFoot` (Transform), `rightFoot` (Transform), `footRaycastOrigin` (Transform, usually pelvis/hip), `strideLength` (float, default ~0.6m), `stepHeight` (float, default ~0.12m), `stepSpeed` (float, default ~4.0 steps/sec), `groundLayer` (LayerMask), `maxStepDistance` (float, default ~1.2m), `raycastDownDistance` (float, default ~2.0m). Foot IK target Transforms: `leftFootTarget`, `rightFootTarget`.
  2. **State machine**: each foot has `FootState { Planted, Stepping }`. Track `leftFootState`, `rightFootState`, `leftStepProgress` (0-1), `rightStepProgress` (0-1), `leftStepStart`, `leftStepEnd` (Vector3), same for right.
  3. **Raycast ground detection** (in `FixedUpdate` or `Update`): from `footRaycastOrigin.position` cast ray down `raycastDownDistance` onto `groundLayer`. Store `groundPoint` and `groundNormal`. Compute ideal foot positions: `leftIdeal = groundPoint + leftOffset`, `rightIdeal = groundPoint + rightOffset` (offsets perpendicular to movement direction, spaced ~0.18m apart).
  4. **Stride trigger**: for each foot, if `Vector3.Distance(foot.position, idealPosition) > strideLength` AND the other foot is NOT stepping, trigger a step: set state to `Stepping`, record `stepStart = foot.position`, `stepEnd = idealPosition`, reset `stepProgress = 0`.
  5. **Parabolic interpolation** (in `Update`): if `Stepping`, increment `stepProgress` by `stepSpeed * Time.deltaTime`. Compute horizontal position via `Vector3.Lerp(stepStart, stepEnd, stepProgress)`. Compute vertical via `stepHeight * Mathf.Sin(stepProgress * Mathf.PI)`. Set `footTarget.position` to result. When `stepProgress >= 1.0`, set state to `Planted`, snap `footTarget.position = stepEnd`.
  6. **Dual-foot sync**: never allow both feet to step simultaneously. The stride trigger check `otherFoot.state != Stepping` enforces this.
  7. **Movement direction awareness**: foot `stepEnd` should be projected along the current movement vector (from motor velocity or `transform.position` delta) so feet land ahead in the walk direction. If no movement, feet plant directly below.
  8. **Public API**: `SetMoveDirection(Vector3 planarDir)`, `SetMoveSpeed(float mps)`, `GetFootState(bool left)` for tests.

  **Must NOT do**: Do not use `OnAnimatorIK` (we use explicit target Transforms, not Unity IK pass). Do not modify the Animator or existing blend tree. Do not move the Rigidbody (motor handles that). Do not make network calls.

  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: 5 | Blocked By: 0

  **References** (executor has NO interview context):
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/SmoothRigidbodyMotor.cs` - provides `CurrentVelocity` and `DesiredVelocity` properties for movement direction
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/MinibotMotionController.cs` - the orchestrator that will hold ProceduralLegController as a sibling
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MinibotMovementController.cs` - shows `LastPlanarSpeed`, `LastAppliedFacingDirection` for movement info
  - Pattern: `SmoothRigidbodyMotor` uses `FixedUpdate` + `Time.fixedDeltaTime` for physics, `Update` for visual. Follow same split.
  - User spec: "발 걸음 조건 체크 (Foot Stepping): 다리 끝(Foot)의 현재 위치와 이동해야 할 '이상적인 발 위치(Target)' 사이의 거리가 일정 기준(Stride Length) 이상 멀어지면 발을 떼서 새로운 타겟 위치로 이동시키는 트리거를 발동"
  - User spec: "포물선 보간 (Lerp & Sine Wave): Vector3.Lerp로 수평 이동, Y축에 사인 파형 또는 Animation Curve로 발이 위로 들렸다가 딛는 포물선 궤적"
  - User spec: "두 발의 동기화: 한쪽 발이 이동 중(Stepping)일 때 반대쪽 발은 바닥에 고정"

  **Acceptance Criteria** (agent-executable only):
  - [ ] `ProceduralLegController.cs` exists in `Motion/` namespace `ArgusUnity.Motion`
  - [ ] Has `leftFoot`, `rightFoot`, `strideLength`, `stepHeight`, `stepSpeed` serialized fields
  - [ ] `SetMoveDirection` / `SetMoveSpeed` public methods exist
  - [ ] EditMode test: when foot distance exceeds strideLength, foot state transitions to Stepping
  - [ ] EditMode test: during Stepping, foot Y follows sine parabola (peaks at stepProgress=0.5)
  - [ ] EditMode test: both feet never Stepping simultaneously (dual-foot sync invariant)

  **QA Scenarios** (MANDATORY):
  ```
  Scenario: Stride triggers step when distance exceeded
    Tool: bash (Unity EditMode test)
    Steps: Create GameObject with ProceduralLegController, set leftFoot at origin, set ground via mock raycast returning y=0, set strideLength=0.5, move ideal position 0.6m away. Call Update. Assert leftFootState == Stepping.
    Expected: state transitions to Stepping
    Evidence: evidence/task-3-stride-trigger.txt

  Scenario: Parabolic arc peaks at mid-step
    Tool: bash (Unity EditMode test)
    Steps: Force a step (set state=Stepping, stepProgress=0.5), call Update. Assert footTarget.position.y == stepHeight (within 0.001f). Then set stepProgress=0.0, assert y == stepStart.y. Then stepProgress=1.0, assert y == stepEnd.y.
    Expected: y at 0.5 equals stepHeight; y at 0 and 1 equals ground level
    Evidence: evidence/task-3-parabolic-arc.txt

  Scenario: Dual-foot sync prevents simultaneous steps
    Tool: bash (Unity EditMode test)
    Steps: Set leftFoot state=Stepping. Move rightFoot ideal position beyond strideLength. Call Update. Assert rightFootState == Planted (not Stepping).
    Expected: right foot stays planted while left is stepping
    Evidence: evidence/task-3-dual-sync.txt

  Scenario: No movement keeps feet planted
    Tool: bash (Unity EditMode test)
    Steps: Set moveSpeed=0, moveDirection=Vector3.zero. Let controller run 10 Update calls. Assert both feet remain Planted.
    Expected: no stepping triggered when idle
    Evidence: evidence/task-3-idle-planted.txt
  ```

  **Commit**: YES | Message: `feat(unity): procedural leg controller with raycast stride and parabolic IK` | Files: `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/ProceduralLegController.cs`

- [ ] 4. Implement HeadFixConstraint (fixed-yaw head orientation)

  **What to do**: Create `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/HeadFixConstraint.cs`:
  1. **Fields**: `headBone` (Transform, the head bone of the rig), `fixYaw` (bool, default true), `fixPitch` (bool, default false), `fixRoll` (bool, default false), `targetYaw` (float, degrees, set from initial head yaw at Awake), `yawStiffness` (float, default 10.0, how quickly head re-aligns).
  2. **Awake**: record `initialYaw = headBone.eulerAngles.y` if `targetYaw` not set. Store as `targetYaw`.
  3. **LateUpdate** (after Animator but before rendering): compute current yaw delta = `Mathf.DeltaAngle(headBone.eulerAngles.y, targetYaw)`. If `fixYaw`, apply rotation to bring head yaw back toward `targetYaw` by `yawStiffness * Time.deltaTime` (or snap if delta small). Use `Quaternion.Euler` preserving pitch/roll unless those are also fixed. Set `headBone.rotation` directly (this runs after Animator, overriding animation head rotation).
  4. **Public API**: `SetTargetYaw(float degrees)`, `GetTargetYaw()`, `GetCurrentYawDelta()` for tests. Also `SetFixYaw(bool)`.
  5. **Alternative if animation-rigging available**: use `Multi-Aim Constraint` on head bone with a fixed target Transform. But the LateUpdate approach is simpler and doesn't require rig setup, so prefer it. Document both options in a comment.

  **Must NOT do**: Do not rotate the body or root (only head bone). Do not interfere with `MinibotMovementController` facing (that controls body/root). Do not use `OnAnimatorIK` (LateUpdate is more predictable).

  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: 5 | Blocked By: 0

  **References** (executor has NO interview context):
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/AgentLocomotionDriver.cs` - runs in `LateUpdate`, so HeadFixConstraint must also run in LateUpdate and execute AFTER the driver (set Script Execution Order or use `LateUpdate` which naturally runs after Animator)
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/MinibotMotionController.cs` - the parent controller; HeadFixConstraint sits as sibling component
  - User spec: "몸통(머리) 제어: 캐릭터의 몸통은 코드로만 이동 및 회전시킵니다. 회전값을 고정하면 이동 방향과 상관없이 머리는 항상 정면을 유지합니다."
  - Unity API: `Quaternion.Euler`, `Mathf.DeltaAngle`, `Mathf.MoveTowardsAngle`

  **Acceptance Criteria** (agent-executable only):
  - [ ] `HeadFixConstraint.cs` exists in `Motion/` namespace `ArgusUnity.Motion`
  - [ ] Has `headBone`, `fixYaw`, `targetYaw`, `yawStiffness` serialized fields
  - [ ] EditMode test: after rotating body 90 degrees, head yaw returns to within 1 degree of targetYaw
  - [ ] EditMode test: `SetTargetYaw(45f)` then Update changes target and head converges

  **QA Scenarios** (MANDATORY):
  ```
  Scenario: Head yaw stays fixed during body rotation
    Tool: bash (Unity EditMode test)
    Steps: Create GameObject with HeadFixConstraint, set headBone to a child transform, targetYaw=0. Rotate parent (body) by 90 degrees. Call LateUpdate multiple times until delta < 0.1. Assert headBone.eulerAngles.y within 1 degree of 0.
    Expected: head yaw returns to 0 despite body rotation
    Evidence: evidence/task-4-head-fix.txt

  Scenario: SetTargetYaw updates target
    Tool: bash (Unity EditMode test)
    Steps: Set targetYaw=0 initially. Call SetTargetYaw(90f). Simulate body at yaw=0. Call LateUpdate. Assert head moves toward 90 degrees (delta decreases).
    Expected: head yaw converges to new target
    Evidence: evidence/task-4-target-yaw.txt

  Scenario: fixYaw=false allows free rotation
    Tool: bash (Unity EditMode test)
    Steps: Set fixYaw=false. Rotate body 90 degrees. Call LateUpdate. Assert head yaw follows body (not fixed).
    Expected: head rotates with body when fix disabled
    Evidence: evidence/task-4-fix-disabled.txt
  ```

  **Commit**: YES | Message: `feat(unity): head fix constraint for fixed-gaze locomotion` | Files: `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/HeadFixConstraint.cs`

- [ ] 5. Integration builder + scene wiring + PlayMode QA + screenshots

  **What to do**: Create `unity/EmbodiedDebate/Assets/Editor/ProceduralWalkBuilder.cs` that:
  1. Adds a menu item `Argus/Build Procedural Walk Rig` that: (a) finds the MiniBot prefab/Idle.fbx rig in the active scene or loads `MiniBotHideAndSeekDesign.unity`, (b) adds `ProceduralLegController` to the rig GameObject, (c) assigns `leftFoot` and `rightFoot` to the rig's left/right foot bones (find by name: `mixamorig:LeftFoot` / `mixamorig:RightFoot` standard Mixamo names, or search `Animator.GetBoneTransform(HumanBodyBones.LeftFoot)`), (d) adds `HeadFixConstraint` and assigns `headBone` via `Animator.GetBoneTransform(HumanBodyBones.Head)`, (e) wires `ProceduralLegController.footRaycastOrigin` to the hip/pelvis bone, (f) creates child Transforms as foot IK targets if none exist.
  2. Extends `MinibotMotionController.cs` or creates a bridge: in `Update`, read `motor.CurrentVelocity` / `motor.DesiredVelocity` and call `proceduralLegController.SetMoveDirection(velocityPlanar)` and `SetMoveSpeed(velocityMagnitude)`.
  3. Creates a PlayMode test `ProceduralWalkIntegrationTests.cs` that: instantiates the rig, applies a walk intent, runs N frames, asserts feet alternate stepping, head yaw stays fixed, and captures a screenshot to `evidence/task-5-walk-screenshot.png`.
  4. Captures before/after screenshots: matte + SSAO visual comparison. Use existing `SmokeScreenshot.cs` pattern (`unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/SmokeScreenshot.cs`).

  **Must NOT do**: Do not destroy the existing Animator or blend tree (procedural legs layer on top). Do not change bridge message types. Do not break existing PlayMode tests.

  **Parallelization**: Can Parallel: NO | Wave 2 | Blocks: F1-F4 | Blocked By: 0, 1, 2, 3, 4

  **References** (executor has NO interview context):
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/MinibotMotionController.cs` - the orchestrator to extend with leg controller wiring
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/ProceduralLegController.cs` - the controller to wire (Task 3)
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/HeadFixConstraint.cs` - the constraint to wire (Task 4)
  - File: `unity/EmbodiedDebate/Assets/Editor/HideAndSeekDesignBuilder.cs` - pattern for editor builders that configure scenes and rigs
  - File: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/SmokeScreenshot.cs` - existing screenshot capture pattern for QA
  - File: `unity/EmbodiedDebate/Assets/Tests/PlayMode/MiniBotBehaviorApplicationPlayModeTests.cs` - PlayMode test pattern
  - Unity API: `Animator.GetBoneTransform(HumanBodyBones.LeftFoot)`, `HumanBodyBones.Head`, `HumanBodyBones.Hips`

  **Acceptance Criteria** (agent-executable only):
  - [ ] `ProceduralWalkBuilder.cs` exists in `Editor/` with `[MenuItem("Argus/Build Procedural Walk Rig")]`
  - [ ] Running the menu item adds both `ProceduralLegController` and `HeadFixConstraint` to the rig
  - [ ] `MinibotMotionController` (or bridge) feeds velocity to leg controller each Update
  - [ ] PlayMode test: after 60 frames of walk intent, at least one step triggered and head yaw delta < 1 degree
  - [ ] Screenshot captured to `evidence/task-5-walk-screenshot.png` (non-zero file size)

  **QA Scenarios** (MANDATORY):
  ```
  Scenario: Walk rig built and components present
    Tool: bash (Unity EditMode/PlayMode test)
    Steps: Run `Argus/Build Procedural Walk Rig`. Assert rig GameObject has GetComponent<ProceduralLegController>() != null and GetComponent<HeadFixConstraint>() != null. Assert leftFoot/rightFoot/headBone assigned (not null).
    Expected: all three component/bone references non-null
    Evidence: evidence/task-5-rig-built.txt

  Scenario: Procedural walk produces alternating steps
    Tool: bash (Unity PlayMode test)
    Steps: Instantiate rig, apply MotionIntent.WalkForward to target 3m ahead. Run 120 frames (2s @ 60fps). Assert leftFootState and rightFootState each transitioned to Stepping at least once. Assert never both Stepping in same frame.
    Expected: both feet stepped, never simultaneously
    Evidence: evidence/task-5-alternating-steps.txt

  Scenario: Head stays fixed during walk
    Tool: bash (Unity PlayMode test)
    Steps: Same walk scenario. Record headBone.eulerAngles.y at frame 0 and frame 120. Assert Mathf.DeltaAngle(initial, final) < 1.0f.
    Expected: head yaw unchanged despite walking and body turning
    Evidence: evidence/task-5-head-fixed-walk.txt

  Scenario: Visual QA screenshot captured
    Tool: bash (Unity PlayMode screenshot)
    Steps: Run walk scenario with SSAO + matte materials active. Capture screenshot via ScreenCapture.CaptureScreenshot to evidence/task-5-walk-screenshot.png. Assert file exists and size > 1000 bytes.
    Expected: PNG file exists, non-trivial size
    Evidence: evidence/task-5-walk-screenshot.png

  Scenario: Existing tests still pass (regression)
    Tool: bash (Unity Test Runner)
    Steps: Run full EditMode + PlayMode test suite. Assert exit code 0, no new failures.
    Expected: all pre-existing tests pass, new tests pass
    Evidence: evidence/task-5-regression.txt
  ```

  **Commit**: YES | Message: `feat(unity): wire procedural walk rig with integration tests and QA screenshots` | Files: `unity/EmbodiedDebate/Assets/Editor/ProceduralWalkBuilder.cs`, `unity/EmbodiedDebate/Assets/Project/Scripts/Motion/MinibotMotionController.cs`, `unity/EmbodiedDebate/Assets/Tests/PlayMode/ProceduralWalkIntegrationTests.cs`

## Final Verification Wave (MANDATORY - after ALL implementation tasks)
> ALL must APPROVE. Present consolidated results to user and get explicit "okay" before completing.
- [ ] F1. Plan Compliance Audit
  - Every task's acceptance criteria met with evidence file path
  - Every QA scenario run with captured artifact
  - No task left in_progress
- [ ] F2. Code Quality Review
  - All new .cs files have `using` statements sorted, no warnings
  - No `#pragma warning disable`, no `// TODO` left in shipped code
  - Namespace consistency (`ArgusUnity.Motion`, `ArgusUnity.Editor`)
  - Serialized fields use `[SerializeField] private` pattern matching existing code
- [ ] F3. Real Manual QA
  - PlayMode screenshot shows: matte flat materials, SSAO depth in corners, head fixed forward, legs alternating parabolic steps
  - Compare side-by-side with OpenAI Hide-and-Seek reference frame
  - Evidence: `evidence/f3-visual-qa.png` + `evidence/f3-comparison-notes.md`
- [ ] F4. Scope Fidelity Check
  - No edits outside `unity/EmbodiedDebate/Assets/`
  - No bridge protocol changes
  - No Python simulation changes
  - No new Mixamo imports or character models
  - Existing blend tree preserved (not destroyed)

## Commit Strategy
Atomic Conventional Commits, one per task. Each commit builds + tests green on its own.
- Task 0: `build(unity): add animation-rigging package for procedural IK`
- Task 1: `feat(unity): add SSAO renderer feature to URP for minimalist depth`
- Task 2: `feat(unity): add matte material + bright ambient lighting configurator`
- Task 3: `feat(unity): procedural leg controller with raycast stride and parabolic IK`
- Task 4: `feat(unity): head fix constraint for fixed-gaze locomotion`
- Task 5: `feat(unity): wire procedural walk rig with integration tests and QA screenshots`
- Final commit footer: `Plan: plans/procedural-walk-minimalist-shader.md`

## Success Criteria
1. Character walks with head yaw fixed to initial forward direction (within 1 degree) during locomotion
2. Legs alternate stepping in parabolic arcs, never simultaneous, triggered by stride distance
3. Feet raycast to ground and land ahead in movement direction
4. URP renderer has SSAO feature producing soft contact shadows
5. Materials are flat matte (Smoothness=0, Metallic=0), no specular highlights
6. Ambient lighting is bright, filling shadow areas
7. Visual matches OpenAI Hide-and-Seek aesthetic (verified by screenshot comparison)
8. All existing tests pass (no regressions)
9. New EditMode + PlayMode tests pass (RED->GREEN proven)
10. No scope creep outside unity/EmbodiedDebate/Assets/
