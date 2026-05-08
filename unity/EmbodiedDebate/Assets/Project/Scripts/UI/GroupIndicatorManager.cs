using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using ArgusUnity.Robots;
using ArgusUnity.Scene;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>Applies hashed group tint and compact badge markers from incremental <c>group.update</c>.</summary>
    public sealed class GroupIndicatorManager : MonoBehaviour
    {
        private const string BadgeChildName = "ArgusGroupBadge";

        [SerializeField]
        private Vector3 badgeLocalPosition = new Vector3(0.55f, 1.38f, 0f);

        [SerializeField]
        private float badgeCharacterSize = 0.035f;

        private SimulationSceneOrchestrator orchestrator;
        private AgentSpawnHandler spawnHandler;

        public void Initialize(SimulationSceneOrchestrator orch, AgentSpawnHandler spawn)
        {
            if (orchestrator != null)
            {
                orchestrator.GroupUpdate -= OnGroupUpdate;
            }

            orchestrator = orch;
            spawnHandler = spawn;

            if (orchestrator != null)
            {
                orchestrator.GroupUpdate += OnGroupUpdate;
            }
        }

        private void OnDestroy()
        {
            if (orchestrator != null)
            {
                orchestrator.GroupUpdate -= OnGroupUpdate;
            }
        }

        private void OnGroupUpdate(BridgeEnvelope envelope)
        {
            if (spawnHandler == null || !BridgePresentationParsing.TryGetGroupUpdate(envelope, out var vm))
            {
                return;
            }

            var hex = GroupColorPalette.HexForGroupId(vm.GroupId);
            if (!ColorUtility.TryParseHtmlString(hex, out var tint))
            {
                tint = Color.gray;
            }

            var badgeUpper = BridgePresentationParsing.TruncateDialogue(vm.BadgeDisplay, 8);

            for (var i = 0; i < vm.MemberAgentIds.Length; i++)
            {
                var agentId = vm.MemberAgentIds[i];
                if (!spawnHandler.TryGetAgentTransform(agentId, out var root) || root == null)
                {
                    continue;
                }

                ApplyTint(root, tint);
                EnsureBadge(root, $"[{badgeUpper}]");
            }
        }

        private static void ApplyTint(Transform root, Color tint)
        {
            var renderer = root.GetComponentInChildren<MeshRenderer>();
            if (renderer?.material != null)
            {
                renderer.material.color = tint;
            }
        }

        private void EnsureBadge(Transform root, string text)
        {
            Transform badgeTransform = null;
            for (var c = 0; c < root.childCount; c++)
            {
                var child = root.GetChild(c);
                if (child.name == BadgeChildName)
                {
                    badgeTransform = child;
                    break;
                }
            }

            if (badgeTransform == null)
            {
                var go = new GameObject(BadgeChildName);
                badgeTransform = go.transform;
                badgeTransform.SetParent(root, false);
                badgeTransform.localPosition = badgeLocalPosition;
                badgeTransform.localRotation = Quaternion.identity;

                badgeTransform.gameObject.AddComponent<BillboardToCamera>();
                var mesh = go.AddComponent<TextMesh>();
                mesh.anchor = TextAnchor.MiddleLeft;
                mesh.alignment = TextAlignment.Left;
                mesh.characterSize = badgeCharacterSize;
                mesh.fontSize = 56;
                mesh.color = Color.white;
            }

            var label = badgeTransform.gameObject.GetComponent<TextMesh>();
            if (label != null)
            {
                label.text = text;
                label.characterSize = badgeCharacterSize;
            }

            badgeTransform.localPosition = badgeLocalPosition;
        }
    }
}
