using System;
using System.IO;
using ArgusUnity.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Editor
{
    public static class IsolatedPrototypeSpaceBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/IsolatedPrototypeSpace.unity";
        private const string AssetRoot =
            "Assets/ThirdParty/ModularPrototype/Free 3D Modular Game Assets For Prototyping";

        public static void BuildScene()
        {
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "IsolatedPrototypeSpace";

            var root = new GameObject("Isolated Prototype Space").transform;
            var pieces = new GameObject("Modular Asset Pieces").transform;
            pieces.SetParent(root);

            var floorMaterial = Material("ArgusFloorCoolGray", new Color(0.46f, 0.50f, 0.55f));
            var wallMaterial = Material("ArgusExteriorWall", new Color(0.22f, 0.27f, 0.31f));
            var accentMaterial = Material("ArgusSignalAmber", new Color(1.0f, 0.63f, 0.18f));
            var propMaterial = Material("ArgusPrototypeBlue", new Color(0.15f, 0.44f, 0.78f));

            BuildFloor(root, floorMaterial);
            BuildPrimitiveWalls(root, wallMaterial);
            BuildImportedShell(pieces, wallMaterial, accentMaterial, propMaterial);
            BuildPlayer(root);
            BuildCameraAndLight(root);

            var smoke = new GameObject("SmokeScreenshot");
            smoke.transform.SetParent(root);
            smoke.AddComponent<SmokeScreenshot>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"IsolatedPrototypeSpaceBuilder: saved {ScenePath}");
        }

        public static void CaptureAndQuit()
        {
            BuildScene();
            Environment.SetEnvironmentVariable("ARGUS_UNITY_BRIDGE_CAPTURE", "1");
            Environment.SetEnvironmentVariable("ARGUS_UNITY_CAPTURE_PATH", ResolveCapturePath());
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void BuildFloor(Transform root, Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Single Isolated Floor Slab";
            floor.transform.SetParent(root);
            floor.transform.position = new Vector3(0f, -0.08f, 0f);
            floor.transform.localScale = new Vector3(18f, 0.16f, 18f);
            ApplyMaterial(floor, material);
        }

        private static void BuildPrimitiveWalls(Transform root, Material material)
        {
            CreateWall(root, "North Exterior Wall", new Vector3(0f, 1.4f, 8.6f), new Vector3(18f, 2.8f, 0.45f), material);
            CreateWall(root, "South Exterior Wall", new Vector3(0f, 1.4f, -8.6f), new Vector3(18f, 2.8f, 0.45f), material);
            CreateWall(root, "West Exterior Wall", new Vector3(-8.6f, 1.4f, 0f), new Vector3(0.45f, 2.8f, 18f), material);
            CreateWall(root, "East Exterior Wall", new Vector3(8.6f, 1.4f, 0f), new Vector3(0.45f, 2.8f, 18f), material);
        }

        private static void CreateWall(
            Transform root,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(root);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, material);
        }

        private static void BuildImportedShell(
            Transform parent,
            Material wallMaterial,
            Material accentMaterial,
            Material propMaterial)
        {
            for (var x = -6; x <= 6; x += 3)
            {
                PlacePiece("Pieces/ground.fbx", parent, new Vector3(x, 0.04f, -3f), Quaternion.identity, Vector3.one * 2.0f, propMaterial);
                PlacePiece("Pieces/ground1.fbx", parent, new Vector3(x, 0.04f, 3f), Quaternion.identity, Vector3.one * 2.0f, propMaterial);
            }

            for (var x = -6; x <= 6; x += 3)
            {
                PlacePiece("Pieces/wall.fbx", parent, new Vector3(x, 0.22f, 8.05f), Quaternion.identity, Vector3.one * 2.1f, wallMaterial);
                PlacePiece("Pieces/wall1.fbx", parent, new Vector3(x, 0.22f, -8.05f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.1f, wallMaterial);
            }

            for (var z = -6; z <= 6; z += 3)
            {
                PlacePiece("Pieces/wall window.fbx", parent, new Vector3(-8.05f, 0.22f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.1f, wallMaterial);
                PlacePiece("Pieces/fence.fbx", parent, new Vector3(8.05f, 0.22f, z), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2.1f, wallMaterial);
            }

            PlacePiece("Pieces/wall door.fbx", parent, new Vector3(0f, 0.22f, -7.85f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.4f, accentMaterial);
            PlacePiece("Pieces/door.fbx", parent, new Vector3(0f, 0.35f, -7.45f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.4f, accentMaterial);

            PlacePiece("Pieces/stairs.fbx", parent, new Vector3(-4.8f, 0.25f, 1.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.4f, propMaterial);
            PlacePiece("Pieces/ramp.fbx", parent, new Vector3(4.8f, 0.25f, -1.8f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2.4f, propMaterial);
            PlacePiece("Pieces/key.fbx", parent, new Vector3(0f, 1.2f, 4.7f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 2.6f, accentMaterial);
            PlacePiece("Pieces/coin.fbx", parent, new Vector3(3.8f, 1.1f, 3.8f), Quaternion.Euler(0f, 20f, 90f), Vector3.one * 2.8f, accentMaterial);
            PlacePiece("Pieces/arrow.fbx", parent, new Vector3(-3.4f, 0.9f, -3.2f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 2.8f, accentMaterial);
            PlacePiece("Pieces/toggle switch.fbx", parent, new Vector3(6.5f, 0.9f, 6.5f), Quaternion.Euler(0f, -35f, 0f), Vector3.one * 2.2f, propMaterial);
            PlacePiece("Pieces/pillar.fbx", parent, new Vector3(-7.0f, 0.22f, -7.0f), Quaternion.identity, Vector3.one * 2.6f, wallMaterial);
            PlacePiece("Pieces/pillar1.fbx", parent, new Vector3(7.0f, 0.22f, 7.0f), Quaternion.identity, Vector3.one * 2.6f, wallMaterial);
        }

        private static void BuildPlayer(Transform root)
        {
            var player = new GameObject("Moveable Player Character");
            player.transform.SetParent(root);
            player.transform.position = new Vector3(0f, 0.05f, -3.4f);
            player.transform.rotation = Quaternion.Euler(0f, 20f, 0f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<MovablePrototypeController>();

            var character = Load("Character/Character.fbx");
            if (character != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(character);
                visual.name = "CC0 Character Visual";
                visual.transform.SetParent(player.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * 1.35f;
            }
        }

        private static void BuildCameraAndLight(Transform root)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(root);
            cameraGo.transform.position = new Vector3(10.5f, 12f, -10.5f);
            cameraGo.transform.LookAt(new Vector3(0f, 0.8f, 0f));
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.74f, 0.82f, 0.9f);
            camera.fieldOfView = 48f;

            var sun = new GameObject("Warm Angled Sun");
            sun.transform.SetParent(root);
            sun.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.92f, 0.82f);

            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.62f);
        }

        private static void PlacePiece(
            string relativePath,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            var prefab = Load(relativePath);
            if (prefab == null)
            {
                Debug.LogWarning($"IsolatedPrototypeSpaceBuilder: missing {relativePath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = Path.GetFileNameWithoutExtension(relativePath);
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = Vector3.one;
            FitToMaxDimension(instance, Mathf.Max(scale.x, scale.y, scale.z));
            ApplyMaterial(instance, material);
        }

        private static GameObject Load(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{AssetRoot}/{relativePath}");
        }

        private static Material Material(string name, Color color)
        {
            var dir = "Assets/Project/Materials";
            Directory.CreateDirectory(dir);
            var path = $"{dir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = UrpMaterialFactory.CreateLit(color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ApplyMaterial(GameObject go, Material material)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
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

            var multiplier = targetMaxDimension / maxDimension;
            go.transform.localScale *= multiplier;
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

        private static string ResolveCapturePath()
        {
            var projectDir = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));
            return Path.Combine(repoRoot, "tmp", "isolated_prototype_space.png");
        }
    }
}
