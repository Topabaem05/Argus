using System;
using Newtonsoft.Json.Linq;
using ArgusUnity.Bridge;

namespace ArgusUnity.Scene
{
    public sealed class SimulationSceneOrchestrator
    {
        public event Action<BridgeEnvelope> AgentSpawn;
        public event Action<BridgeEnvelope> AgentMove;
        public event Action<BridgeEnvelope> AgentDialogue;
        public event Action<BridgeEnvelope> AgentEmotion;
        public event Action<BridgeEnvelope> GroupUpdate;
        public event Action<BridgeEnvelope> ConflictUpdate;
        public event Action<BridgeEnvelope> PhysicsResult;

        public bool TryHandle(BridgeEnvelope envelope, out StructuredError issue)
        {
            issue = null;
            if (envelope == null)
            {
                issue = BuildIssue("error", "Cannot route a null bridge envelope.", null, null);
                return false;
            }

            try
            {
                switch (envelope.Type)
                {
                    case "agent.spawn":
                        AgentSpawn?.Invoke(envelope);
                        return true;
                    case "agent.move":
                        AgentMove?.Invoke(envelope);
                        return true;
                    case "agent.dialogue":
                        AgentDialogue?.Invoke(envelope);
                        return true;
                    case "agent.emotion":
                        AgentEmotion?.Invoke(envelope);
                        return true;
                    case "group.update":
                        GroupUpdate?.Invoke(envelope);
                        return true;
                    case "conflict.update":
                        ConflictUpdate?.Invoke(envelope);
                        return true;
                    case "physics.result":
                        PhysicsResult?.Invoke(envelope);
                        return true;
                    case "replay.status":
                        return true;
                    default:
                        issue = UnsupportedTypeIssue(envelope);
                        return false;
                }
            }
            catch (Exception exception)
            {
                issue = BuildIssue(
                    "error",
                    "Scene handler failed while routing bridge envelope.",
                    envelope.MessageId,
                    new JObject
                    {
                        ["message_type"] = envelope.Type,
                        ["exception"] = exception.Message
                    });
                return false;
            }
        }

        private static StructuredError UnsupportedTypeIssue(BridgeEnvelope envelope)
        {
            var severity = envelope.IsKnownBridgeMessageType() ? "warning" : "error";
            var message = envelope.IsKnownBridgeMessageType()
                ? "Bridge message type is known but not routed by the scene orchestrator."
                : "Bridge message type is unknown to the scene orchestrator.";
            return BuildIssue(
                severity,
                message,
                envelope.MessageId,
                new JObject { ["message_type"] = envelope.Type });
        }

        private static StructuredError BuildIssue(
            string severity,
            string message,
            string correlationId,
            JObject details)
        {
            return new StructuredError
            {
                ErrorId = $"scene-route-{Guid.NewGuid():N}",
                Source = "unity",
                Severity = severity,
                Message = message,
                Recoverable = true,
                CorrelationId = correlationId,
                Details = details ?? new JObject()
            };
        }
    }
}
