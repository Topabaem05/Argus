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
            var hasTarget = speed > 0.01f && PlanarDistance(currentPosition, target) > 0.05f;
            return new MotionIntent(
                TypeForMove(locomotion, intent, speed),
                hasTarget,
                hasTarget ? target : currentPosition,
                null,
                speed,
                0.12f,
                motionEmotion,
                MotionGesture.None,
                ActionForIntent(intent),
                true,
                urgency,
                MotionClipId.None,
                "bridge_move");
        }

        public MotionIntent BuildDialogueIntent(
            Vector3 currentPosition,
            string semanticEmotion,
            string dialogueAct,
            Vector3? focusTarget = null,
            bool allowMovement = true)
        {
            var emotion = MapEmotion(semanticEmotion, dialogueAct);
            var gesture = MapGesture(dialogueAct, semanticEmotion);
            return new MotionIntent(
                TypeForGestureAndEmotion(gesture, emotion, dialogueAct),
                false,
                currentPosition,
                focusTarget,
                0f,
                0.12f,
                emotion,
                gesture,
                MotionAction.None,
                allowMovement,
                0.4f,
                MotionClipId.None,
                "bridge_dialogue");
        }

        public MotionIntent BuildPhysicsReactionIntent(
            Vector3 currentPosition,
            string resultKind,
            float intensity)
        {
            var action = MapPhysicsAction(resultKind, intensity);
            return new MotionIntent(
                MotionCatalog.TypeForAction(action),
                false,
                currentPosition,
                null,
                0f,
                0.12f,
                intensity >= 0.65f ? MotionEmotion.Surprised : MotionEmotion.Confused,
                MotionGesture.None,
                action,
                false,
                Mathf.Clamp01(intensity),
                MotionClipId.None,
                "bridge_physics");
        }

        public MotionIntent BuildFromBehaviorPayload(Vector3 currentPosition, JObject payload)
        {
            var targetToken = payload?["target_position"] as JObject;
            var target = ParseVector(targetToken, currentPosition);
            var urgency = payload?["urgency"]?.ToObject<float>() ?? 0.35f;
            var locomotion = payload?["locomotion"]?.ToObject<string>() ?? "walk";
            var intent = payload?["intent"]?.ToObject<string>() ?? "walk";
            var emotion = ParseEmotion(payload);

            var motionEmotion = MapEmotion(emotion, intent);
            var action = ActionForIntent(intent);
            if (targetToken == null || SpeedFor(locomotion, intent, urgency, motionEmotion) <= 0.01f)
            {
                return new MotionIntent(
                    MotionCatalog.TypeForAction(action) != MotionIntentType.None
                        ? MotionCatalog.TypeForAction(action)
                        : TypeForGestureAndEmotion(MapGesture(intent, emotion), motionEmotion, intent),
                    false,
                    currentPosition,
                    null,
                    0f,
                    0.12f,
                    motionEmotion,
                    MapGesture(intent, emotion),
                    action,
                    action == MotionAction.None,
                    urgency,
                    MotionClipId.None,
                    "bridge_behavior_no_target");
            }

            return BuildMoveIntent(currentPosition, target, urgency, locomotion, emotion, intent);
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

        public static MotionIntentType TypeForMove(string locomotion, string intent, float speed)
        {
            var normalizedLocomotion = Normalize(locomotion);
            var normalizedIntent = Normalize(intent);
            if (normalizedLocomotion == "idle" || speed <= 0.01f)
            {
                return MotionIntentType.Idle;
            }

            if (normalizedIntent == "charge")
            {
                return MotionIntentType.Charge;
            }

            if (normalizedLocomotion == "run" || speed >= 1f)
            {
                return MotionIntentType.Run;
            }

            if (normalizedLocomotion == "backward" || normalizedIntent == "retreat")
            {
                return MotionIntentType.WalkBackward;
            }

            if (normalizedLocomotion == "strafe_left")
            {
                return MotionIntentType.StrafeLeft;
            }

            if (normalizedLocomotion == "strafe_right")
            {
                return MotionIntentType.StrafeRight;
            }

            return MotionIntentType.WalkForward;
        }

        public static MotionIntentType TypeForGestureAndEmotion(
            MotionGesture gesture,
            MotionEmotion emotion,
            string dialogueAct)
        {
            var act = Normalize(dialogueAct);
            if (act == "explain" || act == "ask")
            {
                return MotionIntentType.Explain;
            }

            var gestureType = MotionCatalog.TypeForGesture(gesture);
            if (gestureType != MotionIntentType.None)
            {
                return gestureType;
            }

            switch (emotion)
            {
                case MotionEmotion.Thinking:
                    return MotionIntentType.Think;
                case MotionEmotion.Excited:
                    return MotionIntentType.Excited;
                case MotionEmotion.Sad:
                    return MotionIntentType.Sad;
                case MotionEmotion.Surprised:
                    return MotionIntentType.Surprised;
                default:
                    return MotionIntentType.Talk;
            }
        }

        public static MotionAction ActionForIntent(string intent)
        {
            switch (Normalize(intent))
            {
                case "button":
                case "button_push":
                case "press":
                    return MotionAction.ButtonPush;
                case "pickup":
                case "pick_up":
                case "inspect":
                    return MotionAction.PickUp;
                case "push":
                    return MotionAction.Push;
                case "pull":
                    return MotionAction.PullHeavy;
                case "dodge":
                    return MotionAction.Dodge;
                case "charge":
                    return MotionAction.Charge;
                case "step_back":
                case "retreat":
                    return MotionAction.StepBackward;
                default:
                    return MotionAction.None;
            }
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

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var delta = b - a;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static string ParseEmotion(JObject payload)
        {
            if (payload == null)
            {
                return "neutral";
            }

            if (payload["emotion"] is JObject emotionObject)
            {
                return emotionObject["label"]?.ToObject<string>() ?? "neutral";
            }

            return payload["emotion"]?.ToObject<string>() ?? "neutral";
        }
    }
}
