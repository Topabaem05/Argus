using System.Collections;
using System.Text;
using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using UnityEngine;
using UnityEngine.Networking;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Observer shortcuts: websocket <c>observer.*</c> for replay (when connected) and REST fallbacks.
    /// </summary>
    public sealed class ObserverControls : MonoBehaviour
    {
        private const float DefaultCameraTelemetryIntervalSec = 4f;

        [SerializeField]
        private BridgeReceiver receiver;

        [SerializeField]
        private bool replayHotkeysEnabled = true;

        [SerializeField]
        private KeyCode replayPauseKey = KeyCode.F1;

        [SerializeField]
        private KeyCode replayResumeKey = KeyCode.F2;

        [SerializeField]
        private KeyCode replayStepKey = KeyCode.F3;

        [SerializeField]
        private float cameraTelemetryIntervalSeconds = DefaultCameraTelemetryIntervalSec;

        [SerializeField]
        private bool pushCameraTelemetry;

        private float cameraElapsed;

        public void Bind(BridgeReceiver recv)
        {
            receiver = recv;
        }

        private void Update()
        {
            if (replayHotkeysEnabled && receiver != null && receiver.IsBridgeSocketConnected)
            {
                if (UnityEngine.Input.GetKeyDown(replayPauseKey))
                {
                    TrySendWs(BridgeEnvelope.ObserverPause(receiver.SessionId));
                }
                else if (UnityEngine.Input.GetKeyDown(replayResumeKey))
                {
                    TrySendWs(BridgeEnvelope.ObserverResume(receiver.SessionId));
                }
                else if (UnityEngine.Input.GetKeyDown(replayStepKey))
                {
                    TrySendWs(BridgeEnvelope.ObserverStep(receiver.SessionId));
                }
            }

            if (!pushCameraTelemetry || receiver == null || !receiver.IsBridgeSocketConnected ||
                Camera.main == null)
            {
                return;
            }

            cameraElapsed += Time.unscaledDeltaTime;
            if (cameraElapsed < cameraTelemetryIntervalSeconds)
            {
                return;
            }

            cameraElapsed = 0f;
            var cam = Camera.main.transform;
            var pos = cam.position;
            var fwd = cam.forward;
            TrySendWs(
                BridgeEnvelope.ObserverCameraState(
                    receiver.SessionId,
                    pos.x,
                    pos.y,
                    pos.z,
                    fwd.x,
                    fwd.y,
                    fwd.z,
                    Camera.main.fieldOfView));
        }

        /// <summary>REST fallback usable without an active websocket (tests / tooling).</summary>
        public void ReplayPauseHttp()
        {
            if (receiver != null)
            {
                StartCoroutine(PostReplayRelative("replay/pause"));
            }
        }

        public void ReplayResumeHttp()
        {
            if (receiver != null)
            {
                StartCoroutine(PostReplayRelative("replay/resume"));
            }
        }

        public void ReplayStepHttp()
        {
            if (receiver != null)
            {
                StartCoroutine(PostReplayRelative("replay/step"));
            }
        }

        public void SubmitObserverSelectionWs(string agentId)
        {
            if (receiver == null || string.IsNullOrWhiteSpace(agentId))
            {
                return;
            }

            TrySendWs(BridgeEnvelope.ObserverSelectAgent(receiver.SessionId, agentId.Trim()));
        }

        private IEnumerator PostReplayRelative(string relativePath)
        {
            var baseHttp = BridgeEndpoints.HttpAuthorityFromWs(receiver.ConfiguredWebSocketUrl);
            var trimmed = relativePath.TrimStart('/');
            var url = $"{baseHttp.TrimEnd('/')}/{trimmed}";
            using var req = UnityWebRequest.PostWwwForm(url, "{}");
            req.downloadHandler = new DownloadHandlerBuffer();
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Observer replay POST {url}: {req.responseCode} {req.error}");
            }
        }

        private void TrySendWs(BridgeEnvelope envelope)
        {
            receiver?.TrySendBridgeEnvelope(envelope);
        }
    }
}
