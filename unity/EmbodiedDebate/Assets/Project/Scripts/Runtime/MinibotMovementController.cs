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
                previousPosition = transform.position;
                previousYaw = transform.eulerAngles.y;
                previousSampleTime = sampleTime;
                hasPreviousPose = true;
            }

            var delta = targetPosition - previousPosition;
            delta.y = 0f;
            var dt = Mathf.Max(1f / 30f, sampleTime - previousSampleTime);
            LastPlanarSpeed = Mathf.Clamp(delta.magnitude / dt, 0f, Mathf.Max(0.1f, maxSpeedMetersPerSecond) * 1.65f);
            LastSignedTurnDegrees = LocomotionMath.SignedYawDelta(previousYaw, targetRotation.eulerAngles.y);

            body.MovePosition(targetPosition);
            body.MoveRotation(targetRotation);

            // The capture path samples exact scenario times in LateUpdate, immediately before rendering.
            // Keep the transform synchronized so the frame shows the sampled kinematic pose.
            transform.SetPositionAndRotation(targetPosition, targetRotation);

            previousPosition = targetPosition;
            previousYaw = targetRotation.eulerAngles.y;
            previousSampleTime = sampleTime;
        }

        public void ResetTracking()
        {
            hasPreviousPose = false;
            LastPlanarSpeed = 0f;
            LastSignedTurnDegrees = 0f;
            LastMovementTarget = transform.position;
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
