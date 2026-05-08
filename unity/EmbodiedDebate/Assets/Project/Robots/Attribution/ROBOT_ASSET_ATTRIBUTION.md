# Robot Asset Attribution

## Selected Asset

- Title: Project-owned FallbackRobot placeholder
- Author / Publisher: Argus project
- Source: `unity/EmbodiedDebate/Assets/Project/Robots/Prefabs/FallbackRobot.prefab`
- License: Project license for repository-owned code/assets
- Download Date: Not applicable
- Imported By: Not applicable
- Redistribution Allowed: Yes, for this project-owned placeholder
- Notes: Third-party robot assets must not be committed until license and import validation are complete. The default `FallbackRobot` prefab uses `CuteRobotPrefabInitializer` (built-in primitives) for a friendly placeholder until a rigged model is imported under `Assets/ThirdParty/Robots/` (see that folder’s README).

## Candidate Under Review

- Title: User-provided mini-bot locomotion FBX clips
- Author / Publisher: User-provided local assets, Mixamo-style motion-only skeleton
- Source: `/Users/guribbong/Downloads/Walking-2.fbx`, `/Users/guribbong/Downloads/Right Turn 90.fbx`, `/Users/guribbong/Downloads/Right Turn 90-2.fbx`, `/Users/guribbong/Downloads/Happy Right Turn.fbx`, `/Users/guribbong/Downloads/Happy Right Turn-2.fbx`, `/Users/guribbong/Downloads/Running.fbx`, `/Users/guribbong/Downloads/Slow Run.fbx`, `/Users/guribbong/Downloads/Running To Turn.fbx`, `/Users/guribbong/Downloads/Left Turn W_Briefcase.fbx`, `/Users/guribbong/Downloads/Left Turn W_Briefcase-2.fbx`, `/Users/guribbong/Downloads/Thinking.fbx`, `/Users/guribbong/Downloads/Angry.fbx`, `/Users/guribbong/Downloads/Male Laying Pose.fbx`, `/Users/guribbong/Downloads/Standing Torch Light Torch.fbx`
- License: User must confirm redistribution rights before publishing outside the local project
- Download Date: 2026-05-06 local import
- Imported By: `HideAndSeekDesignBuilder` into `Assets/Project/Resources/Animations/`
- Redistribution Allowed: Not confirmed
- Notes: `Walking-2.fbx` is used as the Animator `Walk_InPlace` source. `Running.fbx`, `Slow Run.fbx`, `Running To Turn.fbx`, and the happy turn clips are imported but quarantined from production after preview review. `Left Turn W_Briefcase.fbx` is used as `TurnLeft_Briefcase`; `Left Turn W_Briefcase-2.fbx` is also copied to `Right Turn W_Briefcase_Mirrored.fbx` and mirrored during import for `TurnRight_Briefcase`. Action clips are imported for review but are not active locomotion states.

## Modifications

- Scale: TBD
- Materials: TBD
- Rig Settings: Humanoid import attempted for `Idle.fbx` and `Walking-2.fbx`; root motion disabled on runtime Animator
- Animation Retargeting: Animator controller uses `Idle`, `Walk_InPlace`, `TurnLeft_Briefcase`, and `TurnRight_Briefcase`; quarantined run/happy and action FBXs remain imported but inactive in production locomotion
- Collider Setup: TBD
