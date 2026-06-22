using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class MinibotMotionController : MonoBehaviour
    {
        [SerializeField]
        private SmoothRigidbodyMotor motor;

        [SerializeField]
        private MinibotAnimatorDriver animatorDriver;

        [SerializeField]
        private StuckDetector stuckDetector;

        [SerializeField]
        private MinibotMicroBehaviorScheduler microBehaviorScheduler;

        [SerializeField]
        private MinibotStuckRecovery stuckRecovery;

        [SerializeField]
        private PersonaMotionProfile motionProfile = new PersonaMotionProfile();

        [SerializeField]
        private MinibotMotionDebugState debugState = new MinibotMotionDebugState();
        [SerializeField]
        private ProceduralLegController proceduralLegController;

        private MotionIntent currentIntent;
        private bool hasIntent;
        private readonly MotionSelectionPolicy selectionPolicy = new MotionSelectionPolicy();

        public string AgentId { get; private set; } = string.Empty;

        public MinibotMotionState State { get; private set; } = MinibotMotionState.SpawnedIdle;

        public MotionIntent CurrentIntent => currentIntent;

        public SmoothRigidbodyMotor Motor => motor;

        public MinibotMotionDebugState DebugState => debugState;

        public PersonaMotionProfile MotionProfile => motionProfile;

        private void Awake()
        {
            EnsureComponents();
        }

        public void Initialize(string agentId, int deterministicSeed)
        {
            AgentId = agentId ?? string.Empty;
            EnsureComponents();
            motionProfile = PersonaMotionProfile.FromAgentId(AgentId, deterministicSeed);
            debugState.AttachProfile(motionProfile);
            selectionPolicy.Initialize(motionProfile.Seed);
            microBehaviorScheduler.Initialize(motionProfile.Seed);
            ApplyIntent(MotionIntent.Idle(transform.position));
        }

        public void ApplyIntent(MotionIntent intent)
        {
            EnsureComponents();
            currentIntent = intent;
            hasIntent = true;
            State = ResolveState(intent);
            var selection = selectionPolicy.Select(intent, motionProfile, Time.time);
            motor.SetIntent(intent);
            animatorDriver.SetIntent(intent);
            animatorDriver.SetSelection(selection, debugState);
            debugState.Apply(intent, selection, default, motor, stuckDetector != null && stuckDetector.IsStuck, ResolveTurn());
            if (stuckDetector != null && State != MinibotMotionState.StuckRecovery)
            {
                stuckDetector.ResetDetector();
            }
        }

        private void Update()
        {
            EnsureComponents();
            var dt = Time.deltaTime;
            if (!hasIntent)
            {
                ApplyIntent(MotionIntent.Idle(transform.position));
            }

            stuckDetector.Tick(motor, currentIntent, dt);
            if (stuckDetector.IsStuck &&
                State != MinibotMotionState.StuckRecovery &&
                stuckRecovery.CanRecover(Time.time))
            {
                var recoveryIntent = stuckRecovery.BuildRecoveryIntent(transform.position, transform.rotation, motionProfile, Time.time);
                debugState.MarkRecovery(Time.time);
                microBehaviorScheduler.MarkRecovery(Time.time);
                ApplyIntent(recoveryIntent);
            }
            else if (hasIntent &&
                     microBehaviorScheduler.TryBuildMicroIntent(
                         currentIntent,
                         motionProfile,
                         transform.position,
                         dt,
                         Time.time,
                         motor.CurrentVelocity.magnitude,
                         out var microIntent))
            {
                var microSelection = selectionPolicy.Select(microIntent, motionProfile, Time.time);
                animatorDriver.SetTransientOverlay(microIntent, microSelection, 1.25f);
            }

            animatorDriver.Tick(dt);
            if (proceduralLegController != null)
            {
                proceduralLegController.SetMoveDirection(motor.CurrentVelocity);
                proceduralLegController.SetMoveSpeed(motor.CurrentVelocity.magnitude);
            }
            debugState.Apply(currentIntent, animatorDriver.CurrentSelection, animatorDriver.AppliedSelection, motor, stuckDetector.IsStuck, ResolveTurn());
        }

        private void EnsureComponents()
        {
            if (motor == null)
            {
                motor = GetComponent<SmoothRigidbodyMotor>() ?? gameObject.AddComponent<SmoothRigidbodyMotor>();
            }

            if (animatorDriver == null)
            {
                animatorDriver = GetComponent<MinibotAnimatorDriver>() ?? gameObject.AddComponent<MinibotAnimatorDriver>();
            }

            if (stuckDetector == null)
            {
                stuckDetector = GetComponent<StuckDetector>() ?? gameObject.AddComponent<StuckDetector>();
            }

            if (microBehaviorScheduler == null)
            {
                microBehaviorScheduler = GetComponent<MinibotMicroBehaviorScheduler>() ??
                                         gameObject.AddComponent<MinibotMicroBehaviorScheduler>();
            }

            if (stuckRecovery == null)
            {
                stuckRecovery = GetComponent<MinibotStuckRecovery>() ?? gameObject.AddComponent<MinibotStuckRecovery>();
            }

            if (motionProfile == null)
            {
                motionProfile = new PersonaMotionProfile();
            }

            if (debugState == null)
            {
                debugState = new MinibotMotionDebugState();
            }
        }

        private float ResolveTurn()
        {
            if (motor == null)
            {
                return 0f;
            }

            var desired = motor.DesiredVelocity;
            desired.y = 0f;
            if (desired.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Clamp(Vector3.SignedAngle(transform.forward, desired.normalized, Vector3.up) / 180f, -1f, 1f);
        }

        private static MinibotMotionState ResolveState(MotionIntent intent)
        {
            switch (intent.Action)
            {
                case MotionAction.Dodge:
                    return MinibotMotionState.AvoidingObstacle;
                case MotionAction.HitReaction:
                    return MinibotMotionState.HitReacting;
                case MotionAction.Fall:
                    return MinibotMotionState.Falling;
                case MotionAction.GetUp:
                    return MinibotMotionState.GettingUp;
                case MotionAction.StepBackward:
                    return MinibotMotionState.StuckRecovery;
                case MotionAction.ButtonPush:
                case MotionAction.PickUp:
                case MotionAction.Push:
                case MotionAction.PullHeavy:
                    return MinibotMotionState.Interacting;
            }

            if (intent.HasMoveTarget && intent.Gesture != MotionGesture.None)
            {
                return MinibotMotionState.SpeakingWhileMoving;
            }

            if (intent.HasMoveTarget && intent.DesiredSpeedMetersPerSecond > 0.01f)
            {
                return MinibotMotionState.MovingToTarget;
            }

            if (intent.Gesture == MotionGesture.Talk || intent.Gesture == MotionGesture.TalkAlt)
            {
                return MinibotMotionState.SpeakingIdle;
            }

            if (intent.Emotion == MotionEmotion.Thinking || intent.Gesture == MotionGesture.Think)
            {
                return MinibotMotionState.Thinking;
            }

            return MinibotMotionState.SpawnedIdle;
        }
    }
}
