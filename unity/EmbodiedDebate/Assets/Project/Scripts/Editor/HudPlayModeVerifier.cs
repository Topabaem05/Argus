#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArgusUnity.Editor
{
    public static class HudPlayModeVerifier
    {
        private const string ScenePath = "Assets/Project/Scenes/AICompanyGame.unity";

        [MenuItem("Argus/Verify HUD")]
        public static void Run()
        {
            EditorApplication.update += OnUpdate;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static double enteredAt = -1;

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                if (enteredAt < 0) return;
                EditorApplication.update -= OnUpdate;
                EditorApplication.Exit(0);
                return;
            }

            if (enteredAt < 0)
            {
                enteredAt = EditorApplication.timeSinceStartup;
            }

            if (EditorApplication.timeSinceStartup - enteredAt > 6)
            {
                EditorApplication.ExitPlaymode();
            }
        }
    }
}
#endif
