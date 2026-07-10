using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Right-edge slide-in drawer showing selected employee details.
    /// </summary>
    public sealed class EmployeeDetailDrawer : MonoBehaviour
    {
        private Text nameText => transform.Find("Name")?.GetComponent<Text>();
        private Text tagsText => transform.Find("Tags")?.GetComponent<Text>();
        private Text moodText => transform.Find("Mood")?.GetComponent<Text>();
        private Text loyaltyText => transform.Find("Loyalty")?.GetComponent<Text>();
        private Text memoryText => transform.Find("Memory")?.GetComponent<Text>();
        private Text taskText => transform.Find("Task")?.GetComponent<Text>();
        [SerializeField] private Button closeButton;

        private string currentEmployeeId;
        private System.Action onClose;
        private JObject lastState;

        private void Start()
        {
            gameObject.SetActive(false);

            if (HudManager.Instance != null)
            {
                HudManager.Instance.OnEmployeeSelected += OnEmployeeSelected;
                HudManager.Instance.OnEmployeeDeselected += Hide;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    Hide();
                    onClose?.Invoke();
                });
            }

            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver != null)
            {
                receiver.OnGameStateSync += HandleStateSync;
            }
        }

        private void OnDestroy()
        {
            if (HudManager.Instance != null)
            {
                HudManager.Instance.OnEmployeeSelected -= OnEmployeeSelected;
                HudManager.Instance.OnEmployeeDeselected -= Hide;
            }

            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver != null)
            {
                receiver.OnGameStateSync -= HandleStateSync;
            }
        }

        private void OnEmployeeSelected(string employeeId)
        {
            currentEmployeeId = employeeId;
            Refresh(lastState);
            gameObject.SetActive(true);
            InputGate.IsMenuOpen = true;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
            InputGate.IsMenuOpen = false;
        }

        public void SetCloseCallback(System.Action callback)
        {
            onClose = callback;
        }

        private void HandleStateSync(JObject payload)
        {
            lastState = payload;
            Refresh(payload);
        }

        private void Refresh(JObject payload)
        {
            if (string.IsNullOrWhiteSpace(currentEmployeeId) || payload == null)
            {
                return;
            }

            var employee = FindEmployee(payload, currentEmployeeId);
            if (employee == null)
            {
                return;
            }

            SetText(nameText, employee.Value<string>("display_name") ?? currentEmployeeId, UITheme.TextPrimary, UITheme.FontL);
            SetText(tagsText, FormatTags(employee.Value<JArray>("personality_tags")), UITheme.TextSecondary, UITheme.FontS);
            var mood = employee.Value<int?>("mood").GetValueOrDefault(50);
            SetText(moodText, $"Mood: {mood}", UITheme.MoodColor(mood), UITheme.FontS);
            SetText(loyaltyText, FormatLoyalty(employee, HudManager.Instance?.LocalPlayerId), UITheme.TextSecondary, UITheme.FontS);
            SetText(memoryText, FormatMemory(employee.Value<JArray>("memory")), UITheme.TextSecondary, UITheme.FontS);
            SetText(taskText, "Task: (none)", UITheme.TextSecondary, UITheme.FontS); // ponytail: task lookup deferred to state cache
        }

        private static JObject FindEmployee(JObject payload, string employeeId)
        {
            var companies = payload.Value<JArray>("companies");
            if (companies == null)
            {
                return null;
            }

            foreach (var token in companies)
            {
                var company = token as JObject;
                if (company == null)
                {
                    continue;
                }

                var employees = company.Value<JArray>("employees");
                if (employees == null)
                {
                    continue;
                }

                foreach (var empToken in employees)
                {
                    var emp = empToken as JObject;
                    if (emp != null && emp.Value<string>("employee_id") == employeeId)
                    {
                        return emp;
                    }
                }
            }

            return null;
        }

        private static void SetText(Text text, string value, Color color, int fontSize)
        {
            if (text == null)
            {
                return;
            }

            text.text = value ?? "-";
            text.color = color;
            text.fontSize = fontSize;
        }

        private static string FormatTags(JArray tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return "Tags: -";
            }

            return "Tags: " + string.Join(", ", tags);
        }

        private static string FormatLoyalty(JObject employee, string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return "Loyalty: -";
            }

            var map = employee.Value<JObject>("loyalty_map");
            if (map != null && map.TryGetValue(playerId, out var token))
            {
                return $"Loyalty: {token.Value<int>()}";
            }

            return "Loyalty: 0";
        }

        private static string FormatMemory(JArray memory)
        {
            if (memory == null || memory.Count == 0)
            {
                return "Memory: -";
            }

            return "Memory:\n" + string.Join("\n", memory);
        }
    }
}
