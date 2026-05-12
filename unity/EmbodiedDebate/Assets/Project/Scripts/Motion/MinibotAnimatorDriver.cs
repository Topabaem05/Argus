using System.Collections.Generic;
using UnityEngine;

namespace ArgusUnity.Motion
{
    /// <summary>
    /// Converts smoothed motor velocity and semantic intent into Animator parameters.
    /// </summary>
    public sealed class MinibotAnimatorDriver : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private SmoothRigidbodyMotor motor;

        [SerializeField]
        private float maxRunSpeed = 1.8f;

        [SerializeField]
        private float dampTime = 0.12f;

        [SerializeField]
        private float gestureFadeSpeed = 5f;

        private readonly Dictionary<string, AnimatorControllerParameterType> parameters =
            new Dictionary<string, AnimatorControllerParameterType>();

        private MotionIntent currentIntent;
        private float gestureWeight;

        public float LastMoveX { get; private set; }

        public float LastMoveZ { get; private set; }

        public float LastSpeed { get; private set; }

        public float LastGestureWeight => gestureWeight;

        private void Awake()
        {
            EnsureReferences();
            CacheParameters();
        }

        public void SetIntent(MotionIntent intent)
        {
            currentIntent = intent;
        }

        public void Tick(float deltaTime)
        {
            EnsureReferences();
            if (animator == null || motor == null || deltaTime <= 0f)
            {
                return;
            }

            CacheParameters();
            var localVelocity = transform.InverseTransformDirection(motor.CurrentVelocity);
            LastMoveX = Mathf.Clamp(localVelocity.x / Mathf.Max(0.001f, maxRunSpeed), -1f, 1f);
            LastMoveZ = Mathf.Clamp(localVelocity.z / Mathf.Max(0.001f, maxRunSpeed), -1f, 1f);
            LastSpeed = Mathf.Clamp01(motor.CurrentVelocity.magnitude / Mathf.Max(0.001f, maxRunSpeed));

            var targetGestureWeight = currentIntent.Gesture != MotionGesture.None ? 1f : 0f;
            if (currentIntent.Emotion != MotionEmotion.Neutral && currentIntent.Gesture == MotionGesture.None)
            {
                targetGestureWeight = 0.45f;
            }

            gestureWeight = Mathf.MoveTowards(
                gestureWeight,
                targetGestureWeight,
                Mathf.Max(0f, gestureFadeSpeed) * deltaTime);

            SetFloat("MoveX", LastMoveX, deltaTime);
            SetFloat("MoveZ", LastMoveZ, deltaTime);
            SetFloat("Speed", LastSpeed, deltaTime);
            SetFloat("AngularSpeed", ResolveAngularSpeed(), deltaTime);
            SetFloat("GestureWeight", gestureWeight, deltaTime);
            SetBool("Grounded", true);
            SetBool("IsMoving", LastSpeed > 0.05f);
            SetBool("IsStuck", motor.ObstacleAhead);
            SetInteger("Emotion", (int)currentIntent.Emotion);
            SetInteger("Gesture", (int)currentIntent.Gesture);
            SetInteger("Action", (int)currentIntent.Action);

            TriggerAction(currentIntent.Action);
        }

        private void TriggerAction(MotionAction action)
        {
            switch (action)
            {
                case MotionAction.Dodge:
                    SetTrigger("DodgeTrigger");
                    break;
                case MotionAction.Fall:
                    SetTrigger("FallTrigger");
                    break;
                case MotionAction.GetUp:
                    SetTrigger("GetUpTrigger");
                    break;
                case MotionAction.HitReaction:
                    SetTrigger("ImpactTrigger");
                    break;
                case MotionAction.StepBackward:
                    SetTrigger("StepBackTrigger");
                    break;
            }
        }

        private float ResolveAngularSpeed()
        {
            var desired = motor.DesiredVelocity;
            desired.y = 0f;
            if (desired.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Clamp(
                Vector3.SignedAngle(transform.forward, desired.normalized, Vector3.up) / 90f,
                -1f,
                1f);
        }

        private void EnsureReferences()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            if (motor == null)
            {
                motor = GetComponent<SmoothRigidbodyMotor>();
            }
        }

        private void CacheParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null || parameters.Count == animator.parameters.Length)
            {
                return;
            }

            parameters.Clear();
            foreach (var parameter in animator.parameters)
            {
                parameters[parameter.name] = parameter.type;
            }
        }

        private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
        {
            return parameters.TryGetValue(parameterName, out var actual) && actual == type;
        }

        private void SetFloat(string parameterName, float value, float deltaTime)
        {
            if (HasParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value, dampTime, deltaTime);
            }
        }

        private void SetBool(string parameterName, bool value)
        {
            if (HasParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterName, value);
            }
        }

        private void SetInteger(string parameterName, int value)
        {
            if (HasParameter(parameterName, AnimatorControllerParameterType.Int))
            {
                animator.SetInteger(parameterName, value);
            }
        }

        private void SetTrigger(string parameterName)
        {
            if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(parameterName);
            }
        }
    }
}
