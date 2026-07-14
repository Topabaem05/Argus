using UnityEngine;

namespace ArgusUnity.Game
{
    /// <summary>
    /// Dependency-free IMGUI vertical-slice HUD. It is intentionally generated from code so the
    /// scene remains reproducible before final sprites, fonts and prefabs are approved.
    /// </summary>
    public sealed class CompanyGameHud : MonoBehaviour
    {
        private static readonly string[] Actions =
        {
            "praise", "scold", "snack", "raise", "bonus", "party",
            "fire", "hire", "gossip", "scout", "assign_task"
        };

        private static readonly string[] Labels =
        {
            "칭찬", "혼내기", "간식", "월급 인상", "보너스", "회식",
            "해고", "채용", "소문", "스카우트", "업무 배정"
        };

        [SerializeField] private CompanyGameCoordinator coordinator;

        private Texture2D panelTexture;
        private Texture2D cardTexture;
        private Texture2D accentTexture;
        private Texture2D dangerTexture;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle buttonStyle;
        private GUIStyle phaseStyle;
        private bool stylesReady;

        private readonly Color panelColor = new(0.102f, 0.114f, 0.149f, 0.94f);
        private readonly Color cardColor = new(0.145f, 0.165f, 0.212f, 0.96f);
        private readonly Color accentColor = new(0.298f, 0.788f, 0.941f, 1f);
        private readonly Color warmColor = new(0.976f, 0.780f, 0.310f, 1f);
        private readonly Color dangerColor = new(0.976f, 0.255f, 0.267f, 1f);

        private void Awake()
        {
            coordinator ??= GetComponent<CompanyGameCoordinator>();
            panelTexture = MakeTexture(panelColor);
            cardTexture = MakeTexture(cardColor);
            accentTexture = MakeTexture(accentColor);
            dangerTexture = MakeTexture(dangerColor);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CompanyMiniBotActor.ClearSelection();
            }
        }

        private void OnDestroy()
        {
            Destroy(panelTexture);
            Destroy(cardTexture);
            Destroy(accentTexture);
            Destroy(dangerTexture);
        }

        private void OnGUI()
        {
            if (coordinator == null)
            {
                return;
            }

            EnsureStyles();
            var scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            scale = Mathf.Max(0.55f, scale);
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale;

            DrawCompanyCard();
            DrawPhaseChip(width);
            DrawConnectionCard(width);
            DrawCommandBar(width, height);
            DrawToast(width, height);

            GUI.matrix = previousMatrix;
        }

        private void DrawCompanyCard()
        {
            var rect = new Rect(24f, 24f, 360f, 124f);
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(44f, 38f, 320f, 30f), coordinator.LocalCompanyName, titleStyle);
            GUI.Label(new Rect(44f, 75f, 300f, 24f), $"자금  {coordinator.Funds:N0}", bodyStyle);
            var oldColor = GUI.color;
            GUI.color = warmColor;
            GUI.Label(new Rect(240f, 75f, 120f, 24f), $"CSAT {coordinator.CustomerSatisfaction}", bodyStyle);
            GUI.color = oldColor;
            GUI.Label(new Rect(44f, 108f, 300f, 22f), "WASD 이동 · 휠 줌 · Q/E 회전 · F 포커스", mutedStyle);
        }

        private void DrawPhaseChip(float width)
        {
            var rect = new Rect(width * 0.5f - 180f, 24f, 360f, 54f);
            GUI.DrawTexture(rect, cardTexture);
            GUI.Label(
                rect,
                $"ROUND {coordinator.RoundNumber}  ·  {PhaseLabel(coordinator.Phase)}",
                phaseStyle);
        }

        private void DrawConnectionCard(float width)
        {
            var rect = new Rect(width - 324f, 24f, 300f, 94f);
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 14f, 260f, 26f), "AI COMPANY", titleStyle);
            var status = coordinator.IsConnected ? "로컬 SLM 브리지 연결" : "오프라인 미리보기";
            var oldColor = GUI.color;
            GUI.color = coordinator.IsConnected ? accentColor : warmColor;
            GUI.Label(new Rect(rect.x + 20f, rect.y + 51f, 260f, 22f), status, bodyStyle);
            GUI.color = oldColor;
        }

        private void DrawCommandBar(float width, float height)
        {
            var selected = CompanyMiniBotActor.Selected;
            var bar = new Rect(width * 0.5f - 470f, height - 174f, 940f, 150f);
            GUI.DrawTexture(bar, panelTexture);

            var selectedText = selected == null
                ? "직원 minibot을 클릭해 명령하세요"
                : $"{selected.DisplayName}  ·  기분 {selected.Mood}  ·  호감도 {selected.Loyalty}  ·  {selected.CurrentTask}";
            GUI.Label(new Rect(bar.x + 22f, bar.y + 14f, 880f, 26f), selectedText, bodyStyle);

            const float buttonWidth = 138f;
            const float buttonHeight = 39f;
            const float gap = 10f;
            for (var i = 0; i < Actions.Length; i++)
            {
                var row = i / 6;
                var column = i % 6;
                var x = bar.x + 22f + column * (buttonWidth + gap);
                var y = bar.y + 50f + row * (buttonHeight + 9f);
                DrawCommandButton(new Rect(x, y, buttonWidth, buttonHeight), Actions[i], Labels[i], selected);
            }
        }

        private void DrawCommandButton(
            Rect rect,
            string action,
            string label,
            CompanyMiniBotActor selected)
        {
            var enabled = coordinator.CanIssue(action, selected);
            var previousEnabled = GUI.enabled;
            var previousBackground = buttonStyle.normal.background;
            GUI.enabled = enabled;
            buttonStyle.normal.background = action is "fire" or "scold" ? dangerTexture : cardTexture;
            if (GUI.Button(rect, label, buttonStyle))
            {
                coordinator.SendSelectedCommand(action);
            }
            buttonStyle.normal.background = previousBackground;
            GUI.enabled = previousEnabled;
        }

        private void DrawToast(float width, float height)
        {
            var rect = new Rect(width - 444f, height - 142f, 420f, 88f);
            GUI.DrawTexture(rect, cardTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 7f, rect.height), accentTexture);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 13f, 370f, 23f), "최근 이벤트", mutedStyle);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 39f, 370f, 38f), coordinator.LatestEvent, bodyStyle);
        }

        private void EnsureStyles()
        {
            if (stylesReady)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.97f, 0.98f, 0.99f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = new Color(0.93f, 0.95f, 0.98f) },
                wordWrap = true
            };
            mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.66f, 0.69f, 0.77f) }
            };
            phaseStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = cardTexture },
                hover = { textColor = Color.white, background = accentTexture },
                active = { textColor = Color.white, background = accentTexture },
                disabled = { textColor = new Color(0.55f, 0.58f, 0.64f), background = cardTexture }
            };
            stylesReady = true;
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "ArgusRuntimeUiColor",
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static string PhaseLabel(string phase) => phase switch
        {
            "morning" => "아침",
            "work" => "업무",
            "event" => "이벤트",
            "evening" => "저녁",
            _ => phase
        };
    }
}
