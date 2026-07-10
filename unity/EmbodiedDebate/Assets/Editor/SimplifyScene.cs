using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ArgusUnity.Editor
{
    public static class SimplifyScene
    {
        private static readonly System.Collections.Generic.HashSet<string> KeepNames = new()
        {
            "Mini-bot Social Simulation Lab",
            "Soft Simulation Floor",
            "Main Camera",
            "Directional Light",
        };

        [MenuItem("Argus/Simplify Scene (Single Character + Floor)")]
        public static void Simplify()
        {
            var scene = SceneManager.GetActiveScene();
            var toRemove = new System.Collections.Generic.List<GameObject>();
            var kept = new System.Collections.Generic.List<string>();
            var firstRobotKept = false;

            Debug.Log("SimplifyScene: root objects = " + string.Join(", ", System.Linq.Enumerable.Select(scene.GetRootGameObjects(), go => go.name)));

            foreach (var root in scene.GetRootGameObjects())
            {
                var isRobot = root.name.IndexOf("mini-bot", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (isRobot && firstRobotKept)
                {
                    toRemove.Add(root);
                    continue;
                }
                if (isRobot)
                {
                    firstRobotKept = true;
                    kept.Add(root.name);
                    continue;
                }
                if (KeepNames.Contains(root.name) || HasKeepChildRecursive(root))
                {
                    kept.Add(root.name);
                    continue;
                }
                toRemove.Add(root);
            }

            foreach (var go in toRemove)
            {
                Object.DestroyImmediate(go);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"SimplifyScene: removed {toRemove.Count} objects, kept: {string.Join(", ", kept)}");
        }

        private static bool HasKeepChildRecursive(GameObject go)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                if (KeepNames.Contains(child.name) || HasKeepChildRecursive(child))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
