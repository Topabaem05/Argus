using System;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;
using ArgusUnity.Bridge;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Top-left persistent strip showing company name, funds, customer satisfaction, and task counts.
    /// </summary>
    public sealed class CompanyStatusStrip : MonoBehaviour
    {
        private Text companyNameText => transform.Find("CompanyName")?.GetComponent<Text>();
        private Text fundsText => transform.Find("Funds")?.GetComponent<Text>();
        private Text satisfactionText => transform.Find("Satisfaction")?.GetComponent<Text>();
        private Text tasksText => transform.Find("Tasks")?.GetComponent<Text>();

        private void Start()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver == null)
            {
                Debug.LogWarning("CompanyStatusStrip: no GameBridgeReceiver found.");
                return;
            }

            receiver.OnGameStateSync += HandleStateSync;
            receiver.OnTaskUpdate += HandleTaskUpdate;
        }

        private void OnDestroy()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver == null)
            {
                return;
            }

            receiver.OnGameStateSync -= HandleStateSync;
            receiver.OnTaskUpdate -= HandleTaskUpdate;
        }

        private void HandleStateSync(JObject payload)
        {
            try
            {
                var localId = HudManager.Instance?.LocalPlayerId ?? "player_1";
                var companies = payload.Value<JArray>("companies");
                if (companies == null)
                {
                    return;
                }

                foreach (var token in companies)
                {
                    var company = token as JObject;
                    if (company == null)
                    {
                        continue;
                    }

                    var playerId = company.Value<string>("player_id");
                    if (playerId != localId)
                    {
                        continue;
                    }

                    SetText(companyNameText, company.Value<string>("company_name"), UITheme.TextPrimary, UITheme.FontL);
                    SetText(fundsText, FormatFunds(company.Value<int?>("funds")), UITheme.AccentWarm, UITheme.FontM);
                    SetText(satisfactionText, FormatPercent(company.Value<int?>("customer_satisfaction"), "Satisfaction"), UITheme.Success, UITheme.FontM);
                    SetText(tasksText, FormatTasks(company.Value<int?>("completed_tasks"), company.Value<int?>("failed_tasks")), UITheme.TextSecondary, UITheme.FontM);
                    return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"CompanyStatusStrip: failed to parse state_sync: {exception.Message}");
            }
        }

        private void HandleTaskUpdate(JObject payload)
        {
            // Task updates are reflected on the next state sync; no immediate UI change here.
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

        private static string FormatFunds(int? funds)
        {
            return funds.HasValue ? $"Funds: ${funds.Value:N0}" : "Funds: -";
        }

        private static string FormatPercent(int? value, string label)
        {
            return value.HasValue ? $"{label}: {value.Value}%" : $"{label}: -";
        }

        private static string FormatTasks(int? completed, int? failed)
        {
            return $"Tasks: {completed.GetValueOrDefault(0)}/{failed.GetValueOrDefault(0)}";
        }
    }
}
