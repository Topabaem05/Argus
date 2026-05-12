using System;
using System.Collections.Generic;
using System.IO;
using ArgusUnity.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Editor
{
    public static class HideAndSeekDesignBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/MiniBotHideAndSeekDesign.unity";
        private const string MiniBotPath = "Assets/Project/Resources/UserModels/Idle.fbx";
        private const string PretendardPath = "Assets/Project/Resources/Fonts/Pretendard-Regular.otf";
        private const string ClassroomRoot =
            "Assets/ThirdParty/StylooClassroomAssetPack/StylooClassroomAssetPack GLTF & FBX/classroom/GLTF";
        private const string TurnAnimationRoot = "Assets/Project/Resources/Animations/FBXTurns";
        private const string LocomotionAnimationRoot = "Assets/Project/Resources/Animations/Locomotion";
        private const string ActionAnimationRoot = "Assets/Project/Resources/Animations/Actions";
        private const string LocomotionControllerRoot = "Assets/Project/Resources/Animations/Controllers";
        private const string LocomotionControllerPath =
            "Assets/Project/Resources/Animations/Controllers/MiniBotLocomotion.controller";
        private const string WalkingSourcePath = "/Users/guribbong/Downloads/Walking-2.fbx";
        private const string WalkingFileName = "Walking-2.fbx";
        private const string SlowRunFileName = "Slow Run.fbx";
        private const string RunningFileName = "Running.fbx";
        private const string HappyRightTurnFileName = "Happy Right Turn.fbx";
        private const string HappyLeftTurnFileName = "Happy Right Turn-2.fbx";
        private const string BriefcaseLeftTurnFileName = "Left Turn W_Briefcase.fbx";
        private const string BriefcaseLeftTurnAltFileName = "Left Turn W_Briefcase-2.fbx";
        private const string BriefcaseRightTurnMirroredFileName = "Right Turn W_Briefcase_Mirrored.fbx";
        private const string IdleStateName = "Idle";
        private const string WalkStateName = "Walk_InPlace";
        private const string SlowRunStateName = "SlowRun";
        private const string RunStateName = "Run";
        private const string HappyRightTurnStateName = "TurnRight_Happy";
        private const string HappyLeftTurnStateName = "TurnLeft_Happy";
        private const string BriefcaseLeftTurnStateName = "TurnLeft_Briefcase";
        private const string BriefcaseLeftTurnAltStateName = "TurnLeft_Briefcase_Alt";
        private const string BriefcaseRightTurnStateName = "TurnRight_Briefcase";
        private const string LocomotionBlendTreeStateName = "LocomotionBlendTree";
        private const string SpeedParameterName = "Speed";
        private const string TurnParameterName = "Turn";
        private const float ProductionLocomotionBlendSpeed = 1f;
        private const float ProductionRunStylePlaybackSpeed = 1.38f;
        private const float LabelSizeScale = 0.6f;
        private const float ClassroomScale = 0.5f;

        private static readonly TurnAnimationImport[] TurnAnimationImports =
        {
            new TurnAnimationImport("/Users/guribbong/Downloads/Right Turn 90.fbx", "Right Turn 90.fbx", "right-90", null),
            new TurnAnimationImport("/Users/guribbong/Downloads/Right Turn 90-2.fbx", "Right Turn 90-2.fbx", "left-90-from-minus-2", null),
            new TurnAnimationImport("/Users/guribbong/Downloads/Happy Right Turn.fbx", HappyRightTurnFileName, "quarantined-happy-right", HappyRightTurnStateName),
            new TurnAnimationImport("/Users/guribbong/Downloads/Happy Right Turn-2.fbx", HappyLeftTurnFileName, "quarantined-happy-left-from-minus-2", HappyLeftTurnStateName),
        };

        private static readonly HumanoidAnimationImport[] ExpandedAnimationImports =
        {
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Running.fbx",
                LocomotionAnimationRoot,
                RunningFileName,
                RunStateName,
                "quarantined-run-locomotion",
                true,
                false),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Slow Run.fbx",
                LocomotionAnimationRoot,
                SlowRunFileName,
                SlowRunStateName,
                "quarantined-slow-run-locomotion",
                true,
                false),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Running To Turn.fbx",
                LocomotionAnimationRoot,
                "Running To Turn.fbx",
                "RunToTurn",
                "quarantined-run-turn-candidate",
                false,
                false),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Left Turn W_Briefcase.fbx",
                TurnAnimationRoot,
                BriefcaseLeftTurnFileName,
                BriefcaseLeftTurnStateName,
                "production-left-briefcase-turn",
                true,
                true),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Left Turn W_Briefcase-2.fbx",
                TurnAnimationRoot,
                BriefcaseLeftTurnAltFileName,
                BriefcaseLeftTurnAltStateName,
                "briefcase-turn-alt-preview",
                false,
                false),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Left Turn W_Briefcase-2.fbx",
                TurnAnimationRoot,
                BriefcaseRightTurnMirroredFileName,
                BriefcaseRightTurnStateName,
                "production-right-briefcase-turn-mirrored",
                true,
                true,
                true),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Thinking.fbx",
                ActionAnimationRoot,
                "Thinking.fbx",
                "Thinking",
                "thinking-action-not-wired",
                false,
                false),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Angry.fbx",
                ActionAnimationRoot,
                "Angry.fbx",
                "Angry",
                "angry-action-not-wired",
                false,
                false),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Male Laying Pose.fbx",
                ActionAnimationRoot,
                "Male Laying Pose.fbx",
                "LayingPose",
                "laying-pose-action-not-wired",
                false,
                false),
            new HumanoidAnimationImport(
                "/Users/guribbong/Downloads/Standing Torch Light Torch.fbx",
                ActionAnimationRoot,
                "Standing Torch Light Torch.fbx",
                "StandingTorch",
                "standing-torch-action-not-wired",
                false,
                false),
        };

        public static void BuildScene()
        {
            AssetDatabase.Refresh();
            EnsureGltfClassroomImports();
            EnsureLocomotionAnimationImports();
            EnsureTurnAnimationImports();
            EnsureExpandedAnimationImports();
            EnsureLocomotionAnimatorController();
            LogMiniBotRigInfo();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MiniBotHideAndSeekDesign";

            var root = new GameObject("Functional Minimalism Hide And Seek").transform;
            var modules = new GameObject("Classroom Asset Layout").transform;
            modules.SetParent(root);

            var blue = Material("HideSeekDefenseBlue", new Color(0.08f, 0.36f, 0.95f));
            var red = Material("HideSeekAttackRed", new Color(0.95f, 0.1f, 0.08f));

            var staticRoomColliderCount = BuildClassroomBackground(modules);
            var furnitureColliderCount = BuildTableOnlyLayout(modules, out var deskObstacle);
            BuildAgents(root, blue, red, deskObstacle);
            BuildCameraAndLight(root);
            AddVideoCapture();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log(
                "HideAndSeekDesignBuilder: physics setup " +
                $"furnitureColliders={furnitureColliderCount}, staticSchoolroomColliders={staticRoomColliderCount}.");
            Debug.Log($"HideAndSeekDesignBuilder: saved {ScenePath}");
        }

        public static void OpenSceneInEditor()
        {
            BuildScene();
            EditorSceneManager.OpenScene(ScenePath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Debug.Log("HideAndSeekDesignBuilder: opened scene in Unity Editor window. Press Play to watch agents move.");
        }

        public static void OpenSceneInPlayMode()
        {
            BuildScene();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.EnterPlaymode();
            Debug.Log("HideAndSeekDesignBuilder: opened scene and entered Play Mode.");
        }

        public static void ValidateGltfClassroomAssets()
        {
            AssetDatabase.Refresh();
            EnsureGltfClassroomImports();
        }

        private static void EnsureGltfClassroomImports()
        {
            foreach (var fileName in new[] { "wallsfloor.glb", "table.glb", "desk.glb" })
            {
                var path = $"{ClassroomRoot}/{fileName}";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                var importer = AssetImporter.GetAtPath(path);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log(
                    "HideAndSeekDesignBuilder: GLTF asset "
                    + $"{fileName}, importer={importer?.GetType().FullName ?? "null"}, "
                    + $"gameObjectLoaded={prefab != null}");
            }
        }

        private static void EnsureTurnAnimationImports()
        {
            Directory.CreateDirectory(TurnAnimationRoot);
            var sharedAvatar = LoadMainMiniBotAvatar();
            foreach (var turnImport in TurnAnimationImports)
            {
                if (!File.Exists(turnImport.SourcePath))
                {
                    Debug.LogWarning($"HideAndSeekDesignBuilder: missing turn animation source {turnImport.SourcePath}");
                    continue;
                }

                var targetPath = $"{TurnAnimationRoot}/{turnImport.FileName}";
                if (ShouldCopyAsset(turnImport.SourcePath, targetPath))
                {
                    File.Copy(turnImport.SourcePath, targetPath, true);
                }

                if (turnImport.HumanoidClipName != null)
                {
                    AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    ConfigureHumanoidAnimation(targetPath, turnImport.HumanoidClipName, true, sharedAvatar);
                }
                else
                {
                    AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    var importer = AssetImporter.GetAtPath(targetPath) as ModelImporter;
                    if (importer != null)
                    {
                        var changed = false;
                        if (!importer.importAnimation)
                        {
                            importer.importAnimation = true;
                            changed = true;
                        }

                        if (importer.animationType != ModelImporterAnimationType.Generic)
                        {
                            importer.animationType = ModelImporterAnimationType.Generic;
                            changed = true;
                        }

                        if (importer.animationCompression != ModelImporterAnimationCompression.Off)
                        {
                            importer.animationCompression = ModelImporterAnimationCompression.Off;
                            changed = true;
                        }

                        if (changed)
                        {
                            importer.SaveAndReimport();
                        }
                    }
                }

                var clipCount = 0;
                foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(targetPath))
                {
                    if (asset is AnimationClip)
                    {
                        clipCount++;
                    }
                }

                Debug.Log(
                    "HideAndSeekDesignBuilder: FBX turn animation " +
                    $"role={turnImport.Role}, asset={turnImport.FileName}, importedClips={clipCount}, " +
                    $"clip={turnImport.HumanoidClipName ?? "generic"}, minus2MeansLeft={turnImport.FileName.Contains("-2")}");
            }
        }

        private static void EnsureExpandedAnimationImports()
        {
            var sharedAvatar = LoadMainMiniBotAvatar();
            foreach (var clipImport in ExpandedAnimationImports)
            {
                Directory.CreateDirectory(clipImport.TargetRoot);
                if (!File.Exists(clipImport.SourcePath))
                {
                    Debug.LogWarning(
                        "HideAndSeekDesignBuilder: missing expanded animation source " +
                        $"{clipImport.SourcePath}, role={clipImport.Role}; skipping.");
                    continue;
                }

                var targetPath = clipImport.AssetPath;
                if (ShouldCopyAsset(clipImport.SourcePath, targetPath))
                {
                    File.Copy(clipImport.SourcePath, targetPath, true);
                }

                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                ConfigureHumanoidAnimation(
                    targetPath,
                    clipImport.ClipName,
                    clipImport.Loop,
                    sharedAvatar,
                    clipImport.Mirror);
                Debug.Log(
                    "HideAndSeekDesignBuilder: expanded animation import " +
                    $"role={clipImport.Role}, activeLocomotion={clipImport.ActiveLocomotion}, " +
                    $"asset={clipImport.FileName}, clip={clipImport.ClipName}, loop={clipImport.Loop}, " +
                    $"mirror={clipImport.Mirror}, " +
                    $"target={targetPath}.");
            }
        }

        private static bool ShouldCopyAsset(string sourcePath, string targetPath)
        {
            if (!File.Exists(targetPath))
            {
                return true;
            }

            var source = new FileInfo(sourcePath);
            var target = new FileInfo(targetPath);
            return source.Length != target.Length || source.LastWriteTimeUtc > target.LastWriteTimeUtc;
        }

        private static void EnsureLocomotionAnimationImports()
        {
            Directory.CreateDirectory(LocomotionAnimationRoot);
            AssetDatabase.ImportAsset(MiniBotPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureHumanoidAnimation(MiniBotPath, IdleStateName, true);
            var sharedAvatar = LoadMainMiniBotAvatar();

            if (!File.Exists(WalkingSourcePath))
            {
                Debug.LogWarning($"HideAndSeekDesignBuilder: missing walk animation source {WalkingSourcePath}");
            }
            else
            {
                var targetPath = $"{LocomotionAnimationRoot}/{WalkingFileName}";
                if (ShouldCopyAsset(WalkingSourcePath, targetPath))
                {
                    File.Copy(WalkingSourcePath, targetPath, true);
                }

                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                ConfigureHumanoidAnimation(targetPath, WalkStateName, true, sharedAvatar);
            }
        }

        private static Avatar LoadMainMiniBotAvatar()
        {
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(MiniBotPath);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogWarning(
                    "HideAndSeekDesignBuilder: Mini-bot avatar is not ready for shared Humanoid animation import " +
                    $"avatarValid={avatar != null && avatar.isValid}, avatarHuman={avatar != null && avatar.isHuman}.");
                return null;
            }

            return avatar;
        }

        private static bool ConfigureHumanoidAnimation(
            string assetPath,
            string clipName,
            bool loop,
            Avatar sourceAvatar = null,
            bool mirror = false)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"HideAndSeekDesignBuilder: no ModelImporter for {assetPath}");
                return false;
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = sourceAvatar != null
                ? ModelImporterAvatarSetup.CopyFromOther
                : ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = sourceAvatar;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = true;

            var clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                clips = importer.clipAnimations;
            }

            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                clip.name = clipName;
                clip.loopTime = loop;
                clip.loopPose = loop;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = false;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = false;
                clip.mirror = mirror;
                clips[i] = clip;
            }

            if (clips.Length > 0)
            {
                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
            var importedClipNames = DescribeAnimationClips(assetPath);
            Debug.Log(
                "HideAndSeekDesignBuilder: humanoid animation import " +
                $"asset={Path.GetFileName(assetPath)}, clip={clipName}, loop={loop}, " +
                $"mirror={mirror}, " +
                "rootLocks=rotation:true,heightY:true,positionXZ:true, " +
                $"avatarSetup={importer.avatarSetup}, sharedAvatar={sourceAvatar != null}, " +
                $"sharedAvatarValid={sourceAvatar != null && sourceAvatar.isValid}, " +
                $"sharedAvatarHuman={sourceAvatar != null && sourceAvatar.isHuman}, " +
                $"assetAvatarValid={avatar != null && avatar.isValid}, assetAvatarHuman={avatar != null && avatar.isHuman}, " +
                $"importedClips={importedClipNames}.");
            return clips.Length > 0;
        }

        private static void EnsureLocomotionAnimatorController()
        {
            Directory.CreateDirectory(LocomotionControllerRoot);

            var idleClip = LoadAnimationClip(MiniBotPath, IdleStateName);
            var walkClip = LoadAnimationClip($"{LocomotionAnimationRoot}/{WalkingFileName}", WalkStateName);
            if (idleClip == null || walkClip == null)
            {
                Debug.LogWarning(
                    "HideAndSeekDesignBuilder: cannot create locomotion controller " +
                    $"idleClip={idleClip != null}, walkClip={walkClip != null}.");
                return;
            }

            var leftTurnClip = LoadAnimationClip(
                $"{TurnAnimationRoot}/{BriefcaseLeftTurnFileName}",
                BriefcaseLeftTurnStateName);
            var rightTurnClip = LoadAnimationClip(
                $"{TurnAnimationRoot}/{BriefcaseRightTurnMirroredFileName}",
                BriefcaseRightTurnStateName);
            if (rightTurnClip == null)
            {
                Debug.LogWarning(
                    "HideAndSeekDesignBuilder: missing mirrored briefcase right turn clip; " +
                    "controller will keep running without TurnRight_Briefcase.");
            }

            if (leftTurnClip == null)
            {
                Debug.LogWarning(
                    "HideAndSeekDesignBuilder: missing briefcase left turn clip; " +
                    "controller will keep running without TurnLeft_Briefcase.");
            }

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(LocomotionControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(LocomotionControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(LocomotionControllerPath);
            controller.AddParameter(SpeedParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(TurnParameterName, AnimatorControllerParameterType.Float);

            var stateMachine = controller.layers[0].stateMachine;
            var idle = stateMachine.AddState(IdleStateName, new Vector3(250f, 120f, 0f));
            idle.motion = idleClip;
            stateMachine.defaultState = idle;

            var locomotion = stateMachine.AddState(LocomotionBlendTreeStateName, new Vector3(520f, 120f, 0f));
            locomotion.motion = CreateLocomotionBlendTree(
                controller,
                idleClip,
                walkClip,
                leftTurnClip,
                rightTurnClip);

            AddTransition(idle, locomotion, 0.15f, AnimatorConditionMode.Greater, 0.05f, SpeedParameterName);
            AddTransition(locomotion, idle, 0.2f, AnimatorConditionMode.Less, 0.05f, SpeedParameterName);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "HideAndSeekDesignBuilder: locomotion AnimatorController ready " +
                $"path={LocomotionControllerPath}, idle={idleClip.name}, blendTree={LocomotionBlendTreeStateName}, " +
                $"runStyle={walkClip.name}@turn0-speed{ProductionLocomotionBlendSpeed:0.00}" +
                $"x{ProductionRunStylePlaybackSpeed:0.00}, " +
                $"leftTurn={leftTurnClip?.name ?? "missing"}@turn-1-speed{ProductionLocomotionBlendSpeed:0.00}, " +
                $"rightTurn={rightTurnClip?.name ?? "missing"}@turn1-speed{ProductionLocomotionBlendSpeed:0.00}, " +
                "quarantined=SlowRun,Run,TurnLeft_Happy,TurnRight_Happy.");
        }

        private static BlendTree CreateLocomotionBlendTree(
            AnimatorController controller,
            AnimationClip idleClip,
            AnimationClip walkClip,
            AnimationClip leftTurnClip,
            AnimationClip rightTurnClip)
        {
            var blendTree = new BlendTree
            {
                name = LocomotionBlendTreeStateName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = TurnParameterName,
                blendParameterY = SpeedParameterName,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            AddBlendChild(blendTree, idleClip, new Vector2(0f, 0f), 1f);
            AddBlendChild(
                blendTree,
                walkClip,
                new Vector2(0f, ProductionLocomotionBlendSpeed),
                ProductionRunStylePlaybackSpeed);

            if (leftTurnClip != null)
            {
                AddBlendChild(blendTree, leftTurnClip, new Vector2(-1f, ProductionLocomotionBlendSpeed), 1f);
            }

            if (rightTurnClip != null)
            {
                AddBlendChild(blendTree, rightTurnClip, new Vector2(1f, ProductionLocomotionBlendSpeed), 1f);
            }

            return blendTree;
        }

        private static void AddBlendChild(BlendTree blendTree, Motion motion, Vector2 position, float timeScale)
        {
            blendTree.AddChild(motion, position);
            var children = blendTree.children;
            if (children.Length == 0)
            {
                return;
            }

            var child = children[children.Length - 1];
            child.timeScale = timeScale;
            children[children.Length - 1] = child;
            blendTree.children = children;
        }

        private static AnimatorStateTransition AddTransition(
            AnimatorState source,
            AnimatorState destination,
            float duration,
            AnimatorConditionMode conditionMode,
            float threshold,
            string parameter)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.AddCondition(conditionMode, threshold, parameter);
            return transition;
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

        private static string DescribeAnimationClips(string assetPath)
        {
            var names = new List<string>();
            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
            {
                if (asset is AnimationClip clip &&
                    !clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    names.Add(clip.name);
                }
            }

            return names.Count > 0 ? string.Join("|", names) : "none";
        }

        private static int BuildClassroomBackground(Transform parent)
        {
            var classroom = PlaceClassroomPiece("wallsfloor.glb", parent, P(0f, -0.02f, 0f), Quaternion.identity, 12.5f);
            if (classroom == null)
            {
                return 0;
            }

            ConfigureStaticSchoolroom(classroom);
            return AddStaticSchoolroomColliders(parent);
        }

        private static int BuildTableOnlyLayout(Transform parent, out Transform deskObstacle)
        {
            deskObstacle = null;
            var tablePositions = new[]
            {
                P(-6f, 0.5f, -5.2f),
                P(-2f, 0.5f, -5.2f),
                P(2f, 0.5f, -5.2f),
                P(6f, 0.5f, -5.2f),
                P(-6f, 0.5f, -2.4f),
                P(-2f, 0.5f, -2.4f),
                P(2f, 0.5f, -2.4f),
                P(6f, 0.5f, -2.4f),
                P(-6f, 0.5f, 0.4f),
                P(6f, 0.5f, 0.4f),
            };

            var activeCount = 0;
            foreach (var position in tablePositions)
            {
                var table = PlaceClassroomPiece("table.glb", parent, position, Quaternion.identity, 0.88f);
                if (ConfigureActiveFurniture(table, 8f, 5f))
                {
                    activeCount++;
                }
            }

            var desk = PlaceClassroomPiece("desk.glb", parent, P(0f, 0.04f, 2.6f), Quaternion.Euler(0f, 180f, 0f), 1.08f);
            if (ConfigureActiveFurniture(desk, 18f, 8f))
            {
                deskObstacle = desk.transform;
                activeCount++;
            }

            return activeCount;
        }

        private static void BuildAgents(Transform root, Material defense, Material attack, Transform deskObstacle)
        {
            var runtime = new GameObject("Hide And Seek Runtime").AddComponent<MiniBotHideAndSeekScenario>();
            runtime.transform.SetParent(root);
            runtime.RegisterObstacleProbeObject(deskObstacle);

            SpawnMiniBot(root, runtime, "Hider 01", P(-4.8f, 0f, -1.4f), 0.66f, 1001, defense, "Hider / blue");
            SpawnMiniBot(root, runtime, "Hider 02", P(-0.8f, 0f, 1.2f), 0.61f, 1002, defense, "Hider / blue");
            SpawnMiniBot(root, runtime, "Seeker 01", P(4.8f, 0f, -1.4f), 0.73f, 2001, attack, "Seeker / red");
            SpawnMiniBot(root, runtime, "Seeker 02", P(3.0f, 0f, 1.2f), 0.70f, 2002, attack, "Seeker / red");
            SpawnKeyboardTestMiniBot(root, attack);
        }

        private static void SpawnMiniBot(
            Transform root,
            MiniBotHideAndSeekScenario runtime,
            string name,
            Vector3 startPosition,
            float speed,
            int seed,
            Material marker,
            string label)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MiniBotPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Mini-bot model missing: {MiniBotPath}");
            }

            var bot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            bot.name = name;
            bot.transform.SetParent(root);
            bot.transform.position = startPosition;
            FitToHeight(bot, 0.95f);
            ConfigureMiniBotPhysics(bot);
            ConfigureAnimatorLocomotion(bot);
            runtime.RegisterAgent(bot.transform, startPosition, speed, seed);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"{name} team marker";
            ring.transform.SetParent(bot.transform);
            ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ring.transform.localScale = new Vector3(0.58f, 0.03f, 0.58f);
            ApplyMaterial(ring, marker);
            RemoveColliders(ring);

            var labelGo = AddLabel($"{name}\n{label}", startPosition + Vector3.up * 1.35f, 0.052f, marker.color);
            labelGo.transform.SetParent(bot.transform, true);
        }

        private static GameObject SpawnKeyboardTestMiniBot(Transform root, Material marker)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MiniBotPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Mini-bot model missing: {MiniBotPath}");
            }

            var startPosition = P(0f, 0f, -3.2f);
            var bot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            bot.name = "Keyboard Test Mini-bot";
            bot.transform.SetParent(root);
            bot.transform.position = startPosition;
            FitToHeight(bot, 0.95f);
            ConfigureMiniBotPhysics(bot);
            ConfigureAnimatorLocomotion(bot);
            ApplyMaterial(bot, marker);

            if (bot.GetComponent<MiniBotKeyboardController>() == null)
            {
                bot.AddComponent<MiniBotKeyboardController>();
            }

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Keyboard Test Mini-bot control marker";
            ring.transform.SetParent(bot.transform);
            ring.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            ring.transform.localScale = new Vector3(0.68f, 0.035f, 0.68f);
            ApplyMaterial(ring, marker);
            RemoveColliders(ring);

            var labelGo = AddLabel("Keyboard Test\nWASD / Arrows", startPosition + Vector3.up * 1.42f, 0.046f, marker.color);
            labelGo.transform.SetParent(bot.transform, true);
            return bot;
        }

        private static void BuildLegend(Transform root, Material defense, Material attack, Material tool, Material state)
        {
            AddLegendMarker(root, "Defense", P(-10.4f, 0.05f, 9.8f), defense);
            AddLegendMarker(root, "Attack", P(-7.8f, 0.05f, 9.8f), attack);
            AddLegendMarker(root, "Tool", P(-5.2f, 0.05f, 9.8f), tool);
            AddLegendMarker(root, "State", P(-2.6f, 0.05f, 9.8f), state);
        }

        private static void AddLegendMarker(Transform root, string label, Vector3 position, Material material)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = $"Legend {label}";
            marker.transform.SetParent(root);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(0.4f, 0.08f, 0.4f);
            ApplyMaterial(marker, material);
            AddLabel(label, position + new Vector3(0f, 0.12f, 0.42f), 0.042f, material.color);
        }

        private static void BuildCameraAndLight(Transform root)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(root);
            cameraGo.transform.position = new Vector3(0f, 4.8f, -6.2f);
            cameraGo.transform.LookAt(new Vector3(0f, 0.75f, -0.8f));
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.8f, 0.88f);
            camera.fieldOfView = 52f;

            var sun = new GameObject("Directional Light");
            sun.transform.SetParent(root);
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
            RenderSettings.ambientLight = new Color(0.2f, 0.25f, 0.32f);
        }

        private static void AddVideoCapture()
        {
            new GameObject("SmokeVideoCapture").AddComponent<SmokeVideoCapture>();
        }

        private static Vector3 P(float x, float y, float z)
        {
            return new Vector3(x * ClassroomScale, y, z * ClassroomScale);
        }

        private static GameObject PlaceClassroomPiece(
            string fileName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            float targetMaxDimension)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ClassroomRoot}/{fileName}");
            if (prefab == null)
            {
                Debug.LogWarning($"HideAndSeekDesignBuilder: missing classroom asset {fileName}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Classroom {Path.GetFileNameWithoutExtension(fileName)}";
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            FitToMaxDimension(instance, targetMaxDimension);
            return instance;
        }

        private static void ConfigureStaticSchoolroom(GameObject classroom)
        {
            if (classroom == null)
            {
                return;
            }

            SetStaticRecursive(classroom);
            var body = classroom.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = classroom.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
        }

        private static int AddStaticSchoolroomColliders(Transform parent)
        {
            var collisionRoot = new GameObject("Schoolroom Static Rigidbody Colliders");
            collisionRoot.transform.SetParent(parent);
            collisionRoot.transform.position = Vector3.zero;
            collisionRoot.isStatic = true;
            var body = collisionRoot.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;

            AddStaticBox(collisionRoot.transform, "Floor", new Vector3(0f, -0.08f, 0f), new Vector3(10.7f, 0.12f, 7.1f));
            AddStaticBox(collisionRoot.transform, "Back Wall", new Vector3(0f, 0.9f, 3.35f), new Vector3(10.7f, 1.9f, 0.22f));
            AddStaticBox(collisionRoot.transform, "Front Wall", new Vector3(0f, 0.9f, -3.35f), new Vector3(10.7f, 1.9f, 0.22f));
            AddStaticBox(collisionRoot.transform, "Left Wall", new Vector3(-5.35f, 0.9f, 0f), new Vector3(0.22f, 1.9f, 7.1f));
            AddStaticBox(collisionRoot.transform, "Right Wall", new Vector3(5.35f, 0.9f, 0f), new Vector3(0.22f, 1.9f, 7.1f));
            return 5;
        }

        private static void AddStaticBox(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject($"Static Collision {name}");
            go.transform.SetParent(parent);
            go.transform.position = center;
            go.isStatic = true;
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
            collider.material = MiniBotLowFrictionContactMaterial();
            AddVisibleColliderBox(go.transform, name, size);
        }

        private static bool ConfigureActiveFurniture(GameObject furniture, float mass, float drag)
        {
            if (furniture == null)
            {
                return false;
            }

            SetNonStaticRecursive(furniture);
            var collider = AddFittedBoxCollider(furniture, new Vector3(0.08f, 0.06f, 0.08f));
            collider.material = FurnitureCollisionMaterial();
            var body = furniture.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = furniture.AddComponent<Rigidbody>();
            }

            body.isKinematic = false;
            body.useGravity = false;
            body.detectCollisions = true;
            body.mass = mass;
            body.drag = drag;
            body.angularDrag = 7f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
            return true;
        }

        private static bool ConfigureSolidFurnitureObstacle(GameObject furniture)
        {
            if (furniture == null)
            {
                return false;
            }

            var collider = AddFittedBoxCollider(furniture, new Vector3(0.08f, 0.06f, 0.08f));
            collider.material = FurnitureCollisionMaterial();
            var body = furniture.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = furniture.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
            body.mass = 40f;
            body.drag = 12f;
            return true;
        }

        private static void ConfigureMiniBotPhysics(GameObject bot)
        {
            SetNonStaticRecursive(bot);
            var body = bot.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = bot.AddComponent<Rigidbody>();
            }

            body.isKinematic = false;
            body.useGravity = false;
            body.detectCollisions = true;
            body.mass = 2.5f;
            body.drag = 0.65f;
            body.angularDrag = 8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.maxDepenetrationVelocity = 1.8f;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            var capsule = bot.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = bot.AddComponent<CapsuleCollider>();
            }

            capsule.center = new Vector3(0f, 0.46f, 0f);
            capsule.height = 0.92f;
            capsule.radius = 0.22f;
            capsule.material = MiniBotLowFrictionContactMaterial();

            if (bot.GetComponent<MiniBotWallContactTracker>() == null)
            {
                bot.AddComponent<MiniBotWallContactTracker>();
            }
        }

        private static void ConfigureAnimatorLocomotion(GameObject bot)
        {
            foreach (var sampler in bot.GetComponents<MiniBotWalkAnimator>())
            {
                sampler.enabled = false;
            }

            var animator = bot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = bot.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(LocomotionControllerPath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }

            var driver = bot.GetComponent<AgentLocomotionDriver>();
            if (driver == null)
            {
                driver = bot.AddComponent<AgentLocomotionDriver>();
            }

            driver.ResetTracking();
        }

        private static BoxCollider AddFittedBoxCollider(GameObject go, Vector3 padding)
        {
            if (!TryGetBounds(go, out var bounds))
            {
                var emptyCollider = go.GetComponent<BoxCollider>();
                if (emptyCollider == null)
                {
                    emptyCollider = go.AddComponent<BoxCollider>();
                }

                return emptyCollider;
            }

            var scale = go.transform.lossyScale;
            var collider = go.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = go.AddComponent<BoxCollider>();
            }

            collider.center = go.transform.InverseTransformPoint(bounds.center);
            collider.size = new Vector3(
                bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)) + padding.x,
                bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)) + padding.y,
                bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)) + padding.z);
            return collider;
        }

        private static void SetStaticRecursive(GameObject go)
        {
            go.isStatic = true;
            foreach (Transform child in go.transform)
            {
                SetStaticRecursive(child.gameObject);
            }
        }

        private static void SetNonStaticRecursive(GameObject go)
        {
            go.isStatic = false;
            foreach (Transform child in go.transform)
            {
                SetNonStaticRecursive(child.gameObject);
            }
        }

        private static void RemoveColliders(GameObject go)
        {
            foreach (var collider in go.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void AddVisibleColliderBox(Transform collisionBox, string name, Vector3 size)
        {
            var display = GameObject.CreatePrimitive(PrimitiveType.Cube);
            display.name = $"Visible WallsFloor Collider {name}";
            display.transform.SetParent(collisionBox, false);
            display.transform.localPosition = Vector3.zero;
            display.transform.localRotation = Quaternion.identity;
            display.transform.localScale = size;
            display.isStatic = true;
            RemoveColliders(display);

            var renderer = display.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = WallsFloorColliderDisplayMaterial();
            }
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

            return label;
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

        private static PhysicMaterial FurnitureCollisionMaterial()
        {
            const string dir = "Assets/Project/Materials";
            Directory.CreateDirectory(dir);
            const string path = "Assets/Project/Materials/FurnitureCollision.physicMaterial";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path);
            if (existing != null)
            {
                existing.staticFriction = 0.18f;
                existing.dynamicFriction = 0.08f;
                existing.bounciness = 0f;
                existing.frictionCombine = PhysicMaterialCombine.Minimum;
                existing.bounceCombine = PhysicMaterialCombine.Minimum;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new PhysicMaterial("FurnitureCollision")
            {
                staticFriction = 0.18f,
                dynamicFriction = 0.08f,
                bounciness = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static PhysicMaterial MiniBotLowFrictionContactMaterial()
        {
            const string dir = "Assets/Project/Materials";
            Directory.CreateDirectory(dir);
            const string path = "Assets/Project/Materials/MiniBotLowFrictionContact.physicMaterial";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path);
            if (existing != null)
            {
                existing.staticFriction = 0f;
                existing.dynamicFriction = 0f;
                existing.bounciness = 0f;
                existing.frictionCombine = PhysicMaterialCombine.Minimum;
                existing.bounceCombine = PhysicMaterialCombine.Minimum;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new PhysicMaterial("MiniBotLowFrictionContact")
            {
                staticFriction = 0f,
                dynamicFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material WallsFloorColliderDisplayMaterial()
        {
            const string dir = "Assets/Project/Materials";
            Directory.CreateDirectory(dir);
            const string path = "Assets/Project/Materials/WallsFloorColliderDisplay.mat";
            var color = new Color(0.1f, 0.78f, 1f, 0.22f);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                UrpMaterialFactory.ApplyColor(existing, color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = UrpMaterialFactory.CreateTransparent(color);
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

        private static void FitToHeight(GameObject go, float targetHeight)
        {
            if (!TryGetBounds(go, out var bounds) || bounds.size.y <= 0.0001f)
            {
                return;
            }

            go.transform.localScale *= targetHeight / bounds.size.y;
        }

        private static void FitToMaxDimension(GameObject go, float targetMaxDimension)
        {
            if (!TryGetBounds(go, out var bounds))
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

        private static void LogMiniBotRigInfo()
        {
            var importer = AssetImporter.GetAtPath(MiniBotPath) as ModelImporter;
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(MiniBotPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MiniBotPath);
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var leftFoot = FindChild(probe.transform, "LeftFoot") != null;
            var rightFoot = FindChild(probe.transform, "RightFoot") != null;
            var leftLeg = FindChild(probe.transform, "LeftLeg") != null;
            var rightLeg = FindChild(probe.transform, "RightLeg") != null;
            UnityEngine.Object.DestroyImmediate(probe);

            Debug.Log(
                "HideAndSeekDesignBuilder: mini-bot import " +
                $"animationType={importer?.animationType}, importAnimation={importer?.importAnimation}, " +
                $"avatarValid={avatar != null && avatar.isValid}, avatarHuman={avatar != null && avatar.isHuman}, " +
                $"walkingBones leftFoot={leftFoot}, rightFoot={rightFoot}, leftLeg={leftLeg}, rightLeg={rightLeg}.");
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

        private readonly struct TurnAnimationImport
        {
            public TurnAnimationImport(string sourcePath, string fileName, string role, string humanoidClipName)
            {
                SourcePath = sourcePath;
                FileName = fileName;
                Role = role;
                HumanoidClipName = humanoidClipName;
            }

            public string SourcePath { get; }
            public string FileName { get; }
            public string Role { get; }
            public string HumanoidClipName { get; }
        }

        private readonly struct HumanoidAnimationImport
        {
            public HumanoidAnimationImport(
                string sourcePath,
                string targetRoot,
                string fileName,
                string clipName,
                string role,
                bool loop,
                bool activeLocomotion,
                bool mirror = false)
            {
                SourcePath = sourcePath;
                TargetRoot = targetRoot;
                FileName = fileName;
                ClipName = clipName;
                Role = role;
                Loop = loop;
                ActiveLocomotion = activeLocomotion;
                Mirror = mirror;
            }

            public string SourcePath { get; }
            public string TargetRoot { get; }
            public string FileName { get; }
            public string ClipName { get; }
            public string Role { get; }
            public bool Loop { get; }
            public bool ActiveLocomotion { get; }
            public bool Mirror { get; }
            public string AssetPath => $"{TargetRoot}/{FileName}";
        }
    }
}
