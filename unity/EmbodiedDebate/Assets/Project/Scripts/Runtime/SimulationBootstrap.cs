using ArgusUnity.Scene;
using ArgusUnity.UI;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Entry point: wires orchestrator, spawn/move handlers, demo vs bridge mode.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class SimulationBootstrap : MonoBehaviour
    {
        [SerializeField]
        private Transform bridgeSceneRootOverride;

        [SerializeField]
        private GameObject robotPrefab;

        [SerializeField]
        private string robotResourceKey = "UserModels/Idle";

        [SerializeField]
        private bool useDemoMode = false;

        [SerializeField]
        private string bridgeUrl = "ws://127.0.0.1:8765/ws/unity";

        [SerializeField]
        private string bridgeSessionId = "unity-session";

        public void SetDemoMode(bool enabled)
        {
            useDemoMode = enabled;
        }

        private void Awake()
        {
            var root = bridgeSceneRootOverride != null
                ? bridgeSceneRootOverride
                : GameObject.Find("BridgeSceneRoot")?.transform;
            if (root == null)
            {
                Debug.LogWarning("SimulationBootstrap: BridgeSceneRoot not found; spawning under this transform.");
                root = transform;
            }

            var prefab = robotPrefab != null
                ? robotPrefab
                : LoadRobotPrefab(robotResourceKey);
            if (prefab == null)
            {
                Debug.LogError(
                    "SimulationBootstrap: No robot prefab assigned and no Resources robot model found.");
            }

            var orchestrator = new SimulationSceneOrchestrator();

            var spawnHandler = GetComponent<AgentSpawnHandler>();
            var moveHandler = GetComponent<AgentMoveHandler>();
            var behaviorHandler = GetComponent<MiniBotBehaviorHandler>() ??
                                  gameObject.AddComponent<MiniBotBehaviorHandler>();
            var demo = GetComponent<DemoSpawner>();
            var bridge = GetComponent<BridgeReceiver>();

            demo.enabled = false;
            bridge.enabled = false;

            spawnHandler.Initialize(orchestrator, prefab, root);
            moveHandler.Initialize(orchestrator, spawnHandler);
            behaviorHandler.Initialize(orchestrator, moveHandler);
            demo.Initialize(orchestrator);
            bridge.Initialize(orchestrator, bridgeUrl, bridgeSessionId);

            var dialogue = GetComponent<DialogueBubbleManager>() ?? gameObject.AddComponent<DialogueBubbleManager>();
            var emotion = GetComponent<EmotionIndicatorManager>() ?? gameObject.AddComponent<EmotionIndicatorManager>();
            dialogue.Initialize(orchestrator, spawnHandler);
            emotion.Initialize(orchestrator, spawnHandler);

            var groups = GetComponent<GroupIndicatorManager>() ??
                         gameObject.AddComponent<GroupIndicatorManager>();
            var conflicts = GetComponent<ConflictVisualizationManager>() ??
                           gameObject.AddComponent<ConflictVisualizationManager>();
            groups.Initialize(orchestrator, spawnHandler);
            conflicts.Initialize(orchestrator, spawnHandler);

            var observerControls = GetComponent<ObserverControls>() ??
                                   gameObject.AddComponent<ObserverControls>();
            observerControls.Bind(bridge);

            var inspector = GetComponent<AgentInspectorPanel>() ??
                            gameObject.AddComponent<AgentInspectorPanel>();
            inspector.Bind(bridge);

            demo.enabled = useDemoMode;
            bridge.enabled = !useDemoMode;
        }

        private static GameObject LoadRobotPrefab(string resourceKey)
        {
            if (!string.IsNullOrWhiteSpace(resourceKey))
            {
                var requested = Resources.Load<GameObject>(resourceKey);
                if (requested != null)
                {
                    Debug.Log($"SimulationBootstrap: Loaded robot model from Resources/{resourceKey}.");
                    return requested;
                }

                Debug.LogWarning(
                    $"SimulationBootstrap: Resources/{resourceKey} not found; falling back to Resources/FallbackRobot.");
            }

            return Resources.Load<GameObject>("FallbackRobot");
        }
    }
}
