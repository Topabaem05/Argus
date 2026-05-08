# Robot Asset Selection

Argus does not download or redistribute robot assets automatically. Robot models must be imported locally after license and Unity import validation.

## Selection Policy

Preferred MVP path:

1. Validate a cute, rigged, low-poly robot candidate from Sketchfab only after checking the current license, attribution requirements, rig import, animation clips, and crowd performance.
2. If that candidate fails license or import validation, use Robot Kyle or another Unity-native fallback only after confirming its license and redistribution limits.
3. If no third-party asset can be redistributed, commit only project-owned wrappers, instructions, and attribution templates. Keep raw third-party files outside Git.

Expected local third-party import path:

```txt
unity/EmbodiedDebate/Assets/ThirdParty/Robots/
```

Expected project-owned wrapper path:

```txt
unity/EmbodiedDebate/Assets/Project/Robots/
├── Attribution/
│   └── ROBOT_ASSET_ATTRIBUTION.md
├── Prefabs/
│   └── FallbackRobot.prefab
├── Animators/
├── Materials/
└── Scripts/
```

## Candidate Priority

| Priority | Candidate | Source | Status |
|---:|---|---|---|
| 1 | Cute Robots - Low Poly - Rigged - Animated | Sketchfab | Candidate only; validate license, rig, animations, and import before use. |
| 2 | Robot Kyle / Unity-native robot fallback | Unity Asset Store | Fallback only; validate Asset Store EULA and redistribution limits before committing assets. |
| 3 | Project-owned placeholder | This repository | Safe fallback for wiring, tests, and non-final visual checks. |

## Local Import Steps

1. Download the candidate manually from the source account in a browser.
2. Record title, author or publisher, source URL, license, download date, and redistribution status in `ROBOT_ASSET_ATTRIBUTION.md`.
3. Import into `unity/EmbodiedDebate/Assets/ThirdParty/Robots/`.
4. Configure rig type, scale, materials, colliders, and animation clips in Unity.
5. Create or update project-owned wrappers under `Assets/Project/Robots/`.
6. Commit raw third-party files only if the license explicitly allows repository redistribution.

## Acceptance Checklist

- [ ] Asset imports without Unity console errors.
- [ ] Prefab can be placed in `MainSimulation.unity`.
- [ ] Robot stands upright at documented scale.
- [ ] Rig type is configured as Humanoid or Generic.
- [ ] Idle and locomotion animations work or have documented fallbacks.
- [ ] Talk, argue, push, stumble, fall, and recover states are mapped or safely fall back to idle.
- [ ] Collider setup does not block bridge-driven movement.
- [ ] Group color or badge customization is possible.
- [ ] Attribution file is complete.
- [ ] License permits the intended use.
- [ ] Raw third-party source files are excluded from Git unless redistribution is allowed.
