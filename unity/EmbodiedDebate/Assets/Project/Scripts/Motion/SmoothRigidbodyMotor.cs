using UnityEngine;

namespace ArgusUnity.Motion
{
    /// <summary>
    /// Moves a minibot through Rigidbody.MovePosition/MoveRotation while animation only follows motion.
    /// </summary>
    public sealed class SmoothRigidbodyMotor : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody body;

        [SerializeField]
        private float acceleration = 2.4f;

        [SerializeField]
        private float deceleration = 3.2f;

        [SerializeField]
        private float turnDegreesPerSecond = 260f;

        [SerializeField]
        private float obstacleProbeRadius = 0.2f;

        [SerializeField]
        private float obstacleProbeDistance = 0.55f;

        [SerializeField]
        private LayerMask obstacleMask = ~0;

        [SerializeField]
        private Vector3 worldMin = new Vector3(-20f, 0f, -20f);

        [SerializeField]
        private Vector3 worldMax = new Vector3(20f, 5f, 20f);

        private MotionIntent currentIntent;
        private Vector3 currentVelocity;
        private Vector3 lastPosition;
        private bool hasIntent;

        public Vector3 CurrentVelocity => currentVelocity;

        public MotionIntent CurrentIntent => currentIntent;

        public bool HasIntent => hasIntent;

        public float DistanceToTarget { get; private set; }

        public bool IsAtTarget { get; private set; } = true;

        public bool ObstacleAhead { get; private set; }

        public Vector3 DesiredVelocity { get; private set; }

        public float LastActualDisplacement { get; private set; }

        public float MaxConfiguredSpeed { get; private set; }

        public Rigidbody Body => EnsureBody();

        private void Awake()
        {
            EnsureBody();
            lastPosition = body.position;
        }

        private void OnEnable()
        {
            lastPosition = EnsureBody().position;
            LastActualDisplacement = 0f;
        }

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }

        public void SetIntent(MotionIntent intent)
        {
            currentIntent = intent;
            hasIntent = true;
            MaxConfiguredSpeed = Mathf.Max(MaxConfiguredSpeed, intent.DesiredSpeedMetersPerSecond);
        }

        public void ClearIntent()
        {
            hasIntent = false;
            currentIntent = MotionIntent.Idle(transform.position);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var rb = EnsureBody();
            var currentPosition = rb.position;
            DesiredVelocity = ComputeDesiredVelocity(currentPosition);
            DesiredVelocity = ApplyObstacleAvoidance(currentPosition, DesiredVelocity);
            DesiredVelocity = ClampVelocityNearBounds(currentPosition, DesiredVelocity, deltaTime);

            var rate = DesiredVelocity.sqrMagnitude > currentVelocity.sqrMagnitude
                ? acceleration
                : deceleration;
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                DesiredVelocity,
                Mathf.Max(0f, rate) * deltaTime);

            var nextPosition = currentPosition + currentVelocity * deltaTime;
            nextPosition = ClampToWorldBounds(nextPosition);
            nextPosition.y = Mathf.Max(worldMin.y, nextPosition.y);

            rb.MovePosition(nextPosition);
            rb.MoveRotation(ComputeNextRotation(rb.rotation, currentVelocity, deltaTime));

            LastActualDisplacement = PlanarDistance(lastPosition, nextPosition);
            lastPosition = nextPosition;
        }

        public Vector3 ComputeDesiredVelocity(Vector3 position)
        {
            if (!hasIntent || !currentIntent.HasMoveTarget || currentIntent.LocksMovement)
            {
                DistanceToTarget = 0f;
                IsAtTarget = true;
                return Vector3.zero;
            }

            var delta = currentIntent.MoveTarget - position;
            delta.y = 0f;
            DistanceToTarget = delta.magnitude;
            if (DistanceToTarget <= currentIntent.StopDistance)
            {
                IsAtTarget = true;
                return Vector3.zero;
            }

            IsAtTarget = false;
            var direction = delta / Mathf.Max(DistanceToTarget, 0.0001f);
            var slowFactor = Mathf.Clamp01((DistanceToTarget - currentIntent.StopDistance) / 1.25f);
            var targetSpeed = Mathf.Lerp(0f, currentIntent.DesiredSpeedMetersPerSecond, slowFactor);
            return direction * targetSpeed;
        }

        private Vector3 ApplyObstacleAvoidance(Vector3 position, Vector3 desiredVelocity)
        {
            ObstacleAhead = false;
            if (desiredVelocity.sqrMagnitude <= 0.0001f || obstacleProbeDistance <= 0f)
            {
                return desiredVelocity;
            }

            var direction = desiredVelocity.normalized;
            var probeOrigin = position + Vector3.up * 0.25f;
            if (!Physics.SphereCast(
                    probeOrigin,
                    Mathf.Max(0.01f, obstacleProbeRadius),
                    direction,
                    out var hit,
                    obstacleProbeDistance,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore))
            {
                return desiredVelocity;
            }

            if (hit.rigidbody == body)
            {
                return desiredVelocity;
            }

            ObstacleAhead = true;
            var slide = Vector3.ProjectOnPlane(desiredVelocity, hit.normal);
            slide.y = 0f;
            return slide.sqrMagnitude > 0.0001f ? slide * 0.45f : Vector3.zero;
        }

        private Vector3 ClampVelocityNearBounds(Vector3 position, Vector3 desiredVelocity, float deltaTime)
        {
            var next = position + desiredVelocity * deltaTime;
            var clamped = ClampToWorldBounds(next);
            var adjusted = clamped - position;
            adjusted.y = 0f;
            return adjusted.sqrMagnitude < desiredVelocity.sqrMagnitude ? adjusted / Mathf.Max(deltaTime, 0.0001f) : desiredVelocity;
        }

        private Quaternion ComputeNextRotation(Quaternion currentRotation, Vector3 velocity, float deltaTime)
        {
            var direction = velocity;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0025f && currentIntent.FocusTarget.HasValue)
            {
                direction = currentIntent.FocusTarget.Value - Body.position;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return currentRotation;
            }

            var target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            return Quaternion.RotateTowards(
                currentRotation,
                target,
                Mathf.Max(0f, turnDegreesPerSecond) * deltaTime);
        }

        private Vector3 ClampToWorldBounds(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, worldMin.x, worldMax.x),
                Mathf.Clamp(position.y, worldMin.y, worldMax.y),
                Mathf.Clamp(position.z, worldMin.z, worldMax.z));
        }

        private Rigidbody EnsureBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            }

            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            return body;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var delta = b - a;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
