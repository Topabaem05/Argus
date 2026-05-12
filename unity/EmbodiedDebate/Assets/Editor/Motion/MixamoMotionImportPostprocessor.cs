#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

namespace ArgusUnity.Editor
{
    public sealed class MixamoMotionImportPostprocessor : AssetPostprocessor
    {
        private const string MixamoRoot = "Assets/Project/Resources/Animations/Mixamo/Raw";

        private static readonly string[] LoopClips =
        {
            "Standing Idle",
            "Breathing Idle",
            "Idle-2",
            "Sad Idle",
            "Thinking-2",
            "Walking-3",
            "Running-2",
            "Charge",
            "Walking Backward",
            "Left Strafe Walking",
            "Right Strafe Walking",
            "Walk Backward Arc Left",
            "Walk Backward Arc Right"
        };

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(MixamoRoot, StringComparison.Ordinal))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.optimizeGameObjects = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.motionNodeName = string.Empty;
        }

        private void OnPostprocessModel(UnityEngine.GameObject model)
        {
            if (!assetPath.StartsWith(MixamoRoot, StringComparison.Ordinal))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.clipAnimations;
            }

            for (var i = 0; i < clips.Length; i++)
            {
                clips[i].name = fileName;
                clips[i].loopTime = ShouldLoop(fileName);
                clips[i].loopPose = ShouldLoop(fileName);
                clips[i].keepOriginalOrientation = false;
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
                clips[i].lockRootPositionXZ = true;
            }

            importer.clipAnimations = clips;
        }

        private static bool ShouldLoop(string fileName)
        {
            for (var i = 0; i < LoopClips.Length; i++)
            {
                if (string.Equals(fileName, LoopClips[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
