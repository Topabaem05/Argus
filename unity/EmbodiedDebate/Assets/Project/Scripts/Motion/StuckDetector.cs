using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class StuckDetector : MonoBehaviour
    {
        [SerializeField]
        private float minimumStateSeconds = 0.65f;

        [SerializeField]
        private float displacementWindowSeconds = 0.75f;

        [SerializeField]
        private float minimumWindowDisplacement = 0.08f;

        private float stateTime;
        private float windowTime;
        private float windowDisplacement;

        public bool IsStuck { get; private set; }

        public void ResetDetector()
        {
            IsStuck = false;
            stateTime = 0f;
            windowTime = 0f;
            windowDisplacement = 0f;
        }

        public void Tick(SmoothRigidbodyMotor motor, MotionIntent intent, float deltaTime)
        {
            if (motor == null || deltaTime <= 0f)
            {
                return;
            }

            var shouldMonitor = intent.HasMoveTarget &&
                                motor.DistanceToTarget > intent.StopDistance + 0.25f &&
                                intent.DesiredSpeedMetersPerSecond > 0.2f;
            if (!shouldMonitor)
            {
                ResetDetector();
                return;
            }

            stateTime += deltaTime;
            windowTime += deltaTime;
            windowDisplacement += motor.LastActualDisplacement;
            if (windowTime < displacementWindowSeconds)
            {
                return;
            }

            IsStuck = stateTime >= minimumStateSeconds &&
                      windowDisplacement < minimumWindowDisplacement;
            windowTime = 0f;
            windowDisplacement = 0f;
        }
    }
}
