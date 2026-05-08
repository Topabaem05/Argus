using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArgusUnity.Editor
{
    public static class UrpProjectConfigurator
    {
        private const string SettingsDir = "Assets/Project/Settings";
        private const string PipelineAssetPath = SettingsDir + "/ArgusUniversalRenderPipeline.asset";
        private const string RendererAssetPath = SettingsDir + "/ArgusUniversalRenderer.asset";

        private static readonly string[] RendererDataTypeNames =
        {
            "UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime",
            "UnityEngine.Rendering.Universal.ForwardRendererData, Unity.RenderPipelines.Universal.Runtime",
        };

        [MenuItem("Argus/Configure URP Project")]
        public static void Configure()
        {
            var pipeline = EnsurePipelineAsset();
            ConfigurePipelineAsset(pipeline);
            GraphicsSettings.renderPipelineAsset = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log($"UrpProjectConfigurator: assigned {PipelineAssetPath}.");
        }

        private static RenderPipelineAsset EnsurePipelineAsset()
        {
            EnsureSettingsFolder();
            var existing = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelineAssetPath);
            if (existing != null)
            {
                ConfigurePipelineAsset(existing);
                return existing;
            }

            var urpAssetType = ResolveType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            var rendererDataType = ResolveFirstType(RendererDataTypeNames);
            if (urpAssetType == null || rendererDataType == null)
            {
                throw new InvalidOperationException(
                    "URP package is not available. Resolve com.unity.render-pipelines.universal before running this configurator.");
            }

            var rendererData = ScriptableObject.CreateInstance(rendererDataType);
            rendererData.name = "ArgusUniversalRenderer";
            AssetDatabase.CreateAsset(rendererData, RendererAssetPath);

            var pipelineAsset = ScriptableObject.CreateInstance(urpAssetType);
            pipelineAsset.name = "ArgusUniversalRenderPipeline";
            AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
            AttachRendererData(pipelineAsset, rendererData);
            ConfigurePipelineAsset(pipelineAsset);

            return (RenderPipelineAsset)pipelineAsset;
        }

        private static void AttachRendererData(UnityEngine.Object pipelineAsset, UnityEngine.Object rendererData)
        {
            var serialized = new SerializedObject(pipelineAsset);
            var rendererList = serialized.FindProperty("m_RendererDataList");
            if (rendererList != null && rendererList.isArray)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }

            var defaultRendererIndex = serialized.FindProperty("m_DefaultRendererIndex");
            if (defaultRendererIndex != null)
            {
                defaultRendererIndex.intValue = 0;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipelineAsset);
            EditorUtility.SetDirty(rendererData);
        }

        private static void ConfigurePipelineAsset(UnityEngine.Object pipelineAsset)
        {
            var serialized = new SerializedObject(pipelineAsset);
            SetBool(serialized, "m_MainLightShadowsSupported", true);
            SetBool(serialized, "m_SoftShadowsSupported", true);
            SetInt(serialized, "m_MainLightShadowmapResolution", 2048);
            SetFloat(serialized, "m_ShadowDistance", 55f);
            SetInt(serialized, "m_ShadowCascadeCount", 2);
            SetFloat(serialized, "m_CascadeBorder", 0.18f);
            SetInt(serialized, "m_SoftShadowQuality", 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipelineAsset);
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder(SettingsDir))
            {
                AssetDatabase.CreateFolder("Assets/Project", "Settings");
            }
        }

        private static Type ResolveFirstType(string[] names)
        {
            for (var i = 0; i < names.Length; i++)
            {
                var type = ResolveType(names[i]);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Type ResolveType(string assemblyQualifiedName)
        {
            return Type.GetType(assemblyQualifiedName, false);
        }
    }
}
