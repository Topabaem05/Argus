using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArgusUnity.Editor
{
    public static class HudBuildVerifier
    {
        [MenuItem("Argus/HUD Build Verifier")]
        public static void Run()
        {
            const string outputDir = "/Users/guribbong/code/Argus/.omo/evidence";
            Directory.CreateDirectory(outputDir);
            var logPath = Path.Combine(outputDir, "hud-build-verify.log");

            var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/AICompanyGame.unity", OpenSceneMode.Single);
            var root = scene.GetRootGameObjects();
            int bootstrapCount = 0;
            int receiverCount = 0;
            foreach (var go in root)
            {
                if (go.GetComponent<ArgusUnity.UI.HudBootstrap>() != null) bootstrapCount++;
                if (go.GetComponent<ArgusUnity.Bridge.GameBridgeReceiver>() != null) receiverCount++;
            }

            File.WriteAllText(logPath,
                $"Scene={scene.name}\n" +
                $"RootObjects={root.Length}\n" +
                $"HudBootstrap={bootstrapCount}\n" +
                $"GameBridgeReceiver={receiverCount}\n" +
                $"Path={scene.path}\n");

            Debug.Log($"[HudBuildVerifier] HudBootstrap={bootstrapCount}, GameBridgeReceiver={receiverCount}");
        }
    }
}
