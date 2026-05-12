using System;
using UnityEngine;

namespace ArgusUnity.Motion
{
    [Serializable]
    public sealed class MinibotMotionDebugState
    {
        [SerializeField]
        private MotionIntentType currentIntent;

        [SerializeField]
        private MotionClipId selectedBaseClip;

        [SerializeField]
        private MotionClipId selectedOverlayClip;

        [SerializeField]
        private MotionClipId selectedEmotionClip;

        [SerializeField]
        private MotionClipId currentBaseClip;

        [SerializeField]
        private MotionClipId currentOverlayClip;

        [SerializeField]
        private MotionClipId currentEmotionClip;

        [SerializeField]
        private string currentBaseClipName = string.Empty;

        [SerializeField]
        private string currentOverlayClipName = string.Empty;

        [SerializeField]
        private string currentEmotionClipName = string.Empty;

        [SerializeField]
        private string selectedBaseClipName = string.Empty;

        [SerializeField]
        private string selectedOverlayClipName = string.Empty;

        [SerializeField]
        private string selectedEmotionClipName = string.Empty;

        [SerializeField]
        private float currentSpeed;

        [SerializeField]
        private float currentTurn;

        [SerializeField]
        private bool isStuck;

        [SerializeField]
        private float lastRecoveryTime = -999f;

        [SerializeField]
        private string[] lastFiveUsedClips = Array.Empty<string>();

        [SerializeField]
        private PersonaMotionProfile personaMotionProfile;

        public MotionIntentType CurrentIntent => currentIntent;

        public MotionClipId CurrentBaseClip => currentBaseClip;

        public MotionClipId CurrentOverlayClip => currentOverlayClip;

        public MotionClipId CurrentEmotionClip => currentEmotionClip;

        public string CurrentBaseClipName => currentBaseClipName;

        public string CurrentOverlayClipName => currentOverlayClipName;

        public string CurrentEmotionClipName => currentEmotionClipName;

        public MotionClipId SelectedBaseClip => selectedBaseClip;

        public MotionClipId SelectedOverlayClip => selectedOverlayClip;

        public MotionClipId SelectedEmotionClip => selectedEmotionClip;

        public string SelectedBaseClipName => selectedBaseClipName;

        public string SelectedOverlayClipName => selectedOverlayClipName;

        public string SelectedEmotionClipName => selectedEmotionClipName;

        public float CurrentSpeed => currentSpeed;

        public float CurrentTurn => currentTurn;

        public bool IsStuck => isStuck;

        public float LastRecoveryTime => lastRecoveryTime;

        public string[] LastFiveUsedClips => lastFiveUsedClips;

        public PersonaMotionProfile PersonaMotionProfile => personaMotionProfile;

        public void AttachProfile(PersonaMotionProfile profile)
        {
            personaMotionProfile = profile;
        }

        public void MarkRecovery(float timeSeconds)
        {
            lastRecoveryTime = timeSeconds;
        }

        public void Apply(
            MotionIntent intent,
            MotionSelection selected,
            MotionSelection applied,
            SmoothRigidbodyMotor motor,
            bool stuck,
            float turn)
        {
            currentIntent = intent.Type;
            selectedBaseClip = selected.BaseClip;
            selectedOverlayClip = selected.OverlayClip;
            selectedEmotionClip = selected.EmotionClip;
            selectedBaseClipName = ClipName(selected.BaseClip);
            selectedOverlayClipName = ClipName(selected.OverlayClip);
            selectedEmotionClipName = ClipName(selected.EmotionClip);
            currentBaseClip = applied.BaseClip;
            currentOverlayClip = applied.OverlayClip;
            currentEmotionClip = applied.EmotionClip;
            currentBaseClipName = ClipName(applied.BaseClip);
            currentOverlayClipName = ClipName(applied.OverlayClip);
            currentEmotionClipName = ClipName(applied.EmotionClip);
            currentSpeed = motor != null ? motor.CurrentVelocity.magnitude : 0f;
            currentTurn = turn;
            isStuck = stuck;
            lastFiveUsedClips = BuildRecentNames(selected.RecentClips);
        }

        private static string ClipName(MotionClipId clipId)
        {
            return MotionCatalog.TryGet(clipId, out var clip) ? clip.ClipName : string.Empty;
        }

        private static string[] BuildRecentNames(MotionClipId[] recent)
        {
            if (recent == null || recent.Length == 0)
            {
                return Array.Empty<string>();
            }

            var names = new string[recent.Length];
            for (var i = 0; i < recent.Length; i++)
            {
                names[i] = ClipName(recent[i]);
            }

            return names;
        }
    }
}
