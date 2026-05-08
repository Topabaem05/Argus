using System.Collections.Generic;
using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using ArgusUnity.Scene;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>World-space dialogue captions above agents; replaces prior bubble per speaker.</summary>
    public sealed class DialogueBubbleManager : MonoBehaviour
    {
        [SerializeField]
        private Vector3 bubbleLocalOffset = new Vector3(0f, 1.65f, 0f);

        [SerializeField]
        private int maxDisplayedChars = 220;

        [SerializeField]
        private float characterSize = 0.06f;

        private SimulationSceneOrchestrator orchestrator;
        private AgentSpawnHandler spawnHandler;
        private readonly Dictionary<string, BubbleInstance> active = new Dictionary<string, BubbleInstance>();

        public void Initialize(SimulationSceneOrchestrator orch, AgentSpawnHandler spawn)
        {
            if (orchestrator != null)
            {
                orchestrator.AgentDialogue -= OnAgentDialogue;
            }

            orchestrator = orch;
            spawnHandler = spawn;

            if (orchestrator != null)
            {
                orchestrator.AgentDialogue += OnAgentDialogue;
            }
        }

        private void OnDestroy()
        {
            if (orchestrator != null)
            {
                orchestrator.AgentDialogue -= OnAgentDialogue;
            }

            foreach (var pair in active)
            {
                if (pair.Value?.Root != null)
                {
                    Destroy(pair.Value.Root);
                }
            }

            active.Clear();
        }

        private void Update()
        {
            if (active.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var expired = new List<string>();
            foreach (var pair in active)
            {
                if (pair.Value.ExpiresAt <= now)
                {
                    expired.Add(pair.Key);
                }
            }

            foreach (var id in expired)
            {
                if (active.TryGetValue(id, out var bubble) && bubble.Root != null)
                {
                    Destroy(bubble.Root);
                }

                active.Remove(id);
            }
        }

        private void OnAgentDialogue(BridgeEnvelope envelope)
        {
            if (spawnHandler == null || !BridgePresentationParsing.TryGetDialogue(envelope, out var vm))
            {
                return;
            }

            var text = BridgePresentationParsing.TruncateDialogue(vm.Text, maxDisplayedChars);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!spawnHandler.TryGetAgentTransform(vm.SpeakerId, out var agentRoot) || agentRoot == null)
            {
                return;
            }

            if (active.TryGetValue(vm.SpeakerId, out var prior) && prior.Root != null)
            {
                Destroy(prior.Root);
            }

            var seconds = vm.DurationMs > 0 ? vm.DurationMs / 1000f : 4f;
            var root = BuildBubble(agentRoot, text);
            active[vm.SpeakerId] = new BubbleInstance(root, Time.unscaledTime + seconds);
        }

        private GameObject BuildBubble(Transform agentRoot, string text)
        {
            var go = new GameObject("DialogueBubble");
            go.transform.SetParent(agentRoot, false);
            go.transform.localPosition = bubbleLocalOffset;
            go.transform.localRotation = Quaternion.identity;
            go.AddComponent<BillboardToCamera>();

            var backing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backing.name = "BubbleBacking";
            backing.transform.SetParent(go.transform, false);
            backing.transform.localPosition = Vector3.zero;
            backing.transform.localScale = new Vector3(2.4f, 0.55f, 1f);
            var collider = backing.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = backing.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = UrpMaterialFactory.CreateTransparent(new Color(0f, 0f, 0f, 0.55f));
            }

            var labelGo = new GameObject("BubbleText");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            var mesh = labelGo.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = characterSize;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;

            return go;
        }

        private sealed class BubbleInstance
        {
            public BubbleInstance(GameObject root, float expiresAt)
            {
                Root = root;
                ExpiresAt = expiresAt;
            }

            public GameObject Root { get; }

            public float ExpiresAt { get; }
        }
    }
}
