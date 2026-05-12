using System.IO;
using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using ArgusUnity.Scene;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class MiniBotAutoDumpTests
    {
        [Test]
        public void AutoDumpWritesBridgeMoveAndRuntimeState()
        {
            var root = new GameObject("bootstrap");
            var spawnRoot = new GameObject("BridgeSceneRoot").transform;
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prefab.name = "MiniBotTestPrefab";
            prefab.AddComponent<Rigidbody>();
            prefab.AddComponent<Animator>();

            try
            {
                var orchestrator = new SimulationSceneOrchestrator();
                var spawn = root.AddComponent<AgentSpawnHandler>();
                var move = root.AddComponent<AgentMoveHandler>();
                var dump = root.AddComponent<MiniBotAutoDump>();

                spawn.Initialize(orchestrator, prefab, spawnRoot);
                move.Initialize(orchestrator, spawn);
                dump.Initialize(orchestrator, spawn, move, null, prefab);

                Assert.That(File.Exists(Path.Combine(dump.OutputDirectory, "scene_dump.json")), Is.True);
                Assert.That(File.Exists(Path.Combine(dump.OutputDirectory, "prefab_dump.json")), Is.True);
                Assert.That(File.Exists(Path.Combine(dump.OutputDirectory, "animator_dump.json")), Is.True);
                Assert.That(File.Exists(Path.Combine(dump.OutputDirectory, "physics_dump.json")), Is.True);
                Assert.That(File.Exists(Path.Combine(dump.OutputDirectory, "bridge_event_dump.json")), Is.True);
                Assert.That(File.Exists(Path.Combine(dump.OutputDirectory, "minibot_runtime_trace.jsonl")), Is.True);

                Assert.That(orchestrator.TryHandle(SpawnEnvelope(), out var spawnIssue), Is.True, spawnIssue?.Message);
                Assert.That(orchestrator.TryHandle(MoveEnvelope(), out var moveIssue), Is.True, moveIssue?.Message);

                var bridgeDump = JObject.Parse(File.ReadAllText(Path.Combine(dump.OutputDirectory, "bridge_event_dump.json")));
                Assert.That(bridgeDump["last_event_type"]?.ToObject<string>(), Is.EqualTo("agent.move"));
                Assert.That(bridgeDump["agent_id"]?.ToObject<string>(), Is.EqualTo("persona_001"));
                Assert.That(bridgeDump["event_applied_to_minibot"]?.ToObject<bool>(), Is.True);
                Assert.That(bridgeDump["target_position"], Has.Count.EqualTo(3));

                var physicsDump = JObject.Parse(File.ReadAllText(Path.Combine(dump.OutputDirectory, "physics_dump.json")));
                Assert.That(physicsDump["agent_count"]?.ToObject<int>(), Is.EqualTo(1));
                Assert.That(physicsDump["active_move_count"]?.ToObject<int>(), Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(spawnRoot.gameObject);
                Object.DestroyImmediate(prefab);
            }
        }

        private static BridgeEnvelope SpawnEnvelope()
        {
            return new BridgeEnvelope
            {
                MessageId = "spawn-1",
                SessionId = "test",
                Sequence = 1,
                SentAtMs = 1,
                Type = "agent.spawn",
                Payload = new JObject
                {
                    ["agent"] = new JObject
                    {
                        ["agent_id"] = "persona_001",
                        ["display_name"] = "Persona 001",
                        ["group_id"] = "group-a",
                        ["position"] = new JObject
                        {
                            ["x"] = 0f,
                            ["y"] = 0f,
                            ["z"] = 0f
                        },
                        ["visible"] = true
                    }
                }
            };
        }

        private static BridgeEnvelope MoveEnvelope()
        {
            return new BridgeEnvelope
            {
                MessageId = "move-1",
                SessionId = "test",
                Sequence = 2,
                SentAtMs = 2,
                Type = "agent.move",
                Payload = new JObject
                {
                    ["agent_id"] = "persona_001",
                    ["target_position"] = new JObject
                    {
                        ["x"] = 2.1f,
                        ["y"] = 0f,
                        ["z"] = -1.5f
                    },
                    ["speed_mps"] = 0.45f,
                    ["intent"] = "walk"
                }
            };
        }
    }
}
