using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Full scoreboard shown inside the pause menu.
    /// </summary>
    public sealed class FullScoreboard : MonoBehaviour
    {
        private Text listText => transform.Find("List")?.GetComponent<Text>();

        private void Start()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver != null)
            {
                receiver.OnGameStateSync += HandleStateSync;
            }
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
            if (listText == null)
            {
                return;
            }

            var localId = HudManager.Instance?.LocalPlayerId ?? "player_1";
            var companies = payload.Value<JArray>("companies");
            if (companies == null)
            {
                listText.text = "No companies available.";
                return;
            }

            var ranked = new List<(string playerId, string name, int value)>();
            foreach (var token in companies)
            {
                var company = token as JObject;
                if (company == null)
                {
                    continue;
                }

                var playerId = company.Value<string>("player_id");
                var name = company.Value<string>("company_name") ?? playerId;
                var value = ComputeValue(company);
                ranked.Add((playerId, name, value));
            }

            ranked = ranked.OrderByDescending(x => x.value).ToList();
            var lines = ranked.Select((x, i) =>
            {
                var marker = x.playerId == localId ? "> " : "  ";
                return $"{marker}{i + 1}. {x.name}\n    Value: ${x.value:N0}";
            });

            listText.text = string.Join("\n", lines);
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
                    var map = employee.Value<JObject>("loyalty_map");
                    if (map != null && map.TryGetValue(playerId, out var token2))
                    {
                        totalLoyalty += token2.Value<int>();
                    }
                }
            }

            var avgLoyalty = activeCount > 0 ? totalLoyalty / activeCount : 0;
            return funds + (activeCount * (avgLoyalty + 100)) + satisfaction;
        }
    }
}
