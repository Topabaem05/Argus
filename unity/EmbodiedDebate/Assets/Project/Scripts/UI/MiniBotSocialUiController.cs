using ArgusUnity.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArgusUnity.UI
{
    public sealed class MiniBotSocialUiController : MonoBehaviour
    {
        [SerializeField]
        private MiniBotRunAroundScenario scenario;

        private Text chatText;
        private Text actionMappingText;
        private Text eventTitleText;
        private Text debugText;
        private Button pauseButton;
        private Text pauseButtonText;
        private Canvas screenCanvas;

        public void Initialize(MiniBotRunAroundScenario targetScenario)
        {
            scenario = targetScenario;
            EnsureBuilt();
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (scenario == null || chatText == null)
            {
                return;
            }

            RefreshCanvasCamera();
            if (eventTitleText != null)
            {
                eventTitleText.text = EventTitleFor(scenario.CurrentChatText);
            }

            chatText.text = EventBodyFor(scenario.CurrentChatText);
            if (actionMappingText != null)
            {
                actionMappingText.text = StructuredActionFor(scenario.CurrentActionMappingText);
            }

            if (debugText != null)
            {
                debugText.text = scenario.CurrentActionMappingText;
            }

            if (pauseButtonText != null)
            {
                pauseButtonText.text = scenario.IsPaused ? "Play" : "Pause";
            }
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("MiniBot Social Screen UI");
            canvasObject.transform.SetParent(transform, false);
            screenCanvas = canvasObject.AddComponent<Canvas>();
            screenCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            screenCanvas.planeDistance = 1f;
            screenCanvas.sortingOrder = 50;
            RefreshCanvasCamera();
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var panel = AddPanel(canvasObject.transform, "Event Card", new Vector2(24f, 24f), new Vector2(410f, 128f));
            eventTitleText = AddText(panel.transform, "Event Title Text", "Mini-bot Social Simulation", font, 17, TextAnchor.UpperLeft);
            SetOffsets(eventTitleText.rectTransform, new Vector2(14f, 72f), new Vector2(-14f, -10f));
            chatText = AddText(panel.transform, "Chat Text", "Mini-bots are wandering freely.", font, 14, TextAnchor.UpperLeft);
            SetOffsets(chatText.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -54f));

            var mappingPanel = AddPanel(canvasObject.transform, "Persona Inspector", new Vector2(24f, 162f), new Vector2(410f, 116f));
            actionMappingText = AddText(mappingPanel.transform, "Action Mapping Text", "Movement: Wander\nAnimation: Walk\nEmotion: Calm", font, 13, TextAnchor.UpperLeft);

            var debugPanel = AddPanel(canvasObject.transform, "Backend Debug Panel", new Vector2(24f, 288f), new Vector2(410f, 52f));
            debugText = AddText(debugPanel.transform, "Debug Text", "backend mapping available", font, 11, TextAnchor.UpperLeft);

            var buttonBar = AddPanel(canvasObject.transform, "Button Bar", new Vector2(24f, 350f), new Vector2(410f, 44f));
            pauseButton = AddButton(buttonBar.transform, "Pause Button", "Pause", font, new Vector2(8f, 8f), OnPauseClicked);
            pauseButtonText = pauseButton.GetComponentInChildren<Text>();
            AddButton(buttonBar.transform, "Gather Button", "Gather", font, new Vector2(106f, 8f), OnGatherClicked);
            AddButton(buttonBar.transform, "Chat Button", "Chat", font, new Vector2(204f, 8f), OnChatClicked);
            AddButton(buttonBar.transform, "Scatter Button", "Scatter", font, new Vector2(302f, 8f), OnScatterClicked);
        }

        private void EnsureBuilt()
        {
            if (chatText != null)
            {
                return;
            }

            EnsureEventSystem();
            if (!TryBindExistingUi())
            {
                BuildUi();
            }

            WireButtons();
        }

        private void OnPauseClicked()
        {
            if (scenario != null)
            {
                scenario.SetPaused(!scenario.IsPaused);
            }
        }

        private void OnGatherClicked()
        {
            scenario?.TriggerGatherNow();
        }

        private void OnChatClicked()
        {
            scenario?.TriggerChatNow();
        }

        private void OnScatterClicked()
        {
            scenario?.TriggerScatterNow();
        }

        private static GameObject AddPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(anchoredPosition.x, -anchoredPosition.y);
            rect.sizeDelta = size;

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.84f);
            return panel;
        }

        private static Text AddText(
            Transform parent,
            string name,
            string text,
            Font font,
            int fontSize,
            TextAnchor alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 10f);
            rect.offsetMax = new Vector2(-14f, -10f);

            var label = textObject.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static void SetOffsets(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        private static Button AddButton(
            Transform parent,
            string name,
            string label,
            Font font,
            Vector2 anchoredPosition,
            UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(anchoredPosition.x, -anchoredPosition.y);
            rect.sizeDelta = new Vector2(90f, 28f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.88f, 0.91f, 0.95f, 0.96f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.08f, 0.1f, 0.13f, 1f);
            return button;
        }

        private bool TryBindExistingUi()
        {
            screenCanvas = GetComponentInChildren<Canvas>(true);
            chatText = FindDescendant("Chat Text")?.GetComponent<Text>();
            actionMappingText = FindDescendant("Action Mapping Text")?.GetComponent<Text>();
            eventTitleText = FindDescendant("Event Title Text")?.GetComponent<Text>();
            debugText = FindDescendant("Debug Text")?.GetComponent<Text>();
            pauseButton = FindDescendant("Pause Button")?.GetComponent<Button>();
            pauseButtonText = pauseButton != null ? pauseButton.GetComponentInChildren<Text>(true) : null;
            return screenCanvas != null && chatText != null && pauseButton != null;
        }

        private void WireButtons()
        {
            WireButton("Pause Button", OnPauseClicked);
            WireButton("Gather Button", OnGatherClicked);
            WireButton("Chat Button", OnChatClicked);
            WireButton("Scatter Button", OnScatterClicked);
        }

        private void WireButton(string name, UnityEngine.Events.UnityAction onClick)
        {
            var button = FindDescendant(name)?.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        private Transform FindDescendant(string descendantName)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == descendantName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void RefreshCanvasCamera()
        {
            if (screenCanvas == null)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                return;
            }

            screenCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            screenCanvas.worldCamera = mainCamera;
        }

        private static string EventTitleFor(string chat)
        {
            if (chat.Contains("approaches"))
            {
                return chat.Replace(".", string.Empty);
            }

            if (chat.Contains(" with "))
            {
                return chat.Replace(" with ", " talks with ").Replace(".", string.Empty);
            }

            return "Mini-bot Social Simulation";
        }

        private static string EventBodyFor(string chat)
        {
            if (chat.Contains("agree"))
            {
                return "A friendly bot agrees and responds with a visible positive gesture.";
            }

            if (chat.Contains("debate"))
            {
                return "A skeptical bot challenges the idea while the partner holds attention.";
            }

            if (chat.Contains("ask"))
            {
                return "A curious bot asks a question and the pair stays in conversation spacing.";
            }

            if (chat.Contains("approaches"))
            {
                return "The active bot walks toward a partner and prepares to speak.";
            }

            return chat;
        }

        private static string StructuredActionFor(string mapping)
        {
            return mapping
                .Replace(": ", "\n")
                .Replace("movement=", "Movement: ")
                .Replace(", animation=", "\nAnimation: ")
                .Replace(", emotion=", "\nEmotion: ")
                .Replace(", gesture=", "\nGesture: ")
                .Replace(", goal=", "\nGoal: ");
        }
    }
}
