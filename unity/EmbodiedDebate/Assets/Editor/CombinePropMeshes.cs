using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArgusUnity.Scene;

// Combines every child MeshFilter under each PushableObject root into the root's
// single MeshFilter/MeshRenderer, then removes the child meshes. Keeps the root
// collider + rigidbody. Fixes "model appears as separate parts" caused by GLB
// full-hierarchy import (parent mesh + child meshes at different origins).
public static class CombinePropMeshes
{
    [MenuItem("Tools/Argus/Combine Pushable Prop Meshes")]
    public static void CombineAll()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MiniBotRunAround")
            EditorSceneManager.OpenScene("Assets/Project/Scenes/MiniBotRunAround.unity");

        var sb = new StringBuilder();
        var count = 0;
        foreach (var pushable in Object.FindObjectsOfType<PushableObject>())
        {
            if (CombineOne(pushable.gameObject, sb))
                count++;
        }
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        var p = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "argus_combine.txt");
        System.IO.File.WriteAllText(p, sb.ToString());
        Debug.Log($"[ArgusCombine] combined {count} props; wrote {p} bytes={sb.Length}");
        EditorApplication.Exit(0);
    }

    static bool CombineOne(GameObject root, StringBuilder sb)
    {
        var childFilters = new List<MeshFilter>(root.GetComponentsInChildren<MeshFilter>(true));
        // keep only true children, not the root itself
        childFilters.RemoveAll(mf => mf.transform == root.transform);
        if (childFilters.Count == 0)
        {
            sb.AppendLine($"{root.name}: no child meshes, skipped");
            return false;
        }

        var rootFilter = root.GetComponent<MeshFilter>();
        var rootRenderer = root.GetComponent<MeshRenderer>();
        if (rootFilter == null) rootFilter = root.AddComponent<MeshFilter>();
        if (rootRenderer == null) rootRenderer = root.AddComponent<MeshRenderer>();

        var rootMat = rootRenderer.sharedMaterial;
        var combineList = new List<CombineInstance>();

        // include the root mesh first if present
        if (rootFilter.sharedMesh != null)
        {
            combineList.Add(new CombineInstance
            {
                mesh = rootFilter.sharedMesh,
                transform = root.transform.worldToLocalMatrix * root.transform.localToWorldMatrix
            });
        }

        int included = combineList.Count;
        foreach (var mf in childFilters)
        {
            if (mf.sharedMesh == null) continue;
            combineList.Add(new CombineInstance
            {
                mesh = mf.sharedMesh,
                transform = root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix
            });
            included++;
        }

        if (included == 0)
        {
            sb.AppendLine($"{root.name}: nothing to combine");
            return false;
        }

        var merged = new Mesh { name = root.name + "_combined" };
        merged.CombineMeshes(combineList.ToArray(), true, true, false);
        // Recalculate bounds so the renderer culls correctly.
        merged.RecalculateBounds();

        var fallback = Shader.Find("Universal Render Pipeline/Lit");
        if (rootMat == null && fallback != null)
            rootMat = new Material(fallback);

        rootFilter.sharedMesh = merged;
        rootRenderer.sharedMaterial = rootMat;

        // remove child mesh gameobjects (keep the transform hierarchy empty)
        foreach (var mf in childFilters)
        {
            if (mf == null) continue;
            // keep the child object only if it carries a collider/rigidbody; it won't.
            Undo.DestroyObjectImmediate(mf.gameObject);
        }

        sb.AppendLine($"{root.name}: combined {included} meshes into one (verts={merged.vertexCount})");
        return true;
    }
}
