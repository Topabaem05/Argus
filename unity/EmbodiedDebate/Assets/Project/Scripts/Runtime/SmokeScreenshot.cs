#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// When <c>ARGUS_UNITY_BRIDGE_CAPTURE=1</c> is set in the Unity process before Play Mode,
    /// captures a fullscreen screenshot after delay then exits Edit Mode / batch runner.
    /// </summary>
    public sealed class SmokeScreenshot : MonoBehaviour
    {
        private const string EnvCapture = "ARGUS_UNITY_BRIDGE_CAPTURE";
        private const int FramesToWait = 1200;

        private int _frames;
        private string _resolvedPath;

        private void Awake()
        {
            if (!ResolveCaptureRequested(out _resolvedPath))
            {
                Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            if (_resolvedPath == null)
            {
                return;
            }

            _frames++;

            // Game view must render frames and receive bridge traffic before capturing.
            if (_frames <= FramesToWait)
            {
                return;
            }

            Debug.Log($"ArgusSmoke: capturing {_resolvedPath} after {_frames} frames.");

            CaptureMainCameraToPng(_resolvedPath);

#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
#else
            Application.Quit(0);
#endif
            Destroy(gameObject);
        }

        private static bool ResolveCaptureRequested(out string path)
        {
            path = null;
            if (System.Environment.GetEnvironmentVariable(EnvCapture) != "1")
            {
                return false;
            }

            path = ResolveOutputPath(System.Environment.GetEnvironmentVariable("ARGUS_UNITY_CAPTURE_PATH"));

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

            return true;
        }

        private static string ResolveOutputPath(string envPath)
        {
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                return Path.GetFullPath(envPath);
            }

            var projectDir = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));

            return Path.Combine(repoRoot, "tmp", "unity_main_simulation.png");
        }

        /// <summary>
        /// Rasterize <see cref="Camera.main"/> instead of relying on the optional Screen Capture module,
        /// so batch/play captures always work once a camera renders the scene.
        /// </summary>
        private static void CaptureMainCameraToPng(string path, int width = 1280, int height = 720)
        {
            var cam = Camera.main;

            if (cam == null)
            {
                Debug.LogError("ArgusSmoke: no Camera.main — cannot capture.");
                return;
            }

            var prevTarget = cam.targetTexture;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default);
            RenderTexture.active = rt;
            cam.targetTexture = rt;
            cam.Render();

            try
            {
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
                Debug.Log($"ArgusSmoke: wrote {width}x{height} PNG to {path}");
                Object.Destroy(tex);
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.ReleaseTemporary(rt);
                RenderTexture.active = null;
            }
        }
    }
}
