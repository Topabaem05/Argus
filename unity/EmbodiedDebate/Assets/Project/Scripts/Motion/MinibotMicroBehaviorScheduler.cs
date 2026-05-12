using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class MinibotMicroBehaviorScheduler : MonoBehaviour
    {
        private System.Random random = new System.Random(1);
        private MotionIntentType lastMicroIntent = MotionIntentType.None;
        private float elapsedSeconds;
        private float nextSeconds = 4f;
        private float recoveryCooldownUntil;

        public void Initialize(int deterministicSeed)
        {
            random = new System.Random(deterministicSeed == 0 ? 1 : deterministicSeed);
            elapsedSeconds = 0f;
            recoveryCooldownUntil = -999f;
            lastMicroIntent = MotionIntentType.None;
            nextSeconds = NextInterval(0f, false, false);
        }

        public void MarkRecovery(float timeSeconds)
        {
            recoveryCooldownUntil = timeSeconds + 2f;
            elapsedSeconds = 0f;
        }

        public bool TryBuildMicroIntent(
            MotionIntent currentIntent,
            PersonaMotionProfile profile,
            Vector3 position,
            float deltaTime,
            float timeSeconds,
            float currentSpeed,
            out MotionIntent microIntent)
        {
            microIntent = default;
            if (deltaTime <= 0f || IsStrongAction(currentIntent) || timeSeconds < recoveryCooldownUntil)
            {
                return false;
            }

            var isTalking = currentIntent.Gesture == MotionGesture.Talk ||
                            currentIntent.Gesture == MotionGesture.TalkAlt ||
                            currentIntent.Type == MotionIntentType.Talk ||
                            currentIntent.Type == MotionIntentType.Explain;
            var isSlow = currentSpeed < 0.35f;
            if (!isSlow && !isTalking)
            {
                return false;
            }

            elapsedSeconds += deltaTime;
            if (elapsedSeconds < nextSeconds)
            {
                return false;
            }

            elapsedSeconds = 0f;
            nextSeconds = NextInterval(profile?.Energy ?? 0.4f, isTalking, !isSlow);
            var microType = PickMicroIntent(currentIntent.Emotion, profile);
            if (microType == lastMicroIntent)
            {
                microType = MotionIntentType.LookAround;
            }

            lastMicroIntent = microType;
            microIntent = new MotionIntent(
                microType,
                currentIntent.HasMoveTarget,
                currentIntent.HasMoveTarget ? currentIntent.MoveTarget : position,
                currentIntent.FocusTarget,
                currentIntent.DesiredSpeedMetersPerSecond,
                currentIntent.StopDistance,
                currentIntent.Emotion,
                GestureFor(microType),
                MotionAction.None,
                true,
                Mathf.Max(0.2f, currentIntent.Urgency),
                MotionClipId.None,
                "micro");
            return true;
        }

        private MotionIntentType PickMicroIntent(MotionEmotion emotion, PersonaMotionProfile profile)
        {
            if (emotion == MotionEmotion.Thinking || (profile != null && profile.Curiosity > 0.62f))
            {
                return random.NextDouble() < 0.55 ? MotionIntentType.Think : MotionIntentType.LookAround;
            }

            if (emotion == MotionEmotion.Excited || (profile != null && profile.Friendliness > 0.65f))
            {
                return random.NextDouble() < 0.45 ? MotionIntentType.Wave : MotionIntentType.Clap;
            }

            if (emotion == MotionEmotion.Angry || (profile != null && profile.Aggression > 0.62f))
            {
                return random.NextDouble() < 0.5 ? MotionIntentType.Agree : MotionIntentType.Yell;
            }

            if (emotion == MotionEmotion.Confused || (profile != null && profile.Anxiety > 0.62f))
            {
                return random.NextDouble() < 0.55 ? MotionIntentType.Disagree : MotionIntentType.LookAround;
            }

            return random.NextDouble() < 0.5 ? MotionIntentType.LookAround : MotionIntentType.Think;
        }

        private float NextInterval(float energy, bool talking, bool walking)
        {
            var min = talking ? 1.5f : walking ? 4f : 2.5f;
            var max = talking ? 4.5f : walking ? 9f : 7f;
            var fasterByEnergy = Mathf.Lerp(0.8f, 1.15f, 1f - Mathf.Clamp01(energy));
            return Mathf.Lerp(min, max, (float)random.NextDouble()) * fasterByEnergy;
        }

        private static MotionGesture GestureFor(MotionIntentType type)
        {
            switch (type)
            {
                case MotionIntentType.Wave:
                    return MotionGesture.Wave;
                case MotionIntentType.Agree:
                    return MotionGesture.ThoughtfulNod;
                case MotionIntentType.Disagree:
                    return MotionGesture.ShakeHeadNo;
                case MotionIntentType.Clap:
                    return MotionGesture.Clap;
                case MotionIntentType.Yell:
                    return MotionGesture.Yell;
                case MotionIntentType.Think:
                    return MotionGesture.Think;
                case MotionIntentType.LookAround:
                    return MotionGesture.LookAround;
                default:
                    return MotionGesture.LookAround;
            }
        }

        private static bool IsStrongAction(MotionIntent intent)
        {
            return intent.Action == MotionAction.ButtonPush ||
                   intent.Action == MotionAction.PickUp ||
                   intent.Action == MotionAction.Push ||
                   intent.Action == MotionAction.PullHeavy ||
                   intent.Action == MotionAction.Dodge ||
                   intent.Action == MotionAction.HitReaction ||
                   intent.Action == MotionAction.Fall ||
                   intent.Action == MotionAction.GetUp ||
                   intent.Action == MotionAction.StepBackward;
        }
    }
}
