using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Listens to task, rumor, economy, and state-sync events and surfaces them as toasts/banners.
    /// </summary>
    public sealed class EventToasts : MonoBehaviour
    {
        [SerializeField] private TransientToast taskToast;
        [SerializeField] private TransientToast rumorToast;
        [SerializeField] private TransientToast economyToast;
        [SerializeField] private StateSyncBanner stateBanner;

        private void Start()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver == null)
            {
                Debug.LogWarning("EventToasts: no GameBridgeReceiver found.");
                return;
            }

            receiver.OnTaskUpdate += HandleTaskUpdate;
            receiver.OnRumorEvent += HandleRumorEvent;
            receiver.OnEconomyUpdate += HandleEconomyUpdate;
            receiver.OnGameStateSync += HandleStateSync;
        }

        private void OnDestroy()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver == null)
            {
                return;
            }

            receiver.OnTaskUpdate -= HandleTaskUpdate;
            receiver.OnRumorEvent -= HandleRumorEvent;
            receiver.OnEconomyUpdate -= HandleEconomyUpdate;
            receiver.OnGameStateSync -= HandleStateSync;
        }

        private void HandleTaskUpdate(JObject payload)
        {
            var desc = payload.Value<string>("description") ?? "Task updated";
            taskToast?.Show("Task Update", desc);
        }

        private void HandleRumorEvent(JObject payload)
        {
            var text = payload.Value<string>("text") ?? payload.Value<string>("description") ?? "Rumor surfaced";
            rumorToast?.Show("Rumor", text);
        }

        private void HandleEconomyUpdate(JObject payload)
        {
            var desc = payload.Value<string>("description") ?? "Economy shifted";
            economyToast?.Show("Economy", desc);
        }

        private void HandleStateSync(JObject payload)
        {
            var round = payload.Value<int?>("round_number");
            var phase = payload.Value<string>("phase");
            if (round.HasValue && !string.IsNullOrWhiteSpace(phase))
            {
                stateBanner?.Show($"Round {round.Value} - {phase}");
            }
        }
    }
}
