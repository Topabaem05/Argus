using System;
using ArgusUnity.Bridge;
using ArgusUnity.Scene;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Local demo: spawns several agents and issues periodic agent.move commands without the Python bridge.
    /// </summary>
    public sealed class DemoSpawner : MonoBehaviour
    {
        private const int AgentCount = 5;

        private SimulationSceneOrchestrator orchestrator;
        private long sequence;
        private float moveCooldown = 3f;
        private float dialogueCooldown = 2.5f;
        private float emotionCooldown = 3.8f;
        private float groupCooldown = 5.5f;
        private float conflictCooldown = 7f;

        private int conflictPhase;

        private static readonly string[] EmotionLabels =
        {
            "neutral",
            "happy",
            "sad",
            "angry",
            "afraid",
            "confused",
            "excited",
        };

        private static readonly string[] DemoConflictStages =
        {
            "tension",
            "argument",
            "physical_risk",
            "deescalating",
            "resolved",
            "none",
        };

        public void Initialize(SimulationSceneOrchestrator orch)
        {
            orchestrator = orch;
        }

        private void Start()
        {
            if (!enabled || orchestrator == null)
            {
                return;
            }

            for (var i = 0; i < AgentCount; i++)
            {
                DispatchSpawn($"demo-agent-{i}", $"Agent {i}", i);
            }
        }

        private void Update()
        {
            if (!enabled || orchestrator == null)
            {
                return;
            }

            moveCooldown -= Time.deltaTime;
            if (moveCooldown <= 0f)
            {
                moveCooldown = 3f;
                DispatchRandomMoves();
            }

            dialogueCooldown -= Time.deltaTime;
            if (dialogueCooldown <= 0f)
            {
                dialogueCooldown = 4f;
                DispatchRandomDialogue();
            }

            emotionCooldown -= Time.deltaTime;
            if (emotionCooldown <= 0f)
            {
                emotionCooldown = 5f;
                DispatchRandomEmotion();
            }

            groupCooldown -= Time.deltaTime;
            if (groupCooldown <= 0f)
            {
                groupCooldown = 9f;
                DispatchSyntheticGroup();
            }

            conflictCooldown -= Time.deltaTime;
            if (conflictCooldown <= 0f)
            {
                conflictCooldown = 10f;
                DispatchSyntheticConflict();
            }
        }

        private void DispatchSpawn(string agentId, string displayName, int index)
        {
            var seq = NextSequence();
            var x = (index % 5) * 2f;
            var z = (index / 5) * 2f;
            var envelope = new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = $"demo-spawn-{agentId}-{seq}",
                SessionId = "demo-session",
                Sequence = seq,
                SentAtMs = NowMs(),
                Type = "agent.spawn",
                Payload = new JObject
                {
                    ["agent"] = new JObject
                    {
                        ["agent_id"] = agentId,
                        ["display_name"] = displayName,
                        ["group_id"] = $"grp-{index % 3}",
                        ["position"] = new JObject { ["x"] = x, ["y"] = 0.6f, ["z"] = z },
                        ["visible"] = true,
                    },
                    ["spawn_reason"] = "scenario_start",
                    ["prefab_key"] = "UserModels/Idle",
                },
            };

            if (!orchestrator.TryHandle(envelope, out var issue) && issue != null)
            {
                Debug.LogWarning($"Demo spawn routing issue: {issue.Message}");
            }
        }

        private void DispatchRandomMoves()
        {
            for (var i = 0; i < AgentCount; i++)
            {
                var agentId = $"demo-agent-{i}";
                var target = new Vector3(
                    Random.Range(-6f, 6f),
                    0.6f,
                    Random.Range(-4f, 8f));

                var seq = NextSequence();
                var envelope = new BridgeEnvelope
                {
                    SchemaVersion = "1.0.0",
                    MessageId = $"demo-move-{agentId}-{seq}",
                    SessionId = "demo-session",
                    Sequence = seq,
                    SentAtMs = NowMs(),
                    Type = "agent.move",
                    Payload = new JObject
                    {
                        ["agent_id"] = agentId,
                        ["target_position"] = new JObject
                        {
                            ["x"] = target.x,
                            ["y"] = target.y,
                            ["z"] = target.z,
                        },
                        ["speed_mps"] = 2.5f,
                        ["movement_style"] = "walk",
                    },
                };

                orchestrator.TryHandle(envelope, out _);
            }
        }

        private void DispatchRandomDialogue()
        {
            if (orchestrator == null)
            {
                return;
            }

            var i = Random.Range(0, AgentCount);
            var agentId = $"demo-agent-{i}";
            var seq = NextSequence();
            var snippet =
                "This is a synthetic line used to verify dialogue bubbles stay readable above the robot.";
            var envelope = new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = $"demo-dialogue-{agentId}-{seq}",
                SessionId = "demo-session",
                Sequence = seq,
                SentAtMs = NowMs(),
                Type = "agent.dialogue",
                Payload = new JObject
                {
                    ["speaker_id"] = agentId,
                    ["target_ids"] = new JArray(),
                    ["text"] = snippet,
                    ["emotion"] = new JObject { ["label"] = "happy", ["intensity"] = 0.5 },
                    ["speech_act"] = "say",
                    ["duration_ms"] = 3500,
                },
            };

            orchestrator.TryHandle(envelope, out _);
        }

        private void DispatchRandomEmotion()
        {
            if (orchestrator == null)
            {
                return;
            }

            var i = Random.Range(0, AgentCount);
            var agentId = $"demo-agent-{i}";
            var label = EmotionLabels[Random.Range(0, EmotionLabels.Length)];
            var intensity = Random.Range(0.2f, 1f);
            var seq = NextSequence();

            var envelope = new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = $"demo-emotion-{agentId}-{seq}",
                SessionId = "demo-session",
                Sequence = seq,
                SentAtMs = NowMs(),
                Type = "agent.emotion",
                Payload = new JObject
                {
                    ["agent_id"] = agentId,
                    ["label"] = label,
                    ["intensity"] = intensity,
                },
            };

            orchestrator.TryHandle(envelope, out _);
        }

        private void DispatchSyntheticGroup()
        {
            if (orchestrator == null)
            {
                return;
            }

            var pickA = Random.Range(0, 2) == 0;
            var members = pickA
                ? new JArray("demo-agent-0", "demo-agent-1", "demo-agent-2")
                : new JArray("demo-agent-3", "demo-agent-4");
            var gid = pickA ? "faction-a-demo" : "faction-b-demo";
            var badge = pickA ? "A" : "B";
            var seq = NextSequence();

            orchestrator.TryHandle(
                new BridgeEnvelope
                {
                    SchemaVersion = "1.0.0",
                    MessageId = $"demo-group-{seq}",
                    SessionId = "demo-session",
                    Sequence = seq,
                    SentAtMs = NowMs(),
                    Type = "group.update",
                    Payload = new JObject
                    {
                        ["group_id"] = gid,
                        ["member_agent_ids"] = members,
                        ["badge_label"] = badge,
                    },
                },
                out _);
        }

        private void DispatchSyntheticConflict()
        {
            if (orchestrator == null)
            {
                return;
            }

            var stage =
                DemoConflictStages[Random.Range(0, DemoConflictStages.Length)];
            var intensity = Random.Range(0.3f, 0.92f);
            conflictPhase++;

            var seq = NextSequence();
            orchestrator.TryHandle(
                new BridgeEnvelope
                {
                    SchemaVersion = "1.0.0",
                    MessageId = $"demo-conflict-{seq}",
                    SessionId = "demo-session",
                    Sequence = seq,
                    SentAtMs = NowMs(),
                    Type = "conflict.update",
                    Payload = new JObject
                    {
                        ["conflict_id"] = "demo-multi-party",
                        ["participant_ids"] = new JArray("demo-agent-0", "demo-agent-3"),
                        ["intensity"] = intensity,
                        ["stage"] = stage,
                        ["public_summary"] =
                            "Sides disagree on procedural rules only; framing stays factual and avoids personal attacks.",
                    },
                },
                out _);
        }

        private long NextSequence()
        {
            sequence += 1;
            return sequence;
        }

        private static long NowMs()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow - epoch).TotalMilliseconds;
        }
    }
}
