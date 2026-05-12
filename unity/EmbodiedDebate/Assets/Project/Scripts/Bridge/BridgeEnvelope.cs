using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArgusUnity.Bridge
{
    public sealed class BridgeEnvelope
    {
        [JsonProperty("schema_version")]
        public string SchemaVersion { get; set; } = "1.0.0";

        [JsonProperty("message_id")]
        public string MessageId { get; set; } = string.Empty;

        [JsonProperty("correlation_id")]
        public string CorrelationId { get; set; }

        [JsonProperty("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonProperty("sequence")]
        public long Sequence { get; set; }

        [JsonProperty("sent_at_ms")]
        public long SentAtMs { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("payload")]
        public JObject Payload { get; set; } = new JObject();

        public static bool TryParse(
            string json,
            out BridgeEnvelope envelope,
            out StructuredError error)
        {
            envelope = null;
            error = null;

            try
            {
                var parsed = JsonConvert.DeserializeObject<BridgeEnvelope>(json);
                if (parsed == null)
                {
                    error = StructuredError.BridgeError("Envelope JSON was empty.", null);
                    return false;
                }

                var validationError = parsed.Validate();
                if (validationError != null)
                {
                    error = validationError;
                    return false;
                }

                envelope = parsed;
                return true;
            }
            catch (JsonException exception)
            {
                error = StructuredError.BridgeError(
                    "Envelope JSON could not be parsed.",
                    null,
                    new JObject { ["exception"] = exception.Message });
                return false;
            }
        }

        public static BridgeEnvelope UnityReady(string sessionId)
        {
            return UnityMessage("unity.ready", sessionId, "unity-ready", null, new JObject());
        }

        public static BridgeEnvelope UnityAck(
            string sessionId,
            string acknowledgedMessageId,
            long acknowledgedSequence,
            bool applied)
        {
            var payload = JObject.FromObject(new UnityAckPayload
            {
                AcknowledgedMessageId = acknowledgedMessageId,
                AcknowledgedSequence = acknowledgedSequence,
                Applied = applied
            });
            return UnityMessage("unity.ack", sessionId, $"unity-ack-{acknowledgedSequence}", acknowledgedMessageId, payload);
        }

        public static BridgeEnvelope UnityError(
            string sessionId,
            StructuredError error,
            string correlationId)
        {
            return UnityMessage(
                "unity.error",
                sessionId,
                $"unity-error-{NowMs()}",
                correlationId,
                JObject.FromObject(error));
        }

        public static BridgeEnvelope ObserverPause(string sessionId)
        {
            return UnityMessage(
                "observer.pause",
                sessionId,
                $"observer-pause-{NowMs()}",
                null,
                new JObject());
        }

        public static BridgeEnvelope ObserverResume(string sessionId)
        {
            return UnityMessage(
                "observer.resume",
                sessionId,
                $"observer-resume-{NowMs()}",
                null,
                new JObject());
        }

        public static BridgeEnvelope ObserverStep(string sessionId)
        {
            return UnityMessage(
                "observer.step",
                sessionId,
                $"observer-step-{NowMs()}",
                null,
                new JObject());
        }

        public static BridgeEnvelope ObserverSelectAgent(string sessionId, string agentId)
        {
            var payload = new JObject();
            if (!string.IsNullOrWhiteSpace(agentId))
            {
                payload["agent_id"] = agentId.Trim();
            }

            return UnityMessage(
                "observer.select_agent",
                sessionId,
                $"observer-select-{NowMs()}",
                null,
                payload);
        }

        public static BridgeEnvelope ObserverCameraState(
            string sessionId,
            float posX,
            float posY,
            float posZ,
            float forwardX,
            float forwardY,
            float forwardZ,
            float verticalFovDegrees)
        {
            var payload = new JObject
            {
                ["world_position"] = new JObject { ["x"] = posX, ["y"] = posY, ["z"] = posZ },
                ["forward"] = new JObject { ["x"] = forwardX, ["y"] = forwardY, ["z"] = forwardZ },
                ["vertical_fov_degrees"] = verticalFovDegrees,
            };

            return UnityMessage(
                "observer.camera_state",
                sessionId,
                $"observer-camera-{NowMs()}",
                null,
                payload);
        }

        public bool IsKnownBridgeMessageType()
        {
            switch (Type)
            {
                case "bridge.ready":
                case "bridge.error":
                case "simulation.snapshot":
                case "simulation.event":
                case "simulation.summary":
                case "environment.load":
                case "ui.status":
                case "agent.spawn":
                case "agent.move":
                case "agent.behavior":
                case "agent.animation":
                case "agent.dialogue":
                case "agent.emotion":
                case "group.update":
                case "conflict.update":
                case "physics.request":
                case "physics.result":
                case "replay.status":
                case "adapter.error":
                case "observer.pause":
                case "observer.resume":
                case "observer.step":
                case "observer.select_agent":
                case "observer.camera_state":
                    return true;
                default:
                    return false;
            }
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        private StructuredError Validate()
        {
            if (string.IsNullOrWhiteSpace(SchemaVersion) || !SchemaVersion.StartsWith("1.", StringComparison.Ordinal))
            {
                return StructuredError.BridgeError("Unsupported bridge schema version.", MessageId);
            }

            if (string.IsNullOrWhiteSpace(MessageId))
            {
                return StructuredError.BridgeError("Bridge envelope is missing message_id.", null);
            }

            if (string.IsNullOrWhiteSpace(SessionId))
            {
                return StructuredError.BridgeError("Bridge envelope is missing session_id.", MessageId);
            }

            if (Sequence < 0)
            {
                return StructuredError.BridgeError("Bridge envelope sequence must be non-negative.", MessageId);
            }

            if (SentAtMs < 0)
            {
                return StructuredError.BridgeError("Bridge envelope sent_at_ms must be non-negative.", MessageId);
            }

            if (string.IsNullOrWhiteSpace(Type))
            {
                return StructuredError.BridgeError("Bridge envelope is missing type.", MessageId);
            }

            if (Payload == null)
            {
                Payload = new JObject();
            }

            return null;
        }

        private static BridgeEnvelope UnityMessage(
            string type,
            string sessionId,
            string messageId,
            string correlationId,
            JObject payload)
        {
            return new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = messageId,
                CorrelationId = correlationId,
                SessionId = sessionId,
                Sequence = 0,
                SentAtMs = NowMs(),
                Type = type,
                Payload = payload ?? new JObject()
            };
        }

        private static long NowMs()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow - epoch).TotalMilliseconds;
        }
    }

    public sealed class UnityAckPayload
    {
        [JsonProperty("acknowledged_message_id")]
        public string AcknowledgedMessageId { get; set; } = string.Empty;

        [JsonProperty("acknowledged_sequence")]
        public long AcknowledgedSequence { get; set; }

        [JsonProperty("applied")]
        public bool Applied { get; set; }

        [JsonProperty("warnings")]
        public string[] Warnings { get; set; } = Array.Empty<string>();
    }

    public sealed class StructuredError
    {
        [JsonProperty("error_id")]
        public string ErrorId { get; set; } = string.Empty;

        [JsonProperty("source")]
        public string Source { get; set; } = "unity";

        [JsonProperty("severity")]
        public string Severity { get; set; } = "error";

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("recoverable")]
        public bool Recoverable { get; set; } = true;

        [JsonProperty("correlation_id")]
        public string CorrelationId { get; set; }

        [JsonProperty("details")]
        public JObject Details { get; set; } = new JObject();

        public static StructuredError BridgeError(string message, string correlationId, JObject details = null)
        {
            return new StructuredError
            {
                ErrorId = $"unity-parse-error-{Guid.NewGuid():N}",
                Source = "unity",
                Severity = "error",
                Message = message,
                Recoverable = true,
                CorrelationId = correlationId,
                Details = details ?? new JObject()
            };
        }
    }
}
