using System.IO;
using ArgusUnity.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Editor
{
    /// <summary>
    /// Builds the AI company operation vertical slice: shared city, four offices, CEO minibots,
    /// employee minibots, task props, bridge coordinator, minimal HUD, camera and lighting.
    /// Run via menu: Argus/Build Game Scene.
    /// </summary>
    public static class BuildGameScene
    {
        private const string ScenePath = "Assets/Project/Scenes/AICompanyGame.unity";
        private const string OfficeModelDir = "Assets/Project/Models/GameAssets/office";
        private const string CityModelDir = "Assets/Project/Models/GameAssets/city";
        private const string KayKitModelDir = "Assets/Project/Models/KayKit";
        private const string RobotPrefabPath = "Assets/Project/Robots/Prefabs/FallbackRobot.prefab";

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

        private static readonly string[] EmployeeNames =
        {
            "김민수", "이지은", "박정훈",
            "최유리", "정대현", "강미라",
            "윤서준", "조하늘", "임수빈",
            "오지호", "한별이", "신예림",
        };

        private static readonly Color[] CompanyColors =
        {
            new(0.30f, 0.70f, 0.96f),
            new(0.48f, 0.80f, 0.45f),
            new(0.98f, 0.62f, 0.27f),
            new(0.72f, 0.52f, 0.94f),
        };

        [MenuItem("Argus/Build Game Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildCityGround();
            BuildCamera();

            var robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RobotPrefabPath);
            if (robotPrefab == null)
            {
                Debug.LogWarning($"BuildGameScene: missing minibot prefab at {RobotPrefabPath}; primitive fallbacks will be used.");
            }

            for (var i = 0; i < CompanyOffsets.Length; i++)
            {
                BuildCompanyOffice(
                    CompanyOffsets[i],
                    CompanyNames[i],
                    CompanyColors[i],
                    i,
                    robotPrefab);
            }

            BuildCityProps();
            BuildGameplayProps();
            BuildGameBridge();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log(
                $"BuildGameScene: saved {ScenePath} with {CompanyOffsets.Length} companies, " +
                "4 CEO minibots and 12 employee minibots.");
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var original = EditorBuildSettings.scenes;
            foreach (var existingScene in original)
            {
                if (existingScene.path == scenePath)
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
            RenderSettings.ambientLight = new Color(0.64f, 0.68f, 0.76f);
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.89f);
            light.shadows = LightShadows.Soft;
        }

        private static void BuildCamera()
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.position = new Vector3(0f, 25f, -25f);
            cameraGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.77f, 0.84f, 0.89f);
            camera.orthographic = true;
            camera.orthographicSize = 22f;
            camera.farClipPlane = 200f;
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<CompanyCameraController>();
        }

        private static void BuildCityGround()
        {
            var terrain = LoadModel(CityModelDir, "Terrain01_Art.glb");
            if (terrain != null)
            {
                var terrainGo = Object.Instantiate(terrain, Vector3.zero, Quaternion.identity);
                terrainGo.name = "CityGround";
                return;
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "CityGround";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                "CityGroundPastel",
                new Color(0.58f, 0.66f, 0.68f));
        }

        private static void BuildCompanyOffice(
            Vector3 offset,
            string companyName,
            Color color,
            int companyIndex,
            GameObject robotPrefab)
        {
            var playerId = $"p{companyIndex + 1}";
            var office = new GameObject($"Office_{companyName}");
            office.transform.position = offset;

            InstantiateModel(OfficeModelDir, "Floor_01.glb", offset, Quaternion.identity, office.transform, "Floor");
            InstantiateModel(
                OfficeModelDir,
                "Wall_01.glb",
                offset + new Vector3(-3f, 0f, -3f),
                Quaternion.identity,
                office.transform,
                "Wall_Back");
            InstantiateModel(
                OfficeModelDir,
                "Wall_01.glb",
                offset + new Vector3(-3f, 0f, 3f),
                Quaternion.Euler(0f, 180f, 0f),
                office.transform,
                "Wall_Front");
            InstantiateModel(
                OfficeModelDir,
                "Table01.glb",
                offset,
                Quaternion.identity,
                office.transform,
                "WorkTable");
            InstantiateModel(
                OfficeModelDir,
                "Computer_Retro.glb",
                offset + new Vector3(0f, 0f, -1f),
                Quaternion.identity,
                office.transform,
                "OfficeComputer");
            InstantiateModel(
                OfficeModelDir,
                "Shelf_01_a.glb",
                offset + new Vector3(2.3f, 0f, -2.1f),
                Quaternion.Euler(0f, 180f, 0f),
                office.transform,
                "StorageShelf");

            var ceo = SpawnMiniBot(
                robotPrefab,
                office.transform,
                offset + new Vector3(0f, 0f, -2.2f),
                $"Player_{companyName}",
                $"player-{playerId}",
                playerId,
                $"{companyName} CEO",
                CompanyActorRole.Player,
                color);
            if (companyIndex == 0)
            {
                ceo.AddComponent<CompanyPlayerMiniBotController>();
            }

            for (var employeeIndex = 0; employeeIndex < 3; employeeIndex++)
            {
                var globalEmployeeIndex = companyIndex * 3 + employeeIndex;
                var employeePosition = offset + new Vector3(-2f + employeeIndex * 2f, 0f, 2f);
                var employee = SpawnMiniBot(
                    robotPrefab,
                    office.transform,
                    employeePosition,
                    $"Employee_{companyName}_{employeeIndex}",
                    $"emp-{globalEmployeeIndex:000}",
                    playerId,
                    EmployeeNames[globalEmployeeIndex],
                    CompanyActorRole.Employee,
                    color);
                employee.AddComponent<CompanyEmployeeAgentController>();
            }

            BuildCompanyLabel(office.transform, offset, companyName, color);
        }

        private static GameObject SpawnMiniBot(
            GameObject robotPrefab,
            Transform parent,
            Vector3 position,
            string objectName,
            string actorId,
            string companyId,
            string displayName,
            CompanyActorRole role,
            Color color)
        {
            GameObject minibot;
            if (robotPrefab != null)
            {
                minibot = Object.Instantiate(robotPrefab, position, Quaternion.identity, parent);
            }
            else
            {
                minibot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                minibot.transform.SetParent(parent);
                minibot.transform.position = position;
                minibot.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            }

            minibot.name = objectName;
            TintRenderers(minibot, color);
            var actor = GetOrAddComponent<CompanyMiniBotActor>(minibot);
            actor.Configure(actorId, companyId, displayName, role);
            GetOrAddComponent<CompanyMiniBotMotionDriver>(minibot);
            GetOrAddComponent<CompanyMiniBotIndicator>(minibot);
            return minibot;
        }

        private static void BuildCompanyLabel(Transform parent, Vector3 offset, string companyName, Color color)
        {
            var label = new GameObject($"Label_{companyName}");
            label.transform.SetParent(parent);
            label.transform.position = offset + new Vector3(0f, 4f, 0f);
            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = companyName;
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.5f;
            textMesh.color = color;
            textMesh.anchor = TextAnchor.MiddleCenter;
        }

        private static void BuildCityProps()
        {
            InstantiateModel(
                CityModelDir,
                "Light_Streetlight_01.glb",
                Vector3.zero,
                Quaternion.identity,
                null,
                "CentralStreetlight");
            InstantiateModel(
                CityModelDir,
                "Bench_01.glb",
                new Vector3(0f, 0f, 4f),
                Quaternion.Euler(0f, 90f, 0f),
                null,
                "CentralBench");
            InstantiateModel(
                CityModelDir,
                "Tree01_Art.glb",
                new Vector3(-4f, 0f, 0f),
                Quaternion.identity,
                null,
                "CityTree_Left");
            InstantiateModel(
                CityModelDir,
                "Tree01_Art.glb",
                new Vector3(4f, 0f, 0f),
                Quaternion.identity,
                null,
                "CityTree_Right");
        }

        private static void BuildGameplayProps()
        {
            var root = new GameObject("GameplayProps");
            var cratePrefab = LoadModel(KayKitModelDir, "Box_A.obj");
            for (var companyIndex = 0; companyIndex < CompanyOffsets.Length; companyIndex++)
            {
                var companyRoot = new GameObject($"Tasks_p{companyIndex + 1}");
                companyRoot.transform.SetParent(root.transform);
                var basePosition = CompanyOffsets[companyIndex] + new Vector3(3.4f, 0.4f, 1.6f);
                for (var crateIndex = 0; crateIndex < 3; crateIndex++)
                {
                    GameObject crate;
                    if (cratePrefab != null)
                    {
                        crate = Object.Instantiate(
                            cratePrefab,
                            basePosition + new Vector3(0f, crateIndex * 0.55f, 0f),
                            Quaternion.identity,
                            companyRoot.transform);
                    }
                    else
                    {
                        crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        crate.transform.SetParent(companyRoot.transform);
                        crate.transform.position = basePosition + new Vector3(0f, crateIndex * 0.55f, 0f);
                        crate.transform.localScale = Vector3.one * 0.5f;
                    }
                    crate.name = $"DeliveryCrate_{crateIndex}";
                }

                var deliveryAnchor = new GameObject($"DeliveryAnchor_p{companyIndex + 1}");
                deliveryAnchor.transform.SetParent(companyRoot.transform);
                deliveryAnchor.transform.position = CompanyOffsets[companyIndex] + new Vector3(0f, 0f, 5.5f);
            }

            var contractBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            contractBoard.name = "SharedContractBoard";
            contractBoard.transform.SetParent(root.transform);
            contractBoard.transform.position = new Vector3(0f, 1.25f, -3f);
            contractBoard.transform.localScale = new Vector3(3.2f, 2.2f, 0.25f);
            contractBoard.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                "ContractBoardPastel",
                new Color(0.98f, 0.78f, 0.31f));

            var rumorKiosk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rumorKiosk.name = "RumorKiosk";
            rumorKiosk.transform.SetParent(root.transform);
            rumorKiosk.transform.position = new Vector3(0f, 0.7f, 3f);
            rumorKiosk.transform.localScale = new Vector3(1.2f, 0.7f, 1.2f);
            rumorKiosk.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                "RumorKioskPastel",
                new Color(0.72f, 0.52f, 0.94f));
        }

        private static void BuildGameBridge()
        {
            var bridgeObject = new GameObject("GameBridge");
            var receiver = bridgeObject.AddComponent<ArgusUnity.Bridge.GameBridgeReceiver>();
            SetPrivateField(receiver, "serverUri", "ws://127.0.0.1:8000/unity");
            SetPrivateField(receiver, "sessionId", "ai-company-game");
            bridgeObject.AddComponent<CompanyGameCoordinator>();
            bridgeObject.AddComponent<CompanyGameHud>();
        }

        private static GameObject InstantiateModel(
            string directory,
            string filename,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            string objectName)
        {
            var model = LoadModel(directory, filename);
            if (model == null)
            {
                return null;
            }

            var instance = Object.Instantiate(model, position, rotation, parent);
            instance.name = objectName;
            return instance;
        }

        private static GameObject LoadModel(string directory, string filename)
        {
            var path = Path.Combine(directory, filename).Replace('\\', '/');
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader)
            {
                name = materialName,
                color = color
            };
        }

        private static void TintRenderers(GameObject target, Color color)
        {
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                var sourceMaterial = renderer.sharedMaterial;
                var material = sourceMaterial != null
                    ? new Material(sourceMaterial)
                    : CreateMaterial("MinibotPastel", color);
                material.color = color;
                renderer.sharedMaterial = material;
            }
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogWarning($"BuildGameScene: field {fieldName} was not found on {target.GetType().Name}.");
                return;
            }
            field.SetValue(target, value);
        }
    }
}
