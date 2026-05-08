using ArgusUnity.UI;
using NUnit.Framework;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class AgentInspectorFormatterTests
    {
        [Test]
        public void DropsSensitiveNestedKeysBeyondAllowlist()
        {
            var json = @"{
""agent_id"": ""a"",
""memory_seeds"": [""PRIVATE""],
""behavior_rules"": [""RULE""],
""background"": ""ok"",
""prompt"": {""chain"": []}
}";

            Assert.That(
                AgentInspectorFormatter.TryFilterPublicSubset(json, out var text),
                Is.True);

            Assert.That(text, Does.Not.Contain("memory_seeds"));
            Assert.That(text, Does.Not.Contain("PRIVATE"));
            Assert.That(text, Does.Not.Contain("behavior_rules"));
            Assert.That(text, Does.Contain("background"));
        }
    }
}
