using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MiniBotHideAndSeekScenario : MonoBehaviour
    {
        [SerializeField]
        private float rotationSharpness = 5.8f;

        [SerializeField]
        private float steeringSharpness = 3.8f;

        [SerializeField]
        private float maxTurnDegreesPerSecond = 92f;

        [SerializeField]
        private float maxPlanarAcceleration = 4.8f;

        [SerializeField]
        private float maxPlanarDeceleration = 7.5f;

        [SerializeField]
        private Vector2 roomMin = new Vector2(-3.85f, -1.85f);

        [SerializeField]
        private Vector2 roomMax = new Vector2(3.85f, 1.85f);

        [SerializeField]
        private float targetReachDistance = 0.36f;

        [SerializeField]
        private float obstacleLookAhead = 1.28f;

        [SerializeField]
        private float wallContactProbeDistance = 0.32f;

        [SerializeField]
        private float wallContactProbeRadius = 0.24f;

        [SerializeField]
        private float retargetSeconds = 2.4f;

        [SerializeField]
        private float wallAvoidanceMargin = 1.24f;

        [SerializeField]
        private float targetPadding = 1.28f;

        [SerializeField]
        private float stuckRetargetSeconds = 0.75f;

        [SerializeField]
        private float minimumProgressSpeed = 0.035f;

        [SerializeField]
        private float boundaryRecoverySeconds = 1.35f;

        [SerializeField]
        private float hardBoundaryInset = 1.04f;

        [SerializeField]
        private float recoveryTravelDistance = 3.35f;

        [SerializeField]
        private List<WanderAgent> agents = new List<WanderAgent>();

        [SerializeField]
        private List<Transform> obstacleProbeObjects = new List<Transform>();

        [SerializeField]
        private bool debugMovementDiagnostics;

        [SerializeField]
        private float debugMovementDiagnosticsInterval = 0.5f;

        private static PhysicMaterial lowFrictionContactMaterial;
        private bool initialObstacleProbeAssigned;

        public void RegisterAgent(Transform agent, Vector3 startPosition, float speed, int seed)
        {
            if (agent == null)
            {
                return;
            }

            agents.Add(new WanderAgent(agent, startPosition, speed, seed));
            agent.position = startPosition;
        }

        public void RegisterObstacleProbeObject(Transform obstacle)
        {
            if (obstacle != null)
            {
                obstacleProbeObjects.Add(obstacle);
            }
        }

        private void Start()
        {
            Debug.Log(
                "MiniBotHideAndSeekScenario: registered free-roam agents=" +
                $"{agents.Count}, bounds=({roomMin.x:0.00},{roomMin.y:0.00})-({roomMax.x:0.00},{roomMax.y:0.00}).");
        }

        private void FixedUpdate()
        {
            for (var i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                EnsureAgentPhysics(agent);

                if (agent.Locomotion == null && agent.Agent != null)
                {
                    agent.Locomotion = agent.Agent.GetComponent<AgentLocomotionDriver>();
                }

                var currentPosition = agent.Body.position;
                var previousRotation = agent.Body.rotation;
                var actualDelta = currentPosition - agent.LastPosition;
                actualDelta.y = 0f;
                var actualDistance = actualDelta.magnitude;
                var moveSpeed = Time.fixedDeltaTime > 0f
                    ? actualDistance / Time.fixedDeltaTime
                    : 0f;
                RecoverIfNearBoundary(agent, currentPosition);
                RetargetIfStuck(agent, currentPosition, moveSpeed);
                var forceRecoveryDrive = agent.BoundaryRecoveryTimer > 0f;
                var desiredDirection = SmoothDesiredDirection(
                    agent,
                    ResolveDesiredDirection(agent, currentPosition),
                    forceRecoveryDrive);

                DriveAgent(agent, desiredDirection, previousRotation, forceRecoveryDrive);
                KeepInsideRoom(agent);

                if (!agent.MovementLogged && actualDistance > 0.002f)
                {
                    Debug.Log($"MiniBotHideAndSeekScenario: movement confirmed for {agent.Agent.name}.");
                    agent.MovementLogged = true;
                }

                if (!agent.RotationLogged && Quaternion.Angle(previousRotation, agent.Body.rotation) > 0.5f)
                {
                    Debug.Log($"MiniBotHideAndSeekScenario: rotation confirmed for {agent.Agent.name}.");
                    agent.RotationLogged = true;
                }

                if (!agent.SpeedLogged && moveSpeed > 0.05f)
                {
                    Debug.Log(
                        "MiniBotHideAndSeekScenario: free-roam speed check " +
                        $"{agent.Agent.name} movementSpeed={moveSpeed:0.00}, walkDistanceDelta={actualDistance:0.000}.");
                    agent.SpeedLogged = true;
                }

                agent.LastPosition = agent.Body.position;
                agents[i] = agent;
            }
        }

        private void EnsureAgentPhysics(WanderAgent agent)
        {
            if (agent.Body == null)
            {
                agent.Body = agent.Agent.GetComponent<Rigidbody>();
            }

            if (agent.Body == null)
            {
                agent.Body = agent.Agent.gameObject.AddComponent<Rigidbody>();
            }

            agent.Body.useGravity = false;
            agent.Body.isKinematic = false;
            agent.Body.mass = 2.5f;
            agent.Body.drag = 0.65f;
            agent.Body.angularDrag = 8f;
            agent.Body.interpolation = RigidbodyInterpolation.Interpolate;
            agent.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            agent.Body.maxDepenetrationVelocity = 1.8f;
            agent.Body.constraints = RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            var capsule = agent.Agent.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = agent.Agent.gameObject.AddComponent<CapsuleCollider>();
            }

            capsule.center = new Vector3(0f, 0.46f, 0f);
            capsule.height = 0.92f;
            capsule.radius = 0.22f;
            capsule.material = MiniBotLowFrictionContactMaterial();

            if (agent.WallContact == null)
            {
                agent.WallContact = agent.Agent.GetComponent<MiniBotWallContactTracker>();
                if (agent.WallContact == null)
                {
                    agent.WallContact = agent.Agent.gameObject.AddComponent<MiniBotWallContactTracker>();
                }
            }

            if (agent.Random == null)
            {
                agent.Random = new System.Random(agent.Seed);
            }

            if (!agent.TargetInitialized)
            {
                agent.Target = PickInitialTarget(agent);
                agent.TargetInitialized = true;
                agent.LastPosition = agent.Body.position;
            }
        }

        private Vector3 PickInitialTarget(WanderAgent agent)
        {
            if (!initialObstacleProbeAssigned && obstacleProbeObjects.Count > 0)
            {
                var obstacle = obstacleProbeObjects[0];
                if (obstacle != null && IsClosestAgentTo(agent, obstacle.position))
                {
                    initialObstacleProbeAssigned = true;
                    var target = obstacle.position;
                    target.y = agent.StartPosition.y;
                    Debug.Log(
                        "MiniBotHideAndSeekScenario: initial obstacle-avoidance probe " +
                        $"{agent.Agent.name} -> {obstacle.name} at {target.x:0.00},{target.z:0.00}.");
                    return target;
                }
            }

            return PickTarget(agent);
        }

        private bool IsClosestAgentTo(WanderAgent candidate, Vector3 position)
        {
            var candidateDistance = PlanarDistanceSquared(candidate.StartPosition, position);
            for (var i = 0; i < agents.Count; i++)
            {
                var other = agents[i];
                if (ReferenceEquals(other, candidate) || other.Agent == null)
                {
                    continue;
                }

                if (PlanarDistanceSquared(other.StartPosition, position) < candidateDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static float PlanarDistanceSquared(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        private Vector3 ResolveDesiredDirection(WanderAgent agent, Vector3 currentPosition)
        {
            agent.RetargetTimer += Time.fixedDeltaTime;
            if (agent.BoundaryRecoveryTimer > 0f && agent.BoundaryRecoveryDirection.sqrMagnitude > 0.001f)
            {
                agent.BoundaryRecoveryTimer -= Time.fixedDeltaTime;
                return SteerAroundObstacle(agent, currentPosition, agent.BoundaryRecoveryDirection.normalized);
            }

            var toTarget = agent.Target - currentPosition;
            toTarget.y = 0f;

            if (toTarget.magnitude < targetReachDistance || agent.RetargetTimer >= retargetSeconds)
            {
                agent.Target = PickTarget(agent);
                agent.RetargetTimer = 0f;
                toTarget = agent.Target - currentPosition;
                toTarget.y = 0f;
                Debug.Log($"MiniBotHideAndSeekScenario: free-roam retarget {agent.Agent.name} -> {agent.Target.x:0.00},{agent.Target.z:0.00}.");
            }

            var desiredDirection = toTarget.sqrMagnitude > 0.001f
                ? toTarget.normalized
                : agent.Agent.forward;

            desiredDirection = SteerInsideRoom(agent, currentPosition, desiredDirection);
            desiredDirection = SteerAroundObstacle(agent, currentPosition, desiredDirection);
            return desiredDirection.sqrMagnitude > 0.001f ? desiredDirection.normalized : agent.Agent.forward;
        }

        private void DriveAgent(
            WanderAgent agent,
            Vector3 desiredDirection,
            Quaternion previousRotation,
            bool forceDirectionDrive)
        {
            if (desiredDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var smoothedRotation = SmoothRotationTowards(
                previousRotation,
                desiredDirection,
                forceDirectionDrive,
                out var turnAngle);

            var forward = smoothedRotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var turnSlowdown = Mathf.Lerp(1f, 0.48f, Mathf.Clamp01(turnAngle / 95f));
            var driveDirection = forward.normalized;
            var driveSlowdown = forceDirectionDrive ? Mathf.Max(turnSlowdown, 0.82f) : turnSlowdown;
            var desiredVelocity = driveDirection * agent.Speed * driveSlowdown;
            var adjustedVelocity = ResolveWallContactVelocity(
                agent,
                agent.Body.position,
                desiredVelocity,
                out var wallNormal,
                out var wallAdjusted);
            var currentVelocity = agent.Body.velocity;
            var currentPlanarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            var nextPlanarVelocity = RoomNavigationMath.MovePlanarVelocityTowards(
                currentPlanarVelocity,
                adjustedVelocity,
                maxPlanarAcceleration,
                maxPlanarDeceleration,
                Time.fixedDeltaTime);

            if (wallAdjusted)
            {
                if (nextPlanarVelocity.sqrMagnitude > 0.0001f)
                {
                    smoothedRotation = SmoothRotationTowards(
                        previousRotation,
                        nextPlanarVelocity.normalized,
                        forceDirectionDrive,
                        out _);
                }
                else
                {
                    smoothedRotation = previousRotation;
                }
            }

            agent.Body.MoveRotation(smoothedRotation);

            agent.Body.velocity = new Vector3(
                nextPlanarVelocity.x,
                currentVelocity.y,
                nextPlanarVelocity.z);
            LogMovementDiagnostics(
                agent,
                desiredVelocity,
                adjustedVelocity,
                currentPlanarVelocity,
                nextPlanarVelocity,
                wallNormal,
                wallAdjusted);
        }

        private Quaternion SmoothRotationTowards(
            Quaternion previousRotation,
            Vector3 desiredDirection,
            bool forceDirectionDrive,
            out float turnAngle)
        {
            var targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
            turnAngle = Quaternion.Angle(previousRotation, targetRotation);
            var nextRotation = Quaternion.RotateTowards(
                previousRotation,
                targetRotation,
                (forceDirectionDrive ? maxTurnDegreesPerSecond * 3.8f : maxTurnDegreesPerSecond) * Time.fixedDeltaTime);
            return Quaternion.Slerp(
                previousRotation,
                nextRotation,
                1f - Mathf.Exp((forceDirectionDrive ? -rotationSharpness * 2.4f : -rotationSharpness) * Time.fixedDeltaTime));
        }

        private Vector3 ResolveWallContactVelocity(
            WanderAgent agent,
            Vector3 position,
            Vector3 desiredVelocity,
            out Vector3 wallNormal,
            out bool adjusted)
        {
            adjusted = false;
            wallNormal = Vector3.zero;
            desiredVelocity.y = 0f;
            if (desiredVelocity.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            var desiredDirection = desiredVelocity.normalized;
            var origin = position + Vector3.up * 0.44f;
            if (!TryFindBlockingWall(agent, origin, desiredDirection, out wallNormal))
            {
                return desiredVelocity;
            }

            adjusted = true;
            var projected = RoomNavigationMath.ProjectPlanarVelocityAlongWall(desiredVelocity, wallNormal);
            if (!agent.WallSlideLogged)
            {
                Debug.Log(
                    "MiniBotHideAndSeekScenario: wall slide adjusted drive " +
                    $"{agent.Agent.name}, normal={wallNormal.x:0.00},{wallNormal.z:0.00}, " +
                    $"speed={projected.magnitude:0.00}.");
                agent.WallSlideLogged = true;
            }

            return projected;
        }

        private bool TryFindBlockingWall(WanderAgent agent, Vector3 origin, Vector3 direction, out Vector3 wallNormal)
        {
            if (agent.WallContact != null &&
                agent.WallContact.TryGetBlockingNormal(direction, out wallNormal))
            {
                return true;
            }

            var hits = Physics.SphereCastAll(
                origin,
                Mathf.Max(0.01f, wallContactProbeRadius),
                direction,
                Mathf.Max(0.01f, wallContactProbeDistance),
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            wallNormal = Vector3.zero;
            var strongestBlockDot = 0f;
            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i];
                if (!RoomNavigationMath.ShouldAvoidObstacleCollider(candidate.collider, agent.Agent))
                {
                    continue;
                }

                var normal = Vector3.ProjectOnPlane(candidate.normal, Vector3.up);
                if (normal.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                normal.Normalize();
                var blockDot = Vector3.Dot(direction, normal);
                if (blockDot >= strongestBlockDot)
                {
                    continue;
                }

                strongestBlockDot = blockDot;
                wallNormal = normal;
            }

            return strongestBlockDot < -0.05f;
        }

        private void LogMovementDiagnostics(
            WanderAgent agent,
            Vector3 desiredVelocity,
            Vector3 adjustedVelocity,
            Vector3 currentPlanarVelocity,
            Vector3 nextPlanarVelocity,
            Vector3 wallNormal,
            bool wallAdjusted)
        {
            if (!debugMovementDiagnostics)
            {
                return;
            }

            agent.DiagnosticsTimer -= Time.fixedDeltaTime;
            if (agent.DiagnosticsTimer > 0f)
            {
                return;
            }

            agent.DiagnosticsTimer = Mathf.Max(0.05f, debugMovementDiagnosticsInterval);
            var animatorSpeed = agent.Locomotion != null ? agent.Locomotion.LastAnimatorSpeed : -1f;
            var animatorTurn = agent.Locomotion != null ? agent.Locomotion.LastAnimatorTurn : 0f;
            Debug.Log(
                "MiniBotHideAndSeekScenario: movement diagnostics " +
                $"{agent.Agent.name}, desired={desiredVelocity.magnitude:0.00}, " +
                $"adjusted={adjustedVelocity.magnitude:0.00}, current={currentPlanarVelocity.magnitude:0.00}, " +
                $"next={nextPlanarVelocity.magnitude:0.00}, wallBlocked={wallAdjusted}, " +
                $"normal={wallNormal.x:0.00},{wallNormal.z:0.00}, " +
                $"animSpeed={animatorSpeed:0.00}, animTurn={animatorTurn:0.00}.");
        }

        private Vector3 SmoothDesiredDirection(WanderAgent agent, Vector3 desiredDirection, bool forceImmediate)
        {
            if (desiredDirection.sqrMagnitude <= 0.001f)
            {
                return agent.SteeringInitialized ? agent.SteeringDirection : agent.Agent.forward;
            }

            if (forceImmediate)
            {
                agent.SteeringDirection = desiredDirection.normalized;
                agent.SteeringInitialized = true;
                return agent.SteeringDirection;
            }

            if (!agent.SteeringInitialized || agent.SteeringDirection.sqrMagnitude <= 0.001f)
            {
                agent.SteeringDirection = agent.Agent.forward.sqrMagnitude > 0.001f
                    ? agent.Agent.forward
                    : desiredDirection.normalized;
                agent.SteeringInitialized = true;
            }

            agent.SteeringDirection = Vector3.Slerp(
                agent.SteeringDirection,
                desiredDirection.normalized,
                1f - Mathf.Exp(-steeringSharpness * Time.fixedDeltaTime)).normalized;
            return agent.SteeringDirection;
        }

        private void RecoverIfNearBoundary(WanderAgent agent, Vector3 currentPosition)
        {
            if (agent.BoundaryRecoveryTimer > 0f)
            {
                return;
            }

            if (!RoomNavigationMath.IsNearBoundary(currentPosition, roomMin, roomMax, hardBoundaryInset))
            {
                return;
            }

            StartBoundaryRecovery(agent, currentPosition);
        }

        private Vector3 PickTarget(WanderAgent agent)
        {
            var min = new Vector2(roomMin.x + targetPadding, roomMin.y + targetPadding);
            var max = new Vector2(roomMax.x - targetPadding, roomMax.y - targetPadding);
            if (min.x >= max.x || min.y >= max.y)
            {
                min = roomMin;
                max = roomMax;
            }

            for (var attempt = 0; attempt < 24; attempt++)
            {
                var x = Mathf.Lerp(min.x, max.x, (float)agent.Random.NextDouble());
                var z = Mathf.Lerp(min.y, max.y, (float)agent.Random.NextDouble());
                var candidate = new Vector3(x, agent.StartPosition.y, z);
                if (!Physics.CheckSphere(candidate + Vector3.up * 0.44f, 0.26f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    return candidate;
                }
            }

            return new Vector3(
                Mathf.Lerp(min.x, max.x, (float)agent.Random.NextDouble()),
                agent.StartPosition.y,
                Mathf.Lerp(min.y, max.y, (float)agent.Random.NextDouble()));
        }

        private Vector3 SteerInsideRoom(WanderAgent agent, Vector3 position, Vector3 desiredDirection)
        {
            var inward = Vector3.zero;
            if (position.x < roomMin.x + wallAvoidanceMargin)
            {
                var weight = Mathf.Clamp01((roomMin.x + wallAvoidanceMargin - position.x) / wallAvoidanceMargin);
                inward += Vector3.right * weight;
            }
            else if (position.x > roomMax.x - wallAvoidanceMargin)
            {
                var weight = Mathf.Clamp01((position.x - (roomMax.x - wallAvoidanceMargin)) / wallAvoidanceMargin);
                inward += Vector3.left * weight;
            }

            if (position.z < roomMin.y + wallAvoidanceMargin)
            {
                var weight = Mathf.Clamp01((roomMin.y + wallAvoidanceMargin - position.z) / wallAvoidanceMargin);
                inward += Vector3.forward * weight;
            }
            else if (position.z > roomMax.y - wallAvoidanceMargin)
            {
                var weight = Mathf.Clamp01((position.z - (roomMax.y - wallAvoidanceMargin)) / wallAvoidanceMargin);
                inward += Vector3.back * weight;
            }

            if (inward.sqrMagnitude <= 0.001f)
            {
                return desiredDirection;
            }

            if (!agent.BoundaryLogged)
            {
                Debug.Log($"MiniBotHideAndSeekScenario: boundary steering active for {agent.Agent.name}.");
                agent.BoundaryLogged = true;
            }

            var blend = Mathf.Lerp(0.38f, 0.9f, Mathf.Clamp01(inward.magnitude));
            return Vector3.Slerp(desiredDirection, inward.normalized, blend);
        }

        private Vector3 SteerAroundObstacle(WanderAgent agent, Vector3 position, Vector3 desiredDirection)
        {
            var origin = position + Vector3.up * 0.44f;
            if (!TryFindObstacle(agent.Agent, origin, desiredDirection, out var hit))
            {
                return desiredDirection;
            }

            var away = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
            if (away.sqrMagnitude < 0.001f)
            {
                away = Quaternion.Euler(0f, 55f, 0f) * desiredDirection;
            }

            away.Normalize();
            var tangent = Vector3.Cross(Vector3.up, away);
            if (tangent.sqrMagnitude < 0.001f)
            {
                tangent = Quaternion.Euler(0f, 90f, 0f) * desiredDirection;
            }

            tangent.Normalize();
            if (Vector3.Dot(tangent, desiredDirection) < 0f)
            {
                tangent = -tangent;
            }

            var avoidanceDirection = (away * 0.78f + tangent * 0.52f).normalized;
            agent.Target = PickTarget(agent);
            agent.RetargetTimer = 0f;
            agent.LowProgressTimer = 0f;
            if (!agent.ObstacleLogged)
            {
                Debug.Log(
                    "MiniBotHideAndSeekScenario: obstacle avoidance retarget " +
                    $"{agent.Agent.name}, collider={hit.collider.name}.");
                agent.ObstacleLogged = true;
            }

            return Vector3.Slerp(desiredDirection, avoidanceDirection, 0.88f);
        }

        private void RetargetIfStuck(WanderAgent agent, Vector3 currentPosition, float moveSpeed)
        {
            if (!agent.TargetInitialized)
            {
                return;
            }

            var toTarget = agent.Target - currentPosition;
            toTarget.y = 0f;
            if (toTarget.magnitude < targetReachDistance || moveSpeed >= minimumProgressSpeed)
            {
                agent.LowProgressTimer = 0f;
                return;
            }

            agent.LowProgressTimer += Time.fixedDeltaTime;
            if (agent.LowProgressTimer < stuckRetargetSeconds)
            {
                return;
            }

            agent.Target = PickTarget(agent);
            agent.RetargetTimer = 0f;
            agent.LowProgressTimer = 0f;
            agent.SteeringInitialized = false;
            if (RoomNavigationMath.IsNearBoundary(currentPosition, roomMin, roomMax, wallAvoidanceMargin))
            {
                StartBoundaryRecovery(agent, currentPosition);
                return;
            }

            Debug.Log($"MiniBotHideAndSeekScenario: stuck retarget {agent.Agent.name} -> {agent.Target.x:0.00},{agent.Target.z:0.00}.");
        }

        private void KeepInsideRoom(WanderAgent agent)
        {
            var position = agent.Body.position;
            var clamped = RoomNavigationMath.ClampPlanarWithInset(position, roomMin, roomMax, hardBoundaryInset);
            if ((clamped - position).sqrMagnitude <= 0.0001f)
            {
                return;
            }

            agent.Body.velocity = RoomNavigationMath.RemoveVelocityTowardClamp(
                position,
                clamped,
                agent.Body.velocity);
            agent.Body.MovePosition(clamped);
            StartBoundaryRecovery(agent, position);
            if (!agent.ClampLogged)
            {
                Debug.Log($"MiniBotHideAndSeekScenario: wall clamp kept {agent.Agent.name} inside walkable room.");
                agent.ClampLogged = true;
            }
        }

        private void StartBoundaryRecovery(WanderAgent agent, Vector3 position)
        {
            var previousDirection = agent.SteeringInitialized && agent.SteeringDirection.sqrMagnitude > 0.001f
                ? agent.SteeringDirection
                : agent.Agent.forward;
            var preferRightTurn = agent.Random == null || agent.Random.NextDouble() >= 0.5;
            var recoveryDirection = RoomNavigationMath.WallContactRecoveryDirection(
                position,
                previousDirection,
                roomMin,
                roomMax,
                wallAvoidanceMargin,
                preferRightTurn);
            var targetMin = new Vector2(roomMin.x + targetPadding, roomMin.y + targetPadding);
            var targetMax = new Vector2(roomMax.x - targetPadding, roomMax.y - targetPadding);
            var recoveryTarget = RoomNavigationMath.ClampPlanar(position + recoveryDirection * recoveryTravelDistance, targetMin, targetMax);
            recoveryTarget.y = agent.StartPosition.y;
            if (TryFindObstacle(agent.Agent, position + Vector3.up * 0.44f, recoveryDirection, out _))
            {
                recoveryTarget = RoomNavigationMath.RandomInteriorTarget(agent.Random, targetMin, targetMax, agent.StartPosition.y);
                recoveryDirection = recoveryTarget - position;
                recoveryDirection.y = 0f;
                recoveryDirection = recoveryDirection.sqrMagnitude > 0.001f
                    ? recoveryDirection.normalized
                    : RoomNavigationMath.BoundaryTurnaroundDirection(
                        position,
                        previousDirection,
                        roomMin,
                        roomMax,
                        wallAvoidanceMargin);
            }

            agent.BoundaryRecoveryDirection = recoveryDirection;
            agent.BoundaryRecoveryTimer = boundaryRecoverySeconds;
            agent.Target = recoveryTarget;
            agent.RetargetTimer = 0f;
            agent.LowProgressTimer = 0f;
            agent.SteeringInitialized = false;

            if (!agent.WallTurnaroundLogged)
            {
                Debug.Log(
                    "MiniBotHideAndSeekScenario: wall pressure turnaround " +
                    $"{agent.Agent.name} -> {recoveryDirection.x:0.00},{recoveryDirection.z:0.00}.");
                agent.WallTurnaroundLogged = true;
            }
        }

        private bool TryFindObstacle(Transform agent, Vector3 origin, Vector3 direction, out RaycastHit hit)
        {
            var hits = Physics.SphereCastAll(
                origin,
                0.24f,
                direction,
                obstacleLookAhead,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            hit = default;
            var closest = float.MaxValue;
            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i];
                if (!RoomNavigationMath.ShouldAvoidObstacleCollider(candidate.collider, agent))
                {
                    continue;
                }

                if (candidate.distance >= closest)
                {
                    continue;
                }

                closest = candidate.distance;
                hit = candidate;
            }

            return closest < float.MaxValue;
        }

        [Serializable]
        private sealed class WanderAgent
        {
            public WanderAgent(Transform agent, Vector3 startPosition, float speed, int seed)
            {
                Agent = agent;
                StartPosition = startPosition;
                Speed = speed;
                Seed = seed;
                Locomotion = agent.GetComponent<AgentLocomotionDriver>();
                Random = new System.Random(seed);
                Target = startPosition;
                LastPosition = startPosition;
                TargetInitialized = false;
                SteeringDirection = agent.forward;
                SteeringInitialized = false;
                MovementLogged = false;
                RotationLogged = false;
                SpeedLogged = false;
                BoundaryLogged = false;
                ObstacleLogged = false;
                ClampLogged = false;
                WallSlideLogged = false;
                WallTurnaroundLogged = false;
            }

            public Transform Agent;
            public Vector3 StartPosition;
            public float Speed;
            public int Seed;
            public AgentLocomotionDriver Locomotion;
            public Rigidbody Body;
            public MiniBotWallContactTracker WallContact;
            public Vector3 Target;
            [NonSerialized]
            public Vector3 LastPosition;
            [NonSerialized]
            public System.Random Random;
            [NonSerialized]
            public float RetargetTimer;
            [NonSerialized]
            public bool TargetInitialized;
            [NonSerialized]
            public Vector3 SteeringDirection;
            [NonSerialized]
            public bool SteeringInitialized;
            [NonSerialized]
            public bool MovementLogged;
            [NonSerialized]
            public bool RotationLogged;
            [NonSerialized]
            public bool SpeedLogged;
            [NonSerialized]
            public float LowProgressTimer;
            [NonSerialized]
            public bool BoundaryLogged;
            [NonSerialized]
            public bool ObstacleLogged;
            [NonSerialized]
            public bool ClampLogged;
            [NonSerialized]
            public bool WallSlideLogged;
            [NonSerialized]
            public Vector3 BoundaryRecoveryDirection;
            [NonSerialized]
            public float BoundaryRecoveryTimer;
            [NonSerialized]
            public bool WallTurnaroundLogged;
            [NonSerialized]
            public float DiagnosticsTimer;
        }

        private static PhysicMaterial MiniBotLowFrictionContactMaterial()
        {
            if (lowFrictionContactMaterial != null)
            {
                return lowFrictionContactMaterial;
            }

            lowFrictionContactMaterial = new PhysicMaterial("MiniBotLowFrictionRuntime")
            {
                staticFriction = 0f,
                dynamicFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
            return lowFrictionContactMaterial;
        }
    }

    public sealed class MiniBotWallContactTracker : MonoBehaviour
    {
        private Vector3 wallNormal;
        private float lastContactFixedTime = -1f;

        private void OnCollisionStay(Collision collision)
        {
            var combined = Vector3.zero;
            for (var i = 0; i < collision.contactCount; i++)
            {
                var normal = Vector3.ProjectOnPlane(collision.GetContact(i).normal, Vector3.up);
                if (normal.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                normal.Normalize();
                combined += normal;
            }

            if (combined.sqrMagnitude <= 0.001f)
            {
                return;
            }

            wallNormal = combined.normalized;
            lastContactFixedTime = Time.fixedTime;
        }

        private void OnCollisionExit(Collision collision)
        {
            wallNormal = Vector3.zero;
            lastContactFixedTime = -1f;
        }

        public bool TryGetBlockingNormal(Vector3 desiredDirection, out Vector3 normal)
        {
            normal = Vector3.zero;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude <= 0.001f ||
                wallNormal.sqrMagnitude <= 0.001f ||
                Time.fixedTime - lastContactFixedTime > Time.fixedDeltaTime * 1.5f)
            {
                return false;
            }

            desiredDirection.Normalize();
            if (Vector3.Dot(desiredDirection, wallNormal) >= -0.05f)
            {
                return false;
            }

            normal = wallNormal;
            return true;
        }
    }

    public static class RoomNavigationMath
    {
        public static Vector3 ClampPlanar(Vector3 position, Vector2 min, Vector2 max)
        {
            return new Vector3(
                Mathf.Clamp(position.x, min.x, max.x),
                position.y,
                Mathf.Clamp(position.z, min.y, max.y));
        }

        public static Vector3 ClampPlanarWithInset(Vector3 position, Vector2 min, Vector2 max, float inset)
        {
            inset = Mathf.Max(0f, inset);
            var insetMin = new Vector2(min.x + inset, min.y + inset);
            var insetMax = new Vector2(max.x - inset, max.y - inset);
            if (insetMin.x > insetMax.x || insetMin.y > insetMax.y)
            {
                return ClampPlanar(position, min, max);
            }

            return ClampPlanar(position, insetMin, insetMax);
        }

        public static bool ShouldAvoidObstacleCollider(Collider collider, Transform agent)
        {
            if (collider == null)
            {
                return false;
            }

            if (agent == null)
            {
                return true;
            }

            return !collider.transform.IsChildOf(agent);
        }

        public static Vector3 ProjectPlanarVelocityAlongWall(Vector3 desiredVelocity, Vector3 wallNormal)
        {
            desiredVelocity.y = 0f;
            wallNormal = Vector3.ProjectOnPlane(wallNormal, Vector3.up);
            if (desiredVelocity.sqrMagnitude <= 0.0001f || wallNormal.sqrMagnitude <= 0.001f)
            {
                return desiredVelocity;
            }

            wallNormal.Normalize();
            if (Vector3.Dot(desiredVelocity.normalized, wallNormal) >= -0.02f)
            {
                return desiredVelocity;
            }

            var projected = Vector3.ProjectOnPlane(desiredVelocity, wallNormal);
            projected.y = 0f;
            return projected.sqrMagnitude > 0.0001f ? projected : Vector3.zero;
        }

        public static Vector3 RemoveVelocityTowardClamp(Vector3 position, Vector3 clamped, Vector3 velocity)
        {
            if (clamped.x > position.x && velocity.x < 0f)
            {
                velocity.x = 0f;
            }
            else if (clamped.x < position.x && velocity.x > 0f)
            {
                velocity.x = 0f;
            }

            if (clamped.z > position.z && velocity.z < 0f)
            {
                velocity.z = 0f;
            }
            else if (clamped.z < position.z && velocity.z > 0f)
            {
                velocity.z = 0f;
            }

            return velocity;
        }

        public static Vector3 MovePlanarVelocityTowards(
            Vector3 currentPlanarVelocity,
            Vector3 targetPlanarVelocity,
            float maxAcceleration,
            float maxDeceleration,
            float deltaTime)
        {
            currentPlanarVelocity.y = 0f;
            targetPlanarVelocity.y = 0f;
            if (deltaTime <= 0f)
            {
                return currentPlanarVelocity;
            }

            var currentSpeed = currentPlanarVelocity.magnitude;
            var targetSpeed = targetPlanarVelocity.magnitude;
            var maxDelta = (targetSpeed < currentSpeed
                ? Mathf.Max(0f, maxDeceleration)
                : Mathf.Max(0f, maxAcceleration)) * deltaTime;
            return Vector3.MoveTowards(currentPlanarVelocity, targetPlanarVelocity, maxDelta);
        }

        public static bool IsNearBoundary(Vector3 position, Vector2 min, Vector2 max, float margin)
        {
            return BoundaryInwardVector(position, min, max, margin).sqrMagnitude > 0.001f;
        }

        public static Vector3 BoundaryTurnaroundDirection(
            Vector3 position,
            Vector3 currentDirection,
            Vector2 min,
            Vector2 max,
            float margin)
        {
            currentDirection.y = 0f;
            if (currentDirection.sqrMagnitude <= 0.001f)
            {
                currentDirection = Vector3.forward;
            }

            currentDirection.Normalize();
            var inward = BoundaryInwardVector(position, min, max, margin);
            if (inward.sqrMagnitude <= 0.001f)
            {
                return -currentDirection;
            }

            inward.Normalize();
            var tangent = Vector3.Cross(Vector3.up, inward);
            if (tangent.sqrMagnitude <= 0.001f)
            {
                tangent = Quaternion.Euler(0f, 90f, 0f) * currentDirection;
            }

            tangent.Normalize();
            if (Vector3.Dot(tangent, currentDirection) < 0f)
            {
                tangent = -tangent;
            }

            return (inward * 0.86f + tangent * 0.52f).normalized;
        }

        public static Vector3 WallContactRecoveryDirection(
            Vector3 position,
            Vector3 currentDirection,
            Vector2 min,
            Vector2 max,
            float margin,
            bool preferRightTurn)
        {
            currentDirection.y = 0f;
            if (currentDirection.sqrMagnitude <= 0.001f)
            {
                currentDirection = Vector3.forward;
            }

            currentDirection.Normalize();
            var inward = BoundaryInwardVector(position, min, max, margin);
            if (inward.sqrMagnitude <= 0.001f)
            {
                return RotatePlanar(currentDirection, preferRightTurn ? 90f : -90f);
            }

            inward.Normalize();
            var rightTurn = RotatePlanar(currentDirection, 90f);
            var leftTurn = RotatePlanar(currentDirection, -90f);
            var chosenTurn = ChooseWallTurn(rightTurn, leftTurn, inward, preferRightTurn);
            return (chosenTurn * 0.58f + inward * 0.86f).normalized;
        }

        public static Vector3 RandomInteriorTarget(System.Random random, Vector2 min, Vector2 max, float y)
        {
            if (random == null)
            {
                random = new System.Random(0);
            }

            if (min.x > max.x || min.y > max.y)
            {
                var center = (min + max) * 0.5f;
                return new Vector3(center.x, y, center.y);
            }

            return new Vector3(
                Mathf.Lerp(min.x, max.x, (float)random.NextDouble()),
                y,
                Mathf.Lerp(min.y, max.y, (float)random.NextDouble()));
        }

        private static Vector3 ChooseWallTurn(Vector3 rightTurn, Vector3 leftTurn, Vector3 inward, bool preferRightTurn)
        {
            var rightScore = Vector3.Dot(rightTurn, inward);
            var leftScore = Vector3.Dot(leftTurn, inward);
            if (Mathf.Abs(rightScore - leftScore) <= 0.001f)
            {
                return preferRightTurn ? rightTurn : leftTurn;
            }

            return rightScore > leftScore ? rightTurn : leftTurn;
        }

        private static Vector3 RotatePlanar(Vector3 direction, float degrees)
        {
            var rotated = Quaternion.Euler(0f, degrees, 0f) * direction;
            rotated.y = 0f;
            return rotated.sqrMagnitude > 0.001f ? rotated.normalized : Vector3.forward;
        }

        private static Vector3 BoundaryInwardVector(Vector3 position, Vector2 min, Vector2 max, float margin)
        {
            if (margin <= 0f)
            {
                margin = 0.01f;
            }

            var inward = Vector3.zero;
            if (position.x < min.x + margin)
            {
                inward += Vector3.right * Mathf.Clamp01((min.x + margin - position.x) / margin);
            }
            else if (position.x > max.x - margin)
            {
                inward += Vector3.left * Mathf.Clamp01((position.x - (max.x - margin)) / margin);
            }

            if (position.z < min.y + margin)
            {
                inward += Vector3.forward * Mathf.Clamp01((min.y + margin - position.z) / margin);
            }
            else if (position.z > max.y - margin)
            {
                inward += Vector3.back * Mathf.Clamp01((position.z - (max.y - margin)) / margin);
            }

            return inward;
        }
    }
}
