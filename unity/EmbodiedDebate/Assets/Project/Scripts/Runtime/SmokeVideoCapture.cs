#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using ArgusUnity.UI;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class SmokeVideoCapture : MonoBehaviour
    {
        private const string EnvCapture = "ARGUS_UNITY_VIDEO_CAPTURE";
        private const string EnvDir = "ARGUS_UNITY_VIDEO_DIR";
        private const string EnvPrefix = "ARGUS_UNITY_VIDEO_PREFIX";
        private const string EnvFrameCount = "ARGUS_UNITY_VIDEO_FRAME_COUNT";
        private const string EnvSeconds = "ARGUS_UNITY_VIDEO_SECONDS";
        private const int Width = 1280;
        private const int Height = 720;
        private const int FrameRate = 30;
        private const int DefaultFrameCount = FrameRate * 8;

        private int frame;
        private int frameCount;
        private string outputDir;
        private string prefix;

        private void Awake()
        {
            if (System.Environment.GetEnvironmentVariable(EnvCapture) != "1")
            {
                Destroy(gameObject);
                return;
            }

            outputDir = ResolveOutputDir(System.Environment.GetEnvironmentVariable(EnvDir));
            prefix = System.Environment.GetEnvironmentVariable(EnvPrefix);
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "mini_bot_run";
            }

            Directory.CreateDirectory(outputDir);
            frameCount = ResolveFrameCount();
            Time.captureFramerate = FrameRate;
            Debug.Log($"ArgusVideo: capturing {frameCount} frames at {FrameRate} FPS to {outputDir}");
        }

        private void LateUpdate()
        {
            if (outputDir == null)
            {
                return;
            }

            var sampleTime = frame / (float)FrameRate;
            foreach (var scenario in FindObjectsOfType<MiniBotRunAroundScenario>())
            {
                scenario.ApplyAtTime(sampleTime);
            }

            foreach (var ui in FindObjectsOfType<MiniBotSocialUiController>())
            {
                ui.Refresh();
            }

            foreach (var embodiment in FindObjectsOfType<MinibotEmbodimentController>())
            {
                embodiment.Refresh();
            }

            foreach (var cameraRig in FindObjectsOfType<MinibotConversationCameraRig>())
            {
                cameraRig.RefreshImmediate();
            }

            CaptureMainCameraToPng(Path.Combine(outputDir, $"{prefix}_{frame:D04}.png"));
            frame++;

            if (frame < frameCount)
            {
                return;
            }

            Debug.Log($"ArgusVideo: wrote {frame} frames to {outputDir}");
            Time.captureFramerate = 0;

#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
#else
            Application.Quit(0);
#endif
            Destroy(gameObject);
        }

        private static int ResolveFrameCount()
        {
            var frameCountValue = System.Environment.GetEnvironmentVariable(EnvFrameCount);
            if (int.TryParse(frameCountValue, out var explicitFrameCount))
            {
                return Mathf.Max(1, explicitFrameCount);
            }

            var secondsValue = System.Environment.GetEnvironmentVariable(EnvSeconds);
            if (float.TryParse(secondsValue, out var seconds))
            {
                return Mathf.Max(1, Mathf.CeilToInt(seconds * FrameRate));
            }

            return DefaultFrameCount;
        }

        private static string ResolveOutputDir(string envPath)
        {
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                return Path.GetFullPath(envPath);
            }

            var projectDir = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));
            return Path.Combine(repoRoot, "tmp", "mini_bot_run_frames");
        }

        private static void CaptureMainCameraToPng(string path)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("ArgusVideo: no Camera.main - cannot capture.");
                return;
            }

            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.Default);
            RenderTexture.active = rt;
            cam.targetTexture = rt;
            cam.Render();

            try
            {
                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
                Object.Destroy(tex);
            }
            finally
            {
                cam.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
