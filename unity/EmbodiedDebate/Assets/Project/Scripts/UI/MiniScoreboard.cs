using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Top-right compact scoreboard ranked by company value.
    /// </summary>
    public sealed class MiniScoreboard : MonoBehaviour
    {
        private Text labelText => transform.Find("Label")?.GetComponent<Text>();

        private void Start()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver == null)
            {
                Debug.LogWarning("MiniScoreboard: no GameBridgeReceiver found.");
                return;
            }

            receiver.OnGameStateSync += HandleStateSync;
        }

        private void OnDestroy()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver != null)
            {
                receiver.OnGameStateSync -= HandleStateSync;
            }
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

                var seen = new HashSet<string>();
                var ranked = new List<(string playerId, string name, int value)>();

                foreach (var token in companies)
                {
                    var company = token as JObject;
                    if (company == null)
                    {
                        continue;
                    }

                    var playerId = company.Value<string>("player_id");
                    if (string.IsNullOrWhiteSpace(playerId) || seen.Contains(playerId))
                    {
                        continue;
                    }

                    seen.Add(playerId);
                    var name = company.Value<string>("company_name") ?? playerId;
                    var value = ComputeValue(company);
                    ranked.Add((playerId, name, value));
                }

                ranked = ranked.OrderByDescending(x => x.value).ToList();

                if (labelText != null)
                {
                    labelText.fontSize = UITheme.FontS;
                    var lines = ranked.Select((x, i) =>
                    {
                        var marker = x.playerId == localId ? "> " : "  ";
                        var colorTag = x.playerId == localId ? ColorTag(UITheme.Accent) : ColorTag(UITheme.TextSecondary);
                        return $"{marker}{i + 1}. {x.name} {colorTag}${x.value:N0}</color>";
                    });
                    labelText.text = string.Join("\n", lines);
                    labelText.color = UITheme.TextPrimary;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MiniScoreboard: failed to parse state_sync: {exception.Message}");
            }
        }

        private static string ColorTag(Color color)
        {
            var hex = ColorUtility.ToHtmlStringRGBA(color);
            return $"<color=#{hex}>";
        }

        private static int ComputeValue(JObject company)
        {
            var funds = company.Value<int?>("funds").GetValueOrDefault(0);
            var satisfaction = company.Value<int?>("customer_satisfaction").GetValueOrDefault(0);
            var employees = company.Value<JArray>("employees");
            var activeCount = 0;
            var totalLoyalty = 0;

            if (employees != null)
            {
                var playerId = company.Value<string>("player_id");
                foreach (var token in employees)
                {
                    var employee = token as JObject;
                    if (employee == null)
                    {
                        continue;
                    }

                    if (!employee.Value<bool?>("is_active").GetValueOrDefault(true))
                    {
                        continue;
                    }

                    if (employee.Value<string>("employed_by") != playerId)
                    {
                        continue;
                    }

                    activeCount++;
                    var loyaltyMap = employee.Value<JObject>("loyalty_map");
                    if (loyaltyMap != null && loyaltyMap.TryGetValue(playerId, out var loyaltyToken))
                    {
                        totalLoyalty += loyaltyToken.Value<int>();
                    }
                }
            }

            var avgLoyalty = activeCount > 0 ? totalLoyalty / activeCount : 0;
            return funds + (activeCount * (avgLoyalty + 100)) + satisfaction;
        }
    }
}
