using UnityEditor;
using UnityEngine;

public static class ApplyOpenAILook
{
    [MenuItem("Tools/Argus/Apply OpenAI Look (SDD)")]
    public static void Run()
    {
        AssignYesMyungjoFont();
        MigrateStandardMaterials();
        StandardizeUrpMaterialParams();

        AssetDatabase.SaveAssets();
        Debug.Log("[ApplyOpenAILook] done");
        EditorApplication.Exit(0);
    }

    static void MigrateStandardMaterials()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpLit == null)
        {
            Debug.LogError("[ApplyOpenAILook] URP/Lit shader not found");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null)
                continue;

            var isStandard = mat.shader.name == "Standard";
            if (!isStandard)
                continue;

            var nameLower = mat.name.ToLowerInvariant();
            var isUi = nameLower.Contains("grid") || nameLower.Contains("background") || nameLower.Contains("black");
            mat.shader = isUi ? urpUnlit : urpLit;
            EditorUtility.SetDirty(mat);
            Debug.Log($"[ApplyOpenAILook] migrated {mat.name} -> {(isUi ? "URP/Unlit" : "URP/Lit")}");
        }
    }

    static void StandardizeUrpMaterialParams()
    {
        var guids = AssetDatabase.FindAssets("t:Material");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null)
                continue;

            if (mat.shader.name != "Universal Render Pipeline/Lit")
                continue;

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.45f);
            if (mat.HasProperty("_SpecularHighlights"))
                mat.SetFloat("_SpecularHighlights", 1f);
            if (mat.HasProperty("_EnvironmentReflections"))
                mat.SetFloat("_EnvironmentReflections", 0f);
            EditorUtility.SetDirty(mat);
        }
    }
    static void AssignYesMyungjoFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Project/Resources/Fonts/YesMyungjo-Bold.ttf");
        if (font == null)
        {
            Debug.LogWarning("[ApplyOpenAILook] YesMyungjo-Bold.ttf not found");
            return;
        }

        var count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Scene"))
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(guid);
            if (!scenePath.Contains("MiniBotRunAround"))
                continue;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            var textMeshes = Object.FindObjectsOfType<TextMesh>();
            foreach (var tm in textMeshes)
            {
                tm.font = font;
                tm.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                EditorUtility.SetDirty(tm);
                count++;
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[ApplyOpenAILook] assigned YesMyungjo to {count} TextMesh components");
    }
}
