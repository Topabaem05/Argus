using System.Collections.Generic;
using ArgusUnity.Bridge;
using ArgusUnity.Runtime;
using ArgusUnity.Scene;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Non-graphic centroid overlay showing conflict summaries; fades on <c>resolved</c>/<c>none</c>.
    /// </summary>
    public sealed class ConflictVisualizationManager : MonoBehaviour
    {
        [SerializeField]
        private float centroidYOffset = 2.05f;

        [SerializeField]
        private float fadeSeconds = 2.4f;

        [SerializeField]
        private int maxSummaryChars = 180;

        private SimulationSceneOrchestrator orchestrator;
        private AgentSpawnHandler spawnHandler;
        private readonly Dictionary<string, ActiveConflictRecord> active =
            new Dictionary<string, ActiveConflictRecord>();

        public void Initialize(SimulationSceneOrchestrator orch, AgentSpawnHandler spawn)
        {
            if (orchestrator != null)
            {
                orchestrator.ConflictUpdate -= OnConflictUpdate;
            }

            orchestrator = orch;
            spawnHandler = spawn;

            if (orchestrator != null)
            {
                orchestrator.ConflictUpdate += OnConflictUpdate;
            }
        }

        private void OnDestroy()
        {
            if (orchestrator != null)
            {
                orchestrator.ConflictUpdate -= OnConflictUpdate;
            }

            ClearAll();
        }

        private void Update()
        {
            if (active.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var remove = new List<string>();
            foreach (var kv in active)
            {
                var ac = kv.Value;
                if (ac.Fading)
                {
                    var t = (now - ac.FadeStartedAtUnscaled) / fadeSeconds;
                    var alpha = Mathf.Lerp(ac.StartAlpha, 0f, Mathf.Clamp01(t));
                    ApplyPanelAlpha(ac, alpha);
                    if (t >= 1f - Mathf.Epsilon)
                    {
                        remove.Add(kv.Key);
                    }
                }
            }

            for (var i = 0; i < remove.Count; i++)
            {
                Discard(remove[i]);
            }
        }

        private void OnConflictUpdate(BridgeEnvelope envelope)
        {
            if (spawnHandler == null || !BridgePresentationParsing.TryGetConflictUpdate(envelope, out var vm))
            {
                return;
            }

            if (!TryComputeCentroid(vm.ParticipantIds, out var center))
            {
                return;
            }

            center.y += centroidYOffset;

            if (!active.TryGetValue(vm.ConflictId, out var ac) || ac.Root == null)
            {
                ac = BuildPanel(vm.ConflictId, center);
                active[vm.ConflictId] = ac;
            }
            else
            {
                ac.Root.transform.position = center;
            }

            ac.Stage = vm.Stage;
            ac.StartAlpha = Mathf.Lerp(0.45f, 0.95f, vm.Intensity);
            var cue = ConflictCueStyle.ForStage(vm.Stage, vm.Intensity);
            var summary = BridgePresentationParsing.TruncateDialogue(vm.PublicSummary, maxSummaryChars);
            ac.Summary.text = $"{StagePrefix(vm.Stage)}{summary}";
            ac.Summary.color = cue.TextColor;

            if (ac.QuadRenderer != null && ac.QuadRenderer.material != null)
            {
                var fill = cue.PanelColor;
                fill.a = Mathf.Lerp(0.35f, 0.72f, vm.Intensity);
                ac.QuadRenderer.material.color = fill;
            }

            if (ShouldBeginFade(vm.Stage))
            {
                if (!ac.Fading)
                {
                    ac.Fading = true;
                    ac.FadeStartedAtUnscaled = Time.unscaledTime;
                }
            }
            else
            {
                ac.Fading = false;
                ApplyPanelAlpha(ac, ac.StartAlpha);
            }
        }

        private bool TryComputeCentroid(string[] participantIds, out Vector3 center)
        {
            center = Vector3.zero;
            if (participantIds == null || participantIds.Length == 0 || spawnHandler == null)
            {
                return false;
            }

            var sum = Vector3.zero;
            var n = 0;
            for (var i = 0; i < participantIds.Length; i++)
            {
                if (spawnHandler.TryGetAgentTransform(participantIds[i], out var t) && t != null)
                {
                    sum += t.position;
                    n++;
                }
            }

            if (n == 0)
            {
                return false;
            }

            center = sum / n;
            return true;
        }

        private ActiveConflictRecord BuildPanel(string conflictId, Vector3 position)
        {
            var root = new GameObject($"ConflictHint_{conflictId}");
            root.transform.SetParent(null, true);
            root.transform.position = position;

            var holder = new GameObject("Billboard");
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = Vector3.zero;
            holder.AddComponent<BillboardToCamera>();

            var backing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backing.name = "ConflictPanel";
            backing.transform.SetParent(holder.transform, false);
            backing.transform.localPosition = Vector3.zero;
            backing.transform.localScale = new Vector3(3.6f, 0.75f, 1f);
            var col = backing.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var quadRenderer = backing.GetComponent<MeshRenderer>();
            if (quadRenderer != null)
            {
                quadRenderer.material = UrpMaterialFactory.CreateTransparent(new Color(0.12f, 0.12f, 0.14f, 0.62f));
            }

            var textGo = new GameObject("Summary");
            textGo.transform.SetParent(holder.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.characterSize = 0.036f;
            tm.fontSize = 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            tm.text = string.Empty;

            return new ActiveConflictRecord
            {
                Root = root,
                QuadRenderer = quadRenderer,
                Summary = tm,
                Stage = string.Empty,
                StartAlpha = 0.82f,
                Fading = false,
            };
        }

        private static bool ShouldBeginFade(string stageLower)
        {
            return stageLower == "resolved" || stageLower == "none";
        }

        private static string StagePrefix(string stageLower)
        {
            switch (stageLower)
            {
                case "tension":
                    return "(tension)\n";
                case "argument":
                    return "(disagreement)\n";
                case "physical_risk":
                    return "(risk)\n";
                case "deescalating":
                    return "(calming)\n";
                case "resolved":
                    return "(resolved)\n";
                case "none":
                    return "(clear)\n";
                default:
                    return string.Empty;
            }
        }

        private static void ApplyPanelAlpha(ActiveConflictRecord ac, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (ac.Summary != null)
            {
                var c = ac.Summary.color;
                c.a = alpha;
                ac.Summary.color = c;
            }

            if (ac.QuadRenderer != null && ac.QuadRenderer.material != null)
            {
                var c = ac.QuadRenderer.material.color;
                var baseAlpha = Mathf.Max(0.15f, ac.StartAlpha);
                c.a = baseAlpha * alpha;
                ac.QuadRenderer.material.color = c;
            }
        }

        private void Discard(string conflictId)
        {
            if (active.TryGetValue(conflictId, out var ac) && ac.Root != null)
            {
                Destroy(ac.Root);
            }

            active.Remove(conflictId);
        }

        private void ClearAll()
        {
            foreach (var kv in active)
            {
                if (kv.Value.Root != null)
                {
                    Destroy(kv.Value.Root);
                }
            }

            active.Clear();
        }

        private sealed class ActiveConflictRecord
        {
            public GameObject Root;

            public MeshRenderer QuadRenderer;

            public TextMesh Summary;

            public string Stage;

            public float StartAlpha;

            public bool Fading;

            public float FadeStartedAtUnscaled;
        }

        private sealed class ConflictCueStyle
        {
            private ConflictCueStyle(Color panel, Color text)
            {
                PanelColor = panel;
                TextColor = text;
            }

            public Color PanelColor { get; }

            public Color TextColor { get; }

            public static ConflictCueStyle ForStage(string stageLower, float intensity)
            {
                var warm = Mathf.Lerp(0.45f, 1f, intensity);
                switch (stageLower)
                {
                    case "tension":
                        return new ConflictCueStyle(
                            new Color(0.93f * warm, 0.9f, 0.45f + 0.2f * warm, 1f),
                            new Color(0.12f, 0.11f, 0.06f));
                    case "argument":
                        return new ConflictCueStyle(
                            new Color(0.96f * warm, 0.62f + 0.2f * warm, 0.28f * warm + 0.08f * (1 - warm),
                                1f),
                            new Color(0.15f, 0.05f, 0.03f));
                    case "physical_risk":
                        return new ConflictCueStyle(
                            new Color(1f * warm, 0.48f + 0.08f * warm, 0.35f + 0.07f * (1 - warm), 1f),
                            Color.white);
                    case "deescalating":
                        return new ConflictCueStyle(
                            new Color(0.42f + 0.12f * warm, 0.68f + 0.06f * warm, 0.62f + 0.06f * warm, 1f),
                            new Color(0.04f, 0.1f, 0.06f));
                    case "resolved":
                    case "none":
                        return new ConflictCueStyle(
                            new Color(0.72f + 0.05f * warm, 0.76f + 0.06f * warm, 0.78f + 0.06f * warm,
                                1f),
                            new Color(0.08f, 0.1f, 0.09f));
                    default:
                        return new ConflictCueStyle(
                            new Color(0.82f + 0.05f * warm, 0.84f + 0.06f * warm, 0.88f + 0.06f * warm,
                                1f),
                            Color.white);
                }
            }
        }

    }
}
