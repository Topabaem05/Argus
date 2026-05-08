using UnityEngine;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Drives code-translated agents through Unity's Animator pipeline.
    /// </summary>
    public sealed class AgentLocomotionDriver : MonoBehaviour
    {
        private const string DefaultControllerResource = "Animations/Controllers/MiniBotLocomotion";

        [SerializeField]
        private string controllerResource = DefaultControllerResource;

        [SerializeField]
        private string speedParameter = "Speed";

        [SerializeField]
        private string turnParameter = "Turn";

        [SerializeField]
        private float movementStartSpeed = 0.05f;

        [SerializeField]
        private float movementStopSpeed = 0.025f;

        [SerializeField]
        private float turnStartThresholdDegrees = 12f;

        [SerializeField]
        private float turnStopThresholdDegrees = 6f;

        [SerializeField]
        private float fullTurnDegrees = 90f;

        [SerializeField]
        private float referenceMoveSpeed = 1.9f;

        [SerializeField]
        private float animatorDampTime = 0.12f;

        private Animator animator;
        private Vector3 lastPosition;
        private float lastYaw;
        private bool isMoving;
        private int speedHash;
        private int turnHash;
        private bool hasSpeedParameter;
        private bool hasTurnParameter;
        private bool missingControllerLogged;
        private bool missingParameterLogged;
        private bool idleLogged;
        private bool walkLogged;
        private bool leftTurnLogged;
        private bool rightTurnLogged;
        private float animatorSpeed;
        private float animatorTurn;

        public float LastPlanarSpeed { get; private set; }
        public float LastSignedTurnDegrees { get; private set; }
        public float LastSignedTurnRateDegreesPerSecond { get; private set; }
        public float LastAnimatorSpeed => animatorSpeed;
        public float LastAnimatorTurn => animatorTurn;

        private void Awake()
        {
            ConfigureAnimator();
            lastPosition = transform.position;
            lastYaw = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
            lastYaw = transform.eulerAngles.y;
            LastPlanarSpeed = 0f;
            LastSignedTurnDegrees = 0f;
            LastSignedTurnRateDegreesPerSecond = 0f;
            animatorSpeed = 0f;
            animatorTurn = 0f;
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            var currentPosition = transform.position;
            var currentYaw = transform.eulerAngles.y;
            var delta = currentPosition - lastPosition;
            delta.y = 0f;
            lastPosition = currentPosition;

            LastPlanarSpeed = LocomotionMath.PlanarSpeed(delta, dt);
            LastSignedTurnRateDegreesPerSecond = LocomotionMath.SignedYawRate(lastYaw, currentYaw, dt);
            lastYaw = currentYaw;
            isMoving = LocomotionMath.ResolveMoving(
                LastPlanarSpeed,
                isMoving,
                movementStartSpeed,
                movementStopSpeed);

            LastSignedTurnDegrees = 0f;
            if (isMoving && delta.sqrMagnitude > 0.000001f)
            {
                var targetYaw = LocomotionMath.YawFromPlanarDirection(delta);
                LastSignedTurnDegrees = LocomotionMath.SignedYawDelta(currentYaw, targetYaw);
            }

            animatorSpeed = isMoving
                ? Mathf.Clamp01(LastPlanarSpeed / Mathf.Max(0.001f, referenceMoveSpeed))
                : 0f;
            animatorTurn = LocomotionMath.ResolveTurnValue(
                LastSignedTurnRateDegreesPerSecond,
                animatorTurn,
                isMoving,
                turnStartThresholdDegrees,
                turnStopThresholdDegrees,
                fullTurnDegrees);

            ApplyAnimatorParameters(dt);
            LogLocomotionSource();
        }

        public void ResetTracking()
        {
            lastPosition = transform.position;
            LastPlanarSpeed = 0f;
            LastSignedTurnDegrees = 0f;
            LastSignedTurnRateDegreesPerSecond = 0f;
            lastYaw = transform.eulerAngles.y;
            isMoving = false;
            animatorSpeed = 0f;
            animatorTurn = 0f;
            idleLogged = false;
            walkLogged = false;
            leftTurnLogged = false;
            rightTurnLogged = false;
            ApplyAnimatorParameters(Time.deltaTime > 0f ? Time.deltaTime : 0.016f);
        }

        private void ConfigureAnimator()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.applyRootMotion = false;

            if (animator.runtimeAnimatorController == null && !string.IsNullOrWhiteSpace(controllerResource))
            {
                var controller = Resources.Load<RuntimeAnimatorController>(controllerResource);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
                else if (!missingControllerLogged)
                {
                    Debug.LogWarning($"AgentLocomotionDriver: missing controller resource {controllerResource}.");
                    missingControllerLogged = true;
                }
            }

            speedHash = Animator.StringToHash(speedParameter);
            turnHash = Animator.StringToHash(turnParameter);
            hasSpeedParameter = HasParameter(animator, speedParameter, AnimatorControllerParameterType.Float);
            hasTurnParameter = HasParameter(animator, turnParameter, AnimatorControllerParameterType.Float);
            Debug.Log(
                "AgentLocomotionDriver: Animator locomotion ready " +
                $"controller={animator.runtimeAnimatorController?.name ?? "null"}, " +
                $"rootMotion={animator.applyRootMotion}, hasSpeed={hasSpeedParameter}, hasTurn={hasTurnParameter}.");

            if ((!hasSpeedParameter || !hasTurnParameter) && !missingParameterLogged)
            {
                Debug.LogWarning(
                    "AgentLocomotionDriver: Animator parameter mismatch " +
                    $"controller={animator.runtimeAnimatorController?.name ?? "null"}, " +
                    $"missingSpeed={!hasSpeedParameter}, missingTurn={!hasTurnParameter}, " +
                    $"expectedSpeed={speedParameter}, expectedTurn={turnParameter}.");
                missingParameterLogged = true;
            }
        }

        private void ApplyAnimatorParameters(float dt)
        {
            if (animator == null)
            {
                return;
            }

            if (hasSpeedParameter)
            {
                animator.SetFloat(speedHash, animatorSpeed, animatorDampTime, dt);
            }

            if (hasTurnParameter)
            {
                animator.SetFloat(turnHash, animatorTurn, animatorDampTime, dt);
            }
        }

        private void LogLocomotionSource()
        {
            if (!isMoving)
            {
                if (!idleLogged)
                {
                    Debug.Log(
                        "AgentLocomotionDriver: idle animation active " +
                        $"state=Idle, source=Idle.fbx, controller={animator?.runtimeAnimatorController?.name ?? "null"}.");
                    idleLogged = true;
                }

                return;
            }

            if (animatorTurn < -0.15f)
            {
                if (!leftTurnLogged)
                {
                    Debug.Log(
                        "AgentLocomotionDriver: briefcase-left turn animation active " +
                        $"state=LocomotionBlendTree, source=Left Turn W_Briefcase.fbx, clip=TurnLeft_Briefcase, " +
                        $"turn={animatorTurn:0.00}, speed={animatorSpeed:0.00}, " +
                        $"turnRate={LastSignedTurnRateDegreesPerSecond:0.0}, " +
                        $"headingDelta={LastSignedTurnDegrees:0.0}.");
                    leftTurnLogged = true;
                }

                return;
            }

            if (animatorTurn > 0.15f)
            {
                if (!rightTurnLogged)
                {
                    Debug.Log(
                        "AgentLocomotionDriver: briefcase-right turn animation active " +
                        "state=LocomotionBlendTree, source=Right Turn W_Briefcase_Mirrored.fbx, " +
                        "clip=TurnRight_Briefcase, " +
                        $"turn={animatorTurn:0.00}, speed={animatorSpeed:0.00}, " +
                        $"turnRate={LastSignedTurnRateDegreesPerSecond:0.0}, " +
                        $"headingDelta={LastSignedTurnDegrees:0.0}.");
                    rightTurnLogged = true;
                }

                return;
            }

            if (!walkLogged)
            {
                Debug.Log(
                    "AgentLocomotionDriver: walk animation active " +
                    $"state=LocomotionBlendTree, source=Walking-2.fbx, speed={animatorSpeed:0.00}, " +
                    $"controller={animator?.runtimeAnimatorController?.name ?? "null"}.");
                walkLogged = true;
            }
        }

        private static bool HasParameter(
            Animator candidate,
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            if (candidate == null || candidate.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (var parameter in candidate.parameters)
            {
                if (parameter.name == parameterName && parameter.type == parameterType)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class LocomotionMath
    {
        public static float PlanarSpeed(Vector3 planarDelta, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return 0f;
            }

            planarDelta.y = 0f;
            return planarDelta.magnitude / deltaTime;
        }

        public static bool ResolveMoving(
            float speed,
            bool wasMoving,
            float startSpeed,
            float stopSpeed)
        {
            return wasMoving
                ? speed > Mathf.Max(0f, stopSpeed)
                : speed >= Mathf.Max(0f, startSpeed);
        }

        public static float YawFromPlanarDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return 0f;
            }

            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        public static float SignedYawDelta(float currentYawDegrees, float targetYawDegrees)
        {
            return Mathf.DeltaAngle(currentYawDegrees, targetYawDegrees);
        }

        public static float SignedYawRate(float previousYawDegrees, float currentYawDegrees, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return 0f;
            }

            return Mathf.DeltaAngle(previousYawDegrees, currentYawDegrees) / deltaTime;
        }

        public static float ResolveTurnValue(
            float signedYawDegrees,
            float previousTurn,
            bool isMoving,
            float startThresholdDegrees,
            float stopThresholdDegrees,
            float fullTurnDegrees)
        {
            if (!isMoving)
            {
                return 0f;
            }

            var start = Mathf.Max(0f, startThresholdDegrees);
            var stop = Mathf.Clamp(stopThresholdDegrees, 0f, start);
            var absYaw = Mathf.Abs(signedYawDegrees);
            if (Mathf.Approximately(previousTurn, 0f))
            {
                return absYaw >= start ? NormalizeTurn(signedYawDegrees, fullTurnDegrees) : 0f;
            }

            if (absYaw <= stop)
            {
                return 0f;
            }

            return absYaw >= start
                ? NormalizeTurn(signedYawDegrees, fullTurnDegrees)
                : Mathf.Sign(previousTurn) * Mathf.Clamp01(absYaw / Mathf.Max(0.001f, fullTurnDegrees));
        }

        private static float NormalizeTurn(float signedYawDegrees, float fullTurnDegrees)
        {
            return Mathf.Clamp(
                signedYawDegrees / Mathf.Max(0.001f, fullTurnDegrees),
                -1f,
                1f);
        }
    }
}
