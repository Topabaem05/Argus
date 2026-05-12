using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class PersonaMotionMapper : MonoBehaviour
    {
        [SerializeField]
        private float naturalWalkSpeed = 0.58f;

        [SerializeField]
        private float fastWalkSpeed = 0.82f;

        [SerializeField]
        private float runSpeed = 1.25f;

        public MotionIntent BuildMoveIntent(
            Vector3 currentPosition,
            Vector3 target,
            float urgency,
            string locomotion = "walk",
            string emotion = "neutral",
            string intent = "walk")
        {
            var motionEmotion = MapEmotion(emotion, intent);
            var speed = SpeedFor(locomotion, intent, urgency, motionEmotion);
            return new MotionIntent(
                true,
                target,
                null,
                speed,
                0.12f,
                motionEmotion,
                MotionGesture.None,
                MotionAction.None,
                true,
                urgency);
        }

        public MotionIntent BuildDialogueIntent(
            Vector3 currentPosition,
            string semanticEmotion,
            string dialogueAct,
            Vector3? focusTarget = null,
            bool allowMovement = true)
        {
            var emotion = MapEmotion(semanticEmotion, dialogueAct);
            return new MotionIntent(
                false,
                currentPosition,
                focusTarget,
                0f,
                0.12f,
                emotion,
                MapGesture(dialogueAct, semanticEmotion),
                MotionAction.None,
                allowMovement,
                0.4f);
        }

        public MotionIntent BuildPhysicsReactionIntent(
            Vector3 currentPosition,
            string resultKind,
            float intensity)
        {
            var action = MapPhysicsAction(resultKind, intensity);
            return new MotionIntent(
                false,
                currentPosition,
                null,
                0f,
                0.12f,
                intensity >= 0.65f ? MotionEmotion.Surprised : MotionEmotion.Confused,
                MotionGesture.None,
                action,
                false,
                Mathf.Clamp01(intensity));
        }

        public MotionIntent BuildFromBehaviorPayload(Vector3 currentPosition, JObject payload)
        {
            var target = ParseVector(payload?["target_position"] as JObject, currentPosition);
            var urgency = payload?["urgency"]?.ToObject<float>() ?? 0.35f;
            var locomotion = payload?["locomotion"]?.ToObject<string>() ?? "walk";
            var intent = payload?["intent"]?.ToObject<string>() ?? "walk";
            var emotion = payload?["emotion"]?["label"]?.ToObject<string>() ??
                          payload?["emotion"]?.ToObject<string>() ??
                          "neutral";

            return BuildMoveIntent(
                currentPosition,
                target,
                urgency,
                locomotion,
                emotion,
                intent);
        }

        public float SpeedFor(string locomotion, string intent, float urgency, MotionEmotion emotion)
        {
            var normalizedLocomotion = Normalize(locomotion);
            var normalizedIntent = Normalize(intent);
            if (normalizedLocomotion == "idle")
            {
                return 0f;
            }

            if (normalizedLocomotion == "run" || normalizedIntent == "charge")
            {
                return runSpeed;
            }

            var baseSpeed = normalizedIntent == "avoid" || normalizedIntent == "leave" || normalizedLocomotion == "fast_walk"
                ? fastWalkSpeed
                : naturalWalkSpeed;

            if (emotion == MotionEmotion.Sad || emotion == MotionEmotion.Scared)
            {
                baseSpeed *= 0.75f;
            }
            else if (emotion == MotionEmotion.Excited || emotion == MotionEmotion.Angry)
            {
                baseSpeed *= 1.08f;
            }

            return Mathf.Lerp(baseSpeed * 0.85f, baseSpeed, Mathf.Clamp01(urgency));
        }

        public static MotionEmotion MapEmotion(string emotion, string intent = "")
        {
            var value = Normalize(emotion);
            var normalizedIntent = Normalize(intent);
            if (normalizedIntent == "disagree" || normalizedIntent == "argue")
            {
                return MotionEmotion.Angry;
            }

            switch (value)
            {
                case "thinking":
                case "thoughtful":
                case "curious":
                    return MotionEmotion.Thinking;
                case "angry":
                case "concerned":
                    return MotionEmotion.Angry;
                case "sad":
                    return MotionEmotion.Sad;
                case "excited":
                case "happy":
                    return MotionEmotion.Excited;
                case "surprised":
                    return MotionEmotion.Surprised;
                case "confused":
                case "skeptical":
                    return MotionEmotion.Confused;
                case "scared":
                case "cautious":
                    return MotionEmotion.Scared;
                default:
                    return MotionEmotion.Neutral;
            }
        }

        public static MotionGesture MapGesture(string dialogueAct, string emotion = "")
        {
            var act = Normalize(dialogueAct);
            switch (act)
            {
                case "agree":
                case "agreement":
                    return MotionGesture.Nod;
                case "disagree":
                case "argue":
                case "no":
                    return MotionGesture.ShakeHeadNo;
                case "greet":
                case "greeting":
                    return MotionGesture.Wave;
                case "think":
                case "uncertainty":
                    return MotionGesture.Think;
                case "yell":
                case "warn":
                    return MotionGesture.Yell;
                case "look":
                case "observe":
                    return MotionGesture.LookAround;
                default:
                    var mappedEmotion = MapEmotion(emotion, act);
                    return mappedEmotion == MotionEmotion.Thinking
                        ? MotionGesture.ThoughtfulNod
                        : MotionGesture.Talk;
            }
        }

        public static MotionAction MapPhysicsAction(string resultKind, float intensity)
        {
            var kind = Normalize(resultKind);
            if (kind.Contains("fall") || intensity >= 0.85f)
            {
                return MotionAction.Fall;
            }

            if (kind.Contains("recover") || kind.Contains("get_up"))
            {
                return MotionAction.GetUp;
            }

            if (kind.Contains("dodge") || kind.Contains("avoid"))
            {
                return MotionAction.Dodge;
            }

            if (kind.Contains("stuck") || kind.Contains("blocked"))
            {
                return MotionAction.StepBackward;
            }

            return intensity >= 0.3f ? MotionAction.HitReaction : MotionAction.StepBackward;
        }

        private static Vector3 ParseVector(JObject token, Vector3 fallback)
        {
            if (token == null)
            {
                return fallback;
            }

            return new Vector3(
                token["x"]?.ToObject<float>() ?? fallback.x,
                token["y"]?.ToObject<float>() ?? fallback.y,
                token["z"]?.ToObject<float>() ?? fallback.z);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace("-", "_");
        }
    }
}
