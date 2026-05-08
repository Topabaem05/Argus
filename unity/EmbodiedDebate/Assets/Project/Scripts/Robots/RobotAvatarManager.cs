using System.Collections.Generic;
using ArgusUnity.Bridge;
using Newtonsoft.Json.Linq;

namespace ArgusUnity.Robots
{
    public sealed class RobotAvatarManager
    {
        private const string FallbackPrefabKey = "FallbackRobot";
        private readonly Dictionary<string, RobotAvatar> avatarsByAgentId = new Dictionary<string, RobotAvatar>();
        private readonly HashSet<string> availablePrefabKeys;
        private readonly List<string> warnings = new List<string>();

        public RobotAvatarManager(IEnumerable<string> availablePrefabKeys = null)
        {
            this.availablePrefabKeys = availablePrefabKeys == null
                ? new HashSet<string>()
                : new HashSet<string>(availablePrefabKeys);
            this.availablePrefabKeys.Add(FallbackPrefabKey);
        }

        public int AvatarCount => avatarsByAgentId.Count;

        public IReadOnlyList<string> Warnings => warnings;

        public RobotAvatar UpsertFromSpawn(BridgeEnvelope envelope)
        {
            if (envelope == null || envelope.Type != "agent.spawn")
            {
                throw new System.ArgumentException("RobotAvatarManager only accepts agent.spawn envelopes.");
            }

            var agent = envelope.Payload["agent"] as JObject;
            if (agent == null)
            {
                throw new System.ArgumentException("agent.spawn payload must include an agent object.");
            }

            var agentId = RequiredString(agent, "agent_id");
            var displayName = OptionalString(agent, "display_name", agentId);
            var groupId = OptionalString(agent, "group_id", null);
            var requestedPrefab = OptionalString(envelope.Payload, "prefab_key", FallbackPrefabKey);
            var prefabKey = ResolvePrefabKey(agentId, requestedPrefab, out var usesFallbackPrefab);
            var position = ParsePosition(agent["position"] as JObject);
            var visible = OptionalBool(agent, "visible", true);
            var groupBadge = string.IsNullOrWhiteSpace(groupId) ? string.Empty : groupId;
            var groupColorHex = string.IsNullOrWhiteSpace(groupId)
                ? "#B8C2CC"
                : GroupColorPalette.HexForGroupId(groupId);

            if (avatarsByAgentId.TryGetValue(agentId, out var existing))
            {
                existing.Update(
                    displayName,
                    groupId,
                    groupBadge,
                    groupColorHex,
                    prefabKey,
                    usesFallbackPrefab,
                    position,
                    visible);
                return existing;
            }

            var avatar = new RobotAvatar(
                agentId,
                displayName,
                groupId,
                groupBadge,
                groupColorHex,
                prefabKey,
                usesFallbackPrefab,
                position,
                visible);
            avatarsByAgentId[agentId] = avatar;
            return avatar;
        }

        public bool TryGetAvatar(string agentId, out RobotAvatar avatar)
        {
            return avatarsByAgentId.TryGetValue(agentId, out avatar);
        }

        private string ResolvePrefabKey(
            string agentId,
            string requestedPrefab,
            out bool usesFallbackPrefab)
        {
            if (!string.IsNullOrWhiteSpace(requestedPrefab) && availablePrefabKeys.Contains(requestedPrefab))
            {
                usesFallbackPrefab = requestedPrefab == FallbackPrefabKey;
                return requestedPrefab;
            }

            usesFallbackPrefab = true;
            warnings.Add($"Missing prefab '{requestedPrefab}' for {agentId}; using {FallbackPrefabKey}.");
            return FallbackPrefabKey;
        }

        private static RobotVector3 ParsePosition(JObject position)
        {
            if (position == null)
            {
                return new RobotVector3();
            }

            return new RobotVector3
            {
                X = OptionalFloat(position, "x", 0),
                Y = OptionalFloat(position, "y", 0),
                Z = OptionalFloat(position, "z", 0)
            };
        }

        private static string RequiredString(JObject payload, string key)
        {
            var value = OptionalString(payload, key, null);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new System.ArgumentException($"Missing required agent field '{key}'.");
            }

            return value;
        }

        private static string OptionalString(JToken payload, string key, string fallback)
        {
            var value = payload?[key];
            return value == null || value.Type == JTokenType.Null ? fallback : value.ToObject<string>();
        }

        private static bool OptionalBool(JObject payload, string key, bool fallback)
        {
            var value = payload?[key];
            return value == null || value.Type == JTokenType.Null ? fallback : value.ToObject<bool>();
        }

        private static float OptionalFloat(JObject payload, string key, float fallback)
        {
            var value = payload?[key];
            return value == null || value.Type == JTokenType.Null ? fallback : value.ToObject<float>();
        }

    }
}
