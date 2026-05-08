using ArgusUnity.Robots;
using NUnit.Framework;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class AnimationStateMapperTests
    {
        [TestCase("idle", "IdleClip")]
        [TestCase("walk", "WalkClip")]
        [TestCase("run", "RunClip")]
        [TestCase("speak", "SpeakClip")]
        [TestCase("argue", "ArgueClip")]
        [TestCase("push", "PushClip")]
        [TestCase("block", "BlockClip")]
        [TestCase("stumble", "StumbleClip")]
        [TestCase("fall", "FallClip")]
        [TestCase("recover", "RecoverClip")]
        public void KnownActionsMapToConfiguredClips(string action, string expectedClip)
        {
            var mapper = new AnimationStateMapper(Config());

            var result = mapper.Resolve(action);

            Assert.That(result.ClipName, Is.EqualTo(expectedClip));
            Assert.That(result.UsedFallback, Is.False);
            Assert.That(result.Warning, Is.Null);
        }

        [Test]
        public void UnknownActionFallsBackToIdleWarning()
        {
            var mapper = new AnimationStateMapper(Config());

            var result = mapper.Resolve("dance");

            Assert.That(result.ClipName, Is.EqualTo("IdleClip"));
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.Warning, Does.Contain("dance"));
        }

        [Test]
        public void EmptyActionFallsBackToIdleWarning()
        {
            var mapper = new AnimationStateMapper(Config());

            var result = mapper.Resolve(" ");

            Assert.That(result.ClipName, Is.EqualTo("IdleClip"));
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.Warning, Does.Contain("empty"));
        }

        private static AnimationClipMap Config()
        {
            return new AnimationClipMap
            {
                Idle = "IdleClip",
                Walk = "WalkClip",
                Run = "RunClip",
                Speak = "SpeakClip",
                Argue = "ArgueClip",
                Push = "PushClip",
                Block = "BlockClip",
                Stumble = "StumbleClip",
                Fall = "FallClip",
                Recover = "RecoverClip"
            };
        }
    }
}
