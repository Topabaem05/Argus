# Robot Asset Selection

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

## Goal

Select and import a cute biped robot model that is already rigged and animation-ready for Unity. The model must support the first embodied simulation MVP:

- idle,
- walk or locomotion,
- talk gesture or neutral talking pose,
- angry/argument gesture or fallback emotion icon,
- push or contact preparation,
- fall or stumble fallback,
- recover or stand-up fallback.

The repository must not redistribute third-party assets unless the license explicitly allows redistribution. The safest implementation path is to document approved asset sources and require the developer to import them locally.

## Selection Criteria

| Criterion | Required | Notes |
|---|---:|---|
| Cute or friendly robot appearance | Yes | Must visually fit a Sims-like social simulation. |
| Biped or humanoid body | Yes | Required for walking, facing, arguing, pushing, and falling. |
| Rigged | Yes | Must have usable bones or Unity Humanoid/Generic rig. |
| Animated or animation-compatible | Yes | Must support idle and locomotion at minimum. |
| Unity compatible | Yes | FBX, GLB, or Unity package preferred. |
| Legal to use | Yes | License must be checked before commit or distribution. |
| Redistributable in repository | Optional | If not redistributable, store only import instructions. |
| Low enough poly count for crowds | Preferred | 20-agent MVP should remain interactive. |

## Recommended Asset Path

### Primary MVP Asset Candidate: Cute Robots - Low Poly - Rigged - Animated

**Source:** Sketchfab  
**Title:** Cute Robots - Low Poly - Rigged - Animated  
**Author:** m1ch3lang3lo / berkankirmitt  
**License shown by source:** Creative Commons Attribution  
**Source-reported properties:** rigged, animated, run, jump, idle, low-poly/cute robot character.  
**Why it fits:** The style is explicitly cute and the source reports rigging and basic animations. This matches the MVP requirement better than a generic industrial robot.  
**Validation needed before final adoption:** Confirm bipedal/humanoid skeleton in Unity import, confirm animation clips import correctly, confirm license attribution requirements, confirm polygon count is acceptable for 20 visible agents.

### Reliability Fallback Asset: Robot Kyle | URP

**Source:** Unity Asset Store  
**Publisher:** Unity Technologies  
**License shown by source:** Standard Unity Asset Store EULA  
**Cost shown by source:** Free  
**Source-reported properties:** URP package, Unity 2021.3+ compatibility, small package size, Unity robot/humanoid-related asset.  
**Why it fits:** It is the safest Unity-native fallback for a humanoid robot avatar when the cute Sketchfab model has import or license issues.  
**Validation needed before final adoption:** Confirm humanoid rig configuration, confirm animation retargeting, confirm whether redistribution in this repository is allowed under the Asset Store EULA. Prefer not to redistribute.

### Optional Paid Production Candidate: Animated Toon Humanoid Robot Characters

**Source:** Unity Asset Store  
**Why it may fit:** A production package with multiple toon humanoid robot characters and animations may be better for visual quality and variety.  
**Why not MVP default:** Paid asset, more licensing and budget friction.

## Asset Import Policy

The implementation must create asset import instructions, not silently download assets.

Expected Unity path after local import:

```txt
unity/EmbodiedDebate/Assets/ThirdParty/Robots/
```

Expected project-owned wrappers:

```txt
unity/EmbodiedDebate/Assets/Project/Robots/
├── Prefabs/
│   └── SimulationRobot.prefab
├── Animators/
│   └── SimulationRobotAnimator.controller
├── Materials/
├── Scripts/
└── Attribution/
    └── ROBOT_ASSET_ATTRIBUTION.md
```

Only project-owned wrappers should be committed if the third-party license does not allow asset redistribution.

## Required Attribution File

Create:

```txt
unity/EmbodiedDebate/Assets/Project/Robots/Attribution/ROBOT_ASSET_ATTRIBUTION.md
```

It must include:

```md
# Robot Asset Attribution

## Selected Asset

- Title:
- Author / Publisher:
- Source:
- License:
- Download Date:
- Imported By:
- Redistribution Allowed:
- Notes:

## Modifications

- Scale:
- Materials:
- Rig Settings:
- Animation Retargeting:
- Collider Setup:
```

## Unity Validation Checklist

The selected robot asset is accepted only when all required checks pass.

- [ ] Asset imports without Unity console errors.
- [ ] Prefab can be placed in an empty scene.
- [ ] Robot stands upright at scale 1.0 or documented scale.
- [ ] Rig type is configured consistently as Humanoid or Generic.
- [ ] Idle animation plays.
- [ ] Locomotion animation plays or can be retargeted.
- [ ] Animator can switch between idle, walk, talk, argue, push, fall, recover.
- [ ] Root motion policy is documented.
- [ ] Robot can be controlled by bridge transform updates.
- [ ] Collider setup does not block NavMesh movement.
- [ ] Prefab supports color/group badge customization.
- [ ] Attribution file is completed.
- [ ] License permits the intended use.
- [ ] If redistribution is not allowed, raw source asset is excluded from Git.

## Animation Mapping

| Bridge Action | Required Animator State | Fallback |
|---|---|---|
| `idle` | `Idle` | Static idle pose |
| `walk_to` | `Walk` | Linear transform interpolation |
| `run_to` | `Run` | Faster walk |
| `speak` | `Talk` | Idle + dialogue bubble |
| `argue` | `Argue` | Talk + angry emotion icon |
| `approach_conflict` | `Walk` or `Run` | Linear movement |
| `push` | `Push` | Short forward gesture |
| `blocked` | `Block` | Step back |
| `stumble` | `Stumble` | Tilt animation |
| `fall` | `Fall` | Preauthored fall animation |
| `recover` | `Recover` | Stand-up or fade to idle |

## Physics Representation

Unity visual mesh and MuJoCo physical model are not required to be the same asset.

For MVP:

- Unity uses cute robot mesh.
- MuJoCo uses a simplified humanoid or capsule-based MJCF model.
- A mapping table connects Unity agent IDs to MuJoCo body IDs.
- MuJoCo results return high-level outcomes, not detailed skeletal poses.

Future work may add closer rig-to-MJCF mapping if needed.

## Non-Goals

- Do not build a custom robot model from scratch during bridge MVP.
- Do not implement full facial expressions.
- Do not simulate injuries.
- Do not require paid assets for MVP.
- Do not require MuJoCo to animate every step of walking.
