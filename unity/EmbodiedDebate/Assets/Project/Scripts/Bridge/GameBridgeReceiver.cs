using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Bridge
{
    public sealed class GameBridgeReceiver : MonoBehaviour
    {
        [SerializeField] private string serverUri = "ws://127.0.0.1:8000/unity";
        [SerializeField] private string sessionId = "ai-company-game";

        private UnityBridgeClient client;

        public bool IsConnected => client?.IsConnected ?? false;

        public string ServerUri => serverUri;
        public string SessionId => sessionId;

        public event Action<JObject> OnPlayerCommand;
        public event Action<JObject> OnTaskUpdate;
        public event Action<JObject> OnRumorEvent;
        public event Action<JObject> OnEconomyUpdate;
        public event Action<JObject> OnGameStateSync;

        public void SendPlayerCommand(JObject payload)
        {
            if (client == null)
            {
                Debug.LogWarning("GameBridgeReceiver: cannot send command, client is null.");
                return;
            }

            var envelope = new BridgeEnvelope
            {
                Type = "game.player_command",
                SessionId = sessionId,
                Payload = payload ?? new JObject(),
                MessageId = Guid.NewGuid().ToString("N"),
                Sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SentAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            _ = client.SendBridgeEnvelopeAsync(envelope, destroyCancellationToken);
        }

        private void Start()
        {
            client = new UnityBridgeClient();
            ConnectAsync(this);
        }

        private static async void ConnectAsync(GameBridgeReceiver self)
        {
            try
            {
                await self.client.ConnectAsync(new Uri(self.serverUri), self.sessionId, self.destroyCancellationToken).ConfigureAwait(true);
                Debug.Log($"GameBridgeReceiver: connected to {self.serverUri} session={self.sessionId}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GameBridgeReceiver: failed to connect to {self.serverUri}: {exception.Message}");
            }
        }

        private void Update()
        {
            if (client == null)
            {
                return;
            }

            while (client.TryDequeueMessage(out var envelope))
            {
                Dispatch(envelope);
            }

            while (client.TryDequeueError(out var error))
            {
                Debug.LogWarning($"GameBridgeReceiver: bridge error {error.Message}");
            }
        }

        private void OnDestroy()
        {
            client?.DisconnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            client?.Dispose();
        }

        private void Dispatch(BridgeEnvelope envelope)
        {
            if (!envelope.IsKnownBridgeMessageType())
            {
                Debug.LogWarning($"GameBridgeReceiver: unknown type {envelope.Type}");
                return;
            }

            switch (envelope.Type)
            {
                case "game.player_command":
                    OnPlayerCommand?.Invoke(envelope.Payload);
                    break;
                case "game.task_update":
                    OnTaskUpdate?.Invoke(envelope.Payload);
                    break;
                case "game.rumor_event":
                    OnRumorEvent?.Invoke(envelope.Payload);
                    break;
                case "game.economy_update":
                    OnEconomyUpdate?.Invoke(envelope.Payload);
                    break;
                case "game.state_sync":
                    OnGameStateSync?.Invoke(envelope.Payload);
                    break;
            }

            _ = client.SendAckAsync(envelope.MessageId, envelope.Sequence, applied: true, destroyCancellationToken);
        }
    }
}
