using UnityEngine;

namespace ArgusUnity.Runtime
{
    public static class UrpMaterialFactory
    {
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        public const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";
        public const string LegacyStandardShaderName = "Standard";
        public const string LegacyTransparentShaderName = "Unlit/Transparent";

        public static readonly string[] PreferredLitShaderNames =
        {
            UrpLitShaderName,
            LegacyStandardShaderName,
        };

        public static readonly string[] PreferredTransparentShaderNames =
        {
            UrpUnlitShaderName,
            LegacyTransparentShaderName,
            LegacyStandardShaderName,
        };

        public static Material CreateLit(Color color)
        {
            var material = new Material(FindFirstAvailable(PreferredLitShaderNames));
            ApplyLit(material, color);
            return material;
        }

        public static Material CreateTransparent(Color color)
        {
            var material = new Material(FindFirstAvailable(PreferredTransparentShaderNames));
            ApplyColor(material, color);
            ConfigureTransparent(material);
            return material;
        }

        public static void ApplyColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        public static void ApplyLit(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            var shader = FindFirstAvailable(PreferredLitShaderNames);
            if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            ApplyColor(material, color);
        }

        private static Shader FindFirstAvailable(string[] names)
        {
            for (var i = 0; i < names.Length; i++)
            {
                var shader = Shader.Find(names[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return Shader.Find(LegacyStandardShaderName);
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
