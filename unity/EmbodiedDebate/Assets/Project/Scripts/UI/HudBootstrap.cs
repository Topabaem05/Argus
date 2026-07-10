using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Builds the HUD canvas and all panels at runtime so the scene file does not need manual edits.
    /// ponytail: one bootstrap script instead of prefab/scene churn.
    /// </summary>
    public sealed class HudBootstrap : MonoBehaviour
    {
        [SerializeField] private Font hudFont;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite dotSprite;

        private void Awake()
        {
            BuildHud();
        }

        private void BuildHud()
        {
            var existing = FindObjectOfType<HudManager>();
            if (existing != null)
            {
                return;
            }

            EnsureEventSystem();

            var canvasGo = new GameObject("HudCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var safeRoot = CreatePanel(canvasGo.transform, "SafeAreaRoot", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, UITheme.BgPrimary);
            safeRoot.anchorMin = Vector2.zero;
            safeRoot.anchorMax = Vector2.one;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            var hudManager = canvasGo.AddComponent<HudManager>();

            var topLeft = CreatePanel(safeRoot, "TopLeft", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(420, 120), UITheme.BgPrimary);
            var topCenter = CreatePanel(safeRoot, "TopCenter", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(360, 60), UITheme.BgSecondary);
            var topRight = CreatePanel(safeRoot, "TopRight", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(360, 140), UITheme.BgPrimary);
            var bottomCenter = CreatePanel(safeRoot, "BottomCenter", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(900, 100), UITheme.BgPrimary);
            var rightEdge = CreatePanel(safeRoot, "RightEdge", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(360, 600), UITheme.BgPrimary);
            var centerOverlay = CreatePanel(safeRoot, "CenterOverlay", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600, 400), UITheme.BgPrimary);

            hudManager.hudCanvas = canvasGo;
            hudManager.topLeftPanel = topLeft;
            hudManager.topCenterPanel = topCenter;
            hudManager.topRightPanel = topRight;
            hudManager.bottomCenterPanel = bottomCenter;
            hudManager.rightEdgePanel = rightEdge;
            hudManager.centerOverlayPanel = centerOverlay;
            hudManager.bridgeReceiver = FindObjectOfType<ArgusUnity.Bridge.GameBridgeReceiver>();

            AddStatusStrip(topLeft);
            AddRoundPhaseChip(topCenter);
            AddMiniScoreboard(topRight);
            var commandBar = AddCommandBar(bottomCenter);
            var detailDrawer = AddDetailDrawer(rightEdge);
            var pausePanels = AddPauseMenu(centerOverlay);
            AddEventToasts(centerOverlay);

            var mobileController = canvasGo.AddComponent<MobileLayoutController>();
            mobileController.SetPanels(topLeft, topCenter, topRight, bottomCenter, rightEdge);

            var selectorGo = new GameObject("EmployeeSelector");
            selectorGo.AddComponent<EmployeeSelector>();

            var registryGo = new GameObject("EmployeeRegistry");
            var registry = registryGo.AddComponent<EmployeeRegistry>();
            registry.SetDefaultSprite(EnsureDotSprite());

            var verifierGo = new GameObject("HudRuntimeVerifier");
            verifierGo.AddComponent<HudRuntimeVerifier>();

            if (pausePanels.menu != null)
            {
                pausePanels.menu.SetScoreboardPanel(pausePanels.scoreboard);
                pausePanels.menu.SetRumorLogPanel(pausePanels.rumorLog);
            }

            if (detailDrawer != null)
            {
                detailDrawer.SetCloseCallback(() => HudManager.Instance?.DeselectEmployee());
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
            }
        }

        private Sprite EnsureDotSprite()
        {
            if (dotSprite != null)
            {
                return dotSprite;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var center = new Vector2(size / 2f, size / 2f);
            var radius = size / 2f - 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var color = distance <= radius ? Color.white : Color.clear;
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            try
            {
                target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(target, value);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"HudBootstrap: failed to set {fieldName}: {exception.Message}");
            }
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            if (panelSprite != null)
            {
                image.sprite = panelSprite;
            }

            return rect;
        }

        private void AddStatusStrip(RectTransform parent)
        {
            var go = new GameObject("CompanyStatusStrip");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);
            var strip = go.AddComponent<CompanyStatusStrip>();
            CreateText(go.transform, "CompanyName", TextAnchor.UpperLeft, 0.75f, UITheme.FontL);
            CreateText(go.transform, "Funds", TextAnchor.UpperLeft, 0.55f, UITheme.FontM);
            CreateText(go.transform, "Satisfaction", TextAnchor.UpperLeft, 0.35f, UITheme.FontM);
            CreateText(go.transform, "Tasks", TextAnchor.UpperLeft, 0.15f, UITheme.FontM);
        }

        private void AddRoundPhaseChip(RectTransform parent)
        {
            var go = new GameObject("RoundPhaseChip");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var chip = go.AddComponent<RoundPhaseChip>();
            CreateText(go.transform, "Label", TextAnchor.MiddleCenter, 0.5f, UITheme.FontM);
        }

        private void AddMiniScoreboard(RectTransform parent)
        {
            var go = new GameObject("MiniScoreboard");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);
            var board = go.AddComponent<MiniScoreboard>();
            CreateText(go.transform, "Label", TextAnchor.UpperRight, 0.85f, UITheme.FontS);
        }

        private CommandActionBar AddCommandBar(RectTransform parent)
        {
            var barGo = new GameObject("CommandActionBar");
            barGo.transform.SetParent(parent, false);
            var rect = barGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var grid = barGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(120, 40);
            grid.spacing = new Vector2(8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 2;
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            grid.childAlignment = TextAnchor.MiddleCenter;

            var bar = barGo.AddComponent<CommandActionBar>();
            SetField(bar, "buttonContainer", barGo.transform);
            SetField(bar, "buttonPrefab", CreateButtonPrefab());

            var pickerGo = new GameObject("TargetPicker");
            pickerGo.transform.SetParent(parent, false);
            var pickerRect = pickerGo.AddComponent<RectTransform>();
            pickerRect.anchorMin = Vector2.zero;
            pickerRect.anchorMax = Vector2.one;
            pickerRect.offsetMin = Vector2.zero;
            pickerRect.offsetMax = Vector2.zero;
            pickerGo.AddComponent<Image>().color = UITheme.BgPrimary;
            var picker = pickerGo.AddComponent<TargetPicker>();
            SetField(picker, "itemContainer", pickerGo.transform);
            SetField(picker, "itemPrefab", CreateButtonPrefab());
            SetField(picker, "titleText", CreateText(pickerGo.transform, "Title", TextAnchor.UpperCenter, 0.9f, UITheme.FontL));

            SetField(bar, "targetPicker", picker);
            return bar;
        }

        private EmployeeDetailDrawer AddDetailDrawer(RectTransform parent)
        {
            var drawerGo = new GameObject("EmployeeDetailDrawer");
            drawerGo.transform.SetParent(parent, false);
            var rect = drawerGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var drawer = drawerGo.AddComponent<EmployeeDetailDrawer>();

            var title = CreateText(drawerGo.transform, "Name", TextAnchor.UpperLeft, 0.9f, UITheme.FontL);
            title.color = UITheme.TextPrimary;
            CreateText(drawerGo.transform, "Tags", TextAnchor.UpperLeft, 0.75f, UITheme.FontS);
            CreateText(drawerGo.transform, "Mood", TextAnchor.UpperLeft, 0.6f, UITheme.FontS);
            CreateText(drawerGo.transform, "Loyalty", TextAnchor.UpperLeft, 0.45f, UITheme.FontS);
            CreateText(drawerGo.transform, "Memory", TextAnchor.UpperLeft, 0.3f, UITheme.FontS);
            CreateText(drawerGo.transform, "Task", TextAnchor.UpperLeft, 0.15f, UITheme.FontS);

            var closeButton = CreateButton(drawerGo.transform, "Close", new Vector2(0.5f, 0.05f));
            SetField(drawer, "closeButton", closeButton);

            return drawer;
        }

        private (PauseMenu menu, GameObject scoreboard, GameObject rumorLog) AddPauseMenu(RectTransform parent)
        {
            var menuGo = new GameObject("PauseMenu");
            menuGo.transform.SetParent(parent, false);
            var rect = menuGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            menuGo.AddComponent<Image>().color = UITheme.BgPrimary;
            var menu = menuGo.AddComponent<PauseMenu>();

            var title = CreateText(menuGo.transform, "Title", TextAnchor.UpperCenter, 0.9f, UITheme.FontXL);
            title.text = "AI Company Game";

            var resumeButton = CreateButton(menuGo.transform, "Resume", new Vector2(0.5f, 0.75f));
            var scoreboardButton = CreateButton(menuGo.transform, "Scoreboard", new Vector2(0.5f, 0.62f));
            var rumorLogButton = CreateButton(menuGo.transform, "Rumor Log", new Vector2(0.5f, 0.49f));
            var quitButton = CreateButton(menuGo.transform, "Quit", new Vector2(0.5f, 0.36f));

            SetField(menu, "resumeButton", resumeButton);
            SetField(menu, "scoreboardButton", scoreboardButton);
            SetField(menu, "rumorLogButton", rumorLogButton);
            SetField(menu, "quitButton", quitButton);

            var scoreboardGo = new GameObject("FullScoreboard");
            scoreboardGo.transform.SetParent(menuGo.transform, false);
            var scoreboardRect = scoreboardGo.AddComponent<RectTransform>();
            scoreboardRect.anchorMin = new Vector2(0.05f, 0.05f);
            scoreboardRect.anchorMax = new Vector2(0.95f, 0.9f);
            scoreboardRect.offsetMin = Vector2.zero;
            scoreboardRect.offsetMax = Vector2.zero;
            scoreboardGo.AddComponent<Image>().color = UITheme.BgSecondary;
            var scoreboard = scoreboardGo.AddComponent<FullScoreboard>();
            var scoreboardList = CreateText(scoreboardGo.transform, "List", TextAnchor.UpperCenter, 0.5f, UITheme.FontM);
            SetField(scoreboard, "listText", scoreboardList);
            scoreboardGo.SetActive(false);

            var rumorGo = new GameObject("RumorLog");
            rumorGo.transform.SetParent(menuGo.transform, false);
            var rumorRect = rumorGo.AddComponent<RectTransform>();
            rumorRect.anchorMin = new Vector2(0.05f, 0.05f);
            rumorRect.anchorMax = new Vector2(0.95f, 0.9f);
            rumorRect.offsetMin = Vector2.zero;
            rumorRect.offsetMax = Vector2.zero;
            rumorGo.AddComponent<Image>().color = UITheme.BgSecondary;
            var rumorLog = rumorGo.AddComponent<RumorLog>();
            var rumorList = CreateText(rumorGo.transform, "Log", TextAnchor.UpperCenter, 0.5f, UITheme.FontM);
            SetField(rumorLog, "logText", rumorList);
            rumorGo.SetActive(false);

            return (menu, scoreboardGo, rumorGo);
        }

        private void AddEventToasts(RectTransform parent)
        {
            var toastGo = new GameObject("EventToasts");
            toastGo.transform.SetParent(parent, false);
            var rect = toastGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var toasts = toastGo.AddComponent<EventToasts>();

            var taskToast = CreateToast(parent, "TaskToast");
            SetField(toasts, "taskToast", taskToast);
            var rumorToast = CreateToast(parent, "RumorToast");
            SetField(toasts, "rumorToast", rumorToast);
            var economyToast = CreateToast(parent, "EconomyToast");
            SetField(toasts, "economyToast", economyToast);

            var bannerGo = new GameObject("StateSyncBanner");
            bannerGo.transform.SetParent(parent, false);
            var bannerRect = bannerGo.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.35f, 0.9f);
            bannerRect.anchorMax = new Vector2(0.65f, 0.98f);
            bannerRect.anchoredPosition = Vector2.zero;
            bannerRect.sizeDelta = Vector2.zero;
            bannerGo.AddComponent<Image>().color = UITheme.BgSecondary;
            var banner = bannerGo.AddComponent<StateSyncBanner>();
            SetField(banner, "labelText", CreateText(bannerGo.transform, "Label", TextAnchor.MiddleCenter, 0.5f, UITheme.FontM));
            SetField(banner, "canvasGroup", bannerGo.GetComponent<CanvasGroup>() ?? bannerGo.AddComponent<CanvasGroup>());
            SetField(toasts, "stateBanner", banner);
        }

        private TransientToast CreateToast(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.65f, 0.05f);
            rect.anchorMax = new Vector2(0.98f, 0.35f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            var toast = go.AddComponent<TransientToast>();
            toast.GetType().GetField("toastContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(toast, go.transform);
            toast.GetType().GetField("toastPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(toast, CreateToastItemPrefab());
            return toast;
        }

        private GameObject CreateToastItemPrefab()
        {
            var go = new GameObject("ToastItem");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260, 60);
            go.AddComponent<Image>().color = UITheme.BgSecondary;
            CreateText(go.transform, "Title", TextAnchor.UpperLeft, 0.7f, UITheme.FontS);
            CreateText(go.transform, "Body", TextAnchor.UpperLeft, 0.35f, UITheme.FontXS);
            return go;
        }

        private Text CreateText(Transform parent, string name, TextAnchor anchor, float yNormalized, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, yNormalized - 0.12f);
            rect.anchorMax = new Vector2(1, yNormalized + 0.12f);
            rect.offsetMin = new Vector2(4, 0);
            rect.offsetMax = new Vector2(-4, 0);
            var text = go.AddComponent<Text>();
            text.font = hudFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = UITheme.TextPrimary;
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private GameObject CreateButtonPrefab()
        {
            var go = new GameObject("ButtonPrefab");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 40);
            var image = go.AddComponent<Image>();
            image.color = UITheme.BgSecondary;
            if (buttonSprite != null)
            {
                image.sprite = buttonSprite;
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<Text>();
            text.font = hudFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = UITheme.TextPrimary;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = UITheme.FontS;
            text.raycastTarget = false;
            go.AddComponent<Button>();
            return go;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchor)
        {
            var prefab = CreateButtonPrefab();
            prefab.transform.SetParent(parent, false);
            var rect = prefab.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchor.x - 0.1f, anchor.y - 0.05f);
            rect.anchorMax = new Vector2(anchor.x + 0.1f, anchor.y + 0.05f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            var text = prefab.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
            return prefab.GetComponent<Button>();
        }
    }
}
