using System.Collections.Generic;
using ArgusUnity.Bridge;
using ArgusUnity.Robots;
using ArgusUnity.Scene;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Instantiates robot visuals for agent.spawn envelopes and tracks transforms for movement.
    /// </summary>
    public sealed class AgentSpawnHandler : MonoBehaviour
    {
        private const float AgentLabelSizeScale = 0.6f;
        private const string AgentLabelFontResource = "Fonts/Pretendard-Regular";
        private SimulationSceneOrchestrator orchestrator;
        private GameObject prefab;
        private Transform spawnParent;
        private RobotAvatarManager avatarManager;
        private readonly Dictionary<string, Transform> transformsByAgentId = new Dictionary<string, Transform>();
        private static readonly string[] AvailablePrefabKeys = { "FallbackRobot", "UserModels/Idle" };

        public void Initialize(
            SimulationSceneOrchestrator orch,
            GameObject robotPrefab,
            Transform parent)
        {
            orchestrator = orch;
            prefab = robotPrefab;
            spawnParent = parent;
            avatarManager = new RobotAvatarManager(AvailablePrefabKeys);

            orchestrator.AgentSpawn += OnAgentSpawn;
        }

        private void OnDestroy()
        {
            if (orchestrator != null)
            {
                orchestrator.AgentSpawn -= OnAgentSpawn;
            }
        }

        public bool TryGetAgentTransform(string agentId, out Transform t)
        {
            return transformsByAgentId.TryGetValue(agentId, out t);
        }

        private void OnAgentSpawn(BridgeEnvelope envelope)
        {
            if (prefab == null || spawnParent == null)
            {
                return;
            }

            var agent = envelope.Payload["agent"] as JObject;
            if (agent == null)
            {
                return;
            }

            var agentId = agent["agent_id"]?.ToObject<string>();
            if (string.IsNullOrEmpty(agentId))
            {
                return;
            }

            avatarManager.UpsertFromSpawn(envelope);
            if (!avatarManager.TryGetAvatar(agentId, out var avatar))
            {
                return;
            }

            var position = avatar.Position;
            var worldPos = new Vector3(position.X, position.Y, position.Z);

            if (!transformsByAgentId.TryGetValue(agentId, out var existing))
            {
                var instance = Instantiate(prefab, spawnParent);
                instance.name = $"Robot_{agentId}";
                existing = instance.transform;
                transformsByAgentId[agentId] = existing;
                ConfigureLocomotion(instance);
                ApplyAgentVisual(existing, agent, avatar);
            }

            existing.position = worldPos;
            existing.gameObject.SetActive(avatar.Visible);
        }

        private static void ConfigureLocomotion(GameObject instance)
        {
            foreach (var sampler in instance.GetComponents<MiniBotWalkAnimator>())
            {
                sampler.enabled = false;
            }

            var animator = instance.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            foreach (var body in instance.GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }

            var driver = instance.GetComponent<AgentLocomotionDriver>();
            if (driver == null)
            {
                driver = instance.AddComponent<AgentLocomotionDriver>();
            }

            driver.ResetTracking();
        }

        private static void ApplyAgentVisual(Transform root, JObject agent, RobotAvatar avatar)
        {
            var displayName = avatar.DisplayName;
            var hexColor = avatar.GroupColorHex;

            var renderer = root.GetComponentInChildren<MeshRenderer>();
            if (renderer != null && UnityEngine.ColorUtility.TryParseHtmlString(hexColor, out var color))
            {
                if (renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }

            AttachNameLabel(root, displayName);
        }

        private static void AttachNameLabel(Transform root, string displayName)
        {
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(root, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            labelGo.transform.localRotation = Quaternion.identity;

            var mesh = labelGo.AddComponent<TextMesh>();
            mesh.text = displayName;
            mesh.characterSize = 0.08f * AgentLabelSizeScale;
            mesh.fontSize = Mathf.RoundToInt(48 * AgentLabelSizeScale);
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;

            var font = Resources.Load<Font>(AgentLabelFontResource);
            if (font != null)
            {
                mesh.font = font;
                var renderer = labelGo.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = font.material;
                }
            }
        }
    }
}
