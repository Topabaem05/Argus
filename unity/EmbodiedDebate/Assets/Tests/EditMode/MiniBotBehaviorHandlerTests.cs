using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class MiniBotBehaviorHandlerTests
    {
        [Test]
        public void ParsesBehaviorIntentIntoMovementCommand()
        {
            var envelope = Envelope(new JObject
            {
                ["agent_id"] = "agent-001",
                ["intent"] = "argue",
                ["locomotion"] = "run",
                ["target_position"] = new JObject
                {
                    ["x"] = 1.2f,
                    ["y"] = 5f,
                    ["z"] = -0.4f
                },
                ["public_reason"] = "Persona stance=opposes, confidence=0.90."
            });

            var parsed = MiniBotBehaviorHandler.TryParseBehavior(envelope, out var command);

            Assert.That(parsed, Is.True);
            Assert.That(command.AgentId, Is.EqualTo("agent-001"));
            Assert.That(command.Intent, Is.EqualTo("argue"));
            Assert.That(command.HasTargetPosition, Is.True);
            Assert.That(command.TargetPosition.y, Is.EqualTo(AgentSpawnHandler.BridgeFloorY));
            Assert.That(command.SpeedMetersPerSecond, Is.EqualTo(1.45f).Within(0.0001f));
        }

        [Test]
        public void IdleBehaviorWithoutTargetIsValidButDoesNotMove()
        {
            var envelope = Envelope(new JObject
            {
                ["agent_id"] = "agent-002",
                ["intent"] = "observe",
                ["locomotion"] = "idle",
                ["public_reason"] = "Fallback observe."
            });

            var parsed = MiniBotBehaviorHandler.TryParseBehavior(envelope, out var command);

            Assert.That(parsed, Is.True);
            Assert.That(command.HasTargetPosition, Is.False);
            Assert.That(command.SpeedMetersPerSecond, Is.Zero);
        }

        private static BridgeEnvelope Envelope(JObject payload)
        {
            return new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = "behavior-test",
                SessionId = "session-1",
                Sequence = 1,
                SentAtMs = 1000,
                Type = "agent.behavior",
                Payload = payload
            };
        }
    }
}
