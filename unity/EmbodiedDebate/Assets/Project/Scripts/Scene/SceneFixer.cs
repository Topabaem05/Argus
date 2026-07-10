using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArgusUnity.Scene
{
    /// <summary>
    /// Runtime patch for GLTF-imported props and generated minibots.
    /// ponytail: self-creating singleton; no scene edits required.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class SceneFixer : MonoBehaviour
    {
        [SerializeField] private Material fallbackPropMaterial;
        [SerializeField] private Material fallbackBotMaterial;

        private static readonly string[] RotatedPrefixes = new[]
        {
            "Computer_Retro", "Table01", "Table02", "Bench_01", "Lamp01",
            "Shelf_01", "Building_", "Wall_01", "Floor_01", "Window_01",
            "Room", "Door_01", "Styloo", "classroom", "principal office"
        };

        private static readonly string[] ShrinkPrefixes = new[] { "Tree01" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            var go = new GameObject("SceneFixer");
            go.AddComponent<SceneFixer>();
            SceneManager.sceneLoaded += (_, __) => { };
        }

        private void Awake()
        {
            FixImportedProps();
            FixMinibots();
        }

        private void FixImportedProps()
        {
            var all = FindObjectsOfType<Transform>(true);
            foreach (var t in all)
            {
                if (t == null)
                {
                    continue;
                }

                if (MatchesAny(t.name, RotatedPrefixes))
                {
                    t.localRotation = Quaternion.Euler(90f, t.localRotation.eulerAngles.y, t.localRotation.eulerAngles.z);
                    ApplyScaleClamp(t, 0.3f, 2.0f);
                    ApplyFallbackMaterial(t);
                }
                else if (MatchesAny(t.name, ShrinkPrefixes))
                {
                    t.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                }
            }
        }

        private void FixMinibots()
        {
            var bots = FindObjectsOfType<ArgusUnity.Robots.CuteRobotPrefabInitializer>();
            foreach (var bot in bots)
            {
                if (bot == null)
                {
                    continue;
                }

                var t = bot.transform;
                t.localScale = new Vector3(0.9f, 0.9f, 0.9f);

                if (t.localPosition.y < 0.02f)
                {
                    t.localPosition = new Vector3(t.localPosition.x, 0.02f, t.localPosition.z);
                }

                foreach (var renderer in t.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.material == null)
                    {
                        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    }

                    if (fallbackBotMaterial != null)
                    {
                        renderer.material = fallbackBotMaterial;
                    }
                    else
                    {
                        renderer.material.color = new Color(0.52f, 0.74f, 0.92f);
                    }
                }
            }
        }

        private static bool MatchesAny(string name, string[] prefixes)
        {
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyScaleClamp(Transform t, float min, float max)
        {
            var s = t.localScale;
            s.x = Mathf.Clamp(s.x, min, max);
            s.y = Mathf.Clamp(s.y, min, max);
            s.z = Mathf.Clamp(s.z, min, max);
            t.localScale = s;
        }

        private void ApplyFallbackMaterial(Transform t)
        {
            foreach (var renderer in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (fallbackPropMaterial != null)
                {
                    renderer.material = fallbackPropMaterial;
                }
                else if (renderer.material == null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        color = new Color(0.75f, 0.85f, 0.95f)
                    };
                }
            }
        }
    }
}
