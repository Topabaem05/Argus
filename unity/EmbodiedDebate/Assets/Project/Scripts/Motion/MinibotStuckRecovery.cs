using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class MinibotStuckRecovery : MonoBehaviour
    {
        [SerializeField]
        private float recoveryDistance = 0.42f;

        [SerializeField]
        private float recoverySpeed = 0.36f;

        [SerializeField]
        private float minimumSecondsBetweenRecoveries = 1.0f;

        private float lastRecoveryTime = -999f;

        public float LastRecoveryTime => lastRecoveryTime;

        public bool CanRecover(float timeSeconds)
        {
            return timeSeconds - lastRecoveryTime >= minimumSecondsBetweenRecoveries;
        }

        public MotionIntent BuildRecoveryIntent(
            Vector3 position,
            Quaternion rotation,
            PersonaMotionProfile profile,
            float timeSeconds)
        {
            lastRecoveryTime = timeSeconds;
            var anxious = profile != null && profile.Anxiety > 0.55f;
            var sideSign = profile != null && ((profile.Seed & 1) == 0) ? -1f : 1f;
            var retreat = -(rotation * Vector3.forward) * recoveryDistance;
            var side = rotation * Vector3.right * recoveryDistance * 0.35f * sideSign;
            var target = position + retreat + (anxious ? side : Vector3.zero);
            target.y = position.y;

            return new MotionIntent(
                anxious ? MotionIntentType.Dodge : MotionIntentType.StepBackward,
                true,
                target,
                null,
                recoverySpeed,
                0.08f,
                MotionEmotion.Confused,
                MotionGesture.LookAround,
                anxious ? MotionAction.Dodge : MotionAction.StepBackward,
                true,
                0.8f,
                anxious ? MotionClipId.Dodging : MotionClipId.StepBackward,
                "stuck_recovery");
        }
    }
}
