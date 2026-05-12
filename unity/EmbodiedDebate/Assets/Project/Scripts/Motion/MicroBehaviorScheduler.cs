using System;
using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class MicroBehaviorScheduler : MonoBehaviour
    {
        private System.Random random = new System.Random(1);
        private MotionGesture lastGesture = MotionGesture.None;
        private float nextIdleGestureSeconds = 4f;
        private float elapsedIdleSeconds;

        public void Initialize(int deterministicSeed)
        {
            random = new System.Random(deterministicSeed == 0 ? 1 : deterministicSeed);
            lastGesture = MotionGesture.None;
            elapsedIdleSeconds = 0f;
            nextIdleGestureSeconds = NextInterval();
        }

        public MotionIntent BuildIdleIntent(Vector3 position, MotionEmotion emotion = MotionEmotion.Neutral)
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

        public MotionIntent TickIdle(Vector3 position, MotionEmotion emotion, float deltaTime)
        {
            elapsedIdleSeconds += Mathf.Max(0f, deltaTime);
            if (elapsedIdleSeconds < nextIdleGestureSeconds)
            {
                return BuildIdleIntent(position, emotion);
            }

            elapsedIdleSeconds = 0f;
            nextIdleGestureSeconds = NextInterval();
            var gesture = PickGesture(emotion);
            lastGesture = gesture;
            return new MotionIntent(
                false,
                position,
                null,
                0f,
                0.12f,
                emotion,
                gesture,
                MotionAction.None,
                true,
                0.2f);
        }

        public MotionIntent BuildStuckRecoveryIntent(Vector3 position, Quaternion rotation)
        {
            var back = position - rotation * Vector3.forward * 0.35f;
            return new MotionIntent(
                true,
                back,
                null,
                0.35f,
                0.1f,
                MotionEmotion.Confused,
                MotionGesture.None,
                MotionAction.StepBackward,
                true,
                0.7f);
        }

        private MotionGesture PickGesture(MotionEmotion emotion)
        {
            var primary = emotion switch
            {
                MotionEmotion.Thinking => MotionGesture.ThoughtfulNod,
                MotionEmotion.Angry => MotionGesture.Yell,
                MotionEmotion.Sad => MotionGesture.LookAround,
                MotionEmotion.Excited => MotionGesture.Clap,
                MotionEmotion.Surprised => MotionGesture.LookAround,
                MotionEmotion.Confused => MotionGesture.ShakeHeadNo,
                _ => MotionGesture.LookAround
            };

            if (primary == lastGesture)
            {
                return MotionGesture.LookAround;
            }

            return primary;
        }

        private float NextInterval()
        {
            return 4f + (float)random.NextDouble() * 5f;
        }
    }
}
