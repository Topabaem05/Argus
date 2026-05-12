using System;
using System.Collections.Generic;

namespace ArgusUnity.Motion
{
    public static class MotionCatalog
    {
        private static readonly MotionClipDefinition[] Clips =
        {
            Clip(MotionClipId.StandingIdle, "Standing Idle", MotionCategory.Idle, true, true, 1.00f, MotionIntentType.Idle),
            Clip(MotionClipId.BreathingIdle, "Breathing Idle", MotionCategory.Idle, true, true, 1.10f, MotionIntentType.Idle),
            Clip(MotionClipId.Idle2, "Idle-2", MotionCategory.Idle, true, true, 0.95f, MotionIntentType.Idle),
            Clip(MotionClipId.SadIdle, "Sad Idle", MotionCategory.Idle, true, true, 0.70f, MotionIntentType.Sad, MotionIntentType.Idle),
            Clip(MotionClipId.Thinking2, "Thinking-2", MotionCategory.Idle, true, true, 0.95f, MotionIntentType.Think, MotionIntentType.Idle),
            Clip(MotionClipId.LookAround, "Look Around", MotionCategory.Idle, false, true, 0.85f, MotionIntentType.LookAround, MotionIntentType.Think, MotionIntentType.Idle),
            Clip(MotionClipId.ThoughtfulHeadNod, "Thoughtful Head Nod", MotionCategory.Talk, false, true, 0.80f, MotionIntentType.Agree, MotionIntentType.Think, MotionIntentType.Talk),
            Clip(MotionClipId.HardHeadNod, "Hard Head Nod", MotionCategory.Talk, false, true, 0.70f, MotionIntentType.Agree, MotionIntentType.Talk),

            Clip(MotionClipId.Walking3, "Walking-3", MotionCategory.Locomotion, true, false, 1.15f, MotionIntentType.WalkForward),
            Clip(MotionClipId.StepWalking, "Step Walking", MotionCategory.Locomotion, true, false, 0.90f, MotionIntentType.WalkForward),
            Clip(MotionClipId.Running2, "Running-2", MotionCategory.Locomotion, true, false, 1.00f, MotionIntentType.Run),
            Clip(MotionClipId.Charge, "Charge", MotionCategory.Locomotion, true, false, 0.80f, MotionIntentType.Charge, MotionIntentType.Run),
            Clip(MotionClipId.WalkingBackward, "Walking Backward", MotionCategory.Locomotion, true, false, 1.00f, MotionIntentType.WalkBackward),
            Clip(MotionClipId.LeftStrafeWalking, "Left Strafe Walking", MotionCategory.Locomotion, true, false, 1.00f, MotionIntentType.StrafeLeft),
            Clip(MotionClipId.RightStrafeWalking, "Right Strafe Walking", MotionCategory.Locomotion, true, false, 1.00f, MotionIntentType.StrafeRight),
            Clip(MotionClipId.WalkBackwardArcLeft, "Walk Backward Arc Left", MotionCategory.Locomotion, true, false, 0.80f, MotionIntentType.WalkBackward, MotionIntentType.TurnLeft),
            Clip(MotionClipId.WalkBackwardArcRight, "Walk Backward Arc Right", MotionCategory.Locomotion, true, false, 0.80f, MotionIntentType.WalkBackward, MotionIntentType.TurnRight),
            Clip(MotionClipId.StopWalking, "Stop Walking", MotionCategory.Locomotion, false, false, 0.55f, MotionIntentType.Idle),

            Clip(MotionClipId.LeftTurn, "Left Turn", MotionCategory.Turn, false, false, 1.00f, MotionIntentType.TurnLeft),
            Clip(MotionClipId.LeftTurn2, "Left Turn-2", MotionCategory.Turn, false, false, 0.85f, MotionIntentType.TurnLeft),
            Clip(MotionClipId.RightTurn, "Right Turn", MotionCategory.Turn, false, false, 1.00f, MotionIntentType.TurnRight),
            Clip(MotionClipId.RightTurn180, "Right Turn-180turn", MotionCategory.Turn, false, false, 1.00f, MotionIntentType.TurnRight, MotionIntentType.TurnAround),
            Clip(MotionClipId.TurnAroundBriefcase, "180 Turn W_ Briefcase", MotionCategory.Turn, false, false, 0.80f, MotionIntentType.TurnAround),

            Clip(MotionClipId.Talking, "Talking", MotionCategory.Talk, false, true, 1.10f, MotionIntentType.Talk, MotionIntentType.Explain),
            Clip(MotionClipId.Talking2, "Talking-2", MotionCategory.Talk, false, true, 1.00f, MotionIntentType.Talk, MotionIntentType.Explain),
            Clip(MotionClipId.Yelling, "Yelling", MotionCategory.Talk, false, true, 0.85f, MotionIntentType.Yell),
            Clip(MotionClipId.Waving, "Waving", MotionCategory.Talk, false, true, 0.90f, MotionIntentType.Wave, MotionIntentType.Talk),
            Clip(MotionClipId.ShakingHeadNo, "Shaking Head No", MotionCategory.Talk, false, true, 1.05f, MotionIntentType.Disagree),

            Clip(MotionClipId.Excited, "Excited", MotionCategory.Emotion, false, true, 1.00f, MotionIntentType.Excited),
            Clip(MotionClipId.Surprised, "Surprised", MotionCategory.Emotion, false, true, 1.00f, MotionIntentType.Surprised),
            Clip(MotionClipId.ZombieReactionHit, "Zombie Reaction Hit", MotionCategory.HitReaction, false, false, 1.00f, MotionIntentType.HitReaction),

            Clip(MotionClipId.ButtonPushing, "Button Pushing", MotionCategory.Interaction, false, true, 1.00f, MotionIntentType.ButtonPush),
            Clip(MotionClipId.PickingUp, "Picking Up", MotionCategory.Interaction, false, false, 1.00f, MotionIntentType.PickUp),
            Clip(MotionClipId.PullHeavyObject, "Pull Heavy Object", MotionCategory.Interaction, false, false, 1.00f, MotionIntentType.Pull),
            Clip(MotionClipId.Push, "Push", MotionCategory.Interaction, false, false, 1.00f, MotionIntentType.Push),
            Clip(MotionClipId.Clapping, "Clapping", MotionCategory.Celebration, false, true, 1.00f, MotionIntentType.Clap, MotionIntentType.Excited),

            Clip(MotionClipId.StepBackward, "Step Backward", MotionCategory.Recovery, false, false, 1.10f, MotionIntentType.StepBackward),
            Clip(MotionClipId.Dodging, "Dodging", MotionCategory.Recovery, false, false, 0.95f, MotionIntentType.Dodge),
            Clip(MotionClipId.FallingFlatImpact, "Falling Flat Impact", MotionCategory.Recovery, false, false, 1.00f, MotionIntentType.Fall),
            Clip(MotionClipId.GettingUp, "Getting Up", MotionCategory.Recovery, false, false, 1.00f, MotionIntentType.GetUp)
        };

