using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class MiniBotAnimationLibraryTests
    {
        private const string ControllerPath =
            "Assets/Project/Resources/Animations/Controllers/MiniBotLocomotion.controller";
        private const string LocomotionBlendTreeName = "LocomotionBlendTree";
        private const float PositionTolerance = 0.0001f;

        private static readonly string[] ExpandedSourceNames =
        {
            "Running.fbx",
            "Slow Run.fbx",
            "Running To Turn.fbx",
            "Left Turn W_Briefcase.fbx",
            "Left Turn W_Briefcase-2.fbx",
            "Thinking.fbx",
            "Angry.fbx",
            "Male Laying Pose.fbx",
            "Standing Torch Light Torch.fbx"
        };

        private static readonly string[] ActionClipNames =
        {
            "Thinking",
            "Angry",
            "LayingPose",
            "StandingTorch"
        };

        private static readonly string[] QuarantinedLocomotionClipNames =
        {
            "SlowRun",
            "Run",
            "TurnLeft_Happy",
            "TurnRight_Happy"
        };

        [Test]
        public void PlanDocumentListsAllExpandedAnimationSourceFbx()
        {
            var planPath = RepoPath("docs/superpowers/plans/2026-05-07-minibot-expanded-animation-library.md");
            var plan = File.ReadAllText(planPath);

            foreach (var sourceName in ExpandedSourceNames)
            {
                StringAssert.Contains(sourceName, plan);
            }
        }

        [Test]
        public void LocomotionControllerKeepsSpeedAndTurnParameters()
        {
            var controller = LoadLocomotionController();

            AssertHasFloatParameter(controller, "Speed");
            AssertHasFloatParameter(controller, "Turn");
        }

        [Test]
        public void LocomotionBlendTreeUsesOnlyValidatedProductionMotions()
        {
            var blendTree = LoadLocomotionBlendTree();

            AssertBlendChild(blendTree, "Idle", 0f, 0f);
            AssertBlendChild(blendTree, "Walk_InPlace", 0f, 1f);
            AssertBlendChildTimeScale(blendTree, "Walk_InPlace", 1.38f);
            AssertBlendChild(blendTree, "TurnLeft_Briefcase", -1f, 1f);
            AssertBlendChild(blendTree, "TurnRight_Briefcase", 1f, 1f);
            AssertBlendChildCount(blendTree, 4);
        }

        [Test]
        public void QuarantinedClipsAreNotInsertedIntoLocomotionBlendTree()
        {
            var blendTree = LoadLocomotionBlendTree();
            var childMotionNames = ChildMotionNames(blendTree);

            foreach (var clipName in QuarantinedLocomotionClipNames)
            {
                Assert.That(childMotionNames, Does.Not.Contain(clipName));
            }
        }

        [Test]
        public void ActionClipsAreNotInsertedIntoLocomotionBlendTree()
        {
            var blendTree = LoadLocomotionBlendTree();
            var childMotionNames = ChildMotionNames(blendTree);

            foreach (var actionClipName in ActionClipNames)
            {
                Assert.That(childMotionNames, Does.Not.Contain(actionClipName));
            }
        }

        [Test]
        public void AgentLocomotionDriverRemainsOnlyProductionAnimatorFloatWriter()
        {
            var scriptsRoot = UnityProjectPath("Assets/Project/Scripts");
            var writers = Directory
                .GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).Contains("Preview"))
                .Where(path => File.ReadAllText(path).Contains("animator.SetFloat("))
                .Select(path => path.Replace('\\', '/'))
                .ToArray();

            Assert.That(
                writers,
                Is.EquivalentTo(new[]
                {
                    UnityProjectPath("Assets/Project/Scripts/Motion/MinibotAnimatorDriver.cs").Replace('\\', '/'),
                    UnityProjectPath("Assets/Project/Scripts/Runtime/AgentLocomotionDriver.cs").Replace('\\', '/')
                }));
        }

        [Test]
        public void AgentLocomotionDriverLogsProductionBriefcaseSources()
        {
            var source = File.ReadAllText(UnityProjectPath("Assets/Project/Scripts/Runtime/AgentLocomotionDriver.cs"));

            StringAssert.Contains("Left Turn W_Briefcase.fbx", source);
            StringAssert.Contains("Right Turn W_Briefcase_Mirrored.fbx", source);
            StringAssert.Contains("TurnLeft_Briefcase", source);
            StringAssert.Contains("TurnRight_Briefcase", source);
            StringAssert.Contains("Walking-2.fbx", source);
            StringAssert.Contains("clip=Walk_InPlace", source);
            StringAssert.Contains("quarantined=Running.fbx", source);
            StringAssert.Contains("Running.fbx", source);
            Assert.That(source, Does.Not.Contain("Happy Right Turn.fbx"));
            Assert.That(source, Does.Not.Contain("Happy Right Turn-2.fbx"));
            Assert.That(source, Does.Not.Contain("Slow Run.fbx"));
        }

        [Test]
        public void PreviewDriverLabelsQuarantinedDiagnosticClips()
        {
            var source = File.ReadAllText(UnityProjectPath("Assets/Project/Scripts/Runtime/MiniBotAnimationPreviewDriver.cs"));

            StringAssert.Contains("QUARANTINED - Slow Run.fbx", source);
            StringAssert.Contains("QUARANTINED - Running.fbx", source);
            StringAssert.Contains("QUARANTINED - Running To Turn.fbx", source);
            StringAssert.Contains("QUARANTINED - Happy Right Turn-2.fbx", source);
            StringAssert.Contains("QUARANTINED - Happy Right Turn.fbx", source);
            StringAssert.Contains("PRODUCTION SAFE RUN STYLE - Walking-2.fbx", source);
            StringAssert.Contains("Right Turn W_Briefcase_Mirrored.fbx / TurnRight_Briefcase", source);
        }

        private static AnimatorController LoadLocomotionController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null, $"Missing AnimatorController at {ControllerPath}");
            return controller;
        }

        private static BlendTree LoadLocomotionBlendTree()
        {
            var controller = LoadLocomotionController();
            foreach (var layer in controller.layers)
            {
                foreach (var childState in layer.stateMachine.states)
                {
                    if (childState.state.motion is BlendTree blendTree &&
                        blendTree.name == LocomotionBlendTreeName)
                    {
                        return blendTree;
                    }
                }
            }

            Assert.Fail($"Missing BlendTree named {LocomotionBlendTreeName}");
            return null;
        }

        private static void AssertHasFloatParameter(AnimatorController controller, string parameterName)
        {
            Assert.That(
                controller.parameters.Any(parameter =>
                    parameter.name == parameterName &&
                    parameter.type == AnimatorControllerParameterType.Float),
                Is.True,
                $"Missing float Animator parameter {parameterName}");
        }

        private static void AssertBlendChild(BlendTree blendTree, string motionName, float x, float y)
        {
            foreach (var child in blendTree.children)
            {
                if (child.motion == null || child.motion.name != motionName)
                {
                    continue;
                }

                Assert.That(child.position.x, Is.EqualTo(x).Within(PositionTolerance), motionName);
                Assert.That(child.position.y, Is.EqualTo(y).Within(PositionTolerance), motionName);
                return;
            }

            Assert.Fail($"Missing BlendTree child motion {motionName}");
        }

        private static void AssertBlendChildCount(BlendTree blendTree, int expectedCount)
        {
            Assert.That(
                blendTree.children.Count(child => child.motion != null),
                Is.EqualTo(expectedCount));
        }

        private static void AssertBlendChildTimeScale(BlendTree blendTree, string motionName, float expectedTimeScale)
        {
            foreach (var child in blendTree.children)
            {
                if (child.motion == null || child.motion.name != motionName)
                {
                    continue;
                }

                Assert.That(child.timeScale, Is.EqualTo(expectedTimeScale).Within(PositionTolerance), motionName);
                return;
            }

            Assert.Fail($"Missing BlendTree child motion {motionName}");
        }

        private static HashSet<string> ChildMotionNames(BlendTree blendTree)
        {
            return new HashSet<string>(
                blendTree.children
                    .Where(child => child.motion != null)
                    .Select(child => child.motion.name));
        }

        private static string UnityProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string RepoPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", relativePath));
        }
    }
}
