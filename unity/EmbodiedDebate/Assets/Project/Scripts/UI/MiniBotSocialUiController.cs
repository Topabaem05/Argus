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
            chatText.text = scenario.CurrentChatText;
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
            var panel = AddPanel(canvasObject.transform, "Chat Panel", new Vector2(24f, 24f), new Vector2(470f, 84f));
            chatText = AddText(panel.transform, "Chat Text", "Mini-bots are wandering freely.", font, 15, TextAnchor.UpperLeft);

            var buttonBar = AddPanel(canvasObject.transform, "Button Bar", new Vector2(24f, 116f), new Vector2(470f, 44f));
            pauseButton = AddButton(buttonBar.transform, "Pause Button", "Pause", font, new Vector2(8f, 8f), OnPauseClicked);
            pauseButtonText = pauseButton.GetComponentInChildren<Text>();
            AddButton(buttonBar.transform, "Gather Button", "Gather", font, new Vector2(122f, 8f), OnGatherClicked);
            AddButton(buttonBar.transform, "Chat Button", "Chat", font, new Vector2(236f, 8f), OnChatClicked);
            AddButton(buttonBar.transform, "Scatter Button", "Scatter", font, new Vector2(350f, 8f), OnScatterClicked);
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
            image.color = new Color(0.05f, 0.07f, 0.08f, 0.78f);
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
            rect.sizeDelta = new Vector2(104f, 28f);

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
    }
}
