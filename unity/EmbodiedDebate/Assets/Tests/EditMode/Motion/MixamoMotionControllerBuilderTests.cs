using System.Collections.Generic;
using System.Linq;
using ArgusUnity.Editor;
using ArgusUnity.Motion;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ArgusUnity.Tests.EditMode.Motion
{
    public sealed class MixamoMotionControllerBuilderTests
    {
        [Test]
        public void BuilderDeclaresRuntimeAnimatorParameters()
        {
            var parameters = MixamoMotionControllerBuilder.RequiredAnimatorParameters
                .ToDictionary(parameter => parameter.Name, parameter => parameter.Type);

            Assert.That(parameters["Speed"], Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters["MoveX"], Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters["MoveZ"], Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters["Turn"], Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters["AngularError"], Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters["IsMoving"], Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(parameters["IsGrounded"], Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(parameters["IsTalking"], Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(parameters["Emotion"], Is.EqualTo(AnimatorControllerParameterType.Int));
            Assert.That(parameters["MotionIntent"], Is.EqualTo(AnimatorControllerParameterType.Int));
            Assert.That(parameters["OverlayWeight"], Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters["RecoveryTrigger"], Is.EqualTo(AnimatorControllerParameterType.Trigger));
        }

        [Test]
        public void BuilderMapsEveryCatalogClipToIgnoredRawFbxPath()
        {
            var seenPaths = new HashSet<string>();
            foreach (var definition in MotionCatalog.All)
            {
                var path = MixamoMotionControllerBuilder.RawAssetPath(definition);
                Assert.That(path, Does.StartWith(MixamoMotionControllerBuilder.RawRoot + "/"));
                Assert.That(path, Does.EndWith(".fbx"));
                Assert.That(seenPaths.Add(path), Is.True, $"Duplicate raw path {path}");
            }
        }

        [Test]
        public void LocalRawAssetSetIsCompleteWhenPresent()
        {
            var availability = MixamoMotionControllerBuilder.ScanRawClipAvailability();
            if (availability.PresentCount == 0)
            {
                Assert.Ignore("Local Mixamo Raw assets are optional and ignored by git.");
            }

            Assert.That(availability.MissingClipNames, Is.Empty);
            Assert.That(availability.PresentCount, Is.EqualTo(availability.RequiredCount));
        }

        [Test]
        public void GeneratedControllerContainsCatalogStatesWhenBuilt()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MixamoMotionControllerBuilder.ControllerPath);
            if (controller == null)
            {
                Assert.Ignore("Generated diverse Mixamo controller is local and ignored by git.");
            }

            foreach (var definition in MotionCatalog.All)
            {
                AssertHasState(controller, 0, definition.ClipName);
                AssertHasState(controller, 1, definition.ClipName);
                AssertHasState(controller, 2, definition.ClipName);
            }
        }

        [Test]
        public void AnimatorDriverAppliesGeneratedNamedStatesWhenBuilt()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MixamoMotionControllerBuilder.ControllerPath);
            if (controller == null)
            {
                Assert.Ignore("Generated diverse Mixamo controller is local and ignored by git.");
            }

            var go = new GameObject("generated-controller-driver-test");
            try
            {
                var animator = go.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                go.AddComponent<SmoothRigidbodyMotor>();
                var driver = go.AddComponent<MinibotAnimatorDriver>();
                var intent = new MotionIntent(
                    MotionIntentType.Talk,
                    false,
                    Vector3.zero,
                    Vector3.forward,
                    0f,
                    0.12f,
                    MotionEmotion.Excited,
                    MotionGesture.Talk,
                    MotionAction.None,
                    true,
                    0.4f);
                var selection = new MotionSelection(
                    MotionClipId.StandingIdle,
                    MotionClipId.Talking,
                    MotionClipId.Excited,
                    new[] { MotionClipId.StandingIdle, MotionClipId.Talking, MotionClipId.Excited });

                driver.SetIntent(intent);
                driver.SetSelection(selection, new MinibotMotionDebugState());
                driver.Tick(0.016f);

                Assert.That(driver.AppliedSelection.BaseClip, Is.EqualTo(MotionClipId.StandingIdle));
                Assert.That(driver.AppliedSelection.OverlayClip, Is.EqualTo(MotionClipId.Talking));
                Assert.That(driver.AppliedSelection.EmotionClip, Is.EqualTo(MotionClipId.Excited));
                Assert.That(animator.GetLayerWeight(1), Is.GreaterThan(0f));
                Assert.That(animator.GetLayerWeight(2), Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void AssertHasState(AnimatorController controller, int layerIndex, string stateName)
        {
            Assert.That(layerIndex, Is.LessThan(controller.layers.Length), stateName);
            foreach (var childState in controller.layers[layerIndex].stateMachine.states)
            {
                if (childState.state != null && childState.state.name == stateName)
                {
                    return;
                }
            }

            Assert.Fail($"Missing {stateName} on layer {layerIndex}");
        }
    }
}
