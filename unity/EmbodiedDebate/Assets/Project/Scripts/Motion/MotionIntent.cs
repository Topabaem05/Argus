using System;
using UnityEngine;

namespace ArgusUnity.Motion
{
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
        {
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
        }

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

        public bool LocksMovement =>
            Action == MotionAction.ButtonPush ||
            Action == MotionAction.PickUp ||
            Action == MotionAction.Fall ||
            Action == MotionAction.GetUp ||
            (!AllowMovementDuringGesture && Gesture != MotionGesture.None);

        public static MotionIntent Idle(Vector3 position, MotionEmotion emotion = MotionEmotion.Neutral)
        {
            return new MotionIntent(
                false,
                position,
                null,
                0f,
                0.12f,
                emotion,
                MotionGesture.None,
                MotionAction.None,
                true,
                0f);
        }
    }
}
