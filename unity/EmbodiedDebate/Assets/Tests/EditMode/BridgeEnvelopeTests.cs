using ArgusUnity.Bridge;
using NUnit.Framework;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class BridgeEnvelopeTests
    {
        [Test]
        public void ParseValidEnvelopePreservesPayload()
        {
            const string json = @"{
                ""schema_version"": ""1.0.0"",
                ""message_id"": ""msg-1"",
                ""session_id"": ""session-1"",
                ""sequence"": 1,
                ""sent_at_ms"": 1000,
                ""type"": ""agent.dialogue"",
                ""payload"": { ""speaker_id"": ""agent-001"", ""text"": ""hello"" }
            }";

            var parsed = BridgeEnvelope.TryParse(json, out var envelope, out var error);

            Assert.That(parsed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(envelope.Type, Is.EqualTo("agent.dialogue"));
            Assert.That(envelope.Payload["speaker_id"].ToObject<string>(), Is.EqualTo("agent-001"));
        }

        [Test]
        public void MalformedJsonReturnsStructuredError()
        {
            var parsed = BridgeEnvelope.TryParse("{", out var envelope, out var error);

            Assert.That(parsed, Is.False);
            Assert.That(envelope, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Source, Is.EqualTo("unity"));
            Assert.That(error.Recoverable, Is.True);
        }

        [Test]
        public void UnknownMessageTypeIsPreservedForCallerHandling()
        {
            const string json = @"{
                ""schema_version"": ""1.0.0"",
                ""message_id"": ""msg-unknown"",
                ""session_id"": ""session-1"",
                ""sequence"": 2,
                ""sent_at_ms"": 1000,
                ""type"": ""future.message"",
                ""payload"": { ""value"": 1 }
            }";

            var parsed = BridgeEnvelope.TryParse(json, out var envelope, out var error);

            Assert.That(parsed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(envelope.Type, Is.EqualTo("future.message"));
            Assert.That(envelope.IsKnownBridgeMessageType(), Is.False);
        }

        [Test]
        public void UnityAckUsesExpectedPayloadShape()
        {
            var envelope = BridgeEnvelope.UnityAck("session-1", "bridge-msg", 4, true);

            Assert.That(envelope.Type, Is.EqualTo("unity.ack"));
            Assert.That(
                envelope.Payload["acknowledged_message_id"].ToObject<string>(),
                Is.EqualTo("bridge-msg"));
            Assert.That(envelope.Payload["acknowledged_sequence"].ToObject<long>(), Is.EqualTo(4));
            Assert.That(envelope.Payload["applied"].ToObject<bool>(), Is.True);
        }

        [Test]
        public void NewPersonaSimulationMessagesAreKnown()
        {
            Assert.That(Envelope("environment.load").IsKnownBridgeMessageType(), Is.True);
            Assert.That(Envelope("agent.behavior").IsKnownBridgeMessageType(), Is.True);
            Assert.That(Envelope("simulation.summary").IsKnownBridgeMessageType(), Is.True);
        }

        private static BridgeEnvelope Envelope(string messageType)
        {
            return new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = $"msg-{messageType}",
                SessionId = "session-1",
                Sequence = 1,
                SentAtMs = 1000,
                Type = messageType
            };
        }
    }
}
