using ArgusUnity.Bridge;
using ArgusUnity.Robots;
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
            var groupLine = groupId == null ? string.Empty : $@",""group_id"":""{groupId}""";
            return ParseEnvelope($@"{{
                ""schema_version"": ""1.0.0"",
                ""message_id"": ""spawn-{agentId}"",
                ""session_id"": ""session-1"",
                ""sequence"": 1,
                ""sent_at_ms"": 1000,
                ""type"": ""agent.spawn"",
                ""payload"": {{
                    ""agent"": {{
                        ""agent_id"": ""{agentId}"",
                        ""display_name"": ""{displayName}"",
                        ""position"": {{ ""x"": 1.0, ""y"": 0.0, ""z"": 2.0 }},
                        ""visible"": true{groupLine}
                    }},
                    ""prefab_key"": ""{prefabKey}""
                }}
            }}");
        }

        private static BridgeEnvelope ParseEnvelope(string json)
        {
            Assert.That(BridgeEnvelope.TryParse(json, out var envelope, out var error), Is.True, error?.ToString());
            return envelope;
        }
    }
}
