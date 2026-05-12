using ArgusUnity.Bridge;
using ArgusUnity.Motion;
using ArgusUnity.Runtime;
using ArgusUnity.Scene;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ArgusUnity.Tests.EditMode.Motion
{
    public sealed class MiniBotMotionRuntimeTests
    {
        [Test]
        public void PersonaMapperBuildsNaturalWalkIntentFromBehaviorPayload()
        {
            var mapperObject = new GameObject("mapper");
            try
            {
                var mapper = mapperObject.AddComponent<PersonaMotionMapper>();
                var intent = mapper.BuildFromBehaviorPayload(Vector3.zero, new JObject
                {
                    ["intent"] = "ask",
                    ["locomotion"] = "walk",
                    ["urgency"] = 0.5f,
                    ["emotion"] = new JObject { ["label"] = "curious" },
                    ["target_position"] = new JObject
                    {
                        ["x"] = 2f,
                        ["y"] = 1f,
                        ["z"] = -1f
                    }
                });

                Assert.That(intent.HasMoveTarget, Is.True);
                Assert.That(intent.MoveTarget.x, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(intent.MoveTarget.z, Is.EqualTo(-1f).Within(0.0001f));
                Assert.That(intent.Emotion, Is.EqualTo(MotionEmotion.Thinking));
                Assert.That(intent.DesiredSpeedMetersPerSecond, Is.InRange(0.45f, 0.65f));
            }
            finally
            {
                Object.DestroyImmediate(mapperObject);
            }
        }

        [Test]
        public void SmoothMotorAcceleratesAndDoesNotTeleportToFarTarget()
        {
            var bot = new GameObject("motion-bot");
            try
            {
                var rb = bot.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                var motor = bot.AddComponent<SmoothRigidbodyMotor>();
                motor.SetIntent(new MotionIntent(
                    true,
                    new Vector3(5f, 0f, 0f),
                    null,
                    0.6f,
                    0.1f,
                    MotionEmotion.Neutral,
                    MotionGesture.None,
                    MotionAction.None,
                    true,
                    0.5f));

                motor.Tick(0.1f);
                Assert.That(motor.LastActualDisplacement, Is.GreaterThan(0f));
                Assert.That(motor.LastActualDisplacement, Is.LessThan(0.5f));
                Assert.That(motor.CurrentVelocity.magnitude, Is.LessThanOrEqualTo(0.6f));
            }
            finally
            {
                Object.DestroyImmediate(bot);
            }
        }

        [Test]
        public void MotionLibraryContainsMinimumViableMixamoSlots()
        {
            var slots = MinibotMotionLibrary.DefaultSlots();

            Assert.That(slots.Length, Is.GreaterThanOrEqualTo(20));
            Assert.That(HasSlot(slots, "WalkForward", "Walking-3"), Is.True);
            Assert.That(HasSlot(slots, "Talk", "Talking"), Is.True);
            Assert.That(HasSlot(slots, "Fall", "Falling Flat Impact"), Is.True);
            Assert.That(HasSlot(slots, "GetUp", "Getting Up"), Is.True);
        }

        [Test]
        public void BridgeMotionAdapterMapsPhysicsResultToReactionIntent()
        {
            var root = new GameObject("root");
            var spawnRoot = new GameObject("spawn-root").transform;
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prefab.AddComponent<Rigidbody>();
            prefab.AddComponent<Animator>();
            try
            {
                var orchestrator = new SimulationSceneOrchestrator();
                var spawn = root.AddComponent<AgentSpawnHandler>();
                var adapter = root.AddComponent<BridgeMotionAdapter>();
                spawn.Initialize(orchestrator, prefab, spawnRoot);
                adapter.Initialize(orchestrator, spawn);

                Assert.That(orchestrator.TryHandle(SpawnEnvelope(), out var issue), Is.True, issue?.Message);
                Assert.That(orchestrator.TryHandle(PhysicsEnvelope(), out issue), Is.True, issue?.Message);
                Assert.That(spawn.TryGetAgentTransform("agent-1", out var transform), Is.True);
                var controller = transform.GetComponent<MinibotMotionController>();

                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.CurrentIntent.Action, Is.EqualTo(MotionAction.Fall));
                Assert.That(controller.State, Is.EqualTo(MinibotMotionState.Falling));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(spawnRoot.gameObject);
                Object.DestroyImmediate(prefab);
            }
        }

        private static bool HasSlot(MotionSlot[] slots, string id, string clipName)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Id == id && slots[i].ClipName == clipName)
                {
                    return true;
                }
            }

            return false;
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
                        ["agent_id"] = "agent-1",
                        ["display_name"] = "Agent 1",
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

        private static BridgeEnvelope PhysicsEnvelope()
        {
            return new BridgeEnvelope
            {
                MessageId = "physics-1",
                SessionId = "test",
                Sequence = 2,
                SentAtMs = 2,
                Type = "physics.result",
                Payload = new JObject
                {
                    ["agent_id"] = "agent-1",
                    ["result_kind"] = "impact",
                    ["intensity"] = 0.95f
                }
            };
        }
    }
}
