using ArgusUnity.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MovePushablesToVisualRoots
{
    [MenuItem("Tools/Argus/Move Pushables To Visual Roots")]
    public static void Run()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MiniBotRunAround")
            EditorSceneManager.OpenScene("Assets/Project/Scenes/MiniBotRunAround.unity");

        var propsRoot = GameObject.Find("Classroom Props");
        if (propsRoot == null)
        {
            Debug.LogError("[MovePushablesToVisualRoots] Classroom Props not found");
            EditorApplication.Exit(1);
            return;
        }

        foreach (Transform child in propsRoot.transform)
            MoveToVisualRoot(child.gameObject, propsRoot.transform);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MovePushablesToVisualRoots] done");
        EditorApplication.Exit(0);
    }

    private static void MoveToVisualRoot(GameObject root, Transform propsRoot)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(root))
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var visual = FindBestVisualRoot(root);
        if (visual == null)
            return;

        if (visual == root)
        {
            EnsureRootPhysics(root);
            return;
        }

        var name = root.name;
        var worldPosition = visual.transform.position;
        var worldRotation = visual.transform.rotation;
        var worldScale = visual.transform.lossyScale;

        visual.transform.SetParent(propsRoot, true);
        visual.name = name;
        visual.transform.position = worldPosition;
        visual.transform.rotation = worldRotation;
        SetWorldScale(visual.transform, worldScale);

        EnsureRootPhysics(visual);
        Object.DestroyImmediate(root);
    }

    private static GameObject FindBestVisualRoot(GameObject root)
    {
        if (root.GetComponent<Renderer>() != null)
            return root;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return null;

        return renderers[0].gameObject;
    }

    private static void EnsureRootPhysics(GameObject root)
    {
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

        if (root.GetComponent<BoxCollider>() == null)
            root.AddComponent<BoxCollider>();
        if (root.GetComponent<Rigidbody>() == null)
            root.AddComponent<Rigidbody>();
        if (root.GetComponent<PushableObject>() == null)
            root.AddComponent<PushableObject>();
    }

    private static void SetWorldScale(Transform transform, Vector3 worldScale)
    {
        var parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z)
        );
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) < 0.0001f ? value : value / divisor;
    }
}
