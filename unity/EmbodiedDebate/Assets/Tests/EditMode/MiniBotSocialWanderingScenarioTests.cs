using ArgusUnity.Motion;
using ArgusUnity.Runtime;
using ArgusUnity.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class MiniBotSocialWanderingScenarioTests
    {
        [Test]
        public void PairEntersChatWithinEightSecondCapture()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(5.4f);

                Assert.That(fixture.Scenario.CountAgentsInPhase(MiniBotSocialPhase.Chat), Is.EqualTo(2));
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var first), Is.True);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A02", out var second), Is.True);
                Assert.That(first.Phase, Is.EqualTo(MiniBotSocialPhase.Chat));
                Assert.That(second.Phase, Is.EqualTo(MiniBotSocialPhase.Chat));
                Assert.That(first.PartnerId, Is.EqualTo("A02"));
                Assert.That(second.PartnerId, Is.EqualTo("A01"));
                Assert.That(first.ActionLabel, Is.EqualTo("agree"));
                Assert.That(fixture.Scenario.CurrentChatText, Does.Contain("agree"));
                Assert.That(fixture.Scenario.CurrentActionMappingText, Does.Contain("talk_idle"));
                Assert.That(fixture.First.GetComponent<MinibotBlackboard>().MappedUnityAction, Does.Contain("talk_idle"));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void ChatPairStopsCloseTogetherAndFacesEachOther()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(5.4f);

                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var first), Is.True);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A02", out var second), Is.True);
                Assert.That(Vector3.Distance(first.Position, second.Position), Is.LessThan(1.15f));

                var firstToSecond = second.Position - first.Position;
                firstToSecond.y = 0f;
                var secondToFirst = first.Position - second.Position;
                secondToFirst.y = 0f;

                Assert.That(Vector3.Dot(first.FacingDirection.normalized, firstToSecond.normalized), Is.GreaterThan(0.92f));
                Assert.That(Vector3.Dot(second.FacingDirection.normalized, secondToFirst.normalized), Is.GreaterThan(0.92f));
                Assert.That(fixture.FirstMarker.activeSelf, Is.True);
                Assert.That(fixture.SecondMarker.activeSelf, Is.True);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void AgentsTranslateDuringWanderAndApproach()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(0f);
                var firstStart = fixture.First.position;
                var secondStart = fixture.Second.position;

                fixture.Scenario.ApplyAtTime(1.4f);

                Assert.That(Vector3.Distance(firstStart, fixture.First.position), Is.GreaterThan(0.08f));
                Assert.That(Vector3.Distance(secondStart, fixture.Second.position), Is.GreaterThan(0.08f));
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var first), Is.True);
                Assert.That(first.Phase, Is.EqualTo(MiniBotSocialPhase.Approach));
                Assert.That(fixture.First.GetComponent<MinibotMovementController>(), Is.Not.Null);
                Assert.That(fixture.First.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That((fixture.First.GetComponent<Rigidbody>().constraints & RigidbodyConstraints.FreezeRotationX) != 0, Is.True);
                Assert.That((fixture.First.GetComponent<Rigidbody>().constraints & RigidbodyConstraints.FreezeRotationZ) != 0, Is.True);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void RunAroundPathSelectsDiverseMixamoClipsInsteadOfLegacyWalkSampler()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(0f);
                fixture.Scenario.ApplyAtTime(0.3f);

                Assert.That(fixture.First.GetComponent<MiniBotWalkAnimator>(), Is.Null);
                Assert.That(fixture.First.GetComponent<MinibotAnimatorDriver>(), Is.Not.Null);
                Assert.That(fixture.Scenario.TryGetMotionDebugState("A01", out var walkingDebug), Is.True);
                Assert.That(walkingDebug.SelectedBaseClip, Is.EqualTo(MotionClipId.Walking3));
                Assert.That(walkingDebug.SelectedOverlayClip, Is.EqualTo(MotionClipId.None));
                Assert.That(walkingDebug.SelectedEmotionClip, Is.EqualTo(MotionClipId.None));
                Assert.That(walkingDebug.CurrentIntent, Is.EqualTo(MotionIntentType.WalkForward));

            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void ApproachWalkFacesMovementDirectionBeforeConversationGaze()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(1.1f);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var previous), Is.True);

                fixture.Scenario.ApplyAtTime(1.4f);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var current), Is.True);
                Assert.That(current.Phase, Is.EqualTo(MiniBotSocialPhase.Approach));

                var movement = current.Position - previous.Position;
                movement.y = 0f;

                Assert.That(movement.magnitude, Is.GreaterThan(0.01f));
                Assert.That(Vector3.Dot(current.FacingDirection.normalized, movement.normalized), Is.GreaterThan(0.92f));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void ObjectTaskMovesPropAndMapsToPushAction()
        {
            var fixture = CreateFixture();
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var boxBody = box.AddComponent<Rigidbody>();
            try
            {
                box.name = "Test Supply Box";
                boxBody.mass = 6f;
                boxBody.isKinematic = false;
                boxBody.useGravity = true;
                fixture.Scenario.RegisterObjectTask(
                    "B01",
                    box.transform,
                    new Vector3(-0.2f, 0.3f, 2.2f),
                    new Vector3(0.8f, 0.3f, 2.2f),
                    24f,
                    3f,
                    4f,
                    2f,
                    "push");

                fixture.Scenario.ApplyAtTime(24f);
                fixture.Scenario.ApplyAtTime(27.5f);
                fixture.Scenario.ApplyAtTime(29.5f);

                Assert.That(box.transform.position.x, Is.GreaterThan(-0.2f));
                Assert.That(boxBody.mass, Is.EqualTo(6f).Within(0.0001f));
                Assert.That(boxBody.isKinematic, Is.False);
                Assert.That(box.GetComponent<BoxCollider>(), Is.Not.Null);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("B01", out var snapshot), Is.True);
                Assert.That(snapshot.ActionLabel, Is.EqualTo("push"));
                Assert.That(fixture.Scenario.TryGetMotionDebugState("B01", out var motionDebug), Is.True);
                Assert.That(motionDebug.CurrentIntent, Is.EqualTo(MotionIntentType.Push));
                Assert.That(motionDebug.SelectedBaseClip, Is.EqualTo(MotionClipId.Push));
                Assert.That(fixture.Third.GetComponent<MinibotBlackboard>().MappedUnityAction, Does.Contain("push"));
            }
            finally
            {
                Object.DestroyImmediate(box);
                fixture.Destroy();
            }
        }

        [Test]
        public void CaptureWindowContainsMultipleSocialMeetups()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(5.4f);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var firstMeeting), Is.True);

                fixture.Scenario.ApplyAtTime(6.2f);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("B01", out var secondMeeting), Is.True);

                Assert.That(firstMeeting.Phase, Is.EqualTo(MiniBotSocialPhase.Chat));
                Assert.That(secondMeeting.Phase, Is.EqualTo(MiniBotSocialPhase.Chat));
                Assert.That(firstMeeting.ActionLabel, Is.EqualTo("agree"));
                Assert.That(secondMeeting.ActionLabel, Is.EqualTo("debate"));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void AgentsDoNotOnlyRotateInPlaceDuringCapture()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(0f);
                var firstStart = fixture.First.position;
                var thirdStart = fixture.Third.position;

                fixture.Scenario.ApplyAtTime(6.9f);

                Assert.That(Vector3.Distance(firstStart, fixture.First.position), Is.GreaterThan(0.5f));
                Assert.That(Vector3.Distance(thirdStart, fixture.Third.position), Is.GreaterThan(0.5f));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void KinematicMovementAppliesActualSpeedCap()
        {
            var bot = new GameObject("speed cap bot");
            try
            {
                bot.AddComponent<Rigidbody>();
                bot.AddComponent<CapsuleCollider>();
                var movement = bot.AddComponent<MinibotMovementController>();

                movement.ApplyKinematicPose(Vector3.zero, Vector3.forward, 0f, 0.55f);
                movement.ApplyKinematicPose(new Vector3(3f, 0f, 0f), Vector3.forward, 1f, 0.55f);

                Assert.That(bot.transform.position.x, Is.EqualTo(0.55f).Within(0.001f));
                Assert.That(movement.LastPlanarSpeed, Is.EqualTo(0.55f).Within(0.001f));
                Assert.That(movement.LastSpeedLimitExceeded, Is.True);
                Assert.That(movement.LastActualStepMeters, Is.LessThanOrEqualTo(movement.LastAllowedStepMeters + 0.001f));
            }
            finally
            {
                Object.DestroyImmediate(bot);
            }
        }

        [Test]
        public void KinematicMovementFacesActualTravelDirectionWhenRequestedFacingIsSideways()
        {
            var bot = new GameObject("heading guard bot");
            try
            {
                bot.AddComponent<Rigidbody>();
                bot.AddComponent<CapsuleCollider>();
                var movement = bot.AddComponent<MinibotMovementController>();

                movement.ApplyKinematicPose(Vector3.zero, Vector3.forward, 0f, 0.55f);
                movement.ApplyKinematicPose(new Vector3(3f, 0f, 0f), Vector3.forward, 1f, 0.55f);

                var expectedTravel = Vector3.right;
                Assert.That(Vector3.Dot(bot.transform.forward.normalized, expectedTravel), Is.GreaterThan(0.95f));
                Assert.That(Vector3.Dot(movement.LastAppliedFacingDirection.normalized, expectedTravel), Is.GreaterThan(0.95f));
                Assert.That(movement.LastFacingAlignedToMovement, Is.True);
                Assert.That(movement.LastHeadingAlignmentDegrees, Is.LessThan(5f));
            }
            finally
            {
                Object.DestroyImmediate(bot);
            }
        }

        [Test]
        public void InteractionDurationsUseWalkCadence()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(2.6f);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var approaching), Is.True);
                Assert.That(approaching.Phase, Is.EqualTo(MiniBotSocialPhase.Approach));

                fixture.Scenario.ApplyAtTime(5.4f);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var chatting), Is.True);
                Assert.That(chatting.Phase, Is.EqualTo(MiniBotSocialPhase.Chat));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void ScreenUiButtonsDriveScenarioAndChatBox()
        {
            var fixture = CreateFixture();
            var uiRoot = new GameObject("ui root");
            try
            {
                var ui = uiRoot.AddComponent<MiniBotSocialUiController>();
                ui.Initialize(fixture.Scenario);

                var pause = GameObject.Find("Pause Button").GetComponent<Button>();
                var chat = GameObject.Find("Chat Button").GetComponent<Button>();
                var chatText = GameObject.Find("Chat Text").GetComponent<Text>();
                var actionMappingText = GameObject.Find("Action Mapping Text").GetComponent<Text>();

                pause.onClick.Invoke();
                Assert.That(fixture.Scenario.IsPaused, Is.True);

                pause.onClick.Invoke();
                Assert.That(fixture.Scenario.IsPaused, Is.False);

                chat.onClick.Invoke();
                fixture.Scenario.ApplyAtTime(1.1f);
                ui.Refresh();

                Assert.That(fixture.Scenario.CurrentChatText, Does.Contain("approaches"));
                Assert.That(chatText.text, Does.Contain("walks toward"));
                Assert.That(actionMappingText.text, Does.Contain("Movement:"));
            }
            finally
            {
                fixture.Destroy();
                Object.DestroyImmediate(uiRoot);
            }
        }

        [Test]
        public void EmbodimentShowsSpeechAndEmotionDuringChat()
        {
            var first = new GameObject("A01");
            var second = new GameObject("A02");
            try
            {
                second.transform.position = Vector3.right;
                var firstBlackboard = first.AddComponent<MinibotBlackboard>();
                second.AddComponent<MinibotBlackboard>().ApplySocialState(
                    "A02",
                    "curious",
                    new MiniBotSocialSnapshot("A02", MiniBotSocialPhase.Chat, second.transform.position, Vector3.left, "A01", "ask"),
                    new MinibotUnityAction("hold_position", "talk_idle", "curious", "question_tilt", "chat with A01"),
                    second.transform.position,
                    0f,
                    0f);
                firstBlackboard.ApplySocialState(
                    "A01",
                    "curious",
                    new MiniBotSocialSnapshot("A01", MiniBotSocialPhase.Chat, first.transform.position, Vector3.right, "A02", "ask"),
                    new MinibotUnityAction("hold_position", "talk_idle", "curious", "question_tilt", "chat with A02"),
                    first.transform.position,
                    0f,
                    0f);

                var expression = new GameObject("expression").transform;
                expression.SetParent(first.transform);
                var nameplate = new GameObject("name").AddComponent<TextMesh>();
                var speech = new GameObject("speech").AddComponent<TextMesh>();
                var panel = new GameObject("speech panel");
                var icon = new GameObject("icon").AddComponent<TextMesh>();
                var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var leftArm = new GameObject("left arm").transform;
                var rightArm = new GameObject("right arm").transform;
                var embodiment = first.AddComponent<MinibotEmbodimentController>();

                embodiment.Initialize(
                    "A01",
                    "friendly",
                    Color.blue,
                    expression,
                    nameplate,
                    speech,
                    panel,
                    icon,
                    face.GetComponent<Renderer>(),
                    leftArm,
                    rightArm,
                    null);
                embodiment.Refresh();

                Assert.That(nameplate.text, Does.Contain("A01"));
                Assert.That(speech.gameObject.activeSelf, Is.True);
                Assert.That(panel.activeSelf, Is.True);
                Assert.That(icon.text, Is.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void ConversationCameraFramesActivePair()
        {
            var cameraObject = new GameObject("camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            var first = new GameObject("A01");
            var second = new GameObject("A02");
            try
            {
                first.transform.position = new Vector3(-1f, 0f, 0f);
                second.transform.position = new Vector3(1f, 0f, 0f);
                first.AddComponent<MinibotBlackboard>().ApplySocialState(
                    "A01",
                    "friendly",
                    new MiniBotSocialSnapshot("A01", MiniBotSocialPhase.Chat, first.transform.position, Vector3.right, "A02", "agree"),
                    new MinibotUnityAction("hold_position", "talk_idle", "friendly", "nod", "chat with A02"),
                    first.transform.position,
                    0f,
                    0f);
                second.AddComponent<MinibotBlackboard>().ApplySocialState(
                    "A02",
                    "curious",
                    new MiniBotSocialSnapshot("A02", MiniBotSocialPhase.Chat, second.transform.position, Vector3.left, "A01", "ask"),
                    new MinibotUnityAction("hold_position", "talk_idle", "curious", "question_tilt", "chat with A01"),
                    second.transform.position,
                    0f,
                    0f);

                var rig = cameraObject.AddComponent<MinibotConversationCameraRig>();
                rig.Initialize(camera);
                rig.RefreshImmediate();
                rig.RefreshImmediate();
                rig.RefreshImmediate();

                Assert.That(camera.fieldOfView, Is.LessThan(60f));
                Assert.That(Vector3.Dot(cameraObject.transform.forward, (Vector3.up * 0.9f - cameraObject.transform.position).normalized), Is.GreaterThan(0.7f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        private static Fixture CreateFixture()
        {
            var root = new GameObject("social wandering fixture");
            var scenario = root.AddComponent<MiniBotRunAroundScenario>();
            var first = AddAgent(root, "A01", out var firstMarker);
            var second = AddAgent(root, "A02", out var secondMarker);
            var third = AddAgent(root, "B01", out var thirdMarker);
            var fourth = AddAgent(root, "B02", out var fourthMarker);

            scenario.RegisterSocialAgent(
                first,
                "A01",
                "friendly",
                new[]
                {
                    new Vector3(-4.5f, 0f, -2.2f),
                    new Vector3(-3.7f, 0f, -3.0f),
                    new Vector3(-2.8f, 0f, -1.2f),
                    new Vector3(-4.2f, 0f, -0.7f),
                },
                0.58f,
                0f,
                firstMarker);
            scenario.RegisterSocialAgent(
                second,
                "A02",
                "curious",
                new[]
                {
                    new Vector3(-4.6f, 0f, 2.4f),
                    new Vector3(-3.6f, 0f, 3.0f),
                    new Vector3(-2.4f, 0f, 1.2f),
                    new Vector3(-4.5f, 0f, 0.8f),
                },
                0.58f,
                0.2f,
                secondMarker);
            scenario.RegisterSocialAgent(
                third,
                "B01",
                "energetic",
                new[]
                {
                    new Vector3(-0.9f, 0f, -1.9f),
                    new Vector3(-2.0f, 0f, -3.6f),
                    new Vector3(1.2f, 0f, -3.4f),
                    new Vector3(-1.8f, 0f, -0.7f),
                },
                0.58f,
                0.4f,
                thirdMarker);
            scenario.RegisterSocialAgent(
                fourth,
                "B02",
                "skeptical",
                new[]
                {
                    new Vector3(0.9f, 0f, 1.9f),
                    new Vector3(2.0f, 0f, 3.6f),
                    new Vector3(-1.2f, 0f, 3.4f),
                    new Vector3(1.8f, 0f, 0.7f),
                },
                0.58f,
                0.6f,
                fourthMarker);

            scenario.RegisterInteraction(
                "A01",
                "A02",
                new Vector3(-4.5f, 0f, 0f),
                Vector3.right,
                0.7f,
                1.35f,
                1.75f,
                0.8f,
                1.2f,
                new Vector3(-4.2f, 0f, -0.8f),
                new Vector3(-4.1f, 0f, 0.8f),
                "agree");
            scenario.RegisterInteraction(
                "B01",
                "B02",
                new Vector3(0f, 0f, 0f),
                Vector3.forward,
                1.65f,
                1.35f,
                1.6f,
                0.75f,
                1.2f,
                new Vector3(-1.6f, 0f, -0.8f),
                new Vector3(1.6f, 0f, 0.8f),
                "debate");

            return new Fixture(root, scenario, first, second, third, firstMarker.gameObject, secondMarker.gameObject);
        }

        private static Transform AddAgent(GameObject root, string name, out Transform marker)
        {
            var agent = new GameObject(name);
            agent.transform.SetParent(root.transform);
            var markerObject = new GameObject(name + " marker");
            markerObject.transform.SetParent(agent.transform);
            markerObject.SetActive(false);
            marker = markerObject.transform;
            return agent.transform;
        }

        private readonly struct Fixture
        {
            public Fixture(
                GameObject root,
                MiniBotRunAroundScenario scenario,
                Transform first,
                Transform second,
                Transform third,
                GameObject firstMarker,
                GameObject secondMarker)
            {
                Root = root;
                Scenario = scenario;
                First = first;
                Second = second;
                Third = third;
                FirstMarker = firstMarker;
                SecondMarker = secondMarker;
            }

            public GameObject Root { get; }
            public MiniBotRunAroundScenario Scenario { get; }
            public Transform First { get; }
            public Transform Second { get; }
            public Transform Third { get; }
            public GameObject FirstMarker { get; }
            public GameObject SecondMarker { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(Root);
            }
        }
    }
}
