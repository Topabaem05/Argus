using System;
using UnityEngine;

namespace ArgusUnity.Motion
{
    public enum MotionIntentType
    {
        None,

        Idle,
        WalkForward,
        WalkBackward,
        StrafeLeft,
        StrafeRight,
        TurnLeft,
        TurnRight,
        TurnAround,
        Run,
        Charge,

        Talk,
        Explain,
        Yell,
        Agree,
        Disagree,
        Wave,

        Think,
        LookAround,

        Excited,
        Sad,
        Surprised,

        Push,
        Pull,
        PickUp,
        ButtonPush,
        Clap,

        StepBackward,
        Dodge,
        HitReaction,
        Fall,
        GetUp
    }

    public enum MotionEmotion
    {
        Neutral = 0,
        Thinking = 1,
        Angry = 2,
        Sad = 3,
        Excited = 4,
        Surprised = 5,
        Confused = 6,
        Scared = 7
    }

    public enum MotionGesture
    {
        None = 0,
        Talk = 1,
        TalkAlt = 2,
        Wave = 3,
        Nod = 4,
        ThoughtfulNod = 5,
        ShakeHeadNo = 6,
        Clap = 7,
        LookAround = 8,
        Yell = 9,
        Think = 10
    }

    public enum MotionAction
    {
        None = 0,
        ButtonPush = 1,
        PickUp = 2,
        Push = 3,
        PullHeavy = 4,
        Dodge = 5,
        Charge = 6,
        HitReaction = 7,
        Fall = 8,
        GetUp = 9,
        StepBackward = 10,
        StopWalking = 11
    }

    public enum MinibotMotionState
    {
        SpawnedIdle = 0,
        MovingToTarget = 1,
        SpeakingIdle = 2,
        SpeakingWhileMoving = 3,
        Thinking = 4,
        Interacting = 5,
        AvoidingObstacle = 6,
        StuckRecovery = 7,
        HitReacting = 8,
        Falling = 9,
        GettingUp = 10,
        Disabled = 11
    }

    [Serializable]
    public readonly struct MotionIntent
    {
        public MotionIntent(
            bool hasMoveTarget,
            Vector3 moveTarget,
            Vector3? focusTarget,
            float desiredSpeedMetersPerSecond,
            float stopDistance,
            MotionEmotion emotion,
            MotionGesture gesture,
            MotionAction action,
            bool allowMovementDuringGesture,
            float urgency)
            : this(
                InferType(hasMoveTarget, desiredSpeedMetersPerSecond, emotion, gesture, action),
                hasMoveTarget,
                moveTarget,
                focusTarget,
                desiredSpeedMetersPerSecond,
                stopDistance,
                emotion,
                gesture,
                action,
                allowMovementDuringGesture,
                urgency,
                MotionClipId.None,
                string.Empty)
        {
        }

        public MotionIntent(
            MotionIntentType type,
            bool hasMoveTarget,
            Vector3 moveTarget,
            Vector3? focusTarget,
            float desiredSpeedMetersPerSecond,
            float stopDistance,
            MotionEmotion emotion,
            MotionGesture gesture,
            MotionAction action,
            bool allowMovementDuringGesture,
            float urgency,
            MotionClipId requestedClip = MotionClipId.None,
            string source = "")
        {
            Type = type;
            HasMoveTarget = hasMoveTarget;
            MoveTarget = moveTarget;
            FocusTarget = focusTarget;
            DesiredSpeedMetersPerSecond = Mathf.Max(0f, desiredSpeedMetersPerSecond);
            StopDistance = Mathf.Max(0.05f, stopDistance);
            Emotion = emotion;
            Gesture = gesture;
            Action = action;
            AllowMovementDuringGesture = allowMovementDuringGesture;
            Urgency = Mathf.Clamp01(urgency);
            RequestedClip = requestedClip;
            Source = source ?? string.Empty;
        }

        public MotionIntentType Type { get; }

        public bool HasMoveTarget { get; }

        public Vector3 MoveTarget { get; }

        public Vector3? FocusTarget { get; }

        public float DesiredSpeedMetersPerSecond { get; }

        public float StopDistance { get; }

        public MotionEmotion Emotion { get; }

        public MotionGesture Gesture { get; }

        public MotionAction Action { get; }

        public bool AllowMovementDuringGesture { get; }

        public float Urgency { get; }

        public MotionClipId RequestedClip { get; }

        public string Source { get; }

        public bool LocksMovement =>
            Action == MotionAction.ButtonPush ||
            Action == MotionAction.PickUp ||
            Action == MotionAction.Push ||
            Action == MotionAction.PullHeavy ||
            Action == MotionAction.HitReaction ||
            Action == MotionAction.Fall ||
            Action == MotionAction.GetUp ||
            (!AllowMovementDuringGesture && Gesture != MotionGesture.None);

        public static MotionIntent Idle(Vector3 position, MotionEmotion emotion = MotionEmotion.Neutral)
        {
            return new MotionIntent(
                MotionIntentType.Idle,
                false,
                position,
                null,
                0f,
                0.12f,
                emotion,
                MotionGesture.None,
                MotionAction.None,
                true,
                0f,
                MotionClipId.None,
                "idle");
        }

        public MotionIntent WithType(MotionIntentType type)
        {
            return new MotionIntent(
                type,
                HasMoveTarget,
                MoveTarget,
                FocusTarget,
                DesiredSpeedMetersPerSecond,
                StopDistance,
                Emotion,
                Gesture,
                Action,
                AllowMovementDuringGesture,
                Urgency,
                RequestedClip,
                Source);
        }

        private static MotionIntentType InferType(
            bool hasMoveTarget,
            float desiredSpeedMetersPerSecond,
            MotionEmotion emotion,
            MotionGesture gesture,
            MotionAction action)
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
            }

            switch (gesture)
            {
                case MotionGesture.Wave:
                    return MotionIntentType.Wave;
                case MotionGesture.Nod:
                case MotionGesture.ThoughtfulNod:
                    return MotionIntentType.Agree;
                case MotionGesture.ShakeHeadNo:
                    return MotionIntentType.Disagree;
                case MotionGesture.Yell:
                    return MotionIntentType.Yell;
                case MotionGesture.LookAround:
                    return MotionIntentType.LookAround;
                case MotionGesture.Think:
                    return MotionIntentType.Think;
                case MotionGesture.Clap:
                    return MotionIntentType.Clap;
                case MotionGesture.Talk:
                case MotionGesture.TalkAlt:
                    return MotionIntentType.Talk;
            }

            if (hasMoveTarget)
            {
                return desiredSpeedMetersPerSecond >= 1f ? MotionIntentType.Run : MotionIntentType.WalkForward;
            }

            switch (emotion)
            {
                case MotionEmotion.Thinking:
                    return MotionIntentType.Think;
                case MotionEmotion.Sad:
                    return MotionIntentType.Sad;
                case MotionEmotion.Excited:
                    return MotionIntentType.Excited;
                case MotionEmotion.Surprised:
                    return MotionIntentType.Surprised;
            }

            return MotionIntentType.Idle;
        }
    }
}
