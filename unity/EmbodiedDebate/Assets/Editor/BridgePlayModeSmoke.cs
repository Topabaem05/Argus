using System;
using System.IO;
using ArgusUnity.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArgusUnity.Editor
{
    /// <summary>
    /// Loads MainSimulation and enters Play Mode. <see cref="ArgusUnity.Runtime.SmokeScreenshot"/>
    /// reads <c>ARGUS_UNITY_BRIDGE_CAPTURE=1</c>, renders the scene, waits in-game frames,
    /// then writes <c>tmp/unity_main_simulation.png</c> under the repo and exits Unity.
    /// </summary>
    public static class BridgePlayModeSmoke
    {
        private const string ScenePath = "Assets/Project/Scenes/MainSimulation.unity";
        private const string ClassroomRoot =
            "Assets/ThirdParty/StylooClassroomAssetPack/StylooClassroomAssetPack GLTF & FBX/classroom/GLTF";
        private const string SchoolroomRootName = "Bridge Schoolroom GLTF Background";

        /// <summary>
        /// Invoke with <c>-executeMethod ArgusUnity.Editor.BridgePlayModeSmoke.CaptureAndQuit</c>.
        /// Omit <c>-quit</c> so Play Mode runs; Unity exits via <c>SmokeScreenshot</c>.
        /// </summary>
        public static void CaptureAndQuit()
        {
            Environment.SetEnvironmentVariable("ARGUS_UNITY_BRIDGE_CAPTURE", "1");
            var screenshotPath = ResolveOutputPath();
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath) ?? ".");

            Debug.Log($"ArgusUnity.Editor.BridgePlayModeSmoke: capture path={screenshotPath}");

            EditorSceneManager.OpenScene(ScenePath);
            ApplySimulationOverrides();
            BuildSchoolroomBackground();
            ApplyCaptureMaterials();
            EditorApplication.EnterPlaymode();
        }

        public static void OpenMainSimulation()
        {
            EditorSceneManager.OpenScene(ScenePath);
            ApplySimulationOverrides();
            BuildSchoolroomBackground();
            ApplyCaptureMaterials();
            Debug.Log($"ArgusUnity.Editor.BridgePlayModeSmoke: opened {ScenePath}");
        }

        private static void ApplySimulationOverrides()
        {
            var configPath = Environment.GetEnvironmentVariable("ARGUS_UNITY_SIMULATION_CONFIG");
            var chatText = Environment.GetEnvironmentVariable("ARGUS_UNITY_SIMULATION_CHAT_TEXT");
            var attachmentsJson = Environment.GetEnvironmentVariable("ARGUS_UNITY_SIMULATION_ATTACHMENTS_JSON");
            if (string.IsNullOrWhiteSpace(configPath) &&
                string.IsNullOrWhiteSpace(chatText) &&
                string.IsNullOrWhiteSpace(attachmentsJson))
            {
                return;
            }

            foreach (var receiver in UnityEngine.Object.FindObjectsOfType<BridgeReceiver>(includeInactive: true))
            {
                receiver.SetSimulationConfigPath(configPath);
                receiver.SetSimulationInput(chatText, attachmentsJson);
                EditorUtility.SetDirty(receiver);
            }

            foreach (var bootstrap in UnityEngine.Object.FindObjectsOfType<SimulationBootstrap>(includeInactive: true))
            {
                bootstrap.SetDemoMode(false);
                EditorUtility.SetDirty(bootstrap);
            }
        }

        private static void ApplyCaptureMaterials()
        {
            foreach (var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(includeInactive: true))
            {
                if (!string.Equals(renderer.name, "Ground", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (renderer.sharedMaterial != null &&
                    renderer.sharedMaterial.shader != null &&
                    renderer.sharedMaterial.shader.name != "Standard")
                {
                    continue;
                }

                var color = renderer.name.IndexOf("ground", StringComparison.OrdinalIgnoreCase) >= 0
                    ? new Color(0.46f, 0.50f, 0.55f)
                    : new Color(0.76f, 0.80f, 0.84f);
                renderer.sharedMaterial = UrpMaterialFactory.CreateLit(color);
            }
        }

        private static void BuildSchoolroomBackground()
        {
            RemoveObject("Ground");
            RemoveObject(SchoolroomRootName);
            EnsureClassroomAssetImports();

            var root = new GameObject(SchoolroomRootName);
            root.isStatic = true;

            PlaceClassroomPiece("wallsfloor.glb", root.transform, new Vector3(2f, -0.06f, 0f), Quaternion.identity, 12.5f);
            PlaceClassroomPiece("blackboardbig.glb", root.transform, new Vector3(2f, 1.25f, 2.72f), Quaternion.Euler(0f, 180f, 0f), 2.15f);
            PlaceClassroomPiece("desk.glb", root.transform, new Vector3(2f, 0.02f, 2.18f), Quaternion.Euler(0f, 180f, 0f), 1.05f);
            PlaceClassroomPiece("shelf.glb", root.transform, new Vector3(-2.9f, 0.12f, 2.25f), Quaternion.Euler(0f, 90f, 0f), 1.25f);

            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    var x = -1.0f + column * 2.0f;
                    var z = -1.75f + row * 1.1f;
                    PlaceClassroomPiece("table.glb", root.transform, new Vector3(x, 0.05f, z), Quaternion.identity, 0.82f);
                    PlaceClassroomPiece("chair.glb", root.transform, new Vector3(x, 0.05f, z - 0.45f), Quaternion.Euler(0f, 180f, 0f), 0.48f);
                }
            }

            ConfigureCameraForSchoolroom();
            ConfigureLightForSchoolroom();
            Selection.activeGameObject = root;
            Debug.Log("BridgePlayModeSmoke: added Styloo GLTF classroom background to MainSimulation.");
        }

        private static void EnsureClassroomAssetImports()
        {
            foreach (var fileName in new[]
                     {
                         "wallsfloor.glb",
                         "blackboardbig.glb",
                         "desk.glb",
                         "shelf.glb",
                         "table.glb",
                         "chair.glb",
                     })
            {
                var path = $"{ClassroomRoot}/{fileName}";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"BridgePlayModeSmoke: missing classroom GLTF asset {path}");
                }
            }
        }

        private static void PlaceClassroomPiece(
            string fileName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            float targetMaxDimension)
        {
            var path = $"{ClassroomRoot}/{fileName}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"BridgePlayModeSmoke: cannot place missing classroom asset {fileName}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Schoolroom {Path.GetFileNameWithoutExtension(fileName)}";
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            FitToMaxDimension(instance, targetMaxDimension);
            SetStaticRecursive(instance);
        }

        private static void ConfigureCameraForSchoolroom()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(2f, 4.55f, -5.85f);
            camera.transform.LookAt(new Vector3(2f, 0.82f, 0.25f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.80f, 0.88f);
            camera.fieldOfView = 50f;
        }

        private static void ConfigureLightForSchoolroom()
        {
            var light = UnityEngine.Object.FindObjectOfType<Light>();
            if (light == null)
            {
                var lightGo = new GameObject("Directional Light");
                light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(36f, -48f, 0f);
            light.type = LightType.Directional;
            light.intensity = 1.45f;
            light.color = new Color(1f, 0.88f, 0.72f);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.26f, 0.30f, 0.34f);
        }

        private static void RemoveObject(string objectName)
        {
            var go = GameObject.Find(objectName);
            if (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void FitToMaxDimension(GameObject go, float targetMaxDimension)
        {
            if (targetMaxDimension <= 0f || !TryGetBounds(go, out var bounds))
            {
                return;
            }

            var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDimension <= 0.0001f)
            {
                return;
            }

            go.transform.localScale *= targetMaxDimension / maxDimension;
        }

        private static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static void SetStaticRecursive(GameObject go)
        {
            go.isStatic = true;
            foreach (Transform child in go.transform)
            {
                SetStaticRecursive(child.gameObject);
            }
        }

        private static string ResolveOutputPath()
        {
            var env = Environment.GetEnvironmentVariable("ARGUS_UNITY_CAPTURE_PATH");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return Path.GetFullPath(env);
            }

            var projectDir = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));

            return Path.Combine(repoRoot, "tmp", "unity_main_simulation.png");
        }
    }
}
