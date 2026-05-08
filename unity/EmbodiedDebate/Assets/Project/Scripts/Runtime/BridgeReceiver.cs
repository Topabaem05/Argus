using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArgusUnity.Bridge;
using ArgusUnity.Scene;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Polls <see cref="UnityBridgeClient"/> messages and routes them through the scene orchestrator.
    /// After <c>bridge.ready</c>, optionally POSTs to <c>/simulation/start</c> on the bridge HTTP server.
    /// </summary>
    public sealed class BridgeReceiver : MonoBehaviour
    {
        [SerializeField]
        private string simulationConfigPath = "examples/run_product_reaction.yaml";

        [SerializeField]
        private int maxTurnsOverride;

        [SerializeField]
        private string simulationChatText = "";

        [SerializeField]
        private string attachmentMetadataJson = "[]";

        [SerializeField]
        private bool triggerSimulationAfterHandshake = true;

        private SimulationSceneOrchestrator orchestrator;
        private string bridgeUrl = "ws://127.0.0.1:8765/ws/unity";
        private string bridgeSessionId = "unity-session";

        private UnityBridgeClient client;
        private CancellationTokenSource connectCts;
        private volatile bool connectLoopRunning;
        private bool _simulationStartRequested;

        public void Initialize(SimulationSceneOrchestrator orch, string url, string sessionId)
        {
            orchestrator = orch;
            bridgeUrl = url;
            bridgeSessionId = sessionId;
        }

        /// <remarks>Prefer using <see cref="BridgeEndpoints.HttpAuthorityFromWs"/> for REST calls.</remarks>
        public string ConfiguredWebSocketUrl => bridgeUrl;

        public string SessionId => bridgeSessionId;

        public bool IsBridgeSocketConnected => client != null && client.IsConnected;

        public void SetSimulationInput(string chatText, string attachmentsJson = "[]")
        {
            simulationChatText = chatText ?? "";
            attachmentMetadataJson = string.IsNullOrWhiteSpace(attachmentsJson)
                ? "[]"
                : attachmentsJson.Trim();
        }

        /// <summary>Send a validated envelope toward the bridge (typically <c>observer.*</c>).</summary>
        public bool TrySendBridgeEnvelope(BridgeEnvelope envelope)
        {
            if (connectCts == null || client == null || !client.IsConnected)
            {
                return false;
            }

            StartCoroutine(SendOutboundCoroutine(envelope, connectCts.Token));
            return true;
        }

        private IEnumerator SendOutboundCoroutine(BridgeEnvelope envelope, CancellationToken token)
        {
            var task = client.SendBridgeEnvelopeAsync(envelope, token);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted && task.Exception != null)
            {
                Debug.LogWarning($"Bridge outbound send failed: {task.Exception.GetBaseException().Message}");
            }
        }

        private void OnEnable()
        {
            _simulationStartRequested = false;

            if (orchestrator == null)
            {
                Debug.LogWarning("BridgeReceiver enabled before Initialize; skipping connection.");
                return;
            }

            connectCts = new CancellationTokenSource();
            client = new UnityBridgeClient();
            connectLoopRunning = true;
            _ = ConnectLoopAsync(connectCts.Token);
        }

        private void OnDisable()
        {
            connectLoopRunning = false;
            _simulationStartRequested = false;
            connectCts?.Cancel();

            if (client != null)
            {
                client.Dispose();
                client = null;
            }

            connectCts?.Dispose();
            connectCts = null;
        }

        private void Update()
        {
            if (client == null || orchestrator == null)
            {
                return;
            }

            while (client.TryDequeueMessage(out var envelope))
            {
                if (triggerSimulationAfterHandshake &&
                    !_simulationStartRequested &&
                    string.Equals(envelope.Type, "bridge.ready", StringComparison.Ordinal))
                {
                    _simulationStartRequested = true;
                    StartCoroutine(PostSimulationStartCoroutine());
                }

                if (!orchestrator.TryHandle(envelope, out var issue) && issue != null)
                {
                    Debug.LogWarning($"Bridge message not routed: {issue.Message} ({envelope.Type})");
                }
            }

            while (client.TryDequeueError(out var error))
            {
                Debug.LogWarning($"Bridge client error: {error.Message}");
            }
        }

        private IEnumerator PostSimulationStartCoroutine()
        {
            var baseHttp = NormalizeHttpBridgeBase(bridgeUrl);
            var url = $"{baseHttp.TrimEnd('/')}/simulation/start";
            var payload = new JObject { ["config_path"] = simulationConfigPath };
            if (maxTurnsOverride > 0)
            {
                payload["max_turns_override"] = maxTurnsOverride;
            }
            if (!string.IsNullOrWhiteSpace(simulationChatText))
            {
                payload["chat_text"] = simulationChatText.Trim();
            }
            if (!string.IsNullOrWhiteSpace(attachmentMetadataJson))
            {
                try
                {
                    payload["attachments"] = JArray.Parse(attachmentMetadataJson);
                }
                catch (JsonReaderException ex)
                {
                    Debug.LogWarning($"Attachment metadata JSON ignored: {ex.Message}");
                }
            }

            var body = Encoding.UTF8.GetBytes(payload.ToString(Formatting.None));
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"Simulation start POST failed ({url}): {request.responseCode} {request.error}");
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else if (request.downloadHandler != null && request.downloadHandler.text.Length > 0)
            {
                Debug.Log($"Bridge simulation/start: {request.downloadHandler.text}");
            }
#endif
        }

        private static string NormalizeHttpBridgeBase(string wsUrl)
        {
            return BridgeEndpoints.HttpAuthorityFromWs(wsUrl);
        }

        private async Task ConnectLoopAsync(CancellationToken token)
        {
            while (connectLoopRunning && !token.IsCancellationRequested)
            {
                try
                {
                    var uri = new Uri(bridgeUrl);
                    await client.ConnectAsync(uri, bridgeSessionId, token).ConfigureAwait(false);
                    while (connectLoopRunning && client != null && client.IsConnected &&
                           !token.IsCancellationRequested)
                    {
                        await Task.Delay(250, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Bridge connection failed ({bridgeUrl}): {ex.Message}");
                }

                if (!connectLoopRunning || token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(2000, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
