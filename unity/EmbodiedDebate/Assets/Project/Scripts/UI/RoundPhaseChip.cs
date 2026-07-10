using System;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Top-center chip showing current round and phase.
    /// </summary>
    public sealed class RoundPhaseChip : MonoBehaviour
    {
        private Text labelText => transform.Find("Label")?.GetComponent<Text>();
        private Image chipImage;

        private void Start()
        {
            chipImage = GetComponent<Image>();
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver == null)
            {
                Debug.LogWarning("RoundPhaseChip: no GameBridgeReceiver found.");
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
                var round = payload.Value<int?>("round_number");
                var phase = payload.Value<string>("phase");

                if (labelText != null)
                {
                    var roundStr = round.HasValue ? round.Value.ToString() : "-";
                    var phaseStr = string.IsNullOrWhiteSpace(phase) ? "-" : phase;
                    labelText.text = $"Round {roundStr} / Phase: {phaseStr}";
                    labelText.color = UITheme.TextPrimary;
                    labelText.fontSize = UITheme.FontM;
                }

                if (chipImage != null)
                {
                    chipImage.color = PhaseColor(phase);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"RoundPhaseChip: failed to parse state_sync: {exception.Message}");
            }
        }

        private static Color PhaseColor(string phase)
        {
            if (string.Equals(phase, "morning", StringComparison.OrdinalIgnoreCase)) return UITheme.AccentWarm;
            if (string.Equals(phase, "work", StringComparison.OrdinalIgnoreCase)) return UITheme.Accent;
            if (string.Equals(phase, "event", StringComparison.OrdinalIgnoreCase)) return UITheme.Warning;
            return UITheme.BgSecondary;
        }
    }
}
