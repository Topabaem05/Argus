using System;
using System.IO;
using ArgusUnity.Bridge;
using ArgusUnity.Motion;
using ArgusUnity.Scene;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Writes machine-readable Unity state for diagnosing MiniBot walking and bridge contract issues.
    /// </summary>
    public sealed class MiniBotAutoDump : MonoBehaviour
    {
        private const string ReportDirectory = "reports/unity_dumps";
        private const string TraceFileName = "minibot_runtime_trace.jsonl";

        private SimulationSceneOrchestrator orchestrator;
        private AgentSpawnHandler spawnHandler;
        private AgentMoveHandler moveHandler;
        private BridgeReceiver bridgeReceiver;
        private GameObject robotPrefab;
        private BridgeEnvelope lastBridgeEnvelope;
        private string outputDirectory;
        private bool initialized;

        public string OutputDirectory => EnsureOutputDirectory();

        public void Initialize(
            SimulationSceneOrchestrator nextOrchestrator,
            AgentSpawnHandler nextSpawnHandler,
            AgentMoveHandler nextMoveHandler,
            BridgeReceiver nextBridgeReceiver,
            GameObject nextRobotPrefab)
        {
            if (initialized && orchestrator != null)
            {
                orchestrator.AgentSpawn -= OnBridgeEventApplied;
                orchestrator.AgentMove -= OnBridgeEventApplied;
                orchestrator.PhysicsResult -= OnBridgeEventApplied;
            }

            orchestrator = nextOrchestrator;
            spawnHandler = nextSpawnHandler;
            moveHandler = nextMoveHandler;
            bridgeReceiver = nextBridgeReceiver;
            robotPrefab = nextRobotPrefab;
            initialized = true;

            if (orchestrator != null)
            {
                orchestrator.AgentSpawn += OnBridgeEventApplied;
                orchestrator.AgentMove += OnBridgeEventApplied;
                orchestrator.PhysicsResult += OnBridgeEventApplied;
            }

            ResetTraceFile();
            DumpAll();
        }

        private void OnDestroy()
        {
            if (orchestrator == null)
            {
                return;
            }

            orchestrator.AgentSpawn -= OnBridgeEventApplied;
            orchestrator.AgentMove -= OnBridgeEventApplied;
            orchestrator.PhysicsResult -= OnBridgeEventApplied;
        }

        private void Update()
        {
            if (!initialized || spawnHandler == null)
            {
                return;
            }

            AppendRuntimeTrace();
        }

        public void DumpAll()
        {
            WriteJson("scene_dump.json", BuildSceneDump());
            WriteJson("prefab_dump.json", BuildPrefabDump(robotPrefab));
            WriteJson("animator_dump.json", BuildAnimatorDump(robotPrefab));
            WriteJson("physics_dump.json", BuildPhysicsDump());
            WriteJson("bridge_event_dump.json", BuildBridgeEventDump(lastBridgeEnvelope, false));
        }

        private void OnBridgeEventApplied(BridgeEnvelope envelope)
        {
            lastBridgeEnvelope = envelope;
            WriteJson("bridge_event_dump.json", BuildBridgeEventDump(envelope, true));
            WriteJson("scene_dump.json", BuildSceneDump());
            WriteJson("physics_dump.json", BuildPhysicsDump());
        }

        private JObject BuildSceneDump()
        {
            var warnings = new JArray();
            var bridgeReceivers = FindObjectsOfType<BridgeReceiver>();
            if (bridgeReceivers.Length != 1)
            {
                warnings.Add($"Expected exactly one BridgeReceiver, found {bridgeReceivers.Length}.");
            }

            var groundHasCollider = HasGroundCollider();
            if (!groundHasCollider)
            {
                warnings.Add("No collider found on an object named Ground/Floor/Terrain.");
            }

            var cameras = FindObjectsOfType<Camera>();
            if (cameras.Length == 0)
            {
                warnings.Add("No camera found in scene.");
            }

            return new JObject
            {
                ["scene_name"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                ["minibot_count"] = spawnHandler != null ? spawnHandler.AgentCount : FindObjectsOfType<AgentLocomotionDriver>().Length,
                ["bridge_receiver_count"] = bridgeReceivers.Length,
                ["ground_has_collider"] = groundHasCollider,
                ["spawn_points"] = BuildSpawnPointArray(),
                ["camera_count"] = cameras.Length,
                ["warnings"] = warnings
            };
        }

        private JObject BuildPrefabDump(GameObject prefab)
        {
            var warnings = new JArray();
            var target = prefab != null ? prefab : gameObject;
            var rigidbody = target.GetComponentInChildren<Rigidbody>();
            var collider = target.GetComponentInChildren<Collider>();
            var animator = target.GetComponentInChildren<Animator>();
            var hasMotor = target.GetComponentInChildren<AgentLocomotionDriver>() != null ||
                           target.GetComponentInChildren<MinibotMotionController>() != null ||
                           target.GetComponentInChildren<SmoothRigidbodyMotor>() != null ||
                           target.GetComponentInChildren<MinibotMovementController>() != null ||
                           moveHandler != null;
            var hasBridgeAdapter = spawnHandler != null && moveHandler != null;

            if (rigidbody == null)
            {
                warnings.Add("Prefab has no Rigidbody; bridge-spawned agents may still be transform-driven by AgentMoveHandler.");
            }

            if (collider == null)
            {
                warnings.Add("Prefab has no Collider; ground/contact diagnostics are limited.");
            }

            if (animator == null)
            {
                warnings.Add("Prefab has no Animator before runtime configuration.");
            }

            if (!hasBridgeAdapter)
            {
                warnings.Add("Bridge route is incomplete: expected AgentSpawnHandler and AgentMoveHandler.");
            }

            return new JObject
            {
                ["prefab_name"] = prefab != null ? prefab.name : "runtime_object",
                ["has_rigidbody"] = rigidbody != null,
                ["has_collider"] = collider != null,
                ["has_animator"] = animator != null,
                ["has_motor"] = hasMotor,
                ["has_bridge_adapter"] = hasBridgeAdapter,
                ["rigidbody"] = BuildRigidbodyDump(rigidbody),
                ["collider"] = BuildColliderDump(collider),
                ["warnings"] = warnings
            };
        }

        private JObject BuildAnimatorDump(GameObject prefab)
        {
            var warnings = new JArray();
            var animator = prefab != null ? prefab.GetComponentInChildren<Animator>() : null;
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            var controller = animator != null ? animator.runtimeAnimatorController : null;
            if (controller == null)
            {
                warnings.Add("Animator controller is missing before AgentLocomotionDriver runtime fallback.");
            }

            var parameters = new JObject();
            if (animator != null && controller != null)
            {
                foreach (var parameter in animator.parameters)
                {
                    parameters[parameter.name] = parameter.type.ToString();
                }
            }

            if (parameters["Speed"] == null)
            {
                warnings.Add("Animator parameter Speed is missing from inspected controller.");
            }

            if (parameters["Turn"] == null)
            {
                warnings.Add("Animator parameter Turn is missing from inspected controller.");
            }

            return new JObject
            {
                ["animator_controller"] = controller != null ? controller.name : string.Empty,
                ["parameters"] = parameters,
                ["states"] = new JArray("Idle", "Walk"),
                ["walk_clip_loop"] = true,
                ["apply_root_motion"] = animator != null && animator.applyRootMotion,
                ["warnings"] = warnings
            };
        }

        private JObject BuildPhysicsDump()
        {
            var warnings = new JArray();
            if (spawnHandler == null)
            {
                warnings.Add("AgentSpawnHandler is missing; no runtime MiniBot physics state available.");
            }

            var agents = new JArray();
            if (spawnHandler != null)
            {
                foreach (var pair in spawnHandler.Agents)
                {
                    var tr = pair.Value;
                    var rb = tr != null ? tr.GetComponentInChildren<Rigidbody>() : null;
                    var collider = tr != null ? tr.GetComponentInChildren<Collider>() : null;
                    var motion = tr != null ? tr.GetComponent<MinibotMotionController>() : null;
                    var motor = motion != null ? motion.Motor : null;
                    agents.Add(new JObject
                    {
                        ["agent_id"] = pair.Key,
                        ["position"] = Vector3ToArray(tr != null ? tr.position : Vector3.zero),
                        ["has_rigidbody"] = rb != null,
                        ["has_collider"] = collider != null,
                        ["has_motion_runtime"] = motion != null,
                        ["motion_state"] = motion != null ? motion.State.ToString() : string.Empty,
                        ["motion_speed_mps"] = motor != null ? motor.CurrentVelocity.magnitude : 0f,
                        ["motion_target_distance"] = motor != null ? motor.DistanceToTarget : 0f,
                        ["rigidbody_is_kinematic"] = rb != null && rb.isKinematic,
                        ["rigidbody_use_gravity"] = rb != null && rb.useGravity
                    });
                }
            }

            return new JObject
            {
                ["agent_count"] = spawnHandler != null ? spawnHandler.AgentCount : 0,
                ["active_move_count"] = moveHandler != null ? moveHandler.ActiveMoveCount : 0,
                ["agents"] = agents,
                ["warnings"] = warnings
            };
        }

        private JObject BuildBridgeEventDump(BridgeEnvelope envelope, bool applied)
        {
            var payload = envelope != null && envelope.Payload != null ? envelope.Payload : new JObject();
            var agentId = payload["agent_id"]?.ToObject<string>() ??
                          payload["agent"]?["agent_id"]?.ToObject<string>() ??
                          string.Empty;
            var target = payload["target_position"] as JObject;

            return new JObject
            {
                ["bridge_connected"] = bridgeReceiver != null && bridgeReceiver.IsBridgeSocketConnected,
                ["last_event_type"] = envelope != null ? envelope.Type : string.Empty,
                ["agent_id"] = agentId,
                ["target_position"] = target != null
                    ? new JArray(
                        target["x"]?.ToObject<float>() ?? 0f,
                        target["y"]?.ToObject<float>() ?? 0f,
                        target["z"]?.ToObject<float>() ?? 0f)
                    : new JArray(),
                ["speed"] = payload["speed"] ?? payload["speed_mps"] ?? 0f,
                ["sequence_id"] = envelope != null ? envelope.Sequence : 0,
                ["event_applied_to_minibot"] = applied,
                ["warnings"] = BuildBridgeWarnings(envelope, payload)
            };
        }

        private void AppendRuntimeTrace()
        {
            var path = Path.Combine(EnsureOutputDirectory(), TraceFileName);
            foreach (var pair in spawnHandler.Agents)
            {
                var tr = pair.Value;
                if (tr == null)
                {
                    continue;
                }

                var driver = tr.GetComponent<AgentLocomotionDriver>();
                var target = Vector3.zero;
                var commandSpeed = 0f;
                var hasMove = moveHandler != null && moveHandler.TryGetActiveMove(pair.Key, out target, out commandSpeed);
                var distance = hasMove ? Vector3.Distance(Planar(tr.position), Planar(target)) : 0f;
                var row = new JObject
                {
                    ["frame"] = Time.frameCount,
                    ["agent_id"] = pair.Key,
                    ["speed"] = driver != null ? driver.LastPlanarSpeed : 0f,
                    ["animator_speed"] = driver != null ? driver.LastAnimatorSpeed : 0f,
                    ["turn"] = driver != null ? driver.LastAnimatorTurn : 0f,
                    ["distance_to_target"] = distance,
                    ["state"] = hasMove && distance > 0.05f ? "Walk" : "Idle"
                };
                File.AppendAllText(path, row.ToString(Formatting.None) + Environment.NewLine);
            }
        }

        private JArray BuildSpawnPointArray()
        {
            var result = new JArray();
            foreach (var spawn in FindObjectsOfType<Transform>())
            {
                if (spawn.name.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                result.Add(new JObject
                {
                    ["name"] = spawn.name,
                    ["position"] = Vector3ToArray(spawn.position)
                });
            }

            return result;
        }

        private static JObject BuildRigidbodyDump(Rigidbody rigidbody)
        {
            if (rigidbody == null)
            {
                return new JObject();
            }

            return new JObject
            {
                ["use_gravity"] = rigidbody.useGravity,
                ["is_kinematic"] = rigidbody.isKinematic,
                ["interpolation"] = rigidbody.interpolation.ToString(),
                ["constraints"] = rigidbody.constraints.ToString()
            };
        }

        private static JObject BuildColliderDump(Collider collider)
        {
            if (collider == null)
            {
                return new JObject();
            }

            var capsule = collider as CapsuleCollider;
            return new JObject
            {
                ["type"] = collider.GetType().Name,
                ["height_valid"] = capsule == null || capsule.height > 0.1f,
                ["center_valid"] = capsule == null || capsule.center.y >= 0f
            };
        }

        private static JArray BuildBridgeWarnings(BridgeEnvelope envelope, JObject payload)
        {
            var warnings = new JArray();
            if (envelope == null)
            {
                warnings.Add("No bridge event has been applied yet.");
                return warnings;
            }

            if (envelope.Type == "agent.move" && payload["target_position"] == null)
            {
                warnings.Add("agent.move missing target_position; Argus Unity expects target-position movement commands.");
            }

            if (envelope.Type == "agent.move" && payload["agent_id"] == null)
            {
                warnings.Add("agent.move missing agent_id.");
            }

            return warnings;
        }

        private static bool HasGroundCollider()
        {
            foreach (var collider in FindObjectsOfType<Collider>())
            {
                var name = collider.gameObject.name;
                if (name.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetTraceFile()
        {
            var path = Path.Combine(EnsureOutputDirectory(), TraceFileName);
            File.WriteAllText(path, string.Empty);
        }

        private void WriteJson(string fileName, JObject data)
        {
            File.WriteAllText(
                Path.Combine(EnsureOutputDirectory(), fileName),
                data.ToString(Formatting.Indented));
        }

        private string EnsureOutputDirectory()
        {
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                return outputDirectory;
            }

            outputDirectory = Path.Combine(FindRepositoryRoot(), ReportDirectory);
            Directory.CreateDirectory(outputDirectory);
            return outputDirectory;
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(Application.dataPath);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
        }

        private static Vector3 Planar(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }

        private static JArray Vector3ToArray(Vector3 value)
        {
            return new JArray(value.x, value.y, value.z);
        }
    }
}
