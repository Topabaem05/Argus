using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArgusUnity.Scene;

// Rebuilds each PushableObject's root BoxCollider so it tightly encloses every
// renderer mesh under the object, using each renderer's LOCAL bounds transformed
// into the object's local space. This avoids the inflation that happens when
// converting world AABBs back through a rotated transform.
public static class RebuildPropCollidersEditor
{
    [MenuItem("Tools/Argus/Rebuild Pushable Prop Colliders")]
    public static void Rebuild()
    {
        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded
            || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MiniBotRunAround")
        {
            EditorSceneManager.OpenScene("Assets/Project/Scenes/MiniBotRunAround.unity");
        }

        var sb = new StringBuilder();
        var count = 0;
        foreach (var pushable in Object.FindObjectsOfType<PushableObject>())
        {
            var go = pushable.gameObject;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                sb.AppendLine($"{go.name}: no renderers, skipped");
                continue;
            }

            var localMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var localMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var r in renderers)
            {
                if (r == null) continue;
                // localBounds is axis-aligned in the renderer's OWN local space.
                var lb = r.localBounds;
                lb.Expand(0.02f);
                // Transform the 8 local-space corners of this renderer into the
                // object root's local space via world space.
                foreach (var lc in Bounds8Corners(lb))
                {
                    var wc = r.transform.TransformPoint(lc);
                    var rootLocal = go.transform.InverseTransformPoint(wc);
                    localMin = Vector3.Min(localMin, rootLocal);
                    localMax = Vector3.Max(localMax, rootLocal);
                }
            }

            var localCenter = (localMin + localMax) * 0.5f;
            var localSize = localMax - localMin;

            var box = go.GetComponent<BoxCollider>();
            if (box == null)
                box = go.AddComponent<BoxCollider>();
            box.center = localCenter;
            box.size = localSize;
            box.isTrigger = false;

            sb.AppendLine($"{go.name}: renderers={renderers.Length} -> center={box.center} size={box.size}");
            count++;
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "argus_colliders_rebuild.txt");
        System.IO.File.WriteAllText(path, sb.ToString());
        Debug.Log($"[ArgusRebuild] rebuilt {count} colliders; wrote {path} bytes={sb.Length}");
        EditorApplication.Exit(0);
    }

    private static Vector3[] Bounds8Corners(Bounds b)
    {
        var c = b.center;
        var e = b.extents;
        return new[]
        {
            c + new Vector3(-e.x, -e.y, -e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z),
            c + new Vector3( e.x,  e.y, -e.z),
            c + new Vector3( e.x,  e.y,  e.z),
            c + new Vector3(-e.x,  e.y,  e.z),
        };
    }
}
