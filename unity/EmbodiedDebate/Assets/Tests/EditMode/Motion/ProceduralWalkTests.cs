using ArgusUnity.Motion;
using NUnit.Framework;
using UnityEngine;

namespace ArgusUnity.Tests.EditMode.Motion
{
    public sealed class ProceduralWalkTests
    {
        [Test]
        public void LegController_StrideTriggersStepWhenDistanceExceeded()
        {
            var go = new GameObject("leg");
            try
            {
                var ctrl = go.AddComponent<ProceduralLegController>();
                var leftFoot = new GameObject("leftFoot").transform;
                leftFoot.position = Vector3.zero;
                var rightFoot = new GameObject("rightFoot").transform;
                rightFoot.position = new Vector3(0f, 0f, 0.18f);

                SetPrivateField(ctrl, "leftFoot", leftFoot);
                SetPrivateField(ctrl, "rightFoot", rightFoot);
                SetPrivateField(ctrl, "strideLength", 0.5f);
                SetPrivateField(ctrl, "stepHeight", 0.12f);
                SetPrivateField(ctrl, "stepSpeed", 10f);

                ctrl.SetGroundPointForTesting(Vector3.zero);
                ctrl.SetMoveDirection(Vector3.forward);
                ctrl.SetMoveSpeed(1.0f);

                var rightIdeal = new Vector3(0f, 0f, 0.18f);
                leftFoot.position = new Vector3(0f, 0f, -0.18f);

                ctrl.Tick();

                Assert.That(ctrl.GetFootState(true),
                    Is.EqualTo(ProceduralLegController.FootState.Stepping),
                    "Left foot should be stepping after exceeding stride length");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void LegController_DualFootSyncPreventsSimultaneousSteps()
        {
            var go = new GameObject("leg");
            try
            {
                var ctrl = go.AddComponent<ProceduralLegController>();
                var leftFoot = new GameObject("leftFoot").transform;
                leftFoot.position = new Vector3(0f, 0f, -0.18f);
                var rightFoot = new GameObject("rightFoot").transform;
                rightFoot.position = new Vector3(0f, 0f, 0.18f);

                SetPrivateField(ctrl, "leftFoot", leftFoot);
                SetPrivateField(ctrl, "rightFoot", rightFoot);
                SetPrivateField(ctrl, "strideLength", 0.3f);
                SetPrivateField(ctrl, "stepSpeed", 10f);
                SetPrivateField(ctrl, "footSpacing", 0.18f);

                ctrl.SetGroundPointForTesting(new Vector3(0f, 0f, 2f));
                ctrl.SetMoveDirection(Vector3.forward);
                ctrl.SetMoveSpeed(1.0f);

                ctrl.Tick();

                var leftStepping = ctrl.GetFootState(true) == ProceduralLegController.FootState.Stepping;
                var rightStepping = ctrl.GetFootState(false) == ProceduralLegController.FootState.Stepping;

                Assert.That(leftStepping && rightStepping, Is.False,
                    "Both feet must never step simultaneously");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HeadFix_KeepsYawFixedDuringBodyRotation()
        {
            var parent = new GameObject("body");
            var head = new GameObject("head");
            head.transform.SetParent(parent.transform, false);
            head.transform.localRotation = Quaternion.identity;

            try
            {
                var constraint = parent.AddComponent<HeadFixConstraint>();
                SetPrivateField(constraint, "headBone", head.transform);
                SetPrivateField(constraint, "fixYaw", true);
                SetPrivateField(constraint, "yawStiffness", 100f);
                SetPrivateField(constraint, "snapThreshold", 0.5f);
                constraint.SetTargetYaw(0f);

                parent.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                for (var i = 0; i < 50; i++)
                {
                    constraint.Tick();
                }

                var delta = Mathf.Abs(Mathf.DeltaAngle(head.transform.eulerAngles.y, 0f));
                Assert.That(delta, Is.LessThan(1f),
                    $"Head yaw should stay within 1 degree of 0, got delta={delta:F2}");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void HeadFix_SetTargetYawChangesTarget()
        {
            var parent = new GameObject("body");
            var head = new GameObject("head");
            head.transform.SetParent(parent.transform, false);

            try
            {
                var constraint = parent.AddComponent<HeadFixConstraint>();
                SetPrivateField(constraint, "headBone", head.transform);
                SetPrivateField(constraint, "yawStiffness", 100f);

                constraint.SetTargetYaw(0f);
                parent.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                for (var i = 0; i < 30; i++) constraint.Tick();

                var deltaBefore = Mathf.Abs(Mathf.DeltaAngle(head.transform.eulerAngles.y, 0f));

                constraint.SetTargetYaw(90f);
                for (var i = 0; i < 30; i++) constraint.Tick();

                var deltaAfter = Mathf.Abs(Mathf.DeltaAngle(head.transform.eulerAngles.y, 90f));

                Assert.That(deltaAfter, Is.LessThan(1f),
                    $"Head should converge to new target 90, got delta={deltaAfter:F2}");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
