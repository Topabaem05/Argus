using System;
using System.IO;
using ArgusUnity.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Editor
{
    public static class MiniBotAnimationPreviewBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/MiniBotAnimationPreview.unity";
        private const string MiniBotPath = "Assets/Project/Resources/UserModels/Idle.fbx";
        private const string PretendardPath = "Assets/Project/Resources/Fonts/Pretendard-Regular.otf";
        private const string LocomotionControllerPath =
            "Assets/Project/Resources/Animations/Controllers/MiniBotLocomotion.controller";
        private const string PreviewControllerPath =
            "Assets/Project/Resources/Animations/Controllers/MiniBotAnimationPreview.controller";
        private const float LabelSizeScale = 0.6f;
        private const int CaptureFrameCount = 2160;

        private static readonly PreviewClipSpec[] PreviewClips =
        {
            new PreviewClipSpec("Assets/Project/Resources/UserModels/Idle.fbx", "Idle", "Preview_Idle"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/Locomotion/Walking-2.fbx", "Walk_InPlace", "Preview_Walk_InPlace"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/Locomotion/Slow Run.fbx", "SlowRun", "Preview_SlowRun"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/Locomotion/Running.fbx", "Run", "Preview_Run"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/Locomotion/Running To Turn.fbx", "RunToTurn", "Preview_RunToTurn"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/FBXTurns/Happy Right Turn-2.fbx", "TurnLeft_Happy", "Preview_TurnLeft_Happy"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/FBXTurns/Happy Right Turn.fbx", "TurnRight_Happy", "Preview_TurnRight_Happy"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/FBXTurns/Left Turn W_Briefcase.fbx", "TurnLeft_Briefcase", "Preview_TurnLeft_Briefcase"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/FBXTurns/Left Turn W_Briefcase-2.fbx", "TurnLeft_Briefcase_Alt", "Preview_TurnLeft_Briefcase_Alt"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/FBXTurns/Right Turn W_Briefcase_Mirrored.fbx", "TurnRight_Briefcase", "Preview_TurnRight_Briefcase"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/Actions/Thinking.fbx", "Thinking", "Preview_Thinking"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/Actions/Angry.fbx", "Angry", "Preview_Angry"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/Actions/Male Laying Pose.fbx", "LayingPose", "Preview_LayingPose"),
            new PreviewClipSpec("Assets/Project/Resources/Animations/Actions/Standing Torch Light Torch.fbx", "StandingTorch", "Preview_StandingTorch")
        };

        public static void BuildPreviewScene()
        {
            AssetDatabase.Refresh();
            var previewController = EnsurePreviewController();
            var locomotionController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(LocomotionControllerPath);
            if (previewController == null || locomotionController == null)
            {
                Debug.LogError(
                    "MiniBotAnimationPreviewBuilder: missing required controller " +
                    $"locomotion={locomotionController != null}, preview={previewController != null}.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MiniBotAnimationPreview";

            BuildRoom();
            var bot = InstantiateMiniBot("Mini-bot animation preview", Vector3.zero, 1.05f);
            var animator = bot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = bot.AddComponent<Animator>();
            }

            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = locomotionController;
            var label = AddLabel(
                "MiniBot animation preview",
                new Vector3(0f, 1.55f, 0.15f),
                0.07f,
                Color.white,
                Quaternion.Euler(62f, 0f, 0f));
            var driver = bot.AddComponent<MiniBotAnimationPreviewDriver>();
            driver.Configure(animator, locomotionController, previewController, label.GetComponent<TextMesh>());
            AddReferenceMarkers();
            BuildCameraAndLight();
            AddVideoCapture();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"MiniBotAnimationPreviewBuilder: saved {ScenePath}");
        }

        public static void CapturePreviewVideo()
        {
            BuildPreviewScene();
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_CAPTURE", "1");
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_DIR", ResolveFrameDir());
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_PREFIX", "minibot_animation_preview");
            Environment.SetEnvironmentVariable("ARGUS_UNITY_VIDEO_FRAME_COUNT", CaptureFrameCount.ToString());
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static RuntimeAnimatorController EnsurePreviewController()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewControllerPath) ?? string.Empty);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PreviewControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(PreviewControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(PreviewControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            AnimatorState firstState = null;
            for (var i = 0; i < PreviewClips.Length; i++)
            {
                var spec = PreviewClips[i];
                var clip = LoadAnimationClip(spec.AssetPath, spec.ClipName);
                if (clip == null)
                {
                    Debug.LogWarning(
                        "MiniBotAnimationPreviewBuilder: missing preview clip " +
                        $"asset={spec.AssetPath}, clip={spec.ClipName}, state={spec.StateName}.");
                    continue;
                }

                var state = stateMachine.AddState(spec.StateName, new Vector3(260f + (i % 3) * 220f, 80f + (i / 3) * 70f, 0f));
                state.motion = clip;
                state.writeDefaultValues = true;
                if (firstState == null)
                {
                    firstState = state;
                    stateMachine.defaultState = state;
                }

                Debug.Log(
                    "MiniBotAnimationPreviewBuilder: preview state " +
                    $"state={spec.StateName}, clip={clip.name}, length={clip.length:0.00}, asset={spec.AssetPath}.");
            }

            if (firstState == null)
            {
                Debug.LogError("MiniBotAnimationPreviewBuilder: no preview states were created.");
                return null;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadAnimationClip(string assetPath, string preferredName)
        {
            AnimationClip fallback = null;
            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
            {
                if (!(asset is AnimationClip clip) ||
                    clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                if (clip.name == preferredName)
                {
                    return clip;
                }

                if (fallback == null)
                {
                    fallback = clip;
                }
            }

            return fallback;
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

        private static void BuildRoom()
        {
            var root = new GameObject("MiniBot Animation Preview Room").transform;
            var floorMaterial = Material("AnimationPreviewFloor", new Color(0.24f, 0.28f, 0.3f));
            var wallMaterial = Material("AnimationPreviewWall", new Color(0.16f, 0.18f, 0.2f));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Preview Floor";
            floor.transform.SetParent(root);
            floor.transform.position = new Vector3(0f, -0.08f, 0f);
            floor.transform.localScale = new Vector3(6f, 0.12f, 4.5f);
            ApplyMaterial(floor, floorMaterial);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Preview Back Wall";
            wall.transform.SetParent(root);
            wall.transform.position = new Vector3(0f, 1.1f, 1.9f);
            wall.transform.localScale = new Vector3(6f, 2.3f, 0.16f);
            ApplyMaterial(wall, wallMaterial);
        }

        private static void AddReferenceMarkers()
        {
            AddLabel(
                "Blend Tree connection pass, then direct clip crossfades",
                new Vector3(0f, 0.06f, -1.75f),
                0.055f,
                new Color(0.9f, 0.76f, 0.32f),
                Quaternion.Euler(68f, 0f, 0f));
            AddAxisMarker(Vector3.forward, "forward", new Color(0.25f, 0.62f, 1f));
            AddAxisMarker(Vector3.right, "right", new Color(1f, 0.46f, 0.28f));
        }

        private static void AddAxisMarker(Vector3 direction, string label, Color color)
        {
            var lineGo = new GameObject($"Preview Axis {label}");
            var line = lineGo.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, Vector3.up * 0.03f);
            line.SetPosition(1, direction.normalized * 1.15f + Vector3.up * 0.03f);
            line.startWidth = 0.035f;
            line.endWidth = 0.015f;
            line.material = Material($"PreviewAxis{label}", color);
            AddLabel(label, direction.normalized * 1.32f + Vector3.up * 0.08f, 0.038f, color, Quaternion.Euler(70f, 0f, 0f));
        }

        private static GameObject AddLabel(string text, Vector3 position, float size, Color color, Quaternion rotation)
        {
            var label = new GameObject($"Label - {text.Split('\n')[0]}");
            label.transform.position = position;
            label.transform.rotation = rotation;
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

            return label;
        }

        private static void BuildCameraAndLight()
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(2.6f, 1.65f, -3.25f);
            cameraGo.transform.LookAt(new Vector3(0f, 0.78f, 0.05f));
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.68f, 0.76f, 0.84f);
            camera.fieldOfView = 38f;

            var sun = new GameObject("Directional Light");
            sun.transform.rotation = Quaternion.Euler(34f, -52f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.65f;
            light.color = new Color(1f, 0.82f, 0.58f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.78f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.27f, 0.32f);
        }

        private static void AddVideoCapture()
        {
            new GameObject("SmokeVideoCapture").AddComponent<SmokeVideoCapture>();
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

        private static string ResolveFrameDir()
        {
            var projectDir = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));
            return Path.Combine(repoRoot, "tmp", "minibot_animation_preview_frames");
        }

        private readonly struct PreviewClipSpec
        {
            public PreviewClipSpec(string assetPath, string clipName, string stateName)
            {
                AssetPath = assetPath;
                ClipName = clipName;
                StateName = stateName;
            }

            public string AssetPath { get; }
            public string ClipName { get; }
            public string StateName { get; }
        }
    }
}