        public static IReadOnlyList<MotionClipDefinition> All => Clips;

        public static MotionClipDefinition[] GetCandidates(MotionIntentType intentType)
        {
            var results = new List<MotionClipDefinition>();
            for (var i = 0; i < Clips.Length; i++)
            {
                if (Clips[i].Supports(intentType))
                {
                    results.Add(Clips[i]);
                }
            }

            if (results.Count == 0 && intentType != MotionIntentType.Idle)
            {
                return GetCandidates(MotionIntentType.Idle);
            }

            return results.ToArray();
        }

        public static MotionClipDefinition[] GetByCategory(MotionCategory category)
        {
            var results = new List<MotionClipDefinition>();
            for (var i = 0; i < Clips.Length; i++)
            {
                if (Clips[i].Category == category)
                {
                    results.Add(Clips[i]);
                }
            }

            return results.ToArray();
        }

        public static bool TryGet(MotionClipId id, out MotionClipDefinition clip)
        {
            for (var i = 0; i < Clips.Length; i++)
            {
                if (Clips[i].ClipId == id)
                {
                    clip = Clips[i];
                    return true;
                }
            }

            clip = default;
            return false;
        }

        public static MotionIntentType TypeForGesture(MotionGesture gesture)
        {
            switch (gesture)
            {
                case MotionGesture.Talk:
                case MotionGesture.TalkAlt:
                    return MotionIntentType.Talk;
                case MotionGesture.Wave:
                    return MotionIntentType.Wave;
                case MotionGesture.Nod:
                case MotionGesture.ThoughtfulNod:
                    return MotionIntentType.Agree;
                case MotionGesture.ShakeHeadNo:
                    return MotionIntentType.Disagree;
                case MotionGesture.Clap:
                    return MotionIntentType.Clap;
                case MotionGesture.LookAround:
                    return MotionIntentType.LookAround;
                case MotionGesture.Yell:
                    return MotionIntentType.Yell;
                case MotionGesture.Think:
                    return MotionIntentType.Think;
                default:
                    return MotionIntentType.None;
            }
        }

        public static MotionIntentType TypeForAction(MotionAction action)
        {
            switch (action)
            {
                case MotionAction.ButtonPush:
                    return MotionIntentType.ButtonPush;
                case MotionAction.PickUp:
                    return MotionIntentType.PickUp;
                case MotionAction.Push:
                    return MotionIntentType.Push;
                case MotionAction.PullHeavy:
                    return MotionIntentType.Pull;
                case MotionAction.Dodge:
                    return MotionIntentType.Dodge;
                case MotionAction.Charge:
                    return MotionIntentType.Charge;
                case MotionAction.HitReaction:
                    return MotionIntentType.HitReaction;
                case MotionAction.Fall:
                    return MotionIntentType.Fall;
                case MotionAction.GetUp:
                    return MotionIntentType.GetUp;
                case MotionAction.StepBackward:
                    return MotionIntentType.StepBackward;
                default:
                    return MotionIntentType.None;
            }
        }

        private static MotionClipDefinition Clip(
            MotionClipId clipId,
            string clipName,
            MotionCategory category,
            bool loopTime,
            bool overlayPreferred,
            float baseWeight,
            params MotionIntentType[] intents)
        {
            return new MotionClipDefinition(
                clipId,
                clipName,
                category,
                loopTime,
                overlayPreferred,
                baseWeight,
                intents);
        }
    }

    [Serializable]
    public readonly struct MotionClipDefinition
    {
        public MotionClipDefinition(
            MotionClipId clipId,
            string clipName,
            MotionCategory category,
            bool loopTime,
            bool overlayPreferred,
            float baseWeight,
            MotionIntentType[] intents)
        {
            ClipId = clipId;
            ClipName = clipName;
            Category = category;
            LoopTime = loopTime;
            OverlayPreferred = overlayPreferred;
            BaseWeight = baseWeight;
            Intents = intents ?? Array.Empty<MotionIntentType>();
        }

        public MotionClipId ClipId { get; }

        public string ClipName { get; }

        public MotionCategory Category { get; }

        public bool LoopTime { get; }

        public bool OverlayPreferred { get; }

        public float BaseWeight { get; }

        public MotionIntentType[] Intents { get; }

        public bool Supports(MotionIntentType intent)
        {
            for (var i = 0; i < Intents.Length; i++)
            {
                if (Intents[i] == intent)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
