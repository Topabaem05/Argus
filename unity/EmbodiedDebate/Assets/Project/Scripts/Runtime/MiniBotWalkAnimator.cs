using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MiniBotWalkAnimator : MonoBehaviour
    {
        private const string WalkResource = "Animations/BVH/WalkBVH";
        private const string TurnLeftResource = "Animations/BVH/TurnLeftBVH";
        private const string TurnRightResource = "Animations/BVH/TurnRightBVH";
        private const string RightTurn90Resource = "Animations/FBXTurns/Right Turn 90";
        private const string LeftTurn90Resource = "Animations/FBXTurns/Right Turn 90-2";
        private const string HappyRightTurnResource = "Animations/FBXTurns/Happy Right Turn";
        private const string HappyLeftTurnResource = "Animations/FBXTurns/Happy Right Turn-2";

        [SerializeField]
        private float turnThresholdDegrees = 10f;

        [SerializeField]
        private float blendSharpness = 12f;

        [SerializeField]
        private float legWeight = 0.85f;

        [SerializeField]
        private float upperBodyWeight = 0.25f;

        [SerializeField]
        private float playbackRate = 1f;

        [SerializeField]
        private float visiblePoseWeight = 1.35f;

        [SerializeField]
        private float referenceMoveSpeed = 1.15f;

        [SerializeField]
        private float sideTurnPoseWeight = 1.45f;

        [SerializeField]
        private float metersPerWalkCycle = 0.62f;

        [SerializeField]
        private float turnClipBlendWeight = 0.55f;

        [SerializeField]
        private float fbxTurnClipBlendWeight = 0.72f;

        [SerializeField]
        private int fbxTurnSamplesPerSecond = 30;

        private static readonly Dictionary<string, BvhClip> ClipCache = new Dictionary<string, BvhClip>();
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

        private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Quaternion> restRotations = new Dictionary<string, Quaternion>();
        private readonly string[] animatedBones =
        {
            "Hips",
            "Spine",
            "Spine1",
            "Spine2",
            "LeftUpLeg",
            "LeftLeg",
            "LeftFoot",
            "RightUpLeg",
            "RightLeg",
            "RightFoot",
            "LeftArm",
            "LeftForeArm",
            "RightArm",
            "RightForeArm",
        };

        private BvhClip walkClip;
        private BvhClip turnLeftClip;
        private BvhClip turnRightClip;
        private SampledFbxClip fbxRightTurnClip;
        private SampledFbxClip fbxLeftTurnClip;
        private SampledFbxClip fbxHappyRightTurnClip;
        private SampledFbxClip fbxHappyLeftTurnClip;
        private float playbackTime;
        private float motionWeight;
        private float movementSpeed;
        private float smoothedMovementSpeed;
        private float distanceThisFrame;
        private bool distanceSyncedThisFrame;
        private bool hasDistanceSync;
        private float turnBlend;
        private float smoothedSignedTurnDegrees;
        private bool moving;
        private float signedTurnDegrees;
        private int mappedTrackCount;
        private bool speedSyncLogged;
        private bool turnAnimationLogged;

        public float MetersPerWalkCycle => metersPerWalkCycle;

        private void Awake()
        {
            DisableImportedIdleAnimation();
            CacheBones();
            walkClip = LoadClip(WalkResource);
            turnLeftClip = LoadClip(TurnLeftResource);
            turnRightClip = LoadClip(TurnRightResource);
            fbxRightTurnClip = LoadSampledFbxClip(RightTurn90Resource, "right-90");
            fbxLeftTurnClip = LoadSampledFbxClip(LeftTurn90Resource, "left-90-from-minus-2");
            fbxHappyRightTurnClip = LoadSampledFbxClip(HappyRightTurnResource, "happy-right");
            fbxHappyLeftTurnClip = LoadSampledFbxClip(HappyLeftTurnResource, "happy-left-from-minus-2");
            mappedTrackCount = CountMappedTracks(walkClip);
            RestoreRestPoseImmediate();

            Debug.Log(
                "MiniBotWalkAnimator: BVH clips " +
                $"walk={walkClip != null}, turnLeft={turnLeftClip != null}, turnRight={turnRightClip != null}; " +
                $"mappedBvhTracks={mappedTrackCount}/{animatedBones.Length}, " +
                $"fbxTurnOverlay={HasFbxTurnClips()}, minus2MeansLeft=True; " +
                $"walking bones leftFoot={bones.ContainsKey("LeftFoot")}, rightFoot={bones.ContainsKey("RightFoot")}, " +
                $"leftLeg={bones.ContainsKey("LeftLeg")}, rightLeg={bones.ContainsKey("RightLeg")}.");
        }

        private void LateUpdate()
        {
            motionWeight = Mathf.MoveTowards(
                motionWeight,
                moving ? 1f : 0f,
                Time.deltaTime * blendSharpness);
            smoothedMovementSpeed = Mathf.MoveTowards(
                smoothedMovementSpeed,
                moving ? movementSpeed : 0f,
                Time.deltaTime * referenceMoveSpeed * blendSharpness);
            smoothedSignedTurnDegrees = Mathf.Lerp(
                smoothedSignedTurnDegrees,
                moving ? signedTurnDegrees : 0f,
                1f - Mathf.Exp(-blendSharpness * Time.deltaTime));
            turnBlend = Mathf.MoveTowards(
                turnBlend,
                moving ? Mathf.Clamp01((Mathf.Abs(smoothedSignedTurnDegrees) - turnThresholdDegrees) / 65f) : 0f,
                Time.deltaTime * blendSharpness);

            if (walkClip == null || motionWeight <= 0.001f)
            {
                RestoreRestPose();
                return;
            }

            var fallbackSpeedScale = Mathf.Clamp(smoothedMovementSpeed / Mathf.Max(0.01f, referenceMoveSpeed), 0.35f, 2.8f);
            float timeAdvance;
            if (distanceSyncedThisFrame)
            {
                timeAdvance = (distanceThisFrame / Mathf.Max(0.01f, metersPerWalkCycle)) * walkClip.Duration;
            }
            else if (hasDistanceSync)
            {
                timeAdvance = 0f;
            }
            else
            {
                timeAdvance = Time.deltaTime * playbackRate * fallbackSpeedScale;
            }

            playbackTime = Mathf.Repeat(playbackTime + timeAdvance, walkClip.Duration);
            distanceThisFrame = 0f;
            distanceSyncedThisFrame = false;
            ApplyBvhPose(playbackTime / Mathf.Max(0.001f, walkClip.Duration), motionWeight);
        }

        public void SetMotionIntent(bool isMoving, float turnDegrees)
        {
            SetMotionIntent(isMoving, turnDegrees, isMoving ? referenceMoveSpeed : 0f);
        }

        public void SetMotionIntent(bool isMoving, float turnDegrees, float moveSpeed)
        {
            SetMotionIntent(isMoving, turnDegrees, moveSpeed, 0f, false);
        }

        public void SetMotionIntent(bool isMoving, float turnDegrees, float moveSpeed, float distanceDelta)
        {
            SetMotionIntent(isMoving, turnDegrees, moveSpeed, distanceDelta, true);
        }

        private void SetMotionIntent(
            bool isMoving,
            float turnDegrees,
            float moveSpeed,
            float distanceDelta,
            bool useDistanceSync)
        {
            moving = isMoving;
            signedTurnDegrees = turnDegrees;
            movementSpeed = moveSpeed;
            if (useDistanceSync)
            {
                distanceThisFrame += Mathf.Max(0f, distanceDelta);
                distanceSyncedThisFrame = true;
                hasDistanceSync = true;
            }

            if (!speedSyncLogged && isMoving && moveSpeed > 0.05f)
            {
                Debug.Log(
                    "MiniBotWalkAnimator: distance-synced gait " +
                    $"moveSpeed={moveSpeed:0.00}, metersPerCycle={metersPerWalkCycle:0.00}.");
                speedSyncLogged = true;
            }

            if (!turnAnimationLogged && isMoving && Mathf.Abs(turnDegrees) > turnThresholdDegrees)
            {
                var direction = turnDegrees < 0f ? "left" : "right";
                var overlaySource = HasFbxTurnClipForDirection(turnDegrees) ? "FBX" : "BVH";
                Debug.Log($"MiniBotWalkAnimator: side-turn animation active direction={direction}, turnDegrees={turnDegrees:0.0}, overlay={overlaySource}, minus2Left={turnDegrees < 0f}.");
                turnAnimationLogged = true;
            }
        }

        public void SamplePose(float sampleTime, bool isMoving)
        {
            moving = isMoving;
            movementSpeed = isMoving ? referenceMoveSpeed : 0f;
            if (walkClip == null)
            {
                return;
            }

            ApplyBvhPose(Mathf.Repeat(sampleTime, walkClip.Duration) / Mathf.Max(0.001f, walkClip.Duration), isMoving ? 1f : 0f);
        }

        public void SampleDistanceSyncedPose(float walkedMeters, bool isMoving, float turnDegrees)
        {
            moving = isMoving;
            signedTurnDegrees = turnDegrees;
            smoothedSignedTurnDegrees = isMoving ? turnDegrees : 0f;
            turnBlend = isMoving
                ? Mathf.Clamp01((Mathf.Abs(turnDegrees) - turnThresholdDegrees) / 65f)
                : 0f;

            if (walkClip == null)
            {
                return;
            }

            var normalizedPhase = Mathf.Repeat(
                walkedMeters / Mathf.Max(0.01f, metersPerWalkCycle),
                1f);
            ApplyBvhPose(normalizedPhase, isMoving ? 1f : 0f);
        }

        private BvhClip SelectTurnOverlayClip()
        {
            if (!moving || turnBlend <= 0.001f)
            {
                return null;
            }

            if (smoothedSignedTurnDegrees < -turnThresholdDegrees && turnLeftClip != null)
            {
                return turnLeftClip;
            }

            if (smoothedSignedTurnDegrees > turnThresholdDegrees && turnRightClip != null)
            {
                return turnRightClip;
            }

            return null;
        }

        private SampledFbxClip SelectFbxTurnOverlayClip()
        {
            if (!moving || turnBlend <= 0.001f)
            {
                return null;
            }

            var isLeft = smoothedSignedTurnDegrees < -turnThresholdDegrees;
            var isRight = smoothedSignedTurnDegrees > turnThresholdDegrees;
            if (!isLeft && !isRight)
            {
                return null;
            }

            var sharpTurn = Mathf.Abs(smoothedSignedTurnDegrees) >= 45f;
            if (isLeft)
            {
                return sharpTurn
                    ? fbxLeftTurnClip ?? fbxHappyLeftTurnClip
                    : fbxHappyLeftTurnClip ?? fbxLeftTurnClip;
            }

            return sharpTurn
                ? fbxRightTurnClip ?? fbxHappyRightTurnClip
                : fbxHappyRightTurnClip ?? fbxRightTurnClip;
        }

        private void ApplyBvhPose(float normalizedPhase, float weight)
        {
            var baseFrame = FrameAtNormalizedPhase(walkClip, normalizedPhase);
            var turnClip = SelectTurnOverlayClip();
            var turnFrame = turnClip != null ? FrameAtNormalizedPhase(turnClip, normalizedPhase) : 0;
            var fbxTurnClip = SelectFbxTurnOverlayClip();
            var fbxTurnPoseWeight = Mathf.Clamp01(turnBlend * fbxTurnClipBlendWeight);
            var bvhTurnPoseWeight = Mathf.Clamp01(turnBlend * turnClipBlendWeight);
            foreach (var boneName in animatedBones)
            {
                if (!bones.TryGetValue(boneName, out var bone) ||
                    !restRotations.TryGetValue(boneName, out var restRotation))
                {
                    continue;
                }

                var bvhJointName = BvhJointForBone(boneName);
                if (!walkClip.TryGetDelta(bvhJointName, baseFrame, out var delta))
                {
                    ApplyFallbackBonePose(boneName, normalizedPhase, weight);
                    continue;
                }

                if (fbxTurnClip != null &&
                    fbxTurnPoseWeight > 0.001f &&
                    fbxTurnClip.TryGetDelta(boneName, normalizedPhase, out var fbxTurnDelta))
                {
                    delta = Quaternion.Slerp(delta, fbxTurnDelta, fbxTurnPoseWeight);
                }
                else if (turnClip != null &&
                    bvhTurnPoseWeight > 0.001f &&
                    turnClip.TryGetDelta(bvhJointName, turnFrame, out var turnDelta))
                {
                    delta = Quaternion.Slerp(delta, turnDelta, bvhTurnPoseWeight);
                }

                var boneWeight = IsLegBone(boneName) ? legWeight : upperBodyWeight;
                bone.localRotation = restRotation * Quaternion.Slerp(Quaternion.identity, delta, weight * boneWeight);
            }
        }

        private void ApplyFallbackBonePose(string boneName, float phase, float weight)
        {
            var stride = Mathf.Sin(phase * Mathf.PI * 2f);
            var oppositeStride = Mathf.Sin(phase * Mathf.PI * 2f + Mathf.PI);
            var footLift = Mathf.Max(0f, stride);
            var oppositeFootLift = Mathf.Max(0f, oppositeStride);
            var turnSign = smoothedSignedTurnDegrees < 0f ? -1f : 1f;
            var sideTurn = Mathf.Clamp01(turnBlend) * sideTurnPoseWeight;
            var poseWeight = weight * visiblePoseWeight;

            switch (boneName)
            {
                case "Hips":
                    SetBonePose("Hips", new Vector3(Mathf.Sin(phase * Mathf.PI * 4f) * 2.5f, turnSign * (7f + sideTurn * 11f), -turnSign * sideTurn * 8f), poseWeight * 0.6f);
                    break;
                case "Spine":
                    SetBonePose("Spine", new Vector3(sideTurn * 2f, turnSign * (5f + sideTurn * 13f), -stride * 3f - turnSign * sideTurn * 7f), poseWeight * 0.45f);
                    break;
                case "Spine1":
                    SetBonePose("Spine1", new Vector3(0f, turnSign * (5f + sideTurn * 10f), oppositeStride * 2f), poseWeight * 0.38f);
                    break;
                case "LeftUpLeg":
                    SetBonePose("LeftUpLeg", new Vector3(stride * 34f + sideTurn * (turnSign < 0f ? 22f : -10f), turnSign * (-7f - sideTurn * 10f), turnSign * sideTurn * 18f), poseWeight);
                    break;
                case "LeftLeg":
                    SetBonePose("LeftLeg", new Vector3(Mathf.Max(0f, -stride) * 42f + sideTurn * (turnSign < 0f ? 20f : 6f), 0f, turnSign * sideTurn * -10f), poseWeight);
                    break;
                case "LeftFoot":
                    SetBonePose("LeftFoot", new Vector3(-stride * 20f + footLift * 10f, turnSign * (-4f - sideTurn * 8f), turnSign * sideTurn * 14f), poseWeight);
                    break;
                case "RightUpLeg":
                    SetBonePose("RightUpLeg", new Vector3(oppositeStride * 34f + sideTurn * (turnSign > 0f ? 22f : -10f), turnSign * (7f + sideTurn * 10f), turnSign * sideTurn * 18f), poseWeight);
                    break;
                case "RightLeg":
                    SetBonePose("RightLeg", new Vector3(Mathf.Max(0f, -oppositeStride) * 42f + sideTurn * (turnSign > 0f ? 20f : 6f), 0f, turnSign * sideTurn * 10f), poseWeight);
                    break;
                case "RightFoot":
                    SetBonePose("RightFoot", new Vector3(-oppositeStride * 20f + oppositeFootLift * 10f, turnSign * (4f + sideTurn * 8f), turnSign * sideTurn * -14f), poseWeight);
                    break;
                case "LeftArm":
                    SetBonePose("LeftArm", new Vector3(oppositeStride * 18f, turnSign * sideTurn * 10f, stride * 6f), poseWeight * 0.35f);
                    break;
                case "LeftForeArm":
                    SetBonePose("LeftForeArm", new Vector3(Mathf.Max(0f, stride) * 16f, 0f, 0f), poseWeight * 0.35f);
                    break;
                case "RightArm":
                    SetBonePose("RightArm", new Vector3(stride * 18f, turnSign * sideTurn * -10f, oppositeStride * 6f), poseWeight * 0.35f);
                    break;
                case "RightForeArm":
                    SetBonePose("RightForeArm", new Vector3(Mathf.Max(0f, oppositeStride) * 16f, 0f, 0f), poseWeight * 0.35f);
                    break;
            }
        }

        private void SetBonePose(string boneName, Vector3 euler, float weight)
        {
            if (!bones.TryGetValue(boneName, out var bone) ||
                !restRotations.TryGetValue(boneName, out var restRotation))
            {
                return;
            }

            bone.localRotation = restRotation *
                Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(euler), Mathf.Clamp01(weight));
        }

        private void RestoreRestPose()
        {
            foreach (var pair in restRotations)
            {
                if (bones.TryGetValue(pair.Key, out var bone))
                {
                    bone.localRotation = Quaternion.Slerp(
                        bone.localRotation,
                        pair.Value,
                        Time.deltaTime * blendSharpness);
                }
            }
        }

        private void RestoreRestPoseImmediate()
        {
            foreach (var pair in restRotations)
            {
                if (bones.TryGetValue(pair.Key, out var bone))
                {
                    bone.localRotation = pair.Value;
                }
            }
        }

        private void CacheBones()
        {
            foreach (var boneName in animatedBones)
            {
                var bone = FindBone(boneName);
                if (bone == null)
                {
                    continue;
                }

                bones[boneName] = bone;
                restRotations[boneName] = bone.localRotation;
            }
        }

        private void DisableImportedIdleAnimation()
        {
            foreach (var animator in GetComponentsInChildren<Animator>())
            {
                animator.applyRootMotion = false;
                animator.enabled = false;
            }

            foreach (var animation in GetComponentsInChildren<Animation>())
            {
                animation.enabled = false;
            }
        }

        private static bool IsLegBone(string boneName)
        {
            return boneName.IndexOf("Leg", StringComparison.Ordinal) >= 0 ||
                boneName.IndexOf("Foot", StringComparison.Ordinal) >= 0 ||
                boneName == "Hips";
        }

        private static string BvhJointForBone(string boneName)
        {
            return BvhJointByMiniBotBone.TryGetValue(boneName, out var jointName)
                ? jointName
                : boneName;
        }

        private int CountMappedTracks(BvhClip clip)
        {
            if (clip == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var boneName in animatedBones)
            {
                if (clip.HasJoint(BvhJointForBone(boneName)))
                {
                    count++;
                }
            }

            return count;
        }

        private static int FrameAtNormalizedPhase(BvhClip clip, float normalizedPhase)
        {
            return Mathf.FloorToInt(Mathf.Repeat(normalizedPhase, 1f) * clip.FrameCount) % clip.FrameCount;
        }

        private static BvhClip LoadClip(string resourcePath)
        {
            if (ClipCache.TryGetValue(resourcePath, out var cached))
            {
                return cached;
            }

            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"MiniBotWalkAnimator: missing BVH resource {resourcePath}.");
                return null;
            }

            var clip = BvhClip.Parse(asset.text);
            ClipCache[resourcePath] = clip;
            return clip;
        }

        private SampledFbxClip LoadSampledFbxClip(string resourcePath, string role)
        {
            var clip = LoadAnimationClip(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"MiniBotWalkAnimator: missing FBX turn resource {resourcePath}.");
                return null;
            }

            var originalPose = TransformPose.Capture(gameObject);
            var sampleCount = Mathf.Max(2, Mathf.CeilToInt(clip.length * Mathf.Max(1, fbxTurnSamplesPerSecond)));
            var sampled = new SampledFbxClip(resourcePath, clip.name, sampleCount, Mathf.Max(clip.length, 0.001f));

            for (var frame = 0; frame < sampleCount; frame++)
            {
                var normalized = sampleCount <= 1 ? 0f : frame / (float)(sampleCount - 1);
                clip.SampleAnimation(gameObject, normalized * clip.length);
                foreach (var boneName in animatedBones)
                {
                    if (!bones.TryGetValue(boneName, out var bone) ||
                        !restRotations.TryGetValue(boneName, out var restRotation))
                    {
                        continue;
                    }

                    sampled.SetDelta(boneName, frame, Quaternion.Inverse(restRotation) * bone.localRotation);
                }
            }

            originalPose.Restore();
            Debug.Log(
                "MiniBotWalkAnimator: loaded FBX turn clip " +
                $"role={role}, resource={resourcePath}, clip={clip.name}, length={clip.length:0.00}, " +
                $"sampledFrames={sampleCount}, mappedMiniBotTracks={sampled.MappedTrackCount}/{animatedBones.Length}, " +
                $"movingMiniBotTracks={sampled.MovingTrackCount}/{animatedBones.Length}.");
            return sampled;
        }

        private static AnimationClip LoadAnimationClip(string resourcePath)
        {
            var clips = Resources.LoadAll<AnimationClip>(resourcePath);
            AnimationClip fallback = null;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = clip;
                }

                if (clip.length > 0.05f && !clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            return fallback;
        }

        private bool HasFbxTurnClips()
        {
            return (fbxRightTurnClip != null || fbxHappyRightTurnClip != null) &&
                (fbxLeftTurnClip != null || fbxHappyLeftTurnClip != null);
        }

        private bool HasFbxTurnClipForDirection(float turnDegrees)
        {
            return turnDegrees < 0f
                ? fbxLeftTurnClip != null || fbxHappyLeftTurnClip != null
                : fbxRightTurnClip != null || fbxHappyRightTurnClip != null;
        }

        private Transform FindBone(string suffix)
        {
            foreach (var child in GetComponentsInChildren<Transform>())
            {
                var childName = child.name;
                if (childName.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private sealed class SampledFbxClip
        {
            private readonly Dictionary<string, Quaternion[]> tracks = new Dictionary<string, Quaternion[]>();

            public SampledFbxClip(string resourcePath, string clipName, int frameCount, float duration)
            {
                ResourcePath = resourcePath;
                ClipName = clipName;
                FrameCount = frameCount;
                Duration = duration;
            }

            public string ResourcePath { get; }
            public string ClipName { get; }
            public int FrameCount { get; }
            public float Duration { get; }
            public int MappedTrackCount => tracks.Count;
            public int MovingTrackCount
            {
                get
                {
                    var count = 0;
                    foreach (var track in tracks.Values)
                    {
                        for (var i = 0; i < track.Length; i++)
                        {
                            if (Quaternion.Angle(Quaternion.identity, track[i]) > 0.25f)
                            {
                                count++;
                                break;
                            }
                        }
                    }

                    return count;
                }
            }

            public void SetDelta(string boneName, int frame, Quaternion delta)
            {
                if (!tracks.TryGetValue(boneName, out var rotations))
                {
                    rotations = new Quaternion[FrameCount];
                    for (var i = 0; i < rotations.Length; i++)
                    {
                        rotations[i] = Quaternion.identity;
                    }

                    tracks[boneName] = rotations;
                }

                rotations[Mathf.Clamp(frame, 0, FrameCount - 1)] = delta;
            }

            public bool TryGetDelta(string boneName, float normalizedPhase, out Quaternion delta)
            {
                if (!tracks.TryGetValue(boneName, out var rotations))
                {
                    delta = Quaternion.identity;
                    return false;
                }

                var frame = Mathf.FloorToInt(Mathf.Repeat(normalizedPhase, 1f) * FrameCount) % FrameCount;
                delta = rotations[frame];
                return true;
            }
        }

        private sealed class TransformPose
        {
            private readonly PoseEntry[] entries;

            private TransformPose(PoseEntry[] entries)
            {
                this.entries = entries;
            }

            public static TransformPose Capture(GameObject root)
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                var entries = new PoseEntry[transforms.Length];
                for (var i = 0; i < transforms.Length; i++)
                {
                    entries[i] = new PoseEntry(transforms[i]);
                }

                return new TransformPose(entries);
            }

            public void Restore()
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    entries[i].Restore();
                }
            }

            private readonly struct PoseEntry
            {
                private readonly Transform transform;
                private readonly Vector3 localPosition;
                private readonly Quaternion localRotation;
                private readonly Vector3 localScale;

                public PoseEntry(Transform transform)
                {
                    this.transform = transform;
                    localPosition = transform.localPosition;
                    localRotation = transform.localRotation;
                    localScale = transform.localScale;
                }

                public void Restore()
                {
                    if (transform == null)
                    {
                        return;
                    }

                    transform.localPosition = localPosition;
                    transform.localRotation = localRotation;
                    transform.localScale = localScale;
                }
            }
        }

        private sealed class BvhClip
        {
            private readonly Dictionary<string, JointTrack> tracks;

            private BvhClip(
                Dictionary<string, JointTrack> tracks,
                int frameCount,
                float frameTime)
            {
                this.tracks = tracks;
                FrameCount = frameCount;
                FrameTime = frameTime;
                Duration = Mathf.Max(FrameTime, FrameCount * FrameTime);
            }

            public int FrameCount { get; }
            public float FrameTime { get; }
            public float Duration { get; }

            public static BvhClip Parse(string text)
            {
                var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var channels = new List<Channel>();
                var motionLine = -1;
                var currentJoint = string.Empty;

                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line == "MOTION")
                    {
                        motionLine = i;
                        break;
                    }

                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                    {
                        continue;
                    }

                    if (parts[0] == "ROOT" || parts[0] == "JOINT")
                    {
                        currentJoint = parts[1];
                    }
                    else if (parts[0] == "CHANNELS")
                    {
                        var count = int.Parse(parts[1], CultureInfo.InvariantCulture);
                        for (var c = 0; c < count; c++)
                        {
                            channels.Add(new Channel(currentJoint, parts[c + 2]));
                        }
                    }
                }

                if (motionLine < 0)
                {
                    throw new FormatException("BVH MOTION section was not found.");
                }

                var frameCount = int.Parse(lines[motionLine + 1].Split(':')[1].Trim(), CultureInfo.InvariantCulture);
                var frameTime = float.Parse(lines[motionLine + 2].Split(':')[1].Trim(), CultureInfo.InvariantCulture);
                var tracks = new Dictionary<string, JointTrack>();

                for (var frame = 0; frame < frameCount; frame++)
                {
                    var values = lines[motionLine + 3 + frame].Split(
                        new[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries);

                    var rotations = new Dictionary<string, Vector3>();
                    for (var channelIndex = 0; channelIndex < channels.Count && channelIndex < values.Length; channelIndex++)
                    {
                        var channel = channels[channelIndex];
                        if (!channel.Name.EndsWith("rotation", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        rotations.TryGetValue(channel.Joint, out var euler);
                        var value = float.Parse(values[channelIndex], CultureInfo.InvariantCulture);
                        if (channel.Name[0] == 'X')
                        {
                            euler.x = value;
                        }
                        else if (channel.Name[0] == 'Y')
                        {
                            euler.y = value;
                        }
                        else if (channel.Name[0] == 'Z')
                        {
                            euler.z = value;
                        }

                        rotations[channel.Joint] = euler;
                    }

                    foreach (var rotation in rotations)
                    {
                        if (!tracks.TryGetValue(rotation.Key, out var track))
                        {
                            track = new JointTrack(frameCount);
                            tracks[rotation.Key] = track;
                        }

                        track.Rotations[frame] = ToBvhRotation(rotation.Value);
                    }
                }

                foreach (var track in tracks.Values)
                {
                    track.ReferenceRotation = track.Rotations[0];
                }

                return new BvhClip(tracks, frameCount, frameTime);
            }

            public bool TryGetDelta(string jointName, int frame, out Quaternion delta)
            {
                if (!tracks.TryGetValue(jointName, out var track))
                {
                    delta = Quaternion.identity;
                    return false;
                }

                delta = track.Rotations[frame] * Quaternion.Inverse(track.ReferenceRotation);
                return true;
            }

            public bool HasJoint(string jointName)
            {
                return tracks.ContainsKey(jointName);
            }

            private static Quaternion ToBvhRotation(Vector3 euler)
            {
                return Quaternion.AngleAxis(euler.z, Vector3.forward) *
                    Quaternion.AngleAxis(euler.y, Vector3.up) *
                    Quaternion.AngleAxis(euler.x, Vector3.right);
            }
        }

        private readonly struct Channel
        {
            public Channel(string joint, string name)
            {
                Joint = joint;
                Name = name;
            }

            public string Joint { get; }
            public string Name { get; }
        }

        private sealed class JointTrack
        {
            public JointTrack(int frameCount)
            {
                Rotations = new Quaternion[frameCount];
                ReferenceRotation = Quaternion.identity;
            }

            public Quaternion[] Rotations { get; }
            public Quaternion ReferenceRotation { get; set; }
        }
    }
}
