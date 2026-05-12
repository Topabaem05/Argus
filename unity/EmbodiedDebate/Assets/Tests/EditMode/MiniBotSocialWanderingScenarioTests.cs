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
                fixture.Scenario.ApplyAtTime(2.6f);

                Assert.That(fixture.Scenario.CountAgentsInPhase(MiniBotSocialPhase.Chat), Is.EqualTo(2));
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var first), Is.True);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A02", out var second), Is.True);
                Assert.That(first.Phase, Is.EqualTo(MiniBotSocialPhase.Chat));
                Assert.That(second.Phase, Is.EqualTo(MiniBotSocialPhase.Chat));
                Assert.That(first.PartnerId, Is.EqualTo("A02"));
                Assert.That(second.PartnerId, Is.EqualTo("A01"));
                Assert.That(first.ActionLabel, Is.EqualTo("agree"));
                Assert.That(fixture.Scenario.CurrentChatText, Does.Contain("agree"));
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
                fixture.Scenario.ApplyAtTime(2.6f);

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

                Assert.That(Vector3.Distance(firstStart, fixture.First.position), Is.GreaterThan(0.35f));
                Assert.That(Vector3.Distance(secondStart, fixture.Second.position), Is.GreaterThan(0.35f));
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var first), Is.True);
                Assert.That(first.Phase, Is.EqualTo(MiniBotSocialPhase.Approach));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void CaptureWindowContainsMultipleSocialMeetups()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Scenario.ApplyAtTime(2.6f);
                Assert.That(fixture.Scenario.TryGetCurrentSnapshot("A01", out var firstMeeting), Is.True);

                fixture.Scenario.ApplyAtTime(4.9f);
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

                pause.onClick.Invoke();
                Assert.That(fixture.Scenario.IsPaused, Is.True);

                pause.onClick.Invoke();
                Assert.That(fixture.Scenario.IsPaused, Is.False);

                chat.onClick.Invoke();
                fixture.Scenario.ApplyAtTime(1.1f);
                ui.Refresh();

                Assert.That(fixture.Scenario.CurrentChatText, Does.Contain("chat"));
                Assert.That(chatText.text, Does.Contain("chat"));
            }
            finally
            {
                fixture.Destroy();
                Object.DestroyImmediate(uiRoot);
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
                    new Vector3(-3.6f, 0f, -3.2f),
                    new Vector3(-2.4f, 0f, -1.2f),
                    new Vector3(-4.2f, 0f, 0.7f),
                },
                0.9f,
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
                0.9f,
                0.2f,
                secondMarker);
            scenario.RegisterSocialAgent(
                third,
                "B01",
                "energetic",
                new[]
                {
                    new Vector3(-1.4f, 0f, -4.0f),
                    new Vector3(1.2f, 0f, -3.5f),
                    new Vector3(0.0f, 0f, -1.2f),
                    new Vector3(-1.8f, 0f, -2.4f),
                },
                0.9f,
                0.4f,
                thirdMarker);
            scenario.RegisterSocialAgent(
                fourth,
                "B02",
                "skeptical",
                new[]
                {
                    new Vector3(1.5f, 0f, 4.0f),
                    new Vector3(-1.0f, 0f, 3.4f),
                    new Vector3(0.3f, 0f, 1.4f),
                    new Vector3(1.9f, 0f, 2.4f),
                },
                0.9f,
                0.6f,
                fourthMarker);

            scenario.RegisterInteraction(
                "A01",
                "A02",
                new Vector3(-2.4f, 0f, 0f),
                Vector3.right,
                0.7f,
                1.35f,
                1.75f,
                0.8f,
                1.2f,
                new Vector3(-4.1f, 0f, 0.8f),
                new Vector3(-3.5f, 0f, 2.8f),
                "agree");
            scenario.RegisterInteraction(
                "B01",
                "B02",
                new Vector3(0.1f, 0f, 0f),
                Vector3.forward,
                3.05f,
                1.35f,
                1.6f,
                0.75f,
                1.2f,
                new Vector3(-1.8f, 0f, -3.0f),
                new Vector3(1.8f, 0f, 3.0f),
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
