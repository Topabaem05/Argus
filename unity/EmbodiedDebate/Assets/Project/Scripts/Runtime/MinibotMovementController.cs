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

        [SerializeField]
        private bool alignFacingToMovement = true;

        [SerializeField]
        private float movementFacingThresholdMeters = 0.002f;

        [SerializeField]
        private float collisionSkinMeters = 0.03f;

        [SerializeField]
        private float pushableMassLimit = 80f;

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
        public Vector3 LastRequestedFacingDirection { get; private set; }
        public Vector3 LastAppliedFacingDirection { get; private set; }
        public float LastHeadingAlignmentDegrees { get; private set; }
        public bool LastFacingAlignedToMovement { get; private set; }
        public Rigidbody LastPushedRigidbody { get; private set; }
        public float LastPushedDistanceMeters { get; private set; }
        public bool LastBlockedByStaticCollider { get; private set; }
        public string LastCollisionName { get; private set; } = string.Empty;

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

            var requestedFacing = ResolvePlanarDirection(facingDirection, transform.forward);
            LastRequestedFacingDirection = requestedFacing;
            LastAppliedFacingDirection = requestedFacing;
            LastFacingAlignedToMovement = false;
            LastHeadingAlignmentDegrees = 0f;

            if (!hasPreviousPose)
            {
                var initialRotation = Quaternion.LookRotation(requestedFacing, Vector3.up);
                LastAppliedPosition = targetPosition;
                previousPosition = targetPosition;
                previousYaw = initialRotation.eulerAngles.y;
                previousSampleTime = sampleTime;
                hasPreviousPose = true;
                LastPlanarSpeed = 0f;
                LastSignedTurnDegrees = 0f;
                LastActualStepMeters = 0f;
                LastAllowedStepMeters = 0f;
                LastSpeedLimitMetersPerSecond = Mathf.Max(0.1f, maxSpeedMetersPerSecond);
                LastSpeedLimitExceeded = false;

                body.MovePosition(targetPosition);
                body.MoveRotation(initialRotation);
                transform.SetPositionAndRotation(targetPosition, initialRotation);
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
            appliedPosition = ResolveCollisionAndPush(previousPosition, appliedPosition);
            var appliedDelta = appliedPosition - previousPosition;
            appliedDelta.y = 0f;
            var appliedFacing = requestedFacing;
            if (alignFacingToMovement && appliedDelta.magnitude > Mathf.Max(0.0001f, movementFacingThresholdMeters))
            {
                appliedFacing = appliedDelta.normalized;
                LastFacingAlignedToMovement = true;
            }

            var targetRotation = Quaternion.LookRotation(appliedFacing, Vector3.up);
            LastAppliedFacingDirection = appliedFacing;
            LastHeadingAlignmentDegrees = ResolveHeadingAlignmentDegrees(appliedDelta, appliedFacing);

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
            LastRequestedFacingDirection = transform.forward;
            LastAppliedFacingDirection = transform.forward;
            LastHeadingAlignmentDegrees = 0f;
            LastFacingAlignedToMovement = false;
            LastPushedRigidbody = null;
            LastPushedDistanceMeters = 0f;
            LastBlockedByStaticCollider = false;
            LastCollisionName = string.Empty;
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
            body.mass = 2.5f;
            body.detectCollisions = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            capsule.height = Mathf.Max(0.2f, colliderHeight);
            capsule.radius = Mathf.Max(0.05f, colliderRadius);
            capsule.center = Vector3.up * (capsule.height * 0.5f);
        }

        private Vector3 ResolveCollisionAndPush(Vector3 from, Vector3 to)
        {
            LastPushedRigidbody = null;
            LastPushedDistanceMeters = 0f;
            LastBlockedByStaticCollider = false;
            LastCollisionName = string.Empty;

            var planarDelta = to - from;
            planarDelta.y = 0f;
            var distance = planarDelta.magnitude;
            if (distance <= 0.0001f)
            {
                return to;
            }

            Physics.SyncTransforms();
            var direction = planarDelta / distance;
            GetCapsuleWorldPoints(from, out var bottom, out var top);
            var hits = Physics.CapsuleCastAll(
                bottom,
                top,
                Mathf.Max(0.01f, capsule.radius - collisionSkinMeters * 0.5f),
                direction,
                distance + collisionSkinMeters,
                ~0,
                QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return to;
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (IsGroundSupportHit(hit.collider, from))
                {
                    continue;
                }

                var hitBody = hit.rigidbody;
                LastCollisionName = hit.collider.name;
                if (CanPush(hitBody))
                {
                    PushBody(hitBody, direction * distance);
                    LastPushedRigidbody = hitBody;
                    LastPushedDistanceMeters = distance;
                    LastCollisionName = hitBody.name;
                    return to;
                }

                LastBlockedByStaticCollider = true;
                var stopDistance = Mathf.Max(0f, hit.distance - collisionSkinMeters);
                return from + direction * Mathf.Min(distance, stopDistance);
            }

            return to;
        }

        private static bool IsGroundSupportHit(Collider hitCollider, Vector3 rootPosition)
        {
            return hitCollider.bounds.max.y <= rootPosition.y + 0.05f;
        }

        private bool CanPush(Rigidbody hitBody)
        {
            return hitBody != null &&
                   hitBody != body &&
                   !hitBody.isKinematic &&
                   hitBody.mass <= Mathf.Max(0.1f, pushableMassLimit);
        }

        private static void PushBody(Rigidbody hitBody, Vector3 planarOffset)
        {
            planarOffset.y = 0f;
            if (planarOffset.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            var target = hitBody.position + planarOffset;
            hitBody.WakeUp();
            hitBody.velocity = Vector3.zero;
            hitBody.angularVelocity = Vector3.zero;
            hitBody.MovePosition(target);
            hitBody.position = target;
            hitBody.transform.position = target;
            Physics.SyncTransforms();
        }

        private void GetCapsuleWorldPoints(Vector3 rootPosition, out Vector3 bottom, out Vector3 top)
        {
            var scale = transform.lossyScale;
            var radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z), 0.001f);
            var heightScale = Mathf.Max(Mathf.Abs(scale.y), 0.001f);
            var radius = Mathf.Max(0.01f, capsule.radius * radiusScale);
            var height = Mathf.Max(radius * 2f, capsule.height * heightScale);
            var center = rootPosition + Vector3.up * (capsule.center.y * heightScale);
            var halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            bottom = center - Vector3.up * halfSegment;
            top = center + Vector3.up * halfSegment;
        }

        private static Vector3 ResolvePlanarDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
        }

        private static float ResolveHeadingAlignmentDegrees(Vector3 planarDelta, Vector3 facingDirection)
        {
            planarDelta.y = 0f;
            facingDirection.y = 0f;
            if (planarDelta.sqrMagnitude <= 0.000001f || facingDirection.sqrMagnitude <= 0.000001f)
            {
                return 0f;
            }

            return Vector3.Angle(planarDelta.normalized, facingDirection.normalized);
        }
    }
}
