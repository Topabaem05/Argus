using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MiniBotAnimationPreviewDriver : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private RuntimeAnimatorController locomotionController;

        [SerializeField]
        private RuntimeAnimatorController clipPreviewController;

        [SerializeField]
        private TextMesh label;

        private readonly LocomotionPreviewSegment[] locomotionSegments =
        {
            new LocomotionPreviewSegment("Production BlendTree / Idle", 2.0f, 0f, 0f),
            new LocomotionPreviewSegment("Production BlendTree / Walk_InPlace", 2.4f, 1f, 0f),
            new LocomotionPreviewSegment("Production BlendTree / TurnLeft_Briefcase", 2.4f, 1f, -1f),
            new LocomotionPreviewSegment("Production BlendTree / TurnRight_Briefcase", 2.4f, 1f, 1f),
            new LocomotionPreviewSegment("Production BlendTree / Walk recovery", 1.8f, 1f, 0f),
            new LocomotionPreviewSegment("Production BlendTree / Idle recovery", 1.8f, 0f, 0f)
        };

        private readonly ClipPreviewSegment[] clipSegments =
        {
            new ClipPreviewSegment("Preview_Idle", "Idle.fbx / Idle", 3.2f),
            new ClipPreviewSegment("Preview_Walk_InPlace", "Walking-2.fbx / Walk_InPlace", 2.4f),
            new ClipPreviewSegment("Preview_SlowRun", "QUARANTINED - Slow Run.fbx / SlowRun - sways left/right", 2.0f),
            new ClipPreviewSegment("Preview_Run", "QUARANTINED - Running.fbx / Run - full-circle/in-air", 3.2f),
            new ClipPreviewSegment("Preview_RunToTurn", "QUARANTINED - Running To Turn.fbx / RunToTurn - candidate only", 2.6f),
            new ClipPreviewSegment("Preview_TurnLeft_Happy", "QUARANTINED - Happy Right Turn-2.fbx / TurnLeft_Happy - rotates in air", 1.6f),
            new ClipPreviewSegment("Preview_TurnRight_Happy", "QUARANTINED - Happy Right Turn.fbx / TurnRight_Happy - in-place/no turn", 1.6f),
            new ClipPreviewSegment("Preview_TurnLeft_Briefcase", "Left Turn W_Briefcase.fbx / TurnLeft_Briefcase", 2.6f),
            new ClipPreviewSegment("Preview_TurnLeft_Briefcase_Alt", "Left Turn W_Briefcase-2.fbx / TurnLeft_Briefcase_Alt", 2.6f),
            new ClipPreviewSegment("Preview_TurnRight_Briefcase", "Right Turn W_Briefcase_Mirrored.fbx / TurnRight_Briefcase", 2.6f),
            new ClipPreviewSegment("Preview_Thinking", "Thinking.fbx / Thinking", 4.6f),
            new ClipPreviewSegment("Preview_Angry", "Angry.fbx / Angry", 19.6f),
            new ClipPreviewSegment("Preview_LayingPose", "Male Laying Pose.fbx / LayingPose", 1.2f),
            new ClipPreviewSegment("Preview_StandingTorch", "Standing Torch Light Torch.fbx / StandingTorch", 4.4f)
        };

        private float locomotionDuration;
        private int activeLocomotionIndex = -1;
        private int activeClipIndex = -1;
        private bool usingClipController;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int TurnHash = Animator.StringToHash("Turn");

        public void Configure(
            Animator previewAnimator,
            RuntimeAnimatorController productionLocomotionController,
            RuntimeAnimatorController perClipPreviewController,
            TextMesh statusLabel)
        {
            animator = previewAnimator;
            locomotionController = productionLocomotionController;
            clipPreviewController = perClipPreviewController;
            label = statusLabel;
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
                if (locomotionController != null)
                {
                    animator.runtimeAnimatorController = locomotionController;
                }
            }

            locomotionDuration = 0f;
            foreach (var segment in locomotionSegments)
            {
                locomotionDuration += segment.Duration;
            }
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            var elapsed = Time.timeSinceLevelLoad;
            if (elapsed < locomotionDuration)
            {
                UpdateLocomotionPreview(elapsed);
                return;
            }

            UpdateClipPreview(elapsed - locomotionDuration);
        }

        private void UpdateLocomotionPreview(float elapsed)
        {
            if (usingClipController && locomotionController != null)
            {
                animator.runtimeAnimatorController = locomotionController;
                usingClipController = false;
            }

            var segmentIndex = ResolveLocomotionIndex(elapsed, out var localTime);
            var segment = locomotionSegments[segmentIndex];
            if (segmentIndex != activeLocomotionIndex)
            {
                activeLocomotionIndex = segmentIndex;
                Debug.Log(
                    "MiniBotAnimationPreview: locomotion segment " +
                    $"label={segment.Label}, speed={segment.Speed:0.00}, turn={segment.Turn:0.00}.");
            }

            var normalized = Mathf.Clamp01(localTime / Mathf.Max(0.001f, segment.Duration));
            var previousSpeed = segmentIndex > 0 ? locomotionSegments[segmentIndex - 1].Speed : segment.Speed;
            var previousTurn = segmentIndex > 0 ? locomotionSegments[segmentIndex - 1].Turn : segment.Turn;
            var speed = Mathf.Lerp(previousSpeed, segment.Speed, Mathf.SmoothStep(0f, 1f, normalized));
            var turn = Mathf.Lerp(previousTurn, segment.Turn, Mathf.SmoothStep(0f, 1f, normalized));
            animator.SetFloat(SpeedHash, speed, 0.12f, Time.deltaTime);
            animator.SetFloat(TurnHash, turn, 0.12f, Time.deltaTime);
            SetLabel($"{segment.Label}\nSpeed {speed:0.00} / Turn {turn:0.00}");
        }

        private int ResolveLocomotionIndex(float elapsed, out float localTime)
        {
            var cursor = 0f;
            for (var i = 0; i < locomotionSegments.Length; i++)
            {
                var next = cursor + locomotionSegments[i].Duration;
                if (elapsed <= next)
                {
                    localTime = elapsed - cursor;
                    return i;
                }

                cursor = next;
            }

            localTime = locomotionSegments[locomotionSegments.Length - 1].Duration;
            return locomotionSegments.Length - 1;
        }

        private void UpdateClipPreview(float elapsed)
        {
            if (!usingClipController && clipPreviewController != null)
            {
                animator.runtimeAnimatorController = clipPreviewController;
                usingClipController = true;
                activeClipIndex = -1;
            }

            var segmentIndex = ResolveClipIndex(elapsed, out var localTime);
            var segment = clipSegments[segmentIndex];
            if (segmentIndex != activeClipIndex)
            {
                activeClipIndex = segmentIndex;
                animator.CrossFadeInFixedTime(segment.StateName, 0.18f, 0, 0f);
                Debug.Log(
                    "MiniBotAnimationPreview: clip segment " +
                    $"label={segment.Label}, state={segment.StateName}.");
            }

            SetLabel($"{segment.Label}\nDirect clip preview / transition crossfade\n{localTime:0.00}s / {segment.Duration:0.00}s");
        }

        private int ResolveClipIndex(float elapsed, out float localTime)
        {
            var cursor = 0f;
            for (var i = 0; i < clipSegments.Length; i++)
            {
                var next = cursor + clipSegments[i].Duration;
                if (elapsed <= next)
                {
                    localTime = elapsed - cursor;
                    return i;
                }

                cursor = next;
            }

            localTime = clipSegments[clipSegments.Length - 1].Duration;
            return clipSegments.Length - 1;
        }

        private void SetLabel(string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private readonly struct LocomotionPreviewSegment
        {
            public LocomotionPreviewSegment(string label, float duration, float speed, float turn)
            {
                Label = label;
                Duration = duration;
                Speed = speed;
                Turn = turn;
            }

            public string Label { get; }
            public float Duration { get; }
            public float Speed { get; }
            public float Turn { get; }
        }

        private readonly struct ClipPreviewSegment
        {
            public ClipPreviewSegment(string stateName, string label, float duration)
            {
                StateName = stateName;
                Label = label;
                Duration = duration;
            }

            public string StateName { get; }
            public string Label { get; }
            public float Duration { get; }
        }
    }
}
