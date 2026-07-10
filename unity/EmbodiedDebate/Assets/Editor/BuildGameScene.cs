using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Editor
{
    /// <summary>
    /// Builds the AI company operation game scene: city ground, four company offices
    /// arranged in a quadrant, minibot employees per company, camera, and lighting.
    /// Run via menu: Argus/Build Game Scene.
    /// </summary>
    public static class BuildGameScene
    {
        private const string ScenePath = "Assets/Project/Scenes/AICompanyGame.unity";
        private const string OfficeModelDir = "Assets/Project/Models/GameAssets/office";
        private const string CityModelDir = "Assets/Project/Models/GameAssets/city";
        private const string RobotPrefabPath = "Assets/Project/Robots/Prefabs/FallbackRobot.prefab";

        // Four company offices arranged in a quadrant around city center.
        private static readonly Vector3[] CompanyOffsets =
        {
            new(-12f, 0f, -12f),
            new(12f, 0f, -12f),
            new(-12f, 0f, 12f),
            new(12f, 0f, 12f),
        };

        private static readonly string[] CompanyNames =
        {
            "AlphaCorp",
            "BetaTech",
            "GammaLogic",
            "DeltaFoods",
        };

        private static readonly Color[] CompanyColors =
        {
            new(0.2f, 0.56f, 0.95f),
            new(0.35f, 0.72f, 0.32f),
            new(0.95f, 0.55f, 0.18f),
            new(0.58f, 0.82f, 0.36f),
        };

        [MenuItem("Argus/Build Game Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildCityGround();
            BuildCamera();

            var robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RobotPrefabPath);

            for (var i = 0; i < CompanyOffsets.Length; i++)
            {
                BuildCompanyOffice(CompanyOffsets[i], CompanyNames[i], CompanyColors[i], i, robotPrefab);
            }

            BuildCityProps();
            BuildGameBridge();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log($"BuildGameScene: saved to {ScenePath} with {CompanyOffsets.Length} companies.");
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var original = EditorBuildSettings.scenes;
            foreach (var s in original)
            {
                if (s.path == scenePath)
                {
                    return;
                }
            }
            var updated = new EditorBuildSettingsScene[original.Length + 1];
            System.Array.Copy(original, updated, original.Length);
            updated[original.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updated;
        }

        private static void BuildLighting()
        {
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.89f);
            light.shadows = LightShadows.Soft;
        }

        private static void BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.transform.position = new Vector3(0f, 25f, -25f);
            camGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.17f, 0.19f, 0.22f);
            cam.orthographic = true;
            cam.orthographicSize = 22f;
            cam.farClipPlane = 200f;
            camGo.tag = "MainCamera";
        }

        private static void BuildCityGround()
        {
            var terrain = LoadGlb(CityModelDir, "Terrain01_Art.glb");
            if (terrain != null)
            {
                var go = Object.Instantiate(terrain, Vector3.zero, Quaternion.identity);
                go.name = "CityGround";
            }
            else
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "CityGround";
                ground.transform.localScale = new Vector3(5f, 1f, 5f);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.22f, 0.24f, 0.27f);
                ground.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private static void BuildCompanyOffice(Vector3 offset, string name, Color color, int index, GameObject robotPrefab)
        {
            var office = new GameObject($"Office_{name}");
            office.transform.position = offset;

            var floor = LoadGlb(OfficeModelDir, "Floor_01.glb");
            if (floor != null)
            {
                var floorGo = Object.Instantiate(floor, offset, Quaternion.identity, office.transform);
                floorGo.name = "Floor";
            }

            var wall = LoadGlb(OfficeModelDir, "Wall_01.glb");
            if (wall != null)
            {
                var w1 = Object.Instantiate(wall, offset + new Vector3(-3f, 0f, -3f), Quaternion.identity, office.transform);
                w1.name = "Wall_Back";
                var w2 = Object.Instantiate(wall, offset + new Vector3(-3f, 0f, 3f), Quaternion.Euler(0f, 180f, 0f), office.transform);
                w2.name = "Wall_Front";
            }

            var table = LoadGlb(OfficeModelDir, "Table01.glb");
            if (table != null)
            {
                Object.Instantiate(table, offset + new Vector3(0f, 0f, 0f), Quaternion.identity, office.transform);
            }

            var computer = LoadGlb(OfficeModelDir, "Computer_Retro.glb");
            if (computer != null)
            {
                Object.Instantiate(computer, offset + new Vector3(0f, 0f, -1f), Quaternion.identity, office.transform);
            }

            if (robotPrefab != null)
            {
                for (var e = 0; e < 3; e++)
                {
                    var empPos = offset + new Vector3(-2f + e * 2f, 0f, 2f);
                    var emp = Object.Instantiate(robotPrefab, empPos, Quaternion.identity, office.transform);
                    emp.name = $"Employee_{name}_{e}";
                    var renderer = emp.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        var mat = new Material(renderer.sharedMaterial ?? renderer.material);
                        mat.color = color;
                        renderer.sharedMaterial = mat;
                    }
                }
            }

            var label = new GameObject($"Label_{name}");
            label.transform.SetParent(office.transform);
            label.transform.position = offset + new Vector3(0f, 4f, 0f);
            var tm = label.AddComponent<TextMesh>();
            tm.text = name;
            tm.fontSize = 48;
            tm.characterSize = 0.5f;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
        }

        private static void BuildCityProps()
        {
            var lamp = LoadGlb(CityModelDir, "Light_Streetlight_01.glb");
            if (lamp != null)
            {
                Object.Instantiate(lamp, new Vector3(0f, 0f, 0f), Quaternion.identity);
            }

            var bench = LoadGlb(CityModelDir, "Bench_01.glb");
            if (bench != null)
            {
                Object.Instantiate(bench, new Vector3(0f, 0f, 4f), Quaternion.Euler(0f, 90f, 0f));
            }

            var tree = LoadGlb(CityModelDir, "Tree01_Art.glb");
            if (tree != null)
            {
                Object.Instantiate(tree, new Vector3(-4f, 0f, 0f), Quaternion.identity);
                Object.Instantiate(tree, new Vector3(4f, 0f, 0f), Quaternion.identity);
            }
        }

        private static GameObject LoadGlb(string dir, string filename)
        {
            var path = Path.Combine(dir, filename);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void BuildGameBridge()
        {
            var go = new GameObject("GameBridge");
            var receiver = go.AddComponent<ArgusUnity.Bridge.GameBridgeReceiver>();
            receiver.GetType().GetField("serverUri", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(receiver, "ws://127.0.0.1:8000/unity");
            receiver.GetType().GetField("sessionId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(receiver, "ai-company-game");
        }
    }
}
