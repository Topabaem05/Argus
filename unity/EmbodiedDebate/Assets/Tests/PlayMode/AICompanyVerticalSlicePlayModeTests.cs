using System.Collections;
using System.Linq;
using ArgusUnity.Bridge;
using ArgusUnity.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ArgusUnity.Tests.PlayMode
{
    public sealed class AICompanyVerticalSlicePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            var operation = SceneManager.LoadSceneAsync("AICompanyGame", LoadSceneMode.Single);
            Assert.IsNotNull(operation, "AICompanyGame must be present in Build Settings.");
            while (!operation.isDone)
            {
                yield return null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneContainsFourCeoAndTwelveEmployeeMinibots()
        {
            var actors = Object.FindObjectsOfType<CompanyMiniBotActor>();
            Assert.AreEqual(16, actors.Length);
            Assert.AreEqual(4, actors.Count(actor => actor.Role == CompanyActorRole.Player));
            Assert.AreEqual(12, actors.Count(actor => actor.Role == CompanyActorRole.Employee));
            Assert.AreEqual(16, actors.Select(actor => actor.ActorId).Distinct().Count());
            Assert.IsTrue(actors.All(actor => actor.GetComponent<Collider>() != null));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CommandOwnershipGateMatchesOwnAndRivalEmployees()
        {
            var coordinator = Object.FindObjectOfType<CompanyGameCoordinator>();
            Assert.IsNotNull(coordinator);
            var employees = Object.FindObjectsOfType<CompanyMiniBotActor>()
                .Where(actor => actor.Role == CompanyActorRole.Employee)
                .ToArray();
            var own = employees.First(actor => actor.CompanyId == coordinator.LocalPlayerId);
            var rival = employees.First(actor => actor.CompanyId != coordinator.LocalPlayerId);

            Assert.IsTrue(coordinator.CanIssue("praise", own));
            Assert.IsFalse(coordinator.CanIssue("praise", rival));
            Assert.IsFalse(coordinator.CanIssue("gossip", own));
            Assert.IsTrue(coordinator.CanIssue("gossip", rival));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneContainsBridgeHudCameraAndMovementControllers()
        {
            Assert.IsNotNull(Object.FindObjectOfType<GameBridgeReceiver>());
            Assert.IsNotNull(Object.FindObjectOfType<CompanyGameHud>());
            Assert.IsNotNull(Object.FindObjectOfType<CompanyCameraController>());
            Assert.AreEqual(1, Object.FindObjectsOfType<CompanyPlayerMiniBotController>().Length);
            Assert.AreEqual(12, Object.FindObjectsOfType<CompanyEmployeeAgentController>().Length);
            yield return null;
        }
    }
}
