using ArgusUnity.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NormalizePushablePropRoots
{
    public static void Run()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MiniBotRunAround")
            EditorSceneManager.OpenScene("Assets/Project/Scenes/MiniBotRunAround.unity");

        var propsRoot = GameObject.Find("Classroom Props");
        if (propsRoot == null)
        {
            Debug.LogError("[NormalizePushablePropRoots] Classroom Props not found");
            EditorApplication.Exit(1);
            return;
        }

        foreach (Transform child in propsRoot.transform)
            Normalize(child.gameObject);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[NormalizePushablePropRoots] normalized pushable prop roots");
        EditorApplication.Exit(0);
    }

    private static void Normalize(GameObject root)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(root))
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        if (root.GetComponent<MeshRenderer>() == null)
            root.AddComponent<MeshRenderer>();
        if (root.GetComponent<MeshFilter>() == null)
            root.AddComponent<MeshFilter>();
        if (root.GetComponent<BoxCollider>() == null)
            root.AddComponent<BoxCollider>();
        if (root.GetComponent<Rigidbody>() == null)
            root.AddComponent<Rigidbody>();
        if (root.GetComponent<PushableObject>() == null)
            root.AddComponent<PushableObject>();

        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb.gameObject != root)
                Object.DestroyImmediate(rb);
        }

        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (collider.gameObject != root)
                Object.DestroyImmediate(collider);
        }
    }
}
