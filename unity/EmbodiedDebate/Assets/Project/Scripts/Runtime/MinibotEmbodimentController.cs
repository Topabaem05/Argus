using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MinibotEmbodimentController : MonoBehaviour
    {
        [SerializeField]
        private string agentId;

        [SerializeField]
        private string archetype;

        [SerializeField]
        private Color personaColor = Color.cyan;

        [SerializeField]
        private Transform expressionRoot;

        [SerializeField]
        private TextMesh nameplate;

        [SerializeField]
        private TextMesh speechBubble;

        [SerializeField]
        private GameObject speechBubblePanel;

        [SerializeField]
        private TextMesh emotionIcon;

        [SerializeField]
        private Renderer faceRenderer;

        [SerializeField]
        private Transform leftArm;

        [SerializeField]
        private Transform rightArm;

        [SerializeField]
        private LineRenderer relationshipLine;

        private MinibotBlackboard blackboard;

        public void Initialize(
            string nextAgentId,
            string nextArchetype,
            Color nextPersonaColor,
            Transform nextExpressionRoot,
            TextMesh nextNameplate,
            TextMesh nextSpeechBubble,
            GameObject nextSpeechBubblePanel,
            TextMesh nextEmotionIcon,
            Renderer nextFaceRenderer,
            Transform nextLeftArm,
            Transform nextRightArm,
            LineRenderer nextRelationshipLine)
        {
            agentId = nextAgentId;
            archetype = nextArchetype;
            personaColor = nextPersonaColor;
            expressionRoot = nextExpressionRoot;
            nameplate = nextNameplate;
            speechBubble = nextSpeechBubble;
            speechBubblePanel = nextSpeechBubblePanel;
            emotionIcon = nextEmotionIcon;
            faceRenderer = nextFaceRenderer;
            leftArm = nextLeftArm;
            rightArm = nextRightArm;
            relationshipLine = nextRelationshipLine;
            Refresh();
        }

        private void Awake()
        {
            blackboard = GetComponent<MinibotBlackboard>();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (blackboard == null)
            {
                blackboard = GetComponent<MinibotBlackboard>();
            }

            var phase = blackboard != null ? blackboard.SocialPhase : MiniBotSocialPhase.Wander;
            var emotion = blackboard != null ? blackboard.CurrentEmotion : archetype;
            var partner = blackboard != null ? blackboard.PartnerId : string.Empty;
            var action = blackboard != null ? blackboard.MappedUnityAction : string.Empty;
            var isInteracting = phase == MiniBotSocialPhase.Approach ||
                phase == MiniBotSocialPhase.Chat ||
                phase == MiniBotSocialPhase.React;
            var showsSpeech = isInteracting && ShouldShowSpeech(emotion, action, phase);

            if (nameplate != null)
            {
                nameplate.text = $"{agentId}\n{archetype}";
                nameplate.color = personaColor;
            }

            if (emotionIcon != null)
            {
                emotionIcon.text = IconFor(emotion, action);
                emotionIcon.color = isInteracting ? Color.white : personaColor;
            }

            if (speechBubble != null)
            {
                speechBubble.gameObject.SetActive(showsSpeech);
                speechBubble.text = SpeechFor(phase, emotion, partner, action);
            }

            if (speechBubblePanel != null)
            {
                speechBubblePanel.SetActive(showsSpeech);
            }

            if (expressionRoot != null)
            {
                var pulse = isInteracting ? 1f + Mathf.Sin(Time.time * 5f) * 0.05f : 1f;
                expressionRoot.localScale = Vector3.one * pulse;
            }

            if (faceRenderer != null)
            {
                faceRenderer.sharedMaterial.color = FaceColorFor(emotion, isInteracting);
            }

            ApplyGesture(action, phase);
            UpdateRelationshipLine(partner, isInteracting);
        }

        private void ApplyGesture(string action, MiniBotSocialPhase phase)
        {
            action = action ?? string.Empty;
            if (leftArm == null || rightArm == null)
            {
                return;
            }

            var wave = phase == MiniBotSocialPhase.Chat || phase == MiniBotSocialPhase.React
                ? Mathf.Sin(Time.time * 7f) * 9f
                : 0f;
            var question = action.Contains("question_tilt") ? 18f : 0f;
            var disagree = action.Contains("small_head_shake") ? -16f : 0f;
            leftArm.localRotation = Quaternion.Euler(0f, 0f, 22f + question + wave);
            rightArm.localRotation = Quaternion.Euler(0f, 0f, -22f + disagree - wave);
        }

        private void UpdateRelationshipLine(string partner, bool isInteracting)
        {
            if (relationshipLine == null)
            {
                return;
            }

            var partnerTransform = FindPartner(partner);
            var visible = isInteracting && partnerTransform != null;
            relationshipLine.enabled = visible;
            if (!visible)
            {
                return;
            }

            relationshipLine.positionCount = 2;
            relationshipLine.SetPosition(0, transform.position + Vector3.up * 1.65f);
            relationshipLine.SetPosition(1, partnerTransform.position + Vector3.up * 1.65f);
        }

        private static Transform FindPartner(string partner)
        {
            if (string.IsNullOrWhiteSpace(partner))
            {
                return null;
            }

            foreach (var blackboard in FindObjectsOfType<MinibotBlackboard>())
            {
                if (blackboard.AgentId == partner)
                {
                    return blackboard.transform;
                }
            }

            return null;
        }

        private static string IconFor(string emotion, string action)
        {
            emotion = emotion ?? string.Empty;
            action = action ?? string.Empty;
            if (action.Contains("question_tilt") || emotion == "curious")
            {
                return "?";
            }

            if (emotion == "focused" || emotion == "concerned")
            {
                return "!";
            }

            if (emotion == "bright")
            {
                return "+";
            }

            return "*";
        }

        private static bool ShouldShowSpeech(string emotion, string action, MiniBotSocialPhase phase)
        {
            emotion = emotion ?? string.Empty;
            action = action ?? string.Empty;
            if (phase == MiniBotSocialPhase.Approach)
            {
                return false;
            }

            return emotion == "curious" ||
                emotion == "focused" ||
                emotion == "concerned" ||
                action.Contains("question_tilt") ||
                action.Contains("small_head_shake");
        }

        private static string SpeechFor(MiniBotSocialPhase phase, string emotion, string partner, string action)
        {
            emotion = emotion ?? string.Empty;
            partner = string.IsNullOrWhiteSpace(partner) ? "partner" : partner;
            action = action ?? string.Empty;
            if (phase == MiniBotSocialPhase.Approach)
            {
                return $"Going to {partner}";
            }

            if (phase == MiniBotSocialPhase.React)
            {
                return emotion == "focused" || emotion == "concerned" ? "I disagree." : "I see.";
            }

            if (action.Contains("question_tilt") || emotion == "curious")
            {
                return "Why do you think that?";
            }

            if (emotion == "focused")
            {
                return "Let's check the risk.";
            }

            if (emotion == "concerned")
            {
                return "This could be risky.";
            }

            return "That makes sense.";
        }

        private Color FaceColorFor(string emotion, bool isInteracting)
        {
            if (!isInteracting)
            {
                return Color.Lerp(Color.white, personaColor, 0.35f);
            }

            if (emotion == "focused")
            {
                return new Color(1f, 0.45f, 0.32f);
            }

            if (emotion == "concerned")
            {
                return new Color(1f, 0.72f, 0.22f);
            }

            if (emotion == "curious")
            {
                return new Color(0.35f, 0.95f, 1f);
            }

            if (emotion == "bright")
            {
                return new Color(1f, 0.78f, 0.28f);
            }

            return new Color(0.55f, 1f, 0.62f);
        }
    }
}
