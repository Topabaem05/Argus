using UnityEngine;

namespace ArgusUnity.Runtime
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class MinibotMovementController : MonoBehaviour
    {
        [SerializeField]
        private float colliderHeight = 0.95f;

        [SerializeField]
        private float colliderRadius = 0.22f;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private bool hasPreviousPose;
        private Vector3 previousPosition;
        private float previousYaw;
        private float previousSampleTime;

        public float LastPlanarSpeed { get; private set; }
        public float LastSignedTurnDegrees { get; private set; }
        public Vector3 LastMovementTarget { get; private set; }
        public Vector3 LastAppliedPosition { get; private set; }
        public float LastActualStepMeters { get; private set; }
        public float LastAllowedStepMeters { get; private set; }
        public float LastSpeedLimitMetersPerSecond { get; private set; }
        public bool LastSpeedLimitExceeded { get; private set; }

        private void Awake()
        {
            ConfigureBody();
        }

        public void ApplyKinematicPose(
            Vector3 targetPosition,
            Vector3 facingDirection,
            float sampleTime,
            float maxSpeedMetersPerSecond)
        {
            ConfigureBody();
            LastMovementTarget = targetPosition;

            var targetRotation = transform.rotation;
            facingDirection.y = 0f;
            if (facingDirection.sqrMagnitude > 0.001f)
            {
                targetRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
            }

            if (!hasPreviousPose)
            {
                LastAppliedPosition = targetPosition;
                previousPosition = targetPosition;
                previousYaw = targetRotation.eulerAngles.y;
                previousSampleTime = sampleTime;
                hasPreviousPose = true;
                LastPlanarSpeed = 0f;
                LastSignedTurnDegrees = 0f;
                LastActualStepMeters = 0f;
                LastAllowedStepMeters = 0f;
                LastSpeedLimitMetersPerSecond = Mathf.Max(0.1f, maxSpeedMetersPerSecond);
                LastSpeedLimitExceeded = false;

                body.MovePosition(targetPosition);
                body.MoveRotation(targetRotation);
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            var delta = targetPosition - previousPosition;
            delta.y = 0f;
            var dt = Mathf.Max(1f / 30f, sampleTime - previousSampleTime);
            var speedLimit = Mathf.Max(0.1f, maxSpeedMetersPerSecond);
            var allowedStep = speedLimit * dt;
            var requestedStep = delta.magnitude;
            var planarPosition = previousPosition;
            if (requestedStep > 0.0001f)
            {
                var appliedStep = Mathf.Min(requestedStep, allowedStep);
                planarPosition += delta.normalized * appliedStep;
            }

            var appliedPosition = new Vector3(planarPosition.x, targetPosition.y, planarPosition.z);
            var appliedDelta = appliedPosition - previousPosition;
            appliedDelta.y = 0f;

            LastAppliedPosition = appliedPosition;
            LastActualStepMeters = appliedDelta.magnitude;
            LastAllowedStepMeters = allowedStep;
            LastSpeedLimitMetersPerSecond = speedLimit;
            LastSpeedLimitExceeded = requestedStep > allowedStep * 1.2f;
            LastPlanarSpeed = LastActualStepMeters / dt;
            LastSignedTurnDegrees = LocomotionMath.SignedYawDelta(previousYaw, targetRotation.eulerAngles.y);

            body.MovePosition(appliedPosition);
            body.MoveRotation(targetRotation);

            // The capture path samples exact scenario times in LateUpdate, immediately before rendering.
            // Keep the transform synchronized so the frame shows the speed-limited kinematic pose.
            transform.SetPositionAndRotation(appliedPosition, targetRotation);

            previousPosition = appliedPosition;
            previousYaw = targetRotation.eulerAngles.y;
            previousSampleTime = sampleTime;
        }

        public void ResetTracking()
        {
            hasPreviousPose = false;
            LastPlanarSpeed = 0f;
            LastSignedTurnDegrees = 0f;
            LastMovementTarget = transform.position;
            LastAppliedPosition = transform.position;
            LastActualStepMeters = 0f;
            LastAllowedStepMeters = 0f;
            LastSpeedLimitMetersPerSecond = 0f;
            LastSpeedLimitExceeded = false;
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider>();
            }

            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            capsule.height = Mathf.Max(0.2f, colliderHeight);
            capsule.radius = Mathf.Max(0.05f, colliderRadius);
            capsule.center = Vector3.up * (capsule.height * 0.5f);
        }
    }
}
