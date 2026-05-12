using System.Collections.Generic;
using ArgusUnity.Bridge;
using ArgusUnity.Motion;
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
        public const float BridgeFloorY = 0f;
        private const float BridgeAgentTargetHeight = 0.72f;
        private SimulationSceneOrchestrator orchestrator;
        private GameObject prefab;
        private Transform spawnParent;
        private RobotAvatarManager avatarManager;
        private readonly Dictionary<string, Transform> transformsByAgentId = new Dictionary<string, Transform>();
        private static readonly string[] AvailablePrefabKeys = { "FallbackRobot", "UserModels/Idle" };

        public int AgentCount => transformsByAgentId.Count;

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

        public IEnumerable<KeyValuePair<string, Transform>> Agents => transformsByAgentId;

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
            var worldPos = GroundedPosition(new Vector3(position.X, BridgeFloorY, position.Z));

            if (!transformsByAgentId.TryGetValue(agentId, out var existing))
            {
                var instance = Instantiate(prefab, spawnParent);
                instance.name = $"Robot_{agentId}";
                existing = instance.transform;
                transformsByAgentId[agentId] = existing;
                FitToHeight(existing, BridgeAgentTargetHeight);
                ConfigureLocomotion(instance, agentId);
                ApplyAgentVisual(existing, agent, avatar);
            }

            existing.position = worldPos;
            GroundToFloor(existing);
            existing.gameObject.SetActive(avatar.Visible);
        }

        public static Vector3 GroundedPosition(Vector3 position)
        {
            return new Vector3(position.x, BridgeFloorY, position.z);
        }

        private static void ConfigureLocomotion(GameObject instance, string agentId)
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
            var bodies = instance.GetComponentsInChildren<Rigidbody>();
            if (bodies.Length == 0)
            {
                bodies = new[] { instance.AddComponent<Rigidbody>() };
            }

            foreach (var body in bodies)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            if (instance.GetComponentInChildren<Collider>() == null)
            {
                var capsule = instance.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 0.36f, 0f);
                capsule.height = 0.72f;
                capsule.radius = 0.18f;
            }

            var driver = instance.GetComponent<AgentLocomotionDriver>();
            if (driver == null)
            {
                driver = instance.AddComponent<AgentLocomotionDriver>();
            }

            driver.ResetTracking();

            var motionController = instance.GetComponent<MinibotMotionController>() ??
                                   instance.AddComponent<MinibotMotionController>();
            motionController.Initialize(agentId, StableHash(agentId));
        }

        private static void ApplyAgentVisual(Transform root, JObject agent, RobotAvatar avatar)
        {
            var displayName = avatar.DisplayName;
            var hexColor = avatar.GroupColorHex;

            var renderer = root.GetComponentInChildren<MeshRenderer>();
            if (renderer != null && UnityEngine.ColorUtility.TryParseHtmlString(hexColor, out var color))
            {
                if (renderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial.color = color;
                }
            }

            AttachNameLabel(root, displayName);
        }

        private static void AttachNameLabel(Transform root, string displayName)
        {
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(root, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.86f, 0f);
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

        private static void FitToHeight(Transform root, float targetHeight)
        {
            if (targetHeight <= 0f || !TryGetBounds(root, out var bounds) || bounds.size.y <= 0.001f)
            {
                return;
            }

            var multiplier = targetHeight / bounds.size.y;
            root.localScale *= multiplier;
        }

        private static void GroundToFloor(Transform root)
        {
            if (!TryGetBounds(root, out var bounds))
            {
                return;
            }

            var delta = BridgeFloorY - bounds.min.y;
            root.position = new Vector3(root.position.x, root.position.y + delta, root.position.z);
        }

        private static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 5381;
                if (value != null)
                {
                    for (var i = 0; i < value.Length; i++)
                    {
                        hash = ((hash << 5) + hash) ^ value[i];
                    }
                }

                return hash;
            }
        }
    }
}
