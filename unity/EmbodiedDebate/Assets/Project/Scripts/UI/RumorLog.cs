using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Timestamped rumor log shown inside the pause menu.
    /// </summary>
    public sealed class RumorLog : MonoBehaviour
    {
        private Text logText => transform.Find("Log")?.GetComponent<Text>();
        [SerializeField] private int maxEntries = 20;

        private readonly List<string> entries = new List<string>();

        private void Start()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver != null)
            {
                receiver.OnRumorEvent += HandleRumorEvent;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver != null)
            {
                receiver.OnRumorEvent -= HandleRumorEvent;
            }
        }

        private void HandleRumorEvent(JObject payload)
        {
            var text = payload.Value<string>("text") ?? payload.Value<string>("description") ?? "Unknown rumor";
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            entries.Add($"[{timestamp}] {text}");

            while (entries.Count > maxEntries)
            {
                entries.RemoveAt(0);
            }

            Refresh();
        }

        private void Refresh()
        {
            if (logText == null)
            {
                return;
            }

            logText.fontSize = UITheme.FontS;
            logText.color = UITheme.TextSecondary;
            if (entries.Count == 0)
            {
                logText.text = "No rumors yet.";
                return;
            }

            logText.text = string.Join("\n", entries);
        }
    }
}
