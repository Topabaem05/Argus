using System.Collections.Generic;
using ArgusUnity.Bridge;
using ArgusUnity.Scene;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Smoothly moves spawned agents toward agent.move targets.
    /// </summary>
    public sealed class AgentMoveHandler : MonoBehaviour
    {
        private sealed class MoveState
        {
            public Vector3 Target;
            public float SpeedMetersPerSecond;
        }

        private SimulationSceneOrchestrator orchestrator;
        private AgentSpawnHandler spawnHandler;
        private readonly Dictionary<string, MoveState> activeMoves = new Dictionary<string, MoveState>();
        private readonly List<string> completedMoves = new List<string>();

        [SerializeField]
        private float movementPaceScale = 0.78f;

        [SerializeField]
        private float maxMoveSpeedMetersPerSecond = 1.45f;

        [SerializeField]
        private float maxTurnDegreesPerSecond = 260f;

        [SerializeField]
        private float turnSharpness = 7.5f;

        [SerializeField]
        private float arrivalDistance = 0.02f;

        public void Initialize(SimulationSceneOrchestrator orch, AgentSpawnHandler spawn)
        {
            orchestrator = orch;
            spawnHandler = spawn;
            orchestrator.AgentMove += OnAgentMove;
        }

        private void OnDestroy()
        {
            if (orchestrator != null)
            {
                orchestrator.AgentMove -= OnAgentMove;
            }
        }

        private void OnAgentMove(BridgeEnvelope envelope)
        {
            var agentId = envelope.Payload["agent_id"]?.ToObject<string>();
            if (string.IsNullOrEmpty(agentId))
            {
                return;
            }

            var targetToken = envelope.Payload["target_position"] as JObject;
            if (targetToken == null)
            {
                return;
            }

            var target = new Vector3(
                targetToken["x"]?.ToObject<float>() ?? 0f,
                targetToken["y"]?.ToObject<float>() ?? 0f,
                targetToken["z"]?.ToObject<float>() ?? 0f);
            target = AgentSpawnHandler.GroundedPosition(target);

            var speed = RoomNavigationMath.PaceScaledSpeed(
                envelope.Payload["speed_mps"]?.ToObject<float>() ?? 1.5f,
                movementPaceScale,
                maxMoveSpeedMetersPerSecond,
                0.05f);

            QueueMove(agentId, target, speed);
        }

        public void QueueMove(string agentId, Vector3 target, float speedMetersPerSecond)
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                return;
            }

            activeMoves[agentId] = new MoveState
            {
                Target = AgentSpawnHandler.GroundedPosition(target),
                SpeedMetersPerSecond = Mathf.Clamp(
                    speedMetersPerSecond,
                    0.05f,
                    maxMoveSpeedMetersPerSecond),
            };
        }

        private void Update()
        {
            if (spawnHandler == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            completedMoves.Clear();
            foreach (var pair in activeMoves)
            {
                if (!spawnHandler.TryGetAgentTransform(pair.Key, out var tr))
                {
                    continue;
                }

                var state = pair.Value;
                if (MoveFacingTarget(tr, state.Target, state.SpeedMetersPerSecond, dt))
                {
                    completedMoves.Add(pair.Key);
                }
            }

            for (var i = 0; i < completedMoves.Count; i++)
            {
                activeMoves.Remove(completedMoves[i]);
            }
        }

        private bool MoveFacingTarget(Transform tr, Vector3 target, float speedMetersPerSecond, float dt)
        {
            var toTarget = target - tr.position;
            var planarToTarget = toTarget;
            planarToTarget.y = 0f;
            var planarDistance = planarToTarget.magnitude;
            var maxStep = speedMetersPerSecond * dt;
            if (planarDistance <= Mathf.Max(arrivalDistance, maxStep))
            {
                tr.position = AgentSpawnHandler.GroundedPosition(Vector3.MoveTowards(tr.position, target, maxStep));
                return true;
            }

            var desiredDirection = planarToTarget / planarDistance;
            var currentForward = tr.forward;
            currentForward.y = 0f;
            var smoothedDirection = LocomotionMath.SmoothPlanarDirection(
                currentForward,
                desiredDirection,
                turnSharpness,
                dt);
            tr.rotation = Quaternion.RotateTowards(
                tr.rotation,
                Quaternion.LookRotation(smoothedDirection, Vector3.up),
                maxTurnDegreesPerSecond * dt);

            var forward = tr.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = desiredDirection;
            }

            forward.Normalize();
            var alignment = Mathf.Clamp01(Vector3.Dot(forward, desiredDirection));
            var step = maxStep * Mathf.Lerp(0.18f, 1f, alignment);
            var planarNext = tr.position + forward * step;
            tr.position = new Vector3(
                planarNext.x,
                AgentSpawnHandler.BridgeFloorY,
                planarNext.z);
            return false;
        }
    }
}
