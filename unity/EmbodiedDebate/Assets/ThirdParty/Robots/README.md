# Third-party robot imports (local only)

This folder is the expected drop zone for rigged avatars per the repository’s [`docs/robot-asset-selection.md`](../../../../../docs/robot-asset-selection.md) (same guidance as the AI Unity MuJoCo Bridge documentation bundle).

1. Download a **CC-licensed** Sketchfab candidate such as *Cute Robots - Low Poly - Rigged - Animated*, or use **Robot Kyle | URP** from the Asset Store under its EULA (often not Git-redistributable).
2. Import the FBX/Unity package **only on your machine** into `Assets/ThirdParty/Robots/`.
3. Build a prefab (e.g. `SimulationRobot`) and assign it on `SimulationBootstrap.robotPrefab`, or register its `Resources` name in `RobotAvatarManager`/`AgentSpawnHandler` when you expose multiple prefab keys.
4. Fill in `Assets/Project/Robots/Attribution/ROBOT_ASSET_ATTRIBUTION.md` before committing any redistribution-allowed asset.

Until then, Play Mode uses the project-owned `FallbackRobot` with `CuteRobotPrefabInitializer`.
