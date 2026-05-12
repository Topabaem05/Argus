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
        private MicroBehaviorScheduler microBehaviorScheduler;

        private MotionIntent currentIntent;
        private bool hasIntent;

        public string AgentId { get; private set; } = string.Empty;

        public MinibotMotionState State { get; private set; } = MinibotMotionState.SpawnedIdle;

        public MotionIntent CurrentIntent => currentIntent;

        public SmoothRigidbodyMotor Motor => motor;

        private void Awake()
        {
            EnsureComponents();
        }

        public void Initialize(string agentId, int deterministicSeed)
        {
            AgentId = agentId ?? string.Empty;
            EnsureComponents();
            microBehaviorScheduler.Initialize(deterministicSeed);
            ApplyIntent(MotionIntent.Idle(transform.position));
        }

        public void ApplyIntent(MotionIntent intent)
        {
            EnsureComponents();
            currentIntent = intent;
            hasIntent = true;
            State = ResolveState(intent);
            motor.SetIntent(intent);
            animatorDriver.SetIntent(intent);
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
                ApplyIntent(microBehaviorScheduler.BuildIdleIntent(transform.position));
            }
            else if (State == MinibotMotionState.SpawnedIdle && dt > 0f)
            {
                var idle = microBehaviorScheduler.TickIdle(transform.position, currentIntent.Emotion, dt);
                if (idle.Gesture != MotionGesture.None)
                {
                    ApplyIntent(idle);
                }
            }

            stuckDetector.Tick(motor, currentIntent, dt);
            if (stuckDetector.IsStuck && State != MinibotMotionState.StuckRecovery)
            {
                ApplyIntent(microBehaviorScheduler.BuildStuckRecoveryIntent(transform.position, transform.rotation));
            }

            animatorDriver.Tick(dt);
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
                microBehaviorScheduler = GetComponent<MicroBehaviorScheduler>() ?? gameObject.AddComponent<MicroBehaviorScheduler>();
            }
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

            if (intent.HasMoveTarget)
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
