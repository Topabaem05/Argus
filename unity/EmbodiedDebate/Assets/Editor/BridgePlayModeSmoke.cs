using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArgusUnity.Editor
{
    /// <summary>
    /// Loads MainSimulation and enters Play Mode. <see cref="ArgusUnity.Runtime.SmokeScreenshot"/>
    /// reads <c>ARGUS_UNITY_BRIDGE_CAPTURE=1</c>, renders the scene, waits in-game frames,
    /// then writes <c>tmp/unity_main_simulation.png</c> under the repo and exits Unity.
    /// </summary>
    public static class BridgePlayModeSmoke
    {
        private const string ScenePath = "Assets/Project/Scenes/MainSimulation.unity";

        /// <summary>
        /// Invoke with <c>-executeMethod ArgusUnity.Editor.BridgePlayModeSmoke.CaptureAndQuit</c>.
        /// Omit <c>-quit</c> so Play Mode runs; Unity exits via <c>SmokeScreenshot</c>.
        /// </summary>
        public static void CaptureAndQuit()
        {
            Environment.SetEnvironmentVariable("ARGUS_UNITY_BRIDGE_CAPTURE", "1");
            var screenshotPath = ResolveOutputPath();
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath) ?? ".");

            Debug.Log($"ArgusUnity.Editor.BridgePlayModeSmoke: capture path={screenshotPath}");

            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static string ResolveOutputPath()
        {
            var env = Environment.GetEnvironmentVariable("ARGUS_UNITY_CAPTURE_PATH");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return Path.GetFullPath(env);
            }

            var projectDir = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));

            return Path.Combine(repoRoot, "tmp", "unity_main_simulation.png");
        }
    }
}
