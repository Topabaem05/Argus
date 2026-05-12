using ArgusUnity.Bridge;
using ArgusUnity.Scene;
using NUnit.Framework;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class SceneOrchestratorTests
    {
        [Test]
        public void RoutesRequiredBridgeMessageTypes()
        {
            var orchestrator = new SimulationSceneOrchestrator();
            var routedCount = 0;
            orchestrator.EnvironmentLoad += _ => routedCount++;
            orchestrator.SimulationSummary += _ => routedCount++;
            orchestrator.UiStatus += _ => routedCount++;
            orchestrator.AgentSpawn += _ => routedCount++;
            orchestrator.AgentMove += _ => routedCount++;
            orchestrator.AgentBehavior += _ => routedCount++;
            orchestrator.AgentAnimation += _ => routedCount++;
            orchestrator.AgentDialogue += _ => routedCount++;
            orchestrator.AgentEmotion += _ => routedCount++;
            orchestrator.GroupUpdate += _ => routedCount++;
            orchestrator.ConflictUpdate += _ => routedCount++;
            orchestrator.PhysicsResult += _ => routedCount++;

            Assert.That(orchestrator.TryHandle(Envelope("agent.spawn"), out var issue), Is.True);
            Assert.That(issue, Is.Null);
            Assert.That(orchestrator.TryHandle(Envelope("environment.load"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("simulation.summary"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("ui.status"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("agent.move"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("agent.behavior"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("agent.animation"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("agent.dialogue"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("agent.emotion"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("group.update"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("conflict.update"), out issue), Is.True);
            Assert.That(orchestrator.TryHandle(Envelope("physics.result"), out issue), Is.True);
            Assert.That(routedCount, Is.EqualTo(12));
        }

        [Test]
        public void KnownButUnroutedTypeReturnsWarning()
        {
            var orchestrator = new SimulationSceneOrchestrator();

            var handled = orchestrator.TryHandle(Envelope("bridge.ready"), out var issue);

            Assert.That(handled, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.Severity, Is.EqualTo("warning"));
        }

        [Test]
        public void UnknownTypeReturnsErrorWithoutThrowing()
        {
            var orchestrator = new SimulationSceneOrchestrator();

            var handled = orchestrator.TryHandle(Envelope("future.message"), out var issue);

            Assert.That(handled, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.Severity, Is.EqualTo("error"));
            Assert.That(issue.Details["message_type"].ToObject<string>(), Is.EqualTo("future.message"));
        }

        [Test]
        public void HandlerExceptionReturnsStructuredError()
        {
            var orchestrator = new SimulationSceneOrchestrator();
            orchestrator.AgentSpawn += _ => throw new System.InvalidOperationException("handler failed");

            var handled = orchestrator.TryHandle(Envelope("agent.spawn"), out var issue);

            Assert.That(handled, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.Message, Does.Contain("Scene handler failed"));
            Assert.That(issue.Details["message_type"].ToObject<string>(), Is.EqualTo("agent.spawn"));
            Assert.That(issue.Details["exception"].ToObject<string>(), Is.EqualTo("handler failed"));
        }

        private static BridgeEnvelope Envelope(string messageType)
        {
            return new BridgeEnvelope
            {
                SchemaVersion = "1.0.0",
                MessageId = $"msg-{messageType}",
                SessionId = "session-1",
                Sequence = 1,
                SentAtMs = 1000,
                Type = messageType
            };
        }
    }
}
