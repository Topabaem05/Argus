#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArgusUnity.Motion;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ArgusUnity.Editor
{
    public static class MixamoMotionControllerBuilder
    {
        public const string RawRoot = "Assets/Project/Resources/Animations/Mixamo/Raw";
        public const string GeneratedRoot = "Assets/Project/Resources/Animations/Mixamo/Generated";
        public const string ControllerPath = GeneratedRoot + "/MiniBotDiverseMixamo.controller";
        public const string UpperBodyMaskPath = GeneratedRoot + "/MiniBotUpperBody.mask";
        public const string ControllerResource = "Animations/Mixamo/Generated/MiniBotDiverseMixamo";

        private const string DefaultLocalMotionFolder = "/Users/guribbong/Downloads/motion";
        private const string ReportPath = "reports/unity_dumps/mixamo_controller_build.json";
        private const string LocomotionBlendTreeName = "LocomotionBlendTree";

        private static readonly AnimatorParameterSpec[] RequiredParameters =
        {
            Float("Speed"),
            Float("MoveX"),
            Float("MoveZ"),
            Float("Turn"),
            Float("AngularError"),
            Float("AngularSpeed"),
            Bool("IsMoving"),
            Bool("IsGrounded"),
            Bool("Grounded"),
            Bool("IsTalking"),
            Bool("IsStuck"),
            Int("Emotion"),
            Int("Gesture"),
            Int("Action"),
            Int("MotionIntent"),
            Float("OverlayWeight"),
            Float("GestureWeight"),
            Int("RecoveryState"),
            Trigger("InteractionTrigger"),
            Trigger("TalkTrigger"),
            Trigger("EmotionTrigger"),
            Trigger("RecoveryTrigger"),
            Trigger("DodgeTrigger"),
            Trigger("FallTrigger"),
            Trigger("GetUpTrigger"),
            Trigger("ImpactTrigger"),
            Trigger("StepBackTrigger")
        };

        public static IReadOnlyList<AnimatorParameterSpec> RequiredAnimatorParameters => RequiredParameters;

        [MenuItem("Argus/Motion/Build Local Diverse Mixamo Setup")]
        public static void BuildLocalDiverseMixamoSetup()
        {
            ImportFromLocalMotionFolder(ResolveLocalMotionFolder(), overwriteExisting: false);
            AssetDatabase.Refresh();
            var report = BuildDiverseMixamoController();
            WriteReport(report);
            if (!report.Success)
            {
                throw new InvalidOperationException(
                    "Diverse Mixamo controller build failed. Missing clips: " +
                    string.Join(", ", report.MissingClipNames));
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Argus/Motion/Build Diverse Mixamo Controller From Raw")]
        public static void BuildDiverseMixamoControllerMenu()
        {
            AssetDatabase.Refresh();
            var report = BuildDiverseMixamoController();
            WriteReport(report);
            if (!report.Success)
            {
                Debug.LogError(
                    "MixamoMotionControllerBuilder: missing clips " +
                    string.Join(", ", report.MissingClipNames));
            }
        }

        public static MixamoControllerBuildReport BuildDiverseMixamoController()
        {
            Directory.CreateDirectory(GeneratedRoot);
            var clipMap = LoadCatalogClips(out var missing);
            var report = new MixamoControllerBuildReport
            {
                CatalogClipCount = MotionCatalog.All.Count,
                AvailableClipCount = clipMap.Count,
                MissingClipNames = missing.ToArray()
            };

            if (missing.Count > 0)
            {
                return report;
            }

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            if (AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath) != null)
            {
                AssetDatabase.DeleteAsset(UpperBodyMaskPath);
            }

            var upperBodyMask = CreateUpperBodyMask();
            AssetDatabase.CreateAsset(upperBodyMask, UpperBodyMaskPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AddParameters(controller);
            ConfigureBaseLayer(controller, clipMap);
            AddOverlayLayer(controller, "Upper Body Overlay", upperBodyMask, clipMap);
            AddOverlayLayer(controller, "Emotion Overlay", upperBodyMask, clipMap);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            report.ControllerPath = ControllerPath;
            report.ControllerStateCount = CountStates(controller);
            report.LayerCount = controller.layers.Length;
            report.Success = true;
            Debug.Log(
                "MixamoMotionControllerBuilder: generated diverse controller " +
                $"path={ControllerPath}, clips={clipMap.Count}, states={report.ControllerStateCount}.");
            return report;
        }

        public static MixamoClipAvailability ScanRawClipAvailability()
        {
            var missing = new List<string>();
            var present = 0;
            foreach (var definition in MotionCatalog.All)
            {
                if (File.Exists(ToProjectAbsolutePath(RawAssetPath(definition))))
                {
                    present++;
                }
                else
                {
                    missing.Add(definition.ClipName);
                }
            }

            return new MixamoClipAvailability(MotionCatalog.All.Count, present, missing.ToArray());
        }

        public static string RawAssetPath(MotionClipDefinition definition)
        {
            return $"{RawRoot}/{definition.ClipName}.fbx";
        }

        private static void ImportFromLocalMotionFolder(string sourceFolder, bool overwriteExisting)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                Debug.LogWarning($"MixamoMotionControllerBuilder: source folder missing {sourceFolder}");
                return;
            }

            Directory.CreateDirectory(RawRoot);
            var copied = 0;
            foreach (var definition in MotionCatalog.All)
            {
                var destination = ToProjectAbsolutePath(RawAssetPath(definition));
                if (!overwriteExisting && File.Exists(destination))
                {
                    continue;
                }

                var source = ResolveSourceFile(sourceFolder, definition.ClipName);
                if (source == null)
                {
                    continue;
                }

                File.Copy(source, destination, overwrite: true);
                copied++;
            }

            if (copied > 0)
            {
                AssetDatabase.Refresh();
                foreach (var definition in MotionCatalog.All)
                {
                    AssetDatabase.ImportAsset(RawAssetPath(definition), ImportAssetOptions.ForceUpdate);
                }
            }

            Debug.Log($"MixamoMotionControllerBuilder: imported {copied} local Mixamo FBX files.");
        }

        private static Dictionary<MotionClipId, AnimationClip> LoadCatalogClips(out List<string> missing)
        {
            missing = new List<string>();
            var clipMap = new Dictionary<MotionClipId, AnimationClip>();
            foreach (var definition in MotionCatalog.All)
            {
                var clip = LoadAnimationClip(RawAssetPath(definition), definition.ClipName);
                if (clip == null)
                {
                    missing.Add(definition.ClipName);
                    continue;
                }

                clipMap[definition.ClipId] = clip;
            }

            return clipMap;
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

                if (string.Equals(clip.name, preferredName, StringComparison.Ordinal))
                {
                    return clip;
                }

                fallback ??= clip;
            }

            return fallback;
        }

        private static void AddParameters(AnimatorController controller)
        {
            for (var i = 0; i < RequiredParameters.Length; i++)
            {
                controller.AddParameter(RequiredParameters[i].Name, RequiredParameters[i].Type);
            }
        }

        private static void ConfigureBaseLayer(
            AnimatorController controller,
            Dictionary<MotionClipId, AnimationClip> clipMap)
        {
            var layer = controller.layers[0];
            layer.name = "Base Layer";
            layer.defaultWeight = 1f;
            controller.layers = ReplaceLayer(controller.layers, 0, layer);

            var stateMachine = controller.layers[0].stateMachine;
            stateMachine.name = "Base Layer";
            AddAllNamedStates(stateMachine, clipMap, new Vector3(280f, 60f, 0f));
            AddLocomotionBlendTree(controller, stateMachine, clipMap);

            if (clipMap.TryGetValue(MotionClipId.StandingIdle, out _))
            {
                stateMachine.defaultState = FindState(stateMachine, "Standing Idle");
            }
        }

        private static void AddOverlayLayer(
            AnimatorController controller,
            string layerName,
            AvatarMask avatarMask,
            Dictionary<MotionClipId, AnimationClip> clipMap)
        {
            var stateMachine = new AnimatorStateMachine { name = layerName };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);
            var layer = new AnimatorControllerLayer
            {
                name = layerName,
                stateMachine = stateMachine,
                avatarMask = avatarMask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 0f
            };
            controller.AddLayer(layer);

            var empty = stateMachine.AddState("Empty", new Vector3(20f, 20f, 0f));
            empty.writeDefaultValues = true;
            stateMachine.defaultState = empty;
            AddAllNamedStates(stateMachine, clipMap, new Vector3(280f, 60f, 0f));
        }

        private static void AddAllNamedStates(
            AnimatorStateMachine stateMachine,
            Dictionary<MotionClipId, AnimationClip> clipMap,
            Vector3 startPosition)
        {
            var index = 0;
            foreach (var definition in MotionCatalog.All)
            {
                if (!clipMap.TryGetValue(definition.ClipId, out var clip))
                {
                    continue;
                }

                var state = stateMachine.AddState(
                    definition.ClipName,
                    startPosition + new Vector3((index % 4) * 220f, (index / 4) * 70f, 0f));
                state.motion = clip;
                state.writeDefaultValues = true;
                state.speed = 1f;
                index++;
            }
        }

        private static void AddLocomotionBlendTree(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            Dictionary<MotionClipId, AnimationClip> clipMap)
        {
            var state = stateMachine.AddState(LocomotionBlendTreeName, new Vector3(20f, 180f, 0f));
            var blendTree = new BlendTree
            {
                name = LocomotionBlendTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveZ",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);
            AddBlendChild(blendTree, clipMap, MotionClipId.Walking3, new Vector2(0f, 0.85f), 1f);
            AddBlendChild(blendTree, clipMap, MotionClipId.Running2, new Vector2(0f, 1.45f), 1f);
            AddBlendChild(blendTree, clipMap, MotionClipId.WalkingBackward, new Vector2(0f, -0.8f), 1f);
            AddBlendChild(blendTree, clipMap, MotionClipId.LeftStrafeWalking, new Vector2(-0.8f, 0.25f), 1f);
            AddBlendChild(blendTree, clipMap, MotionClipId.RightStrafeWalking, new Vector2(0.8f, 0.25f), 1f);
            state.motion = blendTree;
        }

        private static void AddBlendChild(
            BlendTree blendTree,
            Dictionary<MotionClipId, AnimationClip> clipMap,
            MotionClipId clipId,
            Vector2 position,
            float timeScale)
        {
            if (!clipMap.TryGetValue(clipId, out var clip))
            {
                return;
            }

            blendTree.AddChild(clip, position);
            var children = blendTree.children;
            children[children.Length - 1].timeScale = timeScale;
            blendTree.children = children;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null && childState.state.name == stateName)
                {
                    return childState.state;
                }
            }

            return null;
        }

        private static AvatarMask CreateUpperBodyMask()
        {
            var mask = new AvatarMask { name = "MiniBotUpperBody" };
            for (var i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            }

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            return mask;
        }

        private static AnimatorControllerLayer[] ReplaceLayer(
            AnimatorControllerLayer[] layers,
            int index,
            AnimatorControllerLayer layer)
        {
            layers[index] = layer;
            return layers;
        }

        private static int CountStates(AnimatorController controller)
        {
            var count = 0;
            foreach (var layer in controller.layers)
            {
                count += layer.stateMachine.states.Length;
            }

            return count;
        }

        private static string ResolveLocalMotionFolder()
        {
            var env = Environment.GetEnvironmentVariable("ARGUS_MIXAMO_SOURCE_DIR");
            return string.IsNullOrWhiteSpace(env) ? DefaultLocalMotionFolder : env;
        }

        private static string ResolveSourceFile(string sourceFolder, string clipName)
        {
            var direct = Path.Combine(sourceFolder, clipName + ".fbx");
            if (File.Exists(direct))
            {
                return direct;
            }

            var withoutSpaceAfterUnderscore = clipName.Replace("_ ", "_");
            var alternate = Path.Combine(sourceFolder, withoutSpaceAfterUnderscore + ".fbx");
            return File.Exists(alternate) ? alternate : null;
        }

        private static string ToProjectAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static void WriteReport(MixamoControllerBuildReport report)
        {
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            var path = Path.Combine(repoRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
            File.WriteAllText(path, JsonUtility.ToJson(report, prettyPrint: true));
        }

        private static AnimatorParameterSpec Float(string name)
        {
            return new AnimatorParameterSpec(name, AnimatorControllerParameterType.Float);
        }

        private static AnimatorParameterSpec Bool(string name)
        {
            return new AnimatorParameterSpec(name, AnimatorControllerParameterType.Bool);
        }

        private static AnimatorParameterSpec Int(string name)
        {
            return new AnimatorParameterSpec(name, AnimatorControllerParameterType.Int);
        }

        private static AnimatorParameterSpec Trigger(string name)
        {
            return new AnimatorParameterSpec(name, AnimatorControllerParameterType.Trigger);
        }
    }

    [Serializable]
    public readonly struct AnimatorParameterSpec
    {
        public AnimatorParameterSpec(string name, AnimatorControllerParameterType type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public AnimatorControllerParameterType Type { get; }
    }

    public readonly struct MixamoClipAvailability
    {
        public MixamoClipAvailability(int requiredCount, int presentCount, string[] missingClipNames)
        {
            RequiredCount = requiredCount;
            PresentCount = presentCount;
            MissingClipNames = missingClipNames ?? Array.Empty<string>();
        }

        public int RequiredCount { get; }

        public int PresentCount { get; }

        public string[] MissingClipNames { get; }

        public bool HasAllRequiredClips => MissingClipNames.Length == 0;
    }

    [Serializable]
    public sealed class MixamoControllerBuildReport
    {
        public bool Success;
        public string ControllerPath;
        public int CatalogClipCount;
        public int AvailableClipCount;
        public int LayerCount;
        public int ControllerStateCount;
        public string[] MissingClipNames = Array.Empty<string>();
    }
}
#endif
