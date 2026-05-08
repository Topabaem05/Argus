# MiniBot BVH Turn Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make mini-bot walking visibly follow the supplied BVH clips while turning naturally during free-roam movement.

**Architecture:** Keep Unity script-driven locomotion and use the BVH clips only for pose. The Rigidbody controls world movement and capped rotation; the animator advances gait phase from actual traveled distance and retargets BVH joints through an explicit mini-bot-to-BVH map.

**Tech Stack:** Unity 2022.3 C#, Rigidbody physics, manual BVH parser, ffmpeg smoke capture.

---

### Task 1: Fix BVH Retarget Mapping

**Files:**
- Modify: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotWalkAnimator.cs`

- [x] **Step 1: Add explicit mini-bot-to-BVH joint mapping**

Replace identical-name lookup with:

```csharp
private static readonly Dictionary<string, string> BvhJointByMiniBotBone = new Dictionary<string, string>
{
    { "Hips", "Hips" },
    { "Spine", "Spine1" },
    { "Spine1", "Spine2" },
    { "Spine2", "Chest" },
    { "LeftUpLeg", "LeftLeg" },
    { "LeftLeg", "LeftShin" },
    { "LeftFoot", "LeftFoot" },
    { "RightUpLeg", "RightLeg" },
    { "RightLeg", "RightShin" },
    { "RightFoot", "RightFoot" },
    { "LeftArm", "LeftArm" },
    { "LeftForeArm", "LeftForeArm" },
    { "RightArm", "RightArm" },
    { "RightForeArm", "RightForeArm" },
};
```

- [x] **Step 2: Log mapped BVH track coverage**

At startup log `mappedBvhTracks=<count>/<animatedBones.Length>` so verification proves the BVH path is active.

- [x] **Step 3: Stop procedural pose from overriding BVH**

Only use procedural fallback for bones that do not receive a BVH delta. BVH-driven leg/arm bones must remain driven by the clip.

### Task 2: Smooth Turn Animation Without Clip Resets

**Files:**
- Modify: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotWalkAnimator.cs`

- [x] **Step 1: Keep walk cycle phase continuous**

Remove full active clip switching and `playbackTime = 0f` on turn changes.

- [x] **Step 2: Use turn clips as additive overlay**

Sample `TurnLeft` or `TurnRight` at the same normalized phase as the walk cycle and blend it lightly by smoothed turn amount.

- [x] **Step 3: Verify logging**

Keep `side-turn animation active` logs, but add `bvhTurnOverlay=True` in the startup or turn log.

### Task 3: Cap Physical Rotation Speed

**Files:**
- Modify: `unity/EmbodiedDebate/Assets/Project/Scripts/Runtime/MiniBotHideAndSeekScenario.cs`

- [x] **Step 1: Replace exponential Slerp spin with angular cap**

Use `Quaternion.RotateTowards(previousRotation, targetRotation, maxTurnDegreesPerSecond * Time.fixedDeltaTime)` before `Rigidbody.MoveRotation`.

- [x] **Step 2: Smooth steering direction**

Store each agent's smoothed direction and blend desired steering into it before setting velocity.

- [x] **Step 3: Log capped rotation**

Log one line showing free-roam movement speed still matches the distance-synced gait.

### Task 4: Unity Verification

**Files:**
- Capture outputs under `tmp/`

- [x] **Step 1: Rebuild the scene**

Run:

```bash
"/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath unity/EmbodiedDebate -executeMethod ArgusUnity.Editor.HideAndSeekDesignBuilder.BuildScene -logFile tmp/hide-and-seek-bvh-turn-sync-build.log
```

Expected: no `CS####`, `NullReferenceException`, `MissingComponentException`, or `threw exception`.

- [x] **Step 2: Capture Play Mode**

Run:

```bash
ARGUS_UNITY_VIDEO_CAPTURE=1 ARGUS_UNITY_VIDEO_DIR=/Users/guribbong/code/Argus/tmp/bvh_turn_sync_frames ARGUS_UNITY_VIDEO_PREFIX=bvh_turn_sync "/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath unity/EmbodiedDebate -executeMethod ArgusUnity.Editor.HideAndSeekDesignBuilder.OpenSceneInPlayMode -logFile tmp/hide-and-seek-bvh-turn-sync-capture.log
```

Expected: `240` frames, `mappedBvhTracks=14/14`, `distance-synced gait`, `side-turn animation active`, free-roam movement logs.

- [x] **Step 3: Render video and contact sheet**

Render `tmp/bvh_turn_sync_video.mp4`, `tmp/bvh_turn_sync_video_zoom.mp4`, and `tmp/bvh_turn_sync_contact.jpg`.

- [x] **Step 4: Open Unity Play Mode**

Open the editor using `HideAndSeekDesignBuilder.OpenSceneInPlayMode` and verify the live log has no compile/runtime error pattern.
