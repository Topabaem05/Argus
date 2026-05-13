using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class MotionSelectionPolicy
    {
        private const float ClipCooldownSeconds = 1.1f;
        private const float CategoryCooldownSeconds = 0.35f;

        private readonly Dictionary<MotionClipId, float> clipAvailableAt = new Dictionary<MotionClipId, float>();
        private readonly Dictionary<MotionCategory, float> categoryAvailableAt = new Dictionary<MotionCategory, float>();
        private readonly List<MotionClipId> recentClips = new List<MotionClipId>(5);
        private System.Random random = new System.Random(1);
        private MotionClipId lastBaseClip;
        private MotionClipId lastOverlayClip;
        private MotionClipId lastEmotionClip;

        public IReadOnlyList<MotionClipId> RecentClips => recentClips;

        public void Initialize(int deterministicSeed)
        {
            random = new System.Random(deterministicSeed == 0 ? 1 : deterministicSeed);
            clipAvailableAt.Clear();
            categoryAvailableAt.Clear();
            recentClips.Clear();
            lastBaseClip = MotionClipId.None;
            lastOverlayClip = MotionClipId.None;
            lastEmotionClip = MotionClipId.None;
        }

        public MotionSelection Select(MotionIntent intent, PersonaMotionProfile profile, float timeSeconds)
        {
            profile ??= PersonaMotionProfile.FromAgentId(string.Empty, 1);

            var baseIntent = ResolveBaseIntent(intent);
            var overlayIntent = ResolveOverlayIntent(intent);
            var emotionIntent = ResolveEmotionIntent(intent);

            var baseClip = SelectClip(baseIntent, profile, timeSeconds, lastBaseClip);
            var overlayClip = overlayIntent == MotionIntentType.None
                ? MotionClipId.None
                : SelectClip(overlayIntent, profile, timeSeconds, lastOverlayClip);
            var emotionClip = emotionIntent == MotionIntentType.None
                ? MotionClipId.None
                : SelectClip(emotionIntent, profile, timeSeconds, lastEmotionClip);

            lastBaseClip = baseClip;
            lastOverlayClip = overlayClip;
            lastEmotionClip = emotionClip;

            MarkUsed(baseClip, timeSeconds);
            MarkUsed(overlayClip, timeSeconds);
            MarkUsed(emotionClip, timeSeconds);

            return new MotionSelection(baseClip, overlayClip, emotionClip, recentClips);
        }

        public MotionClipId SelectClip(
            MotionIntentType intentType,
            PersonaMotionProfile profile,
            float timeSeconds,
            MotionClipId previousClip = MotionClipId.None)
        {
            if (intentType == MotionIntentType.None)
            {
                return MotionClipId.None;
            }

            var candidates = MotionCatalog.GetCandidates(intentType);
            if (candidates.Length == 0)
            {
                return MotionClipId.None;
            }

            profile ??= PersonaMotionProfile.FromAgentId(string.Empty, 1);
            var totalWeight = 0f;
            var weights = new float[candidates.Length];
            for (var i = 0; i < candidates.Length; i++)
            {
                var weight = ScoreCandidate(candidates[i], profile, timeSeconds, previousClip);
                weights[i] = weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0.0001f)
            {
                return candidates[0].ClipId;
            }

            var pick = (float)random.NextDouble() * totalWeight;
            for (var i = 0; i < candidates.Length; i++)
            {
                pick -= weights[i];
                if (pick <= 0f)
                {
                    return candidates[i].ClipId;
                }
            }

            return candidates[candidates.Length - 1].ClipId;
        }

        public float ScoreCandidate(
            MotionClipDefinition candidate,
            PersonaMotionProfile profile,
            float timeSeconds,
            MotionClipId previousClip)
        {
            var weight = Mathf.Max(0.01f, candidate.BaseWeight) *
                         Mathf.Max(0.01f, profile?.WeightFor(candidate.ClipId) ?? 1f);

            if (candidate.ClipId == previousClip && HasAlternative(candidate, timeSeconds))
            {
                return 0f;
            }

            if (clipAvailableAt.TryGetValue(candidate.ClipId, out var clipReadyAt) && timeSeconds < clipReadyAt)
            {
                weight *= 0.10f;
            }

            if (categoryAvailableAt.TryGetValue(candidate.Category, out var categoryReadyAt) &&
                timeSeconds < categoryReadyAt)
            {
                weight *= 0.45f;
            }

            for (var i = 0; i < recentClips.Count; i++)
            {
                if (recentClips[i] == candidate.ClipId)
                {
                    weight *= 0.35f;
                    break;
                }
            }

            return weight;
        }

        private static MotionIntentType ResolveBaseIntent(MotionIntent intent)
        {
            var isMoving = IsMovingIntent(intent);
            var actionType = MotionCatalog.TypeForAction(intent.Action);
            switch (actionType)
            {
                case MotionIntentType.StepBackward:
                case MotionIntentType.Dodge:
                case MotionIntentType.Charge:
                    return actionType;
                case MotionIntentType.HitReaction:
                case MotionIntentType.Fall:
                case MotionIntentType.GetUp:
                case MotionIntentType.ButtonPush:
                case MotionIntentType.PickUp:
                case MotionIntentType.Push:
                case MotionIntentType.Pull:
                    if (isMoving)
                    {
                        break;
                    }

                    return actionType;
            }

            if (intent.Type == MotionIntentType.TurnLeft ||
                intent.Type == MotionIntentType.TurnRight ||
                intent.Type == MotionIntentType.TurnAround)
            {
                return intent.Type;
            }

            if (intent.HasMoveTarget && intent.DesiredSpeedMetersPerSecond > 0.01f)
            {
                if (intent.DesiredSpeedMetersPerSecond >= 1.0f || intent.Type == MotionIntentType.Run)
                {
                    return MotionIntentType.Run;
                }

                return intent.Type == MotionIntentType.WalkBackward
                    ? MotionIntentType.WalkBackward
                    : MotionIntentType.WalkForward;
            }

            return intent.Type == MotionIntentType.Sad ? MotionIntentType.Sad : MotionIntentType.Idle;
        }

        private static MotionIntentType ResolveOverlayIntent(MotionIntent intent)
        {
            if (IsMovingIntent(intent))
            {
                return MotionIntentType.None;
            }

            var gestureIntent = MotionCatalog.TypeForGesture(intent.Gesture);
            if (gestureIntent != MotionIntentType.None)
            {
                return gestureIntent;
            }

            switch (intent.Type)
            {
                case MotionIntentType.Talk:
                case MotionIntentType.Explain:
                case MotionIntentType.Yell:
                case MotionIntentType.Agree:
                case MotionIntentType.Disagree:
                case MotionIntentType.Wave:
                case MotionIntentType.Clap:
                case MotionIntentType.Think:
                case MotionIntentType.LookAround:
                    return intent.Type;
                default:
                    return MotionIntentType.None;
            }
        }

        private static MotionIntentType ResolveEmotionIntent(MotionIntent intent)
        {
            if (IsMovingIntent(intent))
            {
                return MotionIntentType.None;
            }

            switch (intent.Emotion)
            {
                case MotionEmotion.Excited:
                    return MotionIntentType.Excited;
                case MotionEmotion.Sad:
                    return MotionIntentType.Sad;
                case MotionEmotion.Surprised:
                case MotionEmotion.Confused:
                case MotionEmotion.Scared:
                    return MotionIntentType.Surprised;
                default:
                    return MotionIntentType.None;
            }
        }

        private static bool IsMovingIntent(MotionIntent intent)
        {
            return intent.HasMoveTarget && intent.DesiredSpeedMetersPerSecond > 0.01f;
        }

        private bool HasAlternative(MotionClipDefinition candidate, float timeSeconds)
        {
            var candidates = MotionCatalog.GetCandidates(candidate.Intents.Length > 0
                ? candidate.Intents[0]
                : MotionIntentType.Idle);
            var available = 0;
            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].ClipId != candidate.ClipId &&
                    (!clipAvailableAt.TryGetValue(candidates[i].ClipId, out var readyAt) || timeSeconds >= readyAt))
                {
                    available++;
                }
            }

            return available > 0;
        }

        private void MarkUsed(MotionClipId clipId, float timeSeconds)
        {
            if (clipId == MotionClipId.None || !MotionCatalog.TryGet(clipId, out var clip))
            {
                return;
            }

            clipAvailableAt[clipId] = timeSeconds + ClipCooldownSeconds;
            categoryAvailableAt[clip.Category] = timeSeconds + CategoryCooldownSeconds;
            recentClips.Remove(clipId);
            recentClips.Insert(0, clipId);
            while (recentClips.Count > 5)
            {
                recentClips.RemoveAt(recentClips.Count - 1);
            }
        }
    }

    public readonly struct MotionSelection
    {
        public MotionSelection(
            MotionClipId baseClip,
            MotionClipId overlayClip,
            MotionClipId emotionClip,
            IReadOnlyList<MotionClipId> recentClips)
        {
            BaseClip = baseClip;
            OverlayClip = overlayClip;
            EmotionClip = emotionClip;
            RecentClips = recentClips != null ? CopyRecent(recentClips) : Array.Empty<MotionClipId>();
        }

        public MotionClipId BaseClip { get; }

        public MotionClipId OverlayClip { get; }

        public MotionClipId EmotionClip { get; }

        public MotionClipId[] RecentClips { get; }

        private static MotionClipId[] CopyRecent(IReadOnlyList<MotionClipId> recentClips)
        {
            var copy = new MotionClipId[recentClips.Count];
            for (var i = 0; i < recentClips.Count; i++)
            {
                copy[i] = recentClips[i];
            }

            return copy;
        }
    }
}
