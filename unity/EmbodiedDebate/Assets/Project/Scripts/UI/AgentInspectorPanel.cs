using System;
using System.Collections;
using System.Globalization;
using System.Text;
using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using UnityEngine;
using UnityEngine.Networking;

namespace ArgusUnity.UI
{
    /// <summary>Fetches <c>/agents/{id}/public</c>, then exposes only formatter-allowlisted fields.</summary>
    public sealed class AgentInspectorPanel : MonoBehaviour
    {
        [SerializeField]
        private BridgeReceiver receiver;

        [SerializeField]
        private string inspectorAgentId = "agent-001";

        [SerializeField]
        private TextMesh display;

        [SerializeField]
        private bool mirrorSelectionOverWebSocket = true;

        private string lastSelectionSent;

        private void EnsureDisplay()
        {
            if (display != null)
            {
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var root = new GameObject("InspectorPanel");
            root.transform.SetParent(cam.transform, false);
            root.transform.localPosition = new Vector3(-0.12f, -0.22f, 0.74f);

            display = root.AddComponent<TextMesh>();
            display.anchor = TextAnchor.MiddleLeft;
            display.alignment = TextAlignment.Left;
            display.characterSize = 0.05f;
            display.fontSize = 36;
            display.color = Color.white;

            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "InspectorBacking";
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = Vector3.forward * -0.02f;
            back.transform.localScale = new Vector3(6f, 3.5f, 1f);

            var col = back.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var renderer = back.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = UrpMaterialFactory.CreateTransparent(new Color(0f, 0f, 0f, 0.52f));
            }
        }

        public void Bind(BridgeReceiver recv)
        {
            receiver = recv;
        }

        public void SetAgent(string agentId)
        {
            inspectorAgentId = string.IsNullOrWhiteSpace(agentId) ? inspectorAgentId : agentId.Trim();
            Refresh();
        }

        [ContextMenu("Refresh inspection")]
        public void Refresh()
        {
            EnsureDisplay();
            if (receiver == null)
            {
                return;
            }

            var id =
                string.IsNullOrWhiteSpace(inspectorAgentId) ? string.Empty : inspectorAgentId.Trim();

            if (mirrorSelectionOverWebSocket && receiver.IsBridgeSocketConnected && id.Length > 0 &&
                lastSelectionSent != id)
            {
                receiver.TrySendBridgeEnvelope(BridgeEnvelope.ObserverSelectAgent(receiver.SessionId, id));
                lastSelectionSent = id;
            }

            StartCoroutine(LoadPublicCoroutine(id));
        }

        private IEnumerator LoadPublicCoroutine(string agentIdRaw)
        {
            if (receiver == null)
            {
                yield break;
            }

            var authority = BridgeEndpoints.HttpAuthorityFromWs(receiver.ConfiguredWebSocketUrl)
                .TrimEnd('/');
            if (agentIdRaw.Length == 0)
            {
                AssignPanel("inspectorAgentId empty.");
                yield break;
            }

            var url = $"{authority}/agents/{Encode(agentIdRaw)}/public";
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                var msg =
                    $"Inspect failed\n{req.responseCode} {req.error}\n{(req.downloadHandler?.text ?? string.Empty)}";
                AssignPanel(msg);
                yield break;
            }

            var body = req.downloadHandler?.text ?? string.Empty;
            if (!AgentInspectorFormatter.TryFilterPublicSubset(body, out var filtered))
            {
                AssignPanel("Could not sanitize agent inspect JSON.");
                yield break;
            }

            AssignPanel(filtered.TrimEnd());
        }

        private static string Encode(string segment)
        {
            var escaped = EscapePathSegment(segment);
            return escaped;
        }

        private static string EscapePathSegment(string segment)
        {
            var bytes = Encoding.UTF8.GetBytes(segment);
            var builder = new StringBuilder(Math.Max(16, segment.Length));
            foreach (var b in bytes)
            {
                var unreserved =
                    (b >= 0x61 && b <= 0x7A) ||
                    (b >= 0x41 && b <= 0x5A) ||
                    (b >= 0x30 && b <= 0x39) ||
                    b == '-' ||
                    b == '_' ||
                    b == '~' ||
                    b == '.';

                if (unreserved)
                {
                    builder.Append((char)b);
                }
                else
                {
                    builder.Append('%');
                    builder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            return builder.ToString();
        }

        private void AssignPanel(string text)
        {
            EnsureDisplay();
            if (display != null)
            {
                display.text = text;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"AgentInspect: {(text.Length > 400 ? text.Substring(0, 400) : text)}");
#endif
        }
    }
}
