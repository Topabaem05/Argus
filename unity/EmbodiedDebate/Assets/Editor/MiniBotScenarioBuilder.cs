using System;
using System.IO;
using ArgusUnity.Motion;
using ArgusUnity.Runtime;
using ArgusUnity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Editor
{
    public static class MiniBotScenarioBuilder
    {
        private const string MiniBotPath = "Assets/Project/Resources/UserModels/Idle.fbx";
        private const string PretendardPath = "Assets/Project/Resources/Fonts/Pretendard-Regular.otf";
        private const string ClassroomAssetRoot = "Assets/Project/Resources/Environment/Classroom";
        private const string FootstepScenePath = "Assets/Project/Scenes/MiniBotFootstepPreview.unity";
        private const string PersonaScenePath = "Assets/Project/Scenes/MiniBotPersonaScenario.unity";
        private const string RunAroundScenePath = "Assets/Project/Scenes/MiniBotRunAround.unity";
        private const float LabelSizeScale = 0.6f;

        private readonly struct Persona
        {
            public Persona(
                string id,
                string ageGroup,
                string occupation,
                string goal,
                Vector3 position,
                Vector3 target,
                Color color)
            {
                Id = id;
                AgeGroup = ageGroup;
                Occupation = occupation;
                Goal = goal;
                Position = position;
                Target = target;
                Color = color;
            }

            public string Id { get; }
            public string AgeGroup { get; }
            public string Occupation { get; }
            public string Goal { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
            public Color Color { get; }
        }

        private readonly struct ClassroomMovableProps
        {
            public ClassroomMovableProps(
                GameObject pushBox,
                GameObject pullCart,
                GameObject bookBox,
                GameObject deskBlock,
                GameObject markerBox)
            {
                PushBox = pushBox;
                PullCart = pullCart;
                BookBox = bookBox;
                DeskBlock = deskBlock;
                MarkerBox = markerBox;
            }

            public GameObject PushBox { get; }
            public GameObject PullCart { get; }
            public GameObject BookBox { get; }
            public GameObject DeskBlock { get; }
            public GameObject MarkerBox { get; }
        }

        public static void BuildFootstepPreview()
        {
            AssetDatabase.Refresh();
            LogRigInfo();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MiniBotFootstepPreview";

            BuildRoom("Rig Footstep Test Space", 10f);
            var bot = InstantiateMiniBot("Mini-bot unchanged FBX instance", new Vector3(-1.2f, 0f, -1.2f), 1.65f);
            bot.AddComponent<MiniBotFootstepPreview>();
            AddFootMarkers(bot.transform);
            BuildCamera(new Vector3(3.5f, 2.6f, -4.7f), new Vector3(-0.25f, 0.85f, -0.25f), 38f);
            AddSmoke();

            EditorSceneManager.SaveScene(scene, FootstepScenePath);
            Debug.Log($"MiniBotScenarioBuilder: saved {FootstepScenePath}");
        }

        public static void CaptureFootstepPreview()
        {
            BuildFootstepPreview();
            Capture(FootstepScenePath, "mini_bot_footstep_preview.png");
        }

        public static void BuildPersonaScenario()
        {
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MiniBotPersonaScenario";

            BuildRoom("Persona Interaction Mini-bot Space", 16f);
            var runtime = new GameObject("Persona Scenario Runtime")
                .AddComponent<MiniBotPersonaInteractionScenario>();

            var personas = new[]
            {
                new Persona("A01", "20s", "Student", "tests novelty", new Vector3(-4.4f, 0f, -2.8f), new Vector3(-2.4f, 0f, -1.4f), new Color(0.2f, 0.55f, 0.95f)),
                new Persona("A02", "20s", "Retail worker", "compares price", new Vector3(-4.4f, 0f, 2.8f), new Vector3(-2.2f, 0f, 1.3f), new Color(0.2f, 0.55f, 0.95f)),
                new Persona("B01", "30s", "Designer", "critiques usability", new Vector3(0f, 0f, -4.3f), new Vector3(-0.4f, 0f, -1.8f), new Color(0.98f, 0.55f, 0.18f)),
                new Persona("B02", "30s", "Engineer", "checks feasibility", new Vector3(0f, 0f, 4.3f), new Vector3(0.4f, 0f, 1.8f), new Color(0.98f, 0.55f, 0.18f)),
                new Persona("C01", "40s", "Teacher", "asks for clarity", new Vector3(4.4f, 0f, -2.8f), new Vector3(2.3f, 0f, -1.4f), new Color(0.46f, 0.78f, 0.32f)),
                new Persona("C02", "50s", "Healthcare manager", "flags risk", new Vector3(4.4f, 0f, 2.8f), new Vector3(2.4f, 0f, 1.4f), new Color(0.46f, 0.78f, 0.32f)),
            };

            foreach (var persona in personas)
            {
                var bot = InstantiateMiniBot($"{persona.Id} mini-bot", persona.Position, 0.95f);
                bot.AddComponent<MiniBotWalkAnimator>();
                runtime.RegisterAgent(bot.transform, persona.Target);
                AddBaseRing(persona.Position, persona.Color);
                AddLabel(
                    $"{persona.Id} / {persona.AgeGroup}\n{persona.Occupation}",
                    persona.Position + Vector3.up * 1.85f,
                    0.065f,
                    persona.Color);
            }

            AddInteraction("price concern", new Vector3(-2.4f, 0.06f, -1.4f), new Vector3(-2.2f, 0.06f, 1.3f), new Color(0.2f, 0.55f, 0.95f));
            AddInteraction("feasibility reply", new Vector3(-0.4f, 0.08f, -1.8f), new Vector3(0.4f, 0.08f, 1.8f), new Color(0.98f, 0.55f, 0.18f));
            AddInteraction("risk + clarity", new Vector3(2.3f, 0.1f, -1.4f), new Vector3(2.4f, 0.1f, 1.4f), new Color(0.46f, 0.78f, 0.32f));
            AddLabel("Scenario target: product concept comparison by age + occupation", new Vector3(0f, 0.08f, -6.2f), 0.065f, Color.white);
            AddLabel("Interactions attached to mini-bots: movement, facing, dialogue links", new Vector3(0f, 0.08f, 6.2f), 0.065f, new Color(0.95f, 0.75f, 0.3f));

            BuildCamera(new Vector3(9.8f, 8.5f, -9.8f), new Vector3(0f, 0.9f, 0f), 48f);
            AddSmoke();

            EditorSceneManager.SaveScene(scene, PersonaScenePath);
            Debug.Log($"MiniBotScenarioBuilder: saved {PersonaScenePath}");
        }

        public static void CapturePersonaScenario()
        {
            BuildPersonaScenario();
            Capture(PersonaScenePath, "mini_bot_persona_interaction.png");
        }

        public static void BuildRunAroundScenario()
        {
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MiniBotRunAround";

            BuildRoom("Mini-bot Social Simulation Lab", 18f);
            AddSimulationLabProps();
            var movableProps = AddClassroomObjectProps();
            var runtime = new GameObject("Mini-bot Run Around Runtime")
                .AddComponent<MiniBotRunAroundScenario>();

            var personas = new[]
            {
                new Persona("A01", "friendly", "Student", "greets neighbors", Vector3.zero, Vector3.zero, new Color(0.2f, 0.55f, 0.95f)),
                new Persona("A02", "curious", "Retail worker", "asks questions", Vector3.zero, Vector3.zero, new Color(0.2f, 0.72f, 0.88f)),
                new Persona("B01", "energetic", "Designer", "shares ideas", Vector3.zero, Vector3.zero, new Color(0.98f, 0.55f, 0.18f)),
                new Persona("B02", "skeptical", "Engineer", "checks details", Vector3.zero, Vector3.zero, new Color(0.95f, 0.38f, 0.25f)),
                new Persona("C01", "calm", "Teacher", "listens first", Vector3.zero, Vector3.zero, new Color(0.54f, 0.42f, 0.92f)),
                new Persona("C02", "cautious", "Healthcare manager", "flags risks", Vector3.zero, Vector3.zero, new Color(0.62f, 0.72f, 0.28f)),
            };

            var waypointSets = new[]
            {
                new[] { new Vector3(-4.9f, 0f, -1.8f), new Vector3(-6.2f, 0f, -3.9f), new Vector3(-3.6f, 0f, -4.1f), new Vector3(-5.8f, 0f, -0.5f) },
                new[] { new Vector3(-4.9f, 0f, 1.8f), new Vector3(-6.1f, 0f, 3.9f), new Vector3(-3.4f, 0f, 4.0f), new Vector3(-5.7f, 0f, 0.6f) },
                new[] { new Vector3(-0.8f, 0f, -1.9f), new Vector3(-2.3f, 0f, -4.5f), new Vector3(1.5f, 0f, -4.2f), new Vector3(-1.8f, 0f, -0.7f) },
                new[] { new Vector3(0.8f, 0f, 1.9f), new Vector3(2.4f, 0f, 4.5f), new Vector3(-1.2f, 0f, 4.1f), new Vector3(1.9f, 0f, 0.7f) },
                new[] { new Vector3(4.9f, 0f, -1.8f), new Vector3(6.2f, 0f, -3.8f), new Vector3(3.6f, 0f, -4.0f), new Vector3(5.8f, 0f, -0.5f) },
                new[] { new Vector3(4.9f, 0f, 1.8f), new Vector3(6.1f, 0f, 3.8f), new Vector3(3.5f, 0f, 4.0f), new Vector3(5.7f, 0f, 0.6f) },
            };

            for (var i = 0; i < personas.Length; i++)
            {
                var persona = personas[i];
                var waypoints = waypointSets[i];
                var position = waypoints[0];
                var bot = InstantiateMiniBot($"{persona.Id} running mini-bot", position, 0.95f);
                bot.AddComponent<MinibotMovementController>();
                bot.AddComponent<MinibotBlackboard>();
                ConfigureRunAroundMixamoMotion(bot);
                var marker = AddEmotionMarker(bot.transform);
                AddMinibotEmbodiment(bot, persona, marker);
                runtime.RegisterSocialAgent(
                    bot.transform,
                    persona.Id,
                    persona.AgeGroup,
                    waypoints,
                    0.56f + i * 0.015f,
                    i * 0.38f,
                    marker);
            }

            runtime.RegisterInteraction(
                "A01",
                "A02",
                new Vector3(-4.9f, 0f, 0f),
                Vector3.right,
                0.2f,
                1.35f,
                1.75f,
                0.8f,
                1.2f,
                new Vector3(-5.9f, 0f, -0.7f),
                new Vector3(-5.8f, 0f, 0.8f),
                "agree");
            runtime.RegisterInteraction(
                "B01",
                "B02",
                new Vector3(0f, 0f, 0f),
                Vector3.forward,
                1.65f,
                1.35f,
                1.6f,
                0.75f,
                1.2f,
                new Vector3(-1.8f, 0f, -0.8f),
                new Vector3(1.8f, 0f, 0.8f),
                "debate");
            runtime.RegisterInteraction(
                "C01",
                "C02",
                new Vector3(4.9f, 0f, 0f),
                Vector3.right,
                3.2f,
                1.25f,
                1.25f,
                0.75f,
                1.1f,
                new Vector3(5.8f, 0f, -0.8f),
                new Vector3(5.7f, 0f, 0.8f),
                "ask");

            runtime.RegisterObjectTask(
                "B01",
                movableProps.PushBox.transform,
                new Vector3(-1.9f, 0.34f, 2.2f),
                new Vector3(-1.1f, 0.34f, 2.2f),
                12f,
                3.0f,
                4.0f,
                2.5f,
                "push");
            runtime.RegisterObjectTask(
                "C02",
                movableProps.PullCart.transform,
                new Vector3(3.7f, 0.48f, -2.5f),
                new Vector3(3.0f, 0.48f, -2.5f),
                31f,
                3.2f,
                4.3f,
                2.4f,
                "pull");
            runtime.RegisterObjectTask(
                "A02",
                movableProps.BookBox.transform,
                new Vector3(-5.7f, 0.32f, 2.8f),
                new Vector3(-5.7f, 0.32f, 2.1f),
                52f,
                3.1f,
                3.6f,
                2.2f,
                "push");
            runtime.RegisterObjectTask(
                "C01",
                movableProps.DeskBlock.transform,
                new Vector3(5.5f, 0.38f, 1.6f),
                new Vector3(4.8f, 0.38f, 1.1f),
                77f,
                3.4f,
                4.5f,
                2.6f,
                "pull");
            runtime.RegisterObjectTask(
                "A01",
                movableProps.MarkerBox.transform,
                new Vector3(-6.1f, 0.3f, -2.6f),
                new Vector3(-5.35f, 0.3f, -2.6f),
                101f,
                3.0f,
                3.8f,
                2.2f,
                "push");

            BuildCamera(new Vector3(7.4f, 5.2f, -7.7f), new Vector3(0f, 0.85f, 0f), 42f);
            AddConversationCameraRig();
            AddScreenUi(runtime);
            AddVideoCapture();

            EditorSceneManager.SaveScene(scene, RunAroundScenePath);
            Debug.Log($"MiniBotScenarioBuilder: saved {RunAroundScenePath}");
        }

        public static void CaptureRunAroundVideo()
        {
            BuildRunAroundScenario();
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_CAPTURE", "1");
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_DIR", ResolveVideoFrameDir());
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_PREFIX", "mini_bot_run");
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_SECONDS", "120");
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_FORMAT", "jpg");
            EditorSceneManager.OpenScene(RunAroundScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void BuildRoom(string name, float size)
        {
            var root = new GameObject(name).transform;
            var floorMaterial = Material("MiniBotFloor", new Color(0.48f, 0.54f, 0.5f));
            var wallMaterial = Material("MiniBotWall", new Color(0.2f, 0.28f, 0.31f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Soft Simulation Floor";
            floor.transform.SetParent(root);
            floor.transform.position = new Vector3(0f, -0.08f, 0f);
            floor.transform.localScale = new Vector3(size, 0.16f, size);
            ApplyMaterial(floor, floorMaterial);

            CreateWall(root, "North Low Boundary", new Vector3(0f, 0.34f, size * 0.5f), new Vector3(size, 0.68f, 0.28f), wallMaterial);
            CreateWall(root, "South Low Boundary", new Vector3(0f, 0.34f, -size * 0.5f), new Vector3(size, 0.68f, 0.28f), wallMaterial);
            CreateWall(root, "West Low Boundary", new Vector3(-size * 0.5f, 0.34f, 0f), new Vector3(0.28f, 0.68f, size), wallMaterial);
            CreateWall(root, "East Low Boundary", new Vector3(size * 0.5f, 0.34f, 0f), new Vector3(0.28f, 0.68f, size), wallMaterial);
        }

        private static void AddSimulationLabProps()
        {
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Scenario Board";
            board.transform.position = new Vector3(0f, 1.15f, 7.9f);
            board.transform.localScale = new Vector3(5.7f, 1.9f, 0.16f);
            ApplyMaterial(board, Material("MiniBotScenarioBoard", new Color(0.12f, 0.16f, 0.18f)));
            AddLabel(
                "Scenario Board\nPersona opinions become movement, speech, and emotion.",
                new Vector3(0f, 1.55f, 7.78f),
                0.062f,
                new Color(0.92f, 0.96f, 1f));
        }

        private static ClassroomMovableProps AddClassroomObjectProps()
        {
            var root = new GameObject("Classroom Object Interaction Props").transform;

            AddClassroomAsset(root, "table.glb", "Static Table", new Vector3(-2.4f, 0f, 5.8f), Quaternion.Euler(0f, 18f, 0f), 0.62f, 28f, true);
            AddClassroomAsset(root, "chair.glb", "Static Chair", new Vector3(-3.2f, 0f, 5.15f), Quaternion.Euler(0f, -26f, 0f), 0.56f, 8f, true);
            AddClassroomAsset(root, "locker.glb", "Static Locker", new Vector3(6.85f, 0f, 5.9f), Quaternion.Euler(0f, -92f, 0f), 1.25f, 42f, true);
            AddClassroomAsset(root, "desk.glb", "Static Desk", new Vector3(2.4f, 0f, 5.75f), Quaternion.Euler(0f, -12f, 0f), 0.74f, 35f, true);
            AddClassroomAsset(root, "book.glb", "Static Book Stack", new Vector3(-2.1f, 0.72f, 5.75f), Quaternion.Euler(0f, 35f, 0f), 0.28f, 3f, true);

            var pushBox = CreateMovableInteractionProp(
                root,
                "Blue Supply Box",
                new Vector3(-1.9f, 0.34f, 2.2f),
                new Vector3(0.64f, 0.68f, 0.64f),
                new Color(0.22f, 0.46f, 0.9f),
                "Push task",
                6f);
            var pullCart = CreateMovableInteractionProp(
                root,
                "Rolling Shelf Cart",
                new Vector3(3.7f, 0.48f, -2.5f),
                new Vector3(0.86f, 0.96f, 0.6f),
                new Color(0.42f, 0.56f, 0.62f),
                "Pull task",
                18f);
            AddClassroomAsset(pullCart.transform, "shelfwithwheels.glb", "Shelf Visual", Vector3.zero, Quaternion.identity, 0.85f, 0f, false);

            var bookBox = CreateMovableInteractionProp(
                root,
                "Red Book Box",
                new Vector3(-5.7f, 0.32f, 2.8f),
                new Vector3(0.68f, 0.64f, 0.58f),
                new Color(0.78f, 0.25f, 0.22f),
                "Sort books",
                9f);
            AddClassroomAsset(bookBox.transform, "book.glb", "Book Visual", new Vector3(0f, 0.45f, 0f), Quaternion.Euler(0f, 20f, 0f), 0.22f, 0f, false);

            var deskBlock = CreateMovableInteractionProp(
                root,
                "Heavy Desk Block",
                new Vector3(5.5f, 0.38f, 1.6f),
                new Vector3(0.95f, 0.76f, 0.68f),
                new Color(0.58f, 0.36f, 0.2f),
                "Pull desk",
                32f);

            var markerBox = CreateMovableInteractionProp(
                root,
                "Green Marker Box",
                new Vector3(-6.1f, 0.3f, -2.6f),
                new Vector3(0.62f, 0.6f, 0.62f),
                new Color(0.22f, 0.62f, 0.36f),
                "Push supplies",
                5f);

            return new ClassroomMovableProps(pushBox, pullCart, bookBox, deskBlock, markerBox);
        }

        private static GameObject CreateMovableInteractionProp(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            string label,
            float mass)
        {
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = name;
            prop.transform.SetParent(parent);
            prop.transform.position = position;
            prop.transform.localScale = scale;
            ApplyMaterial(prop, Material($"MiniBot{name.Replace(" ", string.Empty)}", color));
            var rigidbody = prop.AddComponent<Rigidbody>();
            ConfigurePropRigidbody(rigidbody, mass, false);
            var collider = prop.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.size = Vector3.one;
            }

            AddLabel(label, position + Vector3.up * (scale.y + 0.35f), 0.052f, Color.white);
            return prop;
        }

        private static GameObject AddClassroomAsset(
            Transform parent,
            string fileName,
            string name,
            Vector3 position,
            Quaternion rotation,
            float targetHeight,
            float mass,
            bool collidable)
        {
            var assetPath = $"{ClassroomAssetRoot}/{fileName}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"MiniBotScenarioBuilder: classroom asset missing or not imported: {assetPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = parent == null ? position : parent.TransformPoint(position);
            instance.transform.rotation = rotation;
            FitToHeight(instance, targetHeight);
            if (collidable)
            {
                ConfigureStaticClassroomAssetPhysics(instance, mass);
            }

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            return instance;
        }

        private static void ConfigureStaticClassroomAssetPhysics(GameObject instance, float mass)
        {
            var rigidbody = instance.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = instance.AddComponent<Rigidbody>();
            }

            ConfigurePropRigidbody(rigidbody, mass, true);

            var collider = instance.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = instance.AddComponent<BoxCollider>();
            }

            var bounds = CalculateRendererBounds(instance);
            if (!bounds.HasValue)
            {
                collider.center = Vector3.up * 0.5f;
                collider.size = Vector3.one;
                return;
            }

            var worldBounds = bounds.Value;
            var lossyScale = instance.transform.lossyScale;
            collider.center = instance.transform.InverseTransformPoint(worldBounds.center);
            collider.size = new Vector3(
                worldBounds.size.x / Mathf.Max(0.001f, Mathf.Abs(lossyScale.x)),
                worldBounds.size.y / Mathf.Max(0.001f, Mathf.Abs(lossyScale.y)),
                worldBounds.size.z / Mathf.Max(0.001f, Mathf.Abs(lossyScale.z)));
        }

        private static Bounds? CalculateRendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return null;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void ConfigurePropRigidbody(Rigidbody rigidbody, float mass, bool kinematic)
        {
            rigidbody.mass = Mathf.Max(0.1f, mass);
            rigidbody.isKinematic = kinematic;
            rigidbody.useGravity = !kinematic;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = kinematic
                ? CollisionDetectionMode.ContinuousSpeculative
                : CollisionDetectionMode.ContinuousDynamic;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rigidbody.drag = kinematic ? 0f : 1.2f;
            rigidbody.angularDrag = kinematic ? 0.05f : 2.4f;
        }

        private static void CreateWall(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(root);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, material);
        }

        private static GameObject InstantiateMiniBot(string name, Vector3 position, float targetHeight)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MiniBotPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Mini-bot model missing: {MiniBotPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;
            FitToHeight(instance, targetHeight);
            return instance;
        }

        private static void ConfigureRunAroundMixamoMotion(GameObject bot)
        {
            var animator = bot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = bot.AddComponent<Animator>();
            }

            animator.applyRootMotion = false;
            var controller = Resources.Load<RuntimeAnimatorController>("Animations/Mixamo/Generated/MiniBotDiverseMixamo") ??
                             Resources.Load<RuntimeAnimatorController>("Animations/Controllers/MiniBotLocomotion");
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }

            var driver = bot.GetComponent<MinibotAnimatorDriver>();
            if (driver == null)
            {
                bot.AddComponent<MinibotAnimatorDriver>();
            }
        }

        private static void AddFootMarkers(Transform bot)
        {
            AddBoneMarker(bot, "LeftFoot", new Color(0.25f, 0.6f, 1f));
            AddBoneMarker(bot, "RightFoot", new Color(1f, 0.65f, 0.2f));
        }

        private static void AddBoneMarker(Transform bot, string suffix, Color color)
        {
            var bone = FindChild(bot, suffix);
            if (bone == null)
            {
                return;
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"{suffix} marker";
            marker.transform.SetParent(bone);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * 0.22f;
            ApplyMaterial(marker, Material($"{suffix}Marker", color));
        }

        private static Transform FindChild(Transform root, string suffix)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>())
            {
                if (child.name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void AddBaseRing(Vector3 position, Color color)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Persona group base";
            ring.transform.position = position + Vector3.up * 0.02f;
            ring.transform.localScale = new Vector3(0.72f, 0.025f, 0.72f);
            ApplyMaterial(ring, Material($"PersonaBase{ColorUtility.ToHtmlStringRGB(color)}", color));
        }

        private static void AddInteraction(string label, Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject($"Interaction - {label}");
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.SetPosition(0, from + Vector3.up * 0.42f);
            line.SetPosition(1, Vector3.Lerp(from, to, 0.5f) + Vector3.up * 1.05f);
            line.SetPosition(2, to + Vector3.up * 0.42f);
            line.startWidth = 0.05f;
            line.endWidth = 0.025f;
            line.material = Material($"Interaction{label.Replace(" ", string.Empty)}", color);

            AddLabel(label, Vector3.Lerp(from, to, 0.5f) + Vector3.up * 1.35f, 0.055f, color);
        }

        private static Transform AddEmotionMarker(Transform bot)
        {
            var marker = new GameObject("Social Emotion Marker");
            marker.name = "Social Emotion Marker";
            marker.transform.SetParent(bot);
            marker.transform.localPosition = Vector3.up * 1.76f;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one;

            marker.SetActive(false);
            return marker.transform;
        }

        private static void AddMinibotEmbodiment(GameObject bot, Persona persona, Transform marker)
        {
            foreach (var renderer in bot.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            var uiAnchor = new GameObject("UIAnchor").transform;
            uiAnchor.SetParent(bot.transform, false);
            uiAnchor.localPosition = Vector3.up * 2.12f;
            uiAnchor.gameObject.AddComponent<BillboardToCamera>();

            var nameplate = AddWorldText(uiAnchor, "Nameplate", $"{persona.Id}\n{persona.AgeGroup}", new Vector3(0f, 0.32f, 0f), 0.055f, persona.Color);
            var emotionIcon = AddWorldText(uiAnchor, "Emotion Icon", "*", new Vector3(0f, 0.09f, 0f), 0.13f, Color.white);
            var speechBubble = AddWorldText(uiAnchor, "Speech Bubble", "Hello", new Vector3(0f, -0.2f, -0.03f), 0.044f, Color.black);
            var bubblePanel = CreateChildPrimitive(uiAnchor, "Speech Bubble Panel", PrimitiveType.Cube, new Vector3(0f, -0.16f, 0.02f), new Vector3(0.98f, 0.24f, 0.035f), Material($"SpeechBubble{persona.Id}", new Color(0.96f, 0.97f, 0.93f)));
            bubblePanel.SetActive(false);
            speechBubble.gameObject.SetActive(false);

            var embodiment = bot.AddComponent<MinibotEmbodimentController>();
            embodiment.Initialize(
                persona.Id,
                persona.AgeGroup,
                persona.Color,
                marker,
                nameplate,
                speechBubble,
                bubblePanel,
                emotionIcon,
                null,
                null,
                null,
                null);
        }

        private static GameObject CreateChildPrimitive(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var child = GameObject.CreatePrimitive(type);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;
            ApplyMaterial(child, material);
            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return child;
        }

        private static TextMesh AddWorldText(
            Transform parent,
            string name,
            string text,
            Vector3 localPosition,
            float size,
            Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = Quaternion.identity;
            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = size;
            mesh.fontSize = Mathf.RoundToInt(64 * LabelSizeScale);
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            return mesh;
        }

        private static GameObject AddLabel(string text, Vector3 position, float size, Color color)
        {
            var label = new GameObject($"Label - {text.Split('\n')[0]}");
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = size * LabelSizeScale;
            mesh.fontSize = Mathf.RoundToInt(52 * LabelSizeScale);
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            var font = AssetDatabase.LoadAssetAtPath<Font>(PretendardPath);
            if (font != null)
            {
                mesh.font = font;
                var renderer = label.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = font.material;
                }
            }
            else
            {
                Debug.LogWarning($"MiniBotScenarioBuilder: Pretendard font missing at {PretendardPath}");
            }

            return label;
        }

        private static void BuildCamera(Vector3 position, Vector3 target, float fov)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = position;
            cameraGo.transform.LookAt(target);
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.73f, 0.82f, 0.9f);
            camera.fieldOfView = fov;

            var sun = new GameObject("Directional Light");
            sun.transform.rotation = Quaternion.Euler(34f, -52f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.65f;
            light.color = new Color(1f, 0.82f, 0.58f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.78f;
            light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.22f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.38f, 0.42f);
        }

        private static void AddConversationCameraRig()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.gameObject.AddComponent<MinibotConversationCameraRig>();
        }

        private static void AddSmoke()
        {
            new GameObject("SmokeScreenshot").AddComponent<SmokeScreenshot>();
        }

        private static void AddVideoCapture()
        {
            new GameObject("SmokeVideoCapture").AddComponent<SmokeVideoCapture>();
        }

        private static void AddScreenUi(MiniBotRunAroundScenario runtime)
        {
            var ui = new GameObject("MiniBot Social UI").AddComponent<MiniBotSocialUiController>();
            ui.Initialize(runtime);
        }

        private static void Capture(string scenePath, string fileName)
        {
            Environment.SetEnvironmentVariable("ARGUS_UNITY_BRIDGE_CAPTURE", "1");
            Environment.SetEnvironmentVariable("ARGUS_UNITY_CAPTURE_PATH", ResolveCapturePath(fileName));
            EditorSceneManager.OpenScene(scenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void LogRigInfo()
        {
            var importer = AssetImporter.GetAtPath(MiniBotPath) as ModelImporter;
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(MiniBotPath);
            Debug.Log(
                "MiniBotScenarioBuilder: mini-bot import " +
                $"animationType={importer?.animationType}, importAnimation={importer?.importAnimation}, " +
                $"avatarValid={avatar != null && avatar.isValid}, avatarHuman={avatar != null && avatar.isHuman}.");
        }

        private static void FitToHeight(GameObject go, float targetHeight)
        {
            if (!TryGetBounds(go, out var bounds) || bounds.size.y <= 0.0001f)
            {
                return;
            }

            go.transform.localScale *= targetHeight / bounds.size.y;
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

        private static Material Material(string name, Color color)
        {
            const string dir = "Assets/Project/Materials";
            Directory.CreateDirectory(dir);
            var path = $"{dir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                UrpMaterialFactory.ApplyLit(existing, color);
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

        private static string ResolveCapturePath(string fileName)
        {
            var projectDir = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));
            return Path.Combine(repoRoot, "tmp", fileName);
        }

        private static string ResolveVideoFrameDir()
        {
            var projectDir = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));
            return Path.Combine(repoRoot, "tmp", "mini_bot_run_frames");
        }
    }
}
