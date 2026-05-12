using System.Reflection;
using System.Text.RegularExpressions;
using ArgusUnity.Runtime;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class AgentLocomotionDriverTests
    {
        [Test]
        public void PlanarSpeedIgnoresVerticalDelta()
        {
            var speed = LocomotionMath.PlanarSpeed(new Vector3(3f, 99f, 4f), 2f);

            Assert.That(speed, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void ResolveMovingUsesStartStopHysteresis()
        {
            Assert.That(LocomotionMath.ResolveMoving(0.04f, false, 0.05f, 0.02f), Is.False);
            Assert.That(LocomotionMath.ResolveMoving(0.05f, false, 0.05f, 0.02f), Is.True);
            Assert.That(LocomotionMath.ResolveMoving(0.03f, true, 0.05f, 0.02f), Is.True);
            Assert.That(LocomotionMath.ResolveMoving(0.02f, true, 0.05f, 0.02f), Is.False);
        }

        [Test]
        public void YawFromPlanarDirectionUsesUnityForwardAxis()
        {
            Assert.That(LocomotionMath.YawFromPlanarDirection(Vector3.forward), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(LocomotionMath.YawFromPlanarDirection(Vector3.right), Is.EqualTo(90f).Within(0.0001f));
        }

        [Test]
        public void SignedYawDeltaMapsSideTurnsToBlendTreeTurn()
        {
            Assert.That(
                LocomotionMath.ResolveTurnValue(
                    LocomotionMath.SignedYawDelta(0f, -35f),
                    0f,
                    true,
                    12f,
                    6f,
                    90f),
                Is.LessThan(0f));
            Assert.That(
                LocomotionMath.ResolveTurnValue(
                    LocomotionMath.SignedYawDelta(0f, 35f),
                    0f,
                    true,
                    12f,
                    6f,
                    90f),
                Is.GreaterThan(0f));
            Assert.That(
                LocomotionMath.ResolveTurnValue(
                    LocomotionMath.SignedYawDelta(0f, 4f),
                    0f,
                    true,
                    12f,
                    6f,
                    90f),
                Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void SignedYawRateUsesActualRotationDeltaPerSecond()
        {
            Assert.That(LocomotionMath.SignedYawRate(0f, 45f, 0.5f), Is.EqualTo(90f).Within(0.0001f));
            Assert.That(LocomotionMath.SignedYawRate(350f, 10f, 0.1f), Is.EqualTo(200f).Within(0.0001f));
            Assert.That(LocomotionMath.SignedYawRate(10f, 350f, 0.1f), Is.EqualTo(-200f).Within(0.0001f));
            Assert.That(LocomotionMath.SignedYawRate(0f, 90f, 0f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void AnimationTurnInputUsesHeadingDeltaInsteadOfYawRateSpike()
        {
            Assert.That(
                LocomotionMath.AnimationTurnDegrees(0.2f, 1154f),
                Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(
                LocomotionMath.AnimationTurnDegrees(-69f, 26f),
                Is.EqualTo(-69f).Within(0.0001f));
        }

        [Test]
        public void ResolveTurnValueUsesHysteresisToAvoidFlicker()
        {
            Assert.That(
                LocomotionMath.ResolveTurnValue(-14f, 0f, true, 12f, 6f, 90f),
                Is.LessThan(0f));
            Assert.That(
                LocomotionMath.ResolveTurnValue(-8f, -0.15f, true, 12f, 6f, 90f),
                Is.LessThan(0f));
            Assert.That(
                LocomotionMath.ResolveTurnValue(-5.5f, -0.15f, true, 12f, 6f, 90f),
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                LocomotionMath.ResolveTurnValue(7f, 0f, true, 12f, 6f, 90f),
                Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ResolveTurnValueStopsWhenAgentStopsMoving()
        {
            Assert.That(
                LocomotionMath.ResolveTurnValue(45f, 0.5f, false, 12f, 6f, 90f),
                Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ResolveTurnValueClampsToBlendTreeRange()
        {
            Assert.That(
                LocomotionMath.ResolveTurnValue(180f, 0f, true, 12f, 6f, 90f),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                LocomotionMath.ResolveTurnValue(-180f, 0f, true, 12f, 6f, 90f),
                Is.EqualTo(-1f).Within(0.0001f));
        }

        [Test]
        public void SmoothPlanarDirectionEasesTowardDiagonalWithoutSnapping()
        {
            var smoothed = LocomotionMath.SmoothPlanarDirection(
                Vector3.forward,
                new Vector3(1f, 0f, 1f),
                8f,
                0.02f);

            Assert.That(smoothed.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Vector3.Angle(Vector3.forward, smoothed), Is.GreaterThan(1f));
            Assert.That(Vector3.Angle(Vector3.forward, smoothed), Is.LessThan(20f));
            Assert.That(Vector3.Angle(smoothed, new Vector3(1f, 0f, 1f)), Is.GreaterThan(20f));
        }

        [Test]
        public void SmoothPlanarDirectionFallsBackToTargetWhenCurrentIsMissing()
        {
            var smoothed = LocomotionMath.SmoothPlanarDirection(
                Vector3.zero,
                new Vector3(-1f, 0f, 1f),
                8f,
                0.02f);

            Assert.That(smoothed.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Vector3.Angle(smoothed, new Vector3(-1f, 0f, 1f)), Is.LessThan(0.001f));
        }

        [Test]
        public void MissingAnimatorParametersLogsWarning()
        {
            var agent = new GameObject("agent");
            try
            {
                var animator = agent.AddComponent<Animator>();
                animator.runtimeAnimatorController = new AnimatorController
                {
                    name = "MissingParamController"
                };

                LogAssert.Expect(
                    LogType.Warning,
                    new Regex("AgentLocomotionDriver: Animator parameter mismatch .*missingSpeed=True.*missingTurn=True"));

                var driver = agent.AddComponent<AgentLocomotionDriver>();
                typeof(AgentLocomotionDriver)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(driver, null);
            }
            finally
            {
                Object.DestroyImmediate(agent);
            }
        }

        [Test]
        public void ClampPlanarKeepsPositionInsideWalkableRoom()
        {
            var clamped = RoomNavigationMath.ClampPlanar(
                new Vector3(5.2f, 1.1f, -3.4f),
                new Vector2(-4.05f, -2.42f),
                new Vector2(4.05f, 2.42f));

            Assert.That(clamped.x, Is.EqualTo(4.05f).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(clamped.z, Is.EqualTo(-2.42f).Within(0.0001f));
        }

        [Test]
        public void ClampPlanarWithInsetMovesPositionAwayFromBoundary()
        {
            var clamped = RoomNavigationMath.ClampPlanarWithInset(
                new Vector3(5.2f, 1.1f, -3.4f),
                new Vector2(-4.05f, -2.42f),
                new Vector2(4.05f, 2.42f),
                0.35f);

            Assert.That(clamped.x, Is.EqualTo(3.7f).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(clamped.z, Is.EqualTo(-2.07f).Within(0.0001f));
        }

        [Test]
        public void BoundaryTurnaroundDirectionMovesInwardAndKeepsATangent()
        {
            var direction = RoomNavigationMath.BoundaryTurnaroundDirection(
                new Vector3(-4.12f, 0f, -1f),
                Vector3.forward,
                new Vector2(-4.05f, -2.42f),
                new Vector2(4.05f, 2.42f),
                0.82f);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.x, Is.GreaterThan(0.45f));
            Assert.That(direction.z, Is.GreaterThan(0.25f));
        }

        [Test]
        public void WallContactRecoveryTurnsNinetyDegreesWithInwardBias()
        {
            var direction = RoomNavigationMath.WallContactRecoveryDirection(
                new Vector3(0.2f, 0f, 2.5f),
                Vector3.forward,
                new Vector2(-4.05f, -2.42f),
                new Vector2(4.05f, 2.42f),
                0.82f,
                true);

            Assert.That(Mathf.Abs(direction.x), Is.GreaterThan(0.55f));
            Assert.That(direction.z, Is.LessThan(-0.25f));
            Assert.That(Vector3.Angle(Vector3.forward, direction), Is.GreaterThan(65f));
        }

        [Test]
        public void WallContactRecoveryChoosesSideThatMovesAwayFromSideWall()
        {
            var direction = RoomNavigationMath.WallContactRecoveryDirection(
                new Vector3(-4.12f, 0f, -1f),
                Vector3.forward,
                new Vector2(-4.05f, -2.42f),
                new Vector2(4.05f, 2.42f),
                0.82f,
                false);

            Assert.That(direction.x, Is.GreaterThan(0.65f));
            Assert.That(Mathf.Abs(direction.z), Is.LessThan(0.55f));
        }

        [Test]
        public void ProjectPlanarVelocityAlongWallRemovesBlockedComponent()
        {
            var projected = RoomNavigationMath.ProjectPlanarVelocityAlongWall(
                new Vector3(1f, 0f, 1f),
                Vector3.left);

            Assert.That(projected.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(projected.z, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ProjectPlanarVelocityAlongWallStopsHeadOnWallDrive()
        {
            var projected = RoomNavigationMath.ProjectPlanarVelocityAlongWall(
                Vector3.right,
                Vector3.left);

            Assert.That(projected, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void RemoveVelocityTowardClampStopsInsetBoundaryPush()
        {
            var velocity = RoomNavigationMath.RemoveVelocityTowardClamp(
                new Vector3(3.4f, 0f, 0.2f),
                new Vector3(3.0f, 0f, 0.2f),
                new Vector3(0.8f, 0f, 0.3f));

            Assert.That(velocity.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(velocity.z, Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void MovePlanarVelocityTowardsLimitsAcceleration()
        {
            var velocity = RoomNavigationMath.MovePlanarVelocityTowards(
                Vector3.zero,
                Vector3.forward * 2f,
                4f,
                10f,
                0.25f);

            Assert.That(velocity.z, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(velocity.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MovePlanarVelocityTowardsUsesDecelerationWhenSlowing()
        {
            var velocity = RoomNavigationMath.MovePlanarVelocityTowards(
                Vector3.forward * 2f,
                Vector3.zero,
                4f,
                6f,
                0.25f);

            Assert.That(velocity.z, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(velocity.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void PaceScaledSpeedCapsFastBridgeCommands()
        {
            Assert.That(
                RoomNavigationMath.PaceScaledSpeed(2.5f, 0.78f, 1.45f, 0.05f),
                Is.EqualTo(1.45f).Within(0.0001f));
            Assert.That(
                RoomNavigationMath.PaceScaledSpeed(1.2f, 0.78f, 1.45f, 0.05f),
                Is.EqualTo(0.936f).Within(0.0001f));
            Assert.That(
                RoomNavigationMath.PaceScaledSpeed(-3f, 0.78f, 1.45f, 0.05f),
                Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void RandomInteriorTargetStaysInsideBounds()
        {
            var random = new System.Random(1234);
            var target = RoomNavigationMath.RandomInteriorTarget(
                random,
                new Vector2(-3.1f, -1.4f),
                new Vector2(3.1f, 1.4f),
                0.25f);

            Assert.That(target.x, Is.InRange(-3.1f, 3.1f));
            Assert.That(target.y, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(target.z, Is.InRange(-1.4f, 1.4f));
        }

        [Test]
        public void MaterialFactoryPrefersUrpShadersBeforeLegacyFallbacks()
        {
            Assert.That(UrpMaterialFactory.PreferredLitShaderNames[0], Is.EqualTo(UrpMaterialFactory.UrpLitShaderName));
            Assert.That(UrpMaterialFactory.PreferredTransparentShaderNames[0], Is.EqualTo(UrpMaterialFactory.UrpUnlitShaderName));
        }

        [Test]
        public void DynamicFurnitureColliderIsAvoidedAsObstacle()
        {
            var agent = new GameObject("agent").transform;
            var furniture = new GameObject("desk");
            var collider = furniture.AddComponent<BoxCollider>();
            var body = furniture.AddComponent<Rigidbody>();
            body.isKinematic = false;

            Assert.That(
                RoomNavigationMath.ShouldAvoidObstacleCollider(collider, agent),
                Is.True);

            Object.DestroyImmediate(furniture);
            Object.DestroyImmediate(agent.gameObject);
        }

        [Test]
        public void AgentChildColliderIsIgnoredAsObstacle()
        {
            var agent = new GameObject("agent").transform;
            var child = new GameObject("agent child");
            child.transform.SetParent(agent);
            var collider = child.AddComponent<BoxCollider>();

            Assert.That(
                RoomNavigationMath.ShouldAvoidObstacleCollider(collider, agent),
                Is.False);

            Object.DestroyImmediate(child);
            Object.DestroyImmediate(agent.gameObject);
        }
    }
}
