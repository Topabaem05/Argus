using ArgusUnity.Bridge;
using ArgusUnity.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class BridgePresentationParsingTests
    {
        [Test]
        public void DialogueParsingExtractsSpeakerTextAndDuration()
        {
            var envelope = Build(
                "agent.dialogue",
                new JObject
                {
                    ["speaker_id"] = "a-1",
                    ["text"] = "Hello world.",
                    ["duration_ms"] = 800,
                    ["speech_act"] = "say",
                });

            Assert.That(BridgePresentationParsing.TryGetDialogue(envelope, out var vm), Is.True);
            Assert.That(vm.SpeakerId, Is.EqualTo("a-1"));
            Assert.That(vm.Text, Is.EqualTo("Hello world."));
            Assert.That(vm.DurationMs, Is.EqualTo(800));
        }

        [Test]
        public void EmotionParsingExtractsIntensity()
        {
            var envelope = Build(
                "agent.emotion",
                new JObject
                {
                    ["agent_id"] = "robot-9",
                    ["label"] = "angry",
                    ["intensity"] = 0.33,
                });

            Assert.That(BridgePresentationParsing.TryGetAgentEmotion(envelope, out var vm), Is.True);
            Assert.That(vm.AgentId, Is.EqualTo("robot-9"));
            Assert.That(vm.Label, Is.EqualTo("angry"));
            Assert.That(vm.Intensity, Is.EqualTo(0.33f).Within(0.001f));
        }

        [Test]
        public void BehaviorEmotionParsingExtractsNestedEmotion()
        {
            var envelope = Build(
                "agent.behavior",
                new JObject
                {
                    ["agent_id"] = "robot-10",
                    ["intent"] = "ask",
                    ["emotion"] = new JObject
                    {
                        ["label"] = "confused",
                        ["intensity"] = 0.62,
                    },
                    ["public_reason"] = "Persona stance=mixed, confidence=0.62.",
                });

            Assert.That(BridgePresentationParsing.TryGetAgentEmotion(envelope, out var vm), Is.True);
            Assert.That(vm.AgentId, Is.EqualTo("robot-10"));
            Assert.That(vm.Label, Is.EqualTo("confused"));
            Assert.That(vm.Intensity, Is.EqualTo(0.62f).Within(0.001f));
        }

        [Test]
        public void DialogueTruncationAppendsEllipsis()
        {
            var longText = new string('x', 30);
            var cut = BridgePresentationParsing.TruncateDialogue(longText, 10);

            Assert.That(cut.Length, Is.LessThanOrEqualTo(11));
            Assert.That(cut, Does.EndWith("…"));
        }

        [Test]
        public void GroupUpdateParsingReadsMembers()
        {
            var envelope = Build(
                "group.update",
                new JObject
                {
                    ["group_id"] = "g1",
                    ["member_agent_ids"] = new JArray("m1", "m2"),
                    ["badge_label"] = "unit",
                });

            Assert.That(BridgePresentationParsing.TryGetGroupUpdate(envelope, out var vm), Is.True);
            Assert.That(vm.MemberAgentIds.Length, Is.EqualTo(2));
            Assert.That(vm.BadgeDisplay, Is.EqualTo("unit"));
        }

        [Test]
        public void ConflictUpdateNormalizesStageCase()
        {
            var envelope = Build(
                "conflict.update",
                new JObject
                {
                    ["conflict_id"] = "cid",
                    ["participant_ids"] = new JArray("a", "b"),
                    ["intensity"] = 0.4,
                    ["stage"] = "Tension",
                    ["public_summary"] = "Brief summary for test.",
                });

            Assert.That(BridgePresentationParsing.TryGetConflictUpdate(envelope, out var vm), Is.True);
            Assert.That(vm.Stage, Is.EqualTo("tension"));
        }

        private static BridgeEnvelope Build(string type, JObject payload)
        {
            return new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = "m1",
                SessionId = "s1",
                Sequence = 1,
                SentAtMs = 1,
                Type = type,
                Payload = payload,
            };
        }
    }
}
