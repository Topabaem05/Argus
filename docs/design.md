# Multi-Agent Hide and Seek Asset and Level Design

## Art Direction

The Unity scene uses functional minimalism: role colors, grid scale markers, and modular blocks are used to make agent behavior readable without decorative clutter.

- Blue: hider/defense agents
- Red: seeker/attack agents
- Yellow: movable/tool objects such as boxes, ramps, and stairs
- Gray: controlled lab floor and closed-world walls
- Purple: state-change and interaction objects such as doorways, keys, arrows, and switches

## Implemented Unity Scene

Scene: `unity/EmbodiedDebate/Assets/Project/Scenes/MiniBotHideAndSeekDesign.unity`

Builder: `unity/EmbodiedDebate/Assets/Editor/HideAndSeekDesignBuilder.cs`

Runtime movement: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotHideAndSeekScenario.cs`

Background asset source: `unity/EmbodiedDebate/Assets/ThirdParty/StylooClassroomAssetPack/StylooClassroomAssetPack GLTF & FBX/classroom/GLTF`

The current classroom is built at 50% of the previous scene scale.

## Level Composition

The scene combines three training-stage layouts in one inspectable map:

- Basic room: a classroom shell from the Styloo classroom pack with a quantitative grid overlay.
- Retreat room: a smaller wall-enclosed room with a purple doorway and yellow boxes placed near the entrance.
- Randomized field: short walls, fences, ramps, stairs, cubes, keys, and arrows arranged as obstacle/resource candidates.

The current classroom build keeps only the room shell, one front `desk.glb`, rows of `table.glb` aligned in one direction, and the mini-bots. Other classroom props and the previous modular floor-level obstacle assets are removed from the active scene.

## Mini-Bot Constraint

The mini-bot asset is not modified. The scene instantiates `Assets/Project/Resources/UserModels/Idle.fbx` directly, adds team markers as child objects, and applies procedural walking through a Unity component on the scene instance.

Unity import status from validation:

- `animationType=Generic`
- `importAnimation=True`
- `avatarValid=False`
- `avatarHuman=False`
- walking bones present: `LeftFoot`, `RightFoot`, `LeftLeg`, `RightLeg`

Because the model is Generic rather than Humanoid, the current walking action is procedural bone animation, not a Humanoid animation clip.
