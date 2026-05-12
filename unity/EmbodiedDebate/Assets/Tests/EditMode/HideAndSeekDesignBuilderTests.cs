using System.Reflection;
using ArgusUnity.Editor;
using ArgusUnity.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace ArgusUnity.Tests.EditMode
{
    public sealed class HideAndSeekDesignBuilderTests
    {
        [Test]
        public void WallsFloorCollisionBoxesHaveVisibleDisplays()
        {
            var root = new GameObject("test schoolroom root");
            try
            {
                var count = (int)InvokePrivate("AddStaticSchoolroomColliders", root.transform);

                Assert.That(count, Is.EqualTo(5));
                var collisionRoot = root.transform.Find("Schoolroom Static Rigidbody Colliders");
                Assert.That(collisionRoot, Is.Not.Null);

                AssertVisibleCollider(collisionRoot, "Floor");
                AssertVisibleCollider(collisionRoot, "Back Wall");
                AssertVisibleCollider(collisionRoot, "Front Wall");
                AssertVisibleCollider(collisionRoot, "Left Wall");
                AssertVisibleCollider(collisionRoot, "Right Wall");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ActiveFurnitureAndMiniBotPhysicsAreDynamic()
        {
            var furniture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var bot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var furnitureChild = new GameObject("furniture child");
            var botChild = new GameObject("bot child");
            try
            {
                furniture.name = "desk";
                furniture.isStatic = true;
                furnitureChild.transform.SetParent(furniture.transform);
                furnitureChild.isStatic = true;

                bot.name = "mini-bot";
                bot.isStatic = true;
                botChild.transform.SetParent(bot.transform);
                botChild.isStatic = true;

                var furnitureConfigured = (bool)InvokePrivate("ConfigureActiveFurniture", furniture, 18f, 8f);
                InvokePrivate("ConfigureMiniBotPhysics", bot);

                Assert.That(furnitureConfigured, Is.True);
                AssertDynamicBody(furniture.GetComponent<Rigidbody>());
                AssertDynamicBody(bot.GetComponent<Rigidbody>());
                Assert.That(furniture.isStatic, Is.False);
                Assert.That(furnitureChild.isStatic, Is.False);
                Assert.That(bot.isStatic, Is.False);
                Assert.That(botChild.isStatic, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(furniture);
                Object.DestroyImmediate(bot);
            }
        }

        [Test]
        public void KeyboardTestMiniBotIsRedDynamicAndKeyboardControlled()
        {
            var root = new GameObject("keyboard test root");
            var material = UrpMaterialFactory.CreateLit(new Color(0.95f, 0.1f, 0.08f));
            try
            {
                var bot = (GameObject)InvokePrivate("SpawnKeyboardTestMiniBot", root.transform, material);

                Assert.That(bot.name, Is.EqualTo("Keyboard Test Mini-bot"));
                Assert.That(bot.transform.parent, Is.EqualTo(root.transform));
                Assert.That(bot.GetComponent<MiniBotKeyboardController>(), Is.Not.Null);
                AssertDynamicBody(bot.GetComponent<Rigidbody>());

                Renderer renderer = null;
                foreach (var candidate in bot.GetComponentsInChildren<Renderer>())
                {
                    if (candidate.sharedMaterial == material)
                    {
                        renderer = candidate;
                        break;
                    }
                }

                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.sharedMaterial, Is.EqualTo(material));
                Assert.That(renderer.sharedMaterial.color.r, Is.GreaterThan(renderer.sharedMaterial.color.b));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void KeyboardControllerDefaultsFavorSlowerSmoothDiagonalMovement()
        {
            var controller = new GameObject("keyboard defaults").AddComponent<MiniBotKeyboardController>();
            try
            {
                Assert.That(GetPrivateFloat(controller, "moveSpeed"), Is.EqualTo(1.05f).Within(0.0001f));
                Assert.That(GetPrivateFloat(controller, "acceleration"), Is.EqualTo(5.4f).Within(0.0001f));
                Assert.That(GetPrivateFloat(controller, "deceleration"), Is.EqualTo(7.2f).Within(0.0001f));
                Assert.That(
                    GetPrivateFloat(controller, "maxTurnDegreesPerSecond"),
                    Is.EqualTo(320f).Within(0.0001f));
                Assert.That(
                    GetPrivateFloat(controller, "steeringSharpness"),
                    Is.EqualTo(8f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(controller.gameObject);
            }
        }

        [Test]
        public void AgentMoveHandlerDefaultsCapPaceAndSmoothTurns()
        {
            var handler = new GameObject("move handler defaults").AddComponent<AgentMoveHandler>();
            try
            {
                Assert.That(GetPrivateFloat(handler, "movementPaceScale"), Is.EqualTo(0.78f).Within(0.0001f));
                Assert.That(GetPrivateFloat(handler, "maxMoveSpeedMetersPerSecond"), Is.EqualTo(1.45f).Within(0.0001f));
                Assert.That(GetPrivateFloat(handler, "maxTurnDegreesPerSecond"), Is.EqualTo(260f).Within(0.0001f));
                Assert.That(GetPrivateFloat(handler, "turnSharpness"), Is.EqualTo(7.5f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(handler.gameObject);
            }
        }

        private static object InvokePrivate(string methodName, params object[] args)
        {
            var method = typeof(HideAndSeekDesignBuilder).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, args);
        }

        private static float GetPrivateFloat(object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            return (float)field.GetValue(target);
        }

        private static void AssertVisibleCollider(Transform collisionRoot, string name)
        {
            var collisionBox = collisionRoot.Find($"Static Collision {name}");
            Assert.That(collisionBox, Is.Not.Null);
            Assert.That(collisionBox.GetComponent<BoxCollider>(), Is.Not.Null);

            var visible = collisionBox.Find($"Visible WallsFloor Collider {name}");
            Assert.That(visible, Is.Not.Null);
            Assert.That(visible.GetComponent<Collider>(), Is.Null);

            var renderer = visible.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.color.a, Is.LessThan(1f));
        }

        private static void AssertDynamicBody(Rigidbody body)
        {
            Assert.That(body, Is.Not.Null);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(body.detectCollisions, Is.True);
        }
    }
}
