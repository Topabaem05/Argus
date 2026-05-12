using System.Collections.Generic;
using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using ArgusUnity.Scene;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>Small color orb above each agent reflecting the latest emotion label and intensity.</summary>
    public sealed class EmotionIndicatorManager : MonoBehaviour
    {
        [SerializeField]
        private Vector3 orbLocalOffset = new Vector3(0.45f, 1.45f, 0f);

        [SerializeField]
        private float orbScale = 0.16f;

        private SimulationSceneOrchestrator orchestrator;
        private AgentSpawnHandler spawnHandler;
        private readonly Dictionary<string, GameObject> orbsByAgent = new Dictionary<string, GameObject>();

        public void Initialize(SimulationSceneOrchestrator orch, AgentSpawnHandler spawn)
        {
            if (orchestrator != null)
            {
                orchestrator.AgentEmotion -= OnAgentEmotion;
                orchestrator.AgentBehavior -= OnAgentEmotion;
            }

            orchestrator = orch;
            spawnHandler = spawn;

            if (orchestrator != null)
            {
                orchestrator.AgentEmotion += OnAgentEmotion;
                orchestrator.AgentBehavior += OnAgentEmotion;
            }
        }

        private void OnDestroy()
        {
            if (orchestrator != null)
            {
                orchestrator.AgentEmotion -= OnAgentEmotion;
                orchestrator.AgentBehavior -= OnAgentEmotion;
            }

            foreach (var pair in orbsByAgent)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            orbsByAgent.Clear();
        }

        private void OnAgentEmotion(BridgeEnvelope envelope)
        {
            if (spawnHandler == null || !BridgePresentationParsing.TryGetAgentEmotion(envelope, out var vm))
            {
                return;
            }

            if (!spawnHandler.TryGetAgentTransform(vm.AgentId, out var agentRoot) || agentRoot == null)
            {
                return;
            }

            if (!orbsByAgent.TryGetValue(vm.AgentId, out var orb) || orb == null)
            {
                orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"EmotionOrb_{vm.AgentId}";
                var col = orb.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

                orb.transform.SetParent(agentRoot, false);
                orb.transform.localScale = Vector3.one * orbScale;
                orb.transform.localPosition = orbLocalOffset;
                orbsByAgent[vm.AgentId] = orb;
            }
            else
            {
                orb.transform.localPosition = orbLocalOffset;
            }

            var renderer = orb.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (renderer.material == null || renderer.material.shader == null)
                {
                    renderer.material = UrpMaterialFactory.CreateLit(Color.white);
                }

                var baseColor = EmotionPalette.ColorFor(vm.Label);
                var alpha = Mathf.Clamp01(vm.Intensity);
                baseColor.a = Mathf.Lerp(0.35f, 1f, alpha);
                UrpMaterialFactory.ApplyColor(renderer.material, baseColor);
            }
        }

        private static class EmotionPalette
        {
            public static Color ColorFor(string label)
            {
                switch (label.ToLowerInvariant())
                {
                    case "happy":
                    case "excited":
                        return new Color(1f, 0.85f, 0.2f, 1f);
                    case "sad":
                    case "afraid":
                        return new Color(0.35f, 0.55f, 0.95f, 1f);
                    case "angry":
                        return new Color(0.95f, 0.25f, 0.2f, 1f);
                    case "confused":
                        return new Color(0.75f, 0.45f, 0.95f, 1f);
                    default:
                        return new Color(0.75f, 0.78f, 0.82f, 1f);
                }
            }
        }
    }
}
