using System.Collections.Generic;
using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using ArgusUnity.Scene;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class BridgeMotionAdapter : MonoBehaviour
    {
        [SerializeField]
        private PersonaMotionMapper personaMotionMapper;

        private readonly Dictionary<string, MinibotMotionController> agents =
            new Dictionary<string, MinibotMotionController>();

        private SimulationSceneOrchestrator orchestrator;
        private AgentSpawnHandler spawnHandler;

        public int RegisteredAgentCount => agents.Count;

        public void Initialize(SimulationSceneOrchestrator nextOrchestrator, AgentSpawnHandler nextSpawnHandler)
        {
            if (orchestrator != null)
            {
                orchestrator.AgentMove -= OnAgentMove;
                orchestrator.AgentBehavior -= OnAgentBehavior;
                orchestrator.AgentDialogue -= OnAgentDialogue;
                orchestrator.AgentAnimation -= OnAgentAnimation;
                orchestrator.AgentEmotion -= OnAgentEmotion;
                orchestrator.PhysicsResult -= OnPhysicsResult;
            }

            orchestrator = nextOrchestrator;
            spawnHandler = nextSpawnHandler;
            personaMotionMapper = personaMotionMapper ?? GetComponent<PersonaMotionMapper>() ?? gameObject.AddComponent<PersonaMotionMapper>();

            if (orchestrator != null)
            {
                orchestrator.AgentMove += OnAgentMove;
                orchestrator.AgentBehavior += OnAgentBehavior;
                orchestrator.AgentDialogue += OnAgentDialogue;
                orchestrator.AgentAnimation += OnAgentAnimation;
                orchestrator.AgentEmotion += OnAgentEmotion;
                orchestrator.PhysicsResult += OnPhysicsResult;
            }
        }

        private void OnDestroy()
        {
            if (orchestrator == null)
            {
                return;
            }

            orchestrator.AgentMove -= OnAgentMove;
            orchestrator.AgentBehavior -= OnAgentBehavior;
            orchestrator.AgentDialogue -= OnAgentDialogue;
            orchestrator.AgentAnimation -= OnAgentAnimation;
            orchestrator.AgentEmotion -= OnAgentEmotion;
            orchestrator.PhysicsResult -= OnPhysicsResult;
        }

        public void RegisterAgent(string agentId, MinibotMotionController controller)
        {
            if (!string.IsNullOrWhiteSpace(agentId) && controller != null)
            {
                agents[agentId.Trim()] = controller;
            }
        }

        public bool TryApplyMove(string agentId, Vector3 target, float speedMetersPerSecond, float urgency = 0.35f)
        {
            if (!TryResolveController(agentId, out var controller))
            {
                return false;
            }

            var intent = new MotionIntent(
                true,
                AgentSpawnHandler.GroundedPosition(target),
                null,
                speedMetersPerSecond,
                0.12f,
                MotionEmotion.Neutral,
                MotionGesture.None,
                MotionAction.None,
                true,
                urgency);
            controller.ApplyIntent(intent);
            return true;
        }

        private void OnAgentMove(BridgeEnvelope envelope)
        {
            var agentId = envelope.Payload["agent_id"]?.ToObject<string>();
            if (!TryResolveController(agentId, out var controller))
            {
                return;
            }

            var targetToken = envelope.Payload["target_position"] as JObject;
            if (targetToken == null)
            {
                return;
            }

            var target = new Vector3(
                targetToken["x"]?.ToObject<float>() ?? controller.transform.position.x,
                targetToken["y"]?.ToObject<float>() ?? controller.transform.position.y,
                targetToken["z"]?.ToObject<float>() ?? controller.transform.position.z);
            var speed = envelope.Payload["speed_mps"]?.ToObject<float>() ??
                        envelope.Payload["speed"]?.ToObject<float>() ??
                        0.58f;
            var urgency = envelope.Payload["urgency"]?.ToObject<float>() ?? Mathf.Clamp01(speed);
            var intent = new MotionIntent(
                true,
                AgentSpawnHandler.GroundedPosition(target),
                null,
                Mathf.Clamp(speed, 0.05f, 1.25f),
                0.12f,
                PersonaMotionMapper.MapEmotion(ParseEmotion(envelope.Payload), envelope.Payload["intent"]?.ToObject<string>()),
                MotionGesture.None,
                MotionAction.None,
                true,
                urgency);
            controller.ApplyIntent(intent);
        }

        private void OnAgentBehavior(BridgeEnvelope envelope)
        {
            var agentId = envelope.Payload["agent_id"]?.ToObject<string>();
            if (!TryResolveController(agentId, out var controller))
            {
                return;
            }

            var intent = personaMotionMapper.BuildFromBehaviorPayload(controller.transform.position, envelope.Payload);
            controller.ApplyIntent(intent);
        }

        private void OnAgentDialogue(BridgeEnvelope envelope)
        {
            ApplyDialogueLikeEvent(envelope, "dialogue_act");
        }

        private void OnAgentAnimation(BridgeEnvelope envelope)
        {
            ApplyDialogueLikeEvent(envelope, "animation_hint");
        }

        private void OnAgentEmotion(BridgeEnvelope envelope)
        {
            ApplyDialogueLikeEvent(envelope, "emotion");
        }

        private void OnPhysicsResult(BridgeEnvelope envelope)
        {
            var agentId = envelope.Payload["agent_id"]?.ToObject<string>() ??
                          envelope.Payload["primary_agent_id"]?.ToObject<string>();
            if (!TryResolveController(agentId, out var controller))
            {
                return;
            }

            var kind = envelope.Payload["result_kind"]?.ToObject<string>() ??
                       envelope.Payload["kind"]?.ToObject<string>() ??
                       envelope.Payload["state"]?.ToObject<string>() ??
                       string.Empty;
            var intensity = envelope.Payload["intensity"]?.ToObject<float>() ?? 0.4f;
            controller.ApplyIntent(personaMotionMapper.BuildPhysicsReactionIntent(
                controller.transform.position,
                kind,
                intensity));
        }

        private void ApplyDialogueLikeEvent(BridgeEnvelope envelope, string semanticField)
        {
            var agentId = envelope.Payload["agent_id"]?.ToObject<string>() ??
                          envelope.Payload["speaker_agent_id"]?.ToObject<string>();
            if (!TryResolveController(agentId, out var controller))
            {
                return;
            }

            var emotion = ParseEmotion(envelope.Payload);
            var act = envelope.Payload[semanticField]?.ToObject<string>() ??
                      envelope.Payload["intent"]?.ToObject<string>() ??
                      envelope.Payload["dialogue_act"]?.ToObject<string>() ??
                      "talk";
            controller.ApplyIntent(personaMotionMapper.BuildDialogueIntent(
                controller.transform.position,
                emotion,
                act));
        }

        private bool TryResolveController(string agentId, out MinibotMotionController controller)
        {
            controller = null;
            if (string.IsNullOrWhiteSpace(agentId))
            {
                return false;
            }

            agentId = agentId.Trim();
            if (agents.TryGetValue(agentId, out controller) && controller != null)
            {
                return true;
            }

            if (spawnHandler == null || !spawnHandler.TryGetAgentTransform(agentId, out var transform) || transform == null)
            {
                return false;
            }

            controller = transform.GetComponent<MinibotMotionController>() ??
                         transform.gameObject.AddComponent<MinibotMotionController>();
            controller.Initialize(agentId, StableHash(agentId));
            agents[agentId] = controller;
            return true;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 5381;
                for (var i = 0; i < value.Length; i++)
                {
                    hash = ((hash << 5) + hash) ^ value[i];
                }

                return hash;
            }
        }

        private static string ParseEmotion(JObject payload)
        {
            if (payload == null)
            {
                return "neutral";
            }

            if (payload["emotion"] is JObject emotionObject)
            {
                return emotionObject["label"]?.ToObject<string>() ?? "neutral";
            }

            return payload["emotion"]?.ToObject<string>() ?? "neutral";
        }
    }
}
