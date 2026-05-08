using ArgusUnity.Bridge;
using ArgusUnity.Robots;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ArgusUnity.Tests.PlayMode
{
    public sealed class RobotAvatarManagerPlayModeTests
    {
        [Test]
        public void FirstSpawnCreatesOneAvatar()
        {
            var manager = new RobotAvatarManager(new[] { "RobotA" });

            var avatar = manager.UpsertFromSpawn(Spawn("agent-001", "Agent One", "group-a", "RobotA"));

            Assert.That(manager.AvatarCount, Is.EqualTo(1));
            Assert.That(avatar.AgentId, Is.EqualTo("agent-001"));
            Assert.That(avatar.DisplayName, Is.EqualTo("Agent One"));
            Assert.That(avatar.UsesFallbackPrefab, Is.False);
        }

        [Test]
        public void DuplicateSpawnUpdatesExistingAvatar()
        {
            var manager = new RobotAvatarManager(new[] { "RobotA" });
            var first = manager.UpsertFromSpawn(Spawn("agent-001", "Agent One", "group-a", "RobotA"));

            var second = manager.UpsertFromSpawn(Spawn("agent-001", "Agent One Updated", "group-b", "RobotA"));

            Assert.That(manager.AvatarCount, Is.EqualTo(1));
            Assert.That(ReferenceEquals(first, second), Is.True);
            Assert.That(second.DisplayName, Is.EqualTo("Agent One Updated"));
            Assert.That(second.GroupId, Is.EqualTo("group-b"));
        }

        [Test]
        public void MissingPrefabUsesFallbackAndRecordsWarning()
        {
            var manager = new RobotAvatarManager();

            var avatar = manager.UpsertFromSpawn(Spawn("agent-001", "Agent One", null, "MissingRobot"));

            Assert.That(avatar.PrefabKey, Is.EqualTo("FallbackRobot"));
            Assert.That(avatar.UsesFallbackPrefab, Is.True);
            Assert.That(manager.Warnings[0], Does.Contain("Missing prefab"));
        }

        [Test]
        public void GroupPresentationIsApplied()
        {
            var manager = new RobotAvatarManager(new[] { "RobotA" });

            var avatar = manager.UpsertFromSpawn(Spawn("agent-001", "Agent One", "group-a", "RobotA"));

            Assert.That(avatar.GroupBadge, Is.EqualTo("group-a"));
            Assert.That(avatar.GroupColorHex, Does.StartWith("#"));
            Assert.That(avatar.GroupColorHex.Length, Is.EqualTo(7));
        }

        private static BridgeEnvelope Spawn(
            string agentId,
            string displayName,
            string groupId,
            string prefabKey)
        {
            var agent = new JObject
            {
                ["agent_id"] = agentId,
                ["display_name"] = displayName,
                ["position"] = new JObject
                {
                    ["x"] = 1.0,
                    ["y"] = 0.0,
                    ["z"] = 2.0
                },
                ["visible"] = true
            };
            if (groupId != null)
            {
                agent["group_id"] = groupId;
            }

            return new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = "spawn-" + agentId,
                SessionId = "session-1",
                Sequence = 1,
                SentAtMs = 1000,
                Type = "agent.spawn",
                Payload = new JObject
                {
                    ["agent"] = agent,
                    ["prefab_key"] = prefabKey
                }
            };
        }
    }
}
