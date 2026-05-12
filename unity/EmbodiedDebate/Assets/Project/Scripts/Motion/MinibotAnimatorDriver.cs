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

        [SerializeField]
        private bool crossFadeNamedStates = true;

        [SerializeField]
        private int overlayLayerIndex = 1;

        [SerializeField]
        private int emotionLayerIndex = 2;

        private readonly Dictionary<string, AnimatorControllerParameterType> parameters =
            new Dictionary<string, AnimatorControllerParameterType>();

        private MotionIntent currentIntent;
        private MotionSelection currentSelection;
        private MotionSelection appliedSelection;
        private MotionIntent transientOverlayIntent;
        private MotionSelection transientOverlaySelection;
        private MinibotMotionDebugState debugState;
        private float gestureWeight;
        private float transientOverlaySecondsRemaining;
        private MotionClipId lastBaseClip;
        private MotionClipId lastOverlayClip;
        private MotionClipId lastEmotionClip;
        private MotionAction lastTriggeredAction = MotionAction.None;
        private MotionGesture lastTriggeredGesture = MotionGesture.None;
        private MotionEmotion lastTriggeredEmotion = MotionEmotion.Neutral;

        public float LastMoveX { get; private set; }

        public float LastMoveZ { get; private set; }

        public float LastSpeed { get; private set; }

        public float LastGestureWeight => gestureWeight;

        public MotionSelection CurrentSelection => ResolveEffectiveSelection();

        public MotionSelection AppliedSelection => appliedSelection;

        private void Awake()
        {
            EnsureReferences();
            CacheParameters();
        }

        public void SetIntent(MotionIntent intent)
        {
            currentIntent = intent;
        }

        public void SetSelection(MotionSelection selection, MinibotMotionDebugState nextDebugState)
        {
            currentSelection = selection;
            debugState = nextDebugState;
        }

        public void SetTransientOverlay(MotionIntent intent, MotionSelection selection, float durationSeconds)
        {
            transientOverlayIntent = intent;
            transientOverlaySelection = selection;
            transientOverlaySecondsRemaining = Mathf.Max(0.05f, durationSeconds);
        }

        public void Tick(float deltaTime)
        {
            EnsureReferences();
            if (animator == null || motor == null || deltaTime <= 0f)
            {
                return;
            }

            CacheParameters();
            var effectiveIntent = ResolveEffectiveIntent(deltaTime);
            var effectiveSelection = ResolveEffectiveSelection();
            var localVelocity = transform.InverseTransformDirection(motor.CurrentVelocity);
            LastMoveX = Mathf.Clamp(localVelocity.x / Mathf.Max(0.001f, maxRunSpeed), -1f, 1f);
            LastMoveZ = Mathf.Clamp(localVelocity.z / Mathf.Max(0.001f, maxRunSpeed), -1f, 1f);
            LastSpeed = Mathf.Clamp01(motor.CurrentVelocity.magnitude / Mathf.Max(0.001f, maxRunSpeed));

            var targetGestureWeight = effectiveIntent.Gesture != MotionGesture.None ? 1f : 0f;
            if (effectiveIntent.Emotion != MotionEmotion.Neutral && effectiveIntent.Gesture == MotionGesture.None)
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
            var turn = ResolveAngularSpeed();
            SetFloat("Turn", turn, deltaTime);
            SetFloat("AngularSpeed", turn, deltaTime);
            SetFloat("AngularError", Mathf.Abs(turn), deltaTime);
            SetFloat("GestureWeight", gestureWeight, deltaTime);
            SetFloat("OverlayWeight", gestureWeight, deltaTime);
            SetBool("Grounded", true);
            SetBool("IsGrounded", true);
            SetBool("IsMoving", LastSpeed > 0.05f);
            SetBool("IsStuck", motor.ObstacleAhead);
            SetBool("IsTalking", IsTalking(effectiveIntent));
            SetInteger("Emotion", (int)effectiveIntent.Emotion);
            SetInteger("Gesture", (int)effectiveIntent.Gesture);
            SetInteger("Action", (int)effectiveIntent.Action);
            SetInteger("MotionIntent", (int)effectiveIntent.Type);
            SetInteger("RecoveryState", IsRecovery(effectiveIntent.Action) ? (int)effectiveIntent.Action : 0);

            TriggerChangedEvents(effectiveIntent);
            appliedSelection = CrossFadeSelection(effectiveSelection);
            debugState?.Apply(currentIntent, effectiveSelection, appliedSelection, motor, motor.ObstacleAhead, turn);
        }

        private void TriggerChangedEvents(MotionIntent intent)
        {
            if (intent.Action != MotionAction.None && intent.Action != lastTriggeredAction)
            {
                switch (intent.Action)
                {
                    case MotionAction.Dodge:
                        SetTrigger("DodgeTrigger");
                        SetTrigger("RecoveryTrigger");
                        break;
                    case MotionAction.Fall:
                        SetTrigger("FallTrigger");
                        SetTrigger("RecoveryTrigger");
                        break;
                    case MotionAction.GetUp:
                        SetTrigger("GetUpTrigger");
                        SetTrigger("RecoveryTrigger");
                        break;
                    case MotionAction.HitReaction:
                        SetTrigger("ImpactTrigger");
                        SetTrigger("RecoveryTrigger");
                        break;
                    case MotionAction.StepBackward:
                        SetTrigger("StepBackTrigger");
                        SetTrigger("RecoveryTrigger");
                        break;
                    case MotionAction.ButtonPush:
                    case MotionAction.PickUp:
                    case MotionAction.Push:
                    case MotionAction.PullHeavy:
                        SetTrigger("InteractionTrigger");
                        break;
                }
            }

            if (intent.Gesture != MotionGesture.None && intent.Gesture != lastTriggeredGesture)
            {
                SetTrigger("TalkTrigger");
            }

            if (intent.Emotion != MotionEmotion.Neutral && intent.Emotion != lastTriggeredEmotion)
            {
                SetTrigger("EmotionTrigger");
            }

            lastTriggeredAction = intent.Action;
            lastTriggeredGesture = intent.Gesture;
            lastTriggeredEmotion = intent.Emotion;
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

        private MotionSelection CrossFadeSelection(MotionSelection selection)
        {
            if (!crossFadeNamedStates || animator == null || animator.runtimeAnimatorController == null)
            {
                return default;
            }

            var baseApplied = CrossFadeIfChanged(selection.BaseClip, ref lastBaseClip, 0, ResolveBaseFade(currentIntent))
                ? selection.BaseClip
                : MotionClipId.None;
            var overlayApplied = CrossFadeIfChanged(selection.OverlayClip, ref lastOverlayClip, overlayLayerIndex, 0.12f)
                ? selection.OverlayClip
                : MotionClipId.None;
            var emotionApplied = CrossFadeIfChanged(selection.EmotionClip, ref lastEmotionClip, emotionLayerIndex, 0.16f)
                ? selection.EmotionClip
                : MotionClipId.None;
            return new MotionSelection(baseApplied, overlayApplied, emotionApplied, selection.RecentClips);
        }

        private bool CrossFadeIfChanged(MotionClipId clipId, ref MotionClipId lastClip, int layerIndex, float fadeSeconds)
        {
            if (clipId == MotionClipId.None || !MotionCatalog.TryGet(clipId, out var clip))
            {
                return false;
            }

            if (layerIndex < 0 || layerIndex >= animator.layerCount)
            {
                return false;
            }

            var stateHash = Animator.StringToHash(clip.ClipName);
            if (!animator.HasState(layerIndex, stateHash))
            {
                return false;
            }

            if (clipId == lastClip)
            {
                return true;
            }

            animator.CrossFadeInFixedTime(stateHash, Mathf.Max(0.01f, fadeSeconds), layerIndex);
            lastClip = clipId;
            return true;
        }

        private MotionIntent ResolveEffectiveIntent(float deltaTime)
        {
            if (transientOverlaySecondsRemaining <= 0f)
            {
                return currentIntent;
            }

            transientOverlaySecondsRemaining = Mathf.Max(0f, transientOverlaySecondsRemaining - Mathf.Max(0f, deltaTime));
            return new MotionIntent(
                currentIntent.Type,
                currentIntent.HasMoveTarget,
                currentIntent.MoveTarget,
                currentIntent.FocusTarget,
                currentIntent.DesiredSpeedMetersPerSecond,
                currentIntent.StopDistance,
                transientOverlayIntent.Emotion != MotionEmotion.Neutral ? transientOverlayIntent.Emotion : currentIntent.Emotion,
                transientOverlayIntent.Gesture != MotionGesture.None ? transientOverlayIntent.Gesture : currentIntent.Gesture,
                currentIntent.Action,
                currentIntent.AllowMovementDuringGesture,
                currentIntent.Urgency,
                currentIntent.RequestedClip,
                currentIntent.Source);
        }

        private MotionSelection ResolveEffectiveSelection()
        {
            if (transientOverlaySecondsRemaining <= 0f)
            {
                return currentSelection;
            }

            var overlay = transientOverlaySelection.OverlayClip != MotionClipId.None
                ? transientOverlaySelection.OverlayClip
                : transientOverlaySelection.BaseClip;
            var emotion = transientOverlaySelection.EmotionClip != MotionClipId.None
                ? transientOverlaySelection.EmotionClip
                : currentSelection.EmotionClip;
            return new MotionSelection(
                currentSelection.BaseClip,
                overlay,
                emotion,
                transientOverlaySelection.RecentClips);
        }

        private static float ResolveBaseFade(MotionIntent intent)
        {
            if (IsRecovery(intent.Action))
            {
                return 0.08f;
            }

            if (intent.Action == MotionAction.ButtonPush ||
                intent.Action == MotionAction.PickUp ||
                intent.Action == MotionAction.Push ||
                intent.Action == MotionAction.PullHeavy)
            {
                return 0.18f;
            }

            return intent.HasMoveTarget ? 0.18f : 0.22f;
        }

        private static bool IsTalking(MotionIntent intent)
        {
            return intent.Gesture == MotionGesture.Talk ||
                   intent.Gesture == MotionGesture.TalkAlt ||
                   intent.Gesture == MotionGesture.Yell ||
                   intent.Type == MotionIntentType.Talk ||
                   intent.Type == MotionIntentType.Explain ||
                   intent.Type == MotionIntentType.Yell;
        }

        private static bool IsRecovery(MotionAction action)
        {
            return action == MotionAction.StepBackward ||
                   action == MotionAction.Dodge ||
                   action == MotionAction.HitReaction ||
                   action == MotionAction.Fall ||
                   action == MotionAction.GetUp;
        }
    }
}
