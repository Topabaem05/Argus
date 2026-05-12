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
        public void PersonaMapperDoesNotTurnIdleOrMissingTargetPayloadIntoLocomotion()
        {
            var mapperObject = new GameObject("mapper");
            try
            {
                var mapper = mapperObject.AddComponent<PersonaMotionMapper>();
                var missingTarget = mapper.BuildFromBehaviorPayload(Vector3.zero, new JObject
                {
                    ["intent"] = "observe",
                    ["locomotion"] = "walk",
                    ["emotion"] = "neutral"
                });
                var idle = mapper.BuildFromBehaviorPayload(Vector3.zero, new JObject
                {
                    ["intent"] = "observe",
                    ["locomotion"] = "idle",
                    ["target_position"] = new JObject
                    {
                        ["x"] = 3f,
                        ["y"] = 0f,
                        ["z"] = 2f
                    }
                });

                Assert.That(missingTarget.HasMoveTarget, Is.False);
                Assert.That(missingTarget.Type, Is.EqualTo(MotionIntentType.LookAround));
                Assert.That(idle.HasMoveTarget, Is.False);
                Assert.That(idle.DesiredSpeedMetersPerSecond, Is.EqualTo(0f));
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

            Assert.That(slots.Length, Is.GreaterThanOrEqualTo(35));
            Assert.That(HasSlot(slots, MotionClipId.Walking3.ToString(), "Walking-3"), Is.True);
            Assert.That(HasSlot(slots, MotionClipId.Talking.ToString(), "Talking"), Is.True);
            Assert.That(HasSlot(slots, MotionClipId.FallingFlatImpact.ToString(), "Falling Flat Impact"), Is.True);
            Assert.That(HasSlot(slots, MotionClipId.GettingUp.ToString(), "Getting Up"), Is.True);
        }

        [Test]
        public void MotionCatalogCoversRequiredCategories()
        {
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.Idle).Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.Locomotion).Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.Turn).Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.Talk).Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.Emotion).Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.Interaction).Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.Recovery).Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.HitReaction).Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(MotionCatalog.GetByCategory(MotionCategory.Celebration).Length, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void MotionSelectionIsSeededAndAvoidsImmediateRepeatWhenAlternativesExist()
        {
            var profileA = PersonaMotionProfile.FromAgentId("agent-a", 1234);
            var profileB = PersonaMotionProfile.FromAgentId("agent-a", 1234);
            var policyA = new MotionSelectionPolicy();
            var policyB = new MotionSelectionPolicy();
            policyA.Initialize(profileA.Seed);
            policyB.Initialize(profileB.Seed);

            var firstA = policyA.SelectClip(MotionIntentType.Idle, profileA, 0f);
            var firstB = policyB.SelectClip(MotionIntentType.Idle, profileB, 0f);
            var secondA = policyA.SelectClip(MotionIntentType.Idle, profileA, 0.1f, firstA);

            Assert.That(firstB, Is.EqualTo(firstA));
            Assert.That(secondA, Is.Not.EqualTo(firstA));
        }

        [Test]
        public void PersonaProfileBiasesExpectedMotionFamilies()
        {
            var aggressive = PersonaMotionProfile.FromAgentId("aggressive", 99);
            aggressive.Aggression = 1f;
            aggressive.Friendliness = 0f;
            var friendly = PersonaMotionProfile.FromAgentId("friendly", 99);
            friendly.Friendliness = 1f;
            friendly.Aggression = 0f;

            MotionCatalog.TryGet(MotionClipId.Yelling, out var yelling);
            MotionCatalog.TryGet(MotionClipId.Talking, out var talking);

            var policy = new MotionSelectionPolicy();
            policy.Initialize(9);

            Assert.That(
                policy.ScoreCandidate(yelling, aggressive, 0f, MotionClipId.None),
                Is.GreaterThan(policy.ScoreCandidate(yelling, friendly, 0f, MotionClipId.None)));
            Assert.That(
                policy.ScoreCandidate(talking, friendly, 0f, MotionClipId.None),
                Is.GreaterThan(policy.ScoreCandidate(talking, aggressive, 0f, MotionClipId.None)));
        }

        [Test]
        public void StuckRecoveryBuildsRetreatIntentAndClip()
        {
            var bot = new GameObject("recovery-bot");
            try
            {
                var recovery = bot.AddComponent<MinibotStuckRecovery>();
                var profile = PersonaMotionProfile.FromAgentId("cautious", 42);
                profile.Anxiety = 0f;
                var intent = recovery.BuildRecoveryIntent(
                    Vector3.zero,
                    Quaternion.identity,
                    profile,
                    10f);

                Assert.That(intent.Type, Is.EqualTo(MotionIntentType.StepBackward));
                Assert.That(intent.Action, Is.EqualTo(MotionAction.StepBackward));
                Assert.That(intent.RequestedClip, Is.EqualTo(MotionClipId.StepBackward));
                Assert.That(intent.HasMoveTarget, Is.True);
                Assert.That(intent.MoveTarget.z, Is.LessThan(0f));
                Assert.That(recovery.CanRecover(10.2f), Is.False);
                Assert.That(recovery.CanRecover(11.2f), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(bot);
            }
        }

        [Test]
        public void MotionControllerUpdatesDebugStateWithSelectedClips()
        {
            var bot = new GameObject("debug-bot");
            try
            {
                bot.AddComponent<Rigidbody>().isKinematic = true;
                var controller = bot.AddComponent<MinibotMotionController>();
                controller.Initialize("agent-debug", 11);
                controller.ApplyIntent(new MotionIntent(
                    MotionIntentType.Talk,
                    false,
                    Vector3.zero,
                    null,
                    0f,
                    0.12f,
                    MotionEmotion.Excited,
                    MotionGesture.Talk,
                    MotionAction.None,
                    true,
                    0.4f));

                Assert.That(controller.DebugState.CurrentIntent, Is.EqualTo(MotionIntentType.Talk));
                Assert.That(controller.DebugState.SelectedBaseClip, Is.Not.EqualTo(MotionClipId.None));
                Assert.That(controller.DebugState.SelectedOverlayClip, Is.Not.EqualTo(MotionClipId.None));
                Assert.That(controller.DebugState.SelectedEmotionClip, Is.Not.EqualTo(MotionClipId.None));
                Assert.That(controller.DebugState.CurrentBaseClip, Is.EqualTo(MotionClipId.None));
                Assert.That(controller.DebugState.CurrentOverlayClip, Is.EqualTo(MotionClipId.None));
                Assert.That(controller.DebugState.LastFiveUsedClips.Length, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(bot);
            }
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
