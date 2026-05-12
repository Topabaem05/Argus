using System.Collections;
using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using ArgusUnity.Scene;
using ArgusUnity.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArgusUnity.Tests.PlayMode
{
    public sealed class MiniBotBehaviorApplicationPlayModeTests
    {
        [UnityTest]
        public IEnumerator OpposedPersonaBehaviorMovesMiniBotAndShowsEmotion()
        {
            var root = new GameObject("BehaviorHarness");
            var parent = new GameObject("SpawnParent").transform;
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "MiniBotTestPrefab";

            try
            {
                var orchestrator = new SimulationSceneOrchestrator();
                var spawn = root.AddComponent<AgentSpawnHandler>();
                var move = root.AddComponent<AgentMoveHandler>();
                var behavior = root.AddComponent<MiniBotBehaviorHandler>();
                var emotion = root.AddComponent<EmotionIndicatorManager>();

                spawn.Initialize(orchestrator, prefab, parent);
                move.Initialize(orchestrator, spawn);
                behavior.Initialize(orchestrator, move);
                emotion.Initialize(orchestrator, spawn);

                Assert.That(orchestrator.TryHandle(SpawnEnvelope(), out var issue), Is.True);
                Assert.That(issue, Is.Null);
                Assert.That(spawn.TryGetAgentTransform("agent-p-007", out var agent), Is.True);
                var start = agent.position;

                Assert.That(orchestrator.TryHandle(OpposedBehaviorEnvelope(), out issue), Is.True);
                Assert.That(issue, Is.Null);

                for (var i = 0; i < 180; i++)
                {
                    yield return null;
                }

                Assert.That(agent.position.z, Is.GreaterThan(start.z + 0.1f));
                Assert.That(agent.Find("EmotionOrb_agent-p-007"), Is.Not.Null);
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(parent.gameObject);
                Object.Destroy(prefab);
            }
        }

        private static BridgeEnvelope SpawnEnvelope()
        {
            return ParseEnvelope(@"{
                ""schema_version"": ""1.0.0"",
                ""message_id"": ""spawn-agent-p-007"",
                ""session_id"": ""playmode-session"",
                ""sequence"": 1,
                ""sent_at_ms"": 1000,
                ""type"": ""agent.spawn"",
                ""payload"": {
                    ""agent"": {
                        ""agent_id"": ""agent-p-007"",
                        ""display_name"": ""20대 신입 개발자"",
                        ""position"": { ""x"": 0.0, ""y"": 0.0, ""z"": 0.0 },
                        ""facing"": 0.0,
                        ""emotion"": {
                            ""label"": ""neutral"",
                            ""intensity"": 0.0
                        },
                        ""current_action"": ""idle"",
                        ""visible"": true
                    },
                    ""spawn_reason"": ""scenario_start""
                }
            }");
        }

        private static BridgeEnvelope OpposedBehaviorEnvelope()
        {
            return ParseEnvelope(@"{
                ""schema_version"": ""1.0.0"",
                ""message_id"": ""behavior-agent-p-007"",
                ""session_id"": ""playmode-session"",
                ""sequence"": 2,
                ""sent_at_ms"": 1100,
                ""type"": ""agent.behavior"",
                ""payload"": {
                    ""agent_id"": ""agent-p-007"",
                    ""intent"": ""argue"",
                    ""target_position"": { ""x"": 0.0, ""y"": 0.0, ""z"": 1.6 },
                    ""locomotion"": ""walk"",
                    ""emotion"": {
                        ""label"": ""angry"",
                        ""intensity"": 0.75
                    },
                    ""animation_hint"": ""argue"",
                    ""urgency"": 0.75,
                    ""duration_ms"": 1800,
                    ""public_reason"": ""Persona stance=opposes, confidence=0.75."",
                    ""safety_tags"": [""non_graphic"", ""synthetic_persona""]
                }
            }");
        }

        private static BridgeEnvelope ParseEnvelope(string json)
        {
            Assert.That(BridgeEnvelope.TryParse(json, out var envelope, out var error), Is.True, error?.ToString());
            return envelope;
        }
    }
}
