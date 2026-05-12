using ArgusUnity.Bridge;
using ArgusUnity.Scene;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Applies high-level persona behavior intents to the concrete mini-bot motor.
    /// </summary>
    public sealed class MiniBotBehaviorHandler : MonoBehaviour
    {
        private SimulationSceneOrchestrator orchestrator;
        private AgentMoveHandler moveHandler;

        public void Initialize(SimulationSceneOrchestrator orch, AgentMoveHandler moves)
        {
            if (orchestrator != null)
            {
                orchestrator.AgentBehavior -= OnAgentBehavior;
            }

            orchestrator = orch;
            moveHandler = moves;

            if (orchestrator != null)
            {
                orchestrator.AgentBehavior += OnAgentBehavior;
            }
        }

        private void OnDestroy()
        {
            if (orchestrator != null)
            {
                orchestrator.AgentBehavior -= OnAgentBehavior;
            }
        }

        private void OnAgentBehavior(BridgeEnvelope envelope)
        {
            if (moveHandler == null || !TryParseBehavior(envelope, out var behavior))
            {
                return;
            }

            if (behavior.HasTargetPosition && behavior.SpeedMetersPerSecond > 0f)
            {
                moveHandler.QueueMove(
                    behavior.AgentId,
                    behavior.TargetPosition,
                    behavior.SpeedMetersPerSecond);
            }
        }

        public static bool TryParseBehavior(
            BridgeEnvelope envelope,
            out MiniBotBehaviorCommand command)
        {
            command = default;
            if (envelope == null || envelope.Type != "agent.behavior" || envelope.Payload == null)
            {
                return false;
            }

            var agentId = envelope.Payload["agent_id"]?.ToObject<string>();
            if (string.IsNullOrWhiteSpace(agentId))
            {
                return false;
            }

            var intent = envelope.Payload["intent"]?.ToObject<string>() ?? "observe";
            var locomotion = envelope.Payload["locomotion"]?.ToObject<string>() ?? "walk";
            var speed = SpeedFor(locomotion, intent);

            var targetToken = envelope.Payload["target_position"] as JObject;
            if (targetToken == null)
            {
                command = new MiniBotBehaviorCommand(
                    agentId.Trim(),
                    intent.Trim(),
                    Vector3.zero,
                    false,
                    speed);
                return true;
            }

            var target = new Vector3(
                targetToken["x"]?.ToObject<float>() ?? 0f,
                targetToken["y"]?.ToObject<float>() ?? 0f,
                targetToken["z"]?.ToObject<float>() ?? 0f);

            command = new MiniBotBehaviorCommand(
                agentId.Trim(),
                intent.Trim(),
                AgentSpawnHandler.GroundedPosition(target),
                true,
                speed);
            return true;
        }

        private static float SpeedFor(string locomotion, string intent)
        {
            var mode = string.IsNullOrWhiteSpace(locomotion)
                ? "walk"
                : locomotion.Trim().ToLowerInvariant();
            if (mode == "idle")
            {
                return 0f;
            }

            if (mode == "run")
            {
                return 1.25f;
            }

            var normalizedIntent = string.IsNullOrWhiteSpace(intent)
                ? string.Empty
                : intent.Trim().ToLowerInvariant();
            return normalizedIntent == "avoid" || normalizedIntent == "leave" ? 0.82f : 0.58f;
        }
    }

    public readonly struct MiniBotBehaviorCommand
    {
        public MiniBotBehaviorCommand(
            string agentId,
            string intent,
            Vector3 targetPosition,
            bool hasTargetPosition,
            float speedMetersPerSecond)
        {
            AgentId = agentId;
            Intent = intent;
            TargetPosition = targetPosition;
            HasTargetPosition = hasTargetPosition;
            SpeedMetersPerSecond = speedMetersPerSecond;
        }

        public string AgentId { get; }

        public string Intent { get; }

        public Vector3 TargetPosition { get; }

        public bool HasTargetPosition { get; }

        public float SpeedMetersPerSecond { get; }
    }
}
