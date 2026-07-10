using System.Collections.Generic;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Discovers minibots in the scene and ensures they have EmployeeInteractable + MoodDot.
    /// ponytail: runtime annotation avoids fragile prefab/scene YAML edits.
    /// </summary>
    public sealed class EmployeeRegistry : MonoBehaviour
    {
        [SerializeField] private string[] employeeTags = new[] { "Employee", "Minibot", "Robot" };
        [SerializeField] private Sprite dotSprite;

        public void SetDefaultSprite(Sprite sprite)
        {
            if (dotSprite == null && sprite != null)
            {
                dotSprite = sprite;
            }
        }

        private readonly Dictionary<string, MoodDot> dots = new Dictionary<string, MoodDot>();

        private void Start()
        {
            AnnotateMinibots();

            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver != null)
            {
                receiver.OnGameStateSync += HandleStateSync;
            }
        }

        private void OnDestroy()
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver != null)
            {
                receiver.OnGameStateSync -= HandleStateSync;
            }
        }

        private void HandleStateSync(Newtonsoft.Json.Linq.JObject payload)
        {
            var companies = payload.Value<Newtonsoft.Json.Linq.JArray>("companies");
            if (companies == null)
            {
                return;
            }

            foreach (var token in companies)
            {
                var company = token as Newtonsoft.Json.Linq.JObject;
                if (company == null)
                {
                    continue;
                }

                var employees = company.Value<Newtonsoft.Json.Linq.JArray>("employees");
                if (employees == null)
                {
                    continue;
                }

                foreach (var empToken in employees)
                {
                    var emp = empToken as Newtonsoft.Json.Linq.JObject;
                    if (emp == null)
                    {
                        continue;
                    }

                    var id = emp.Value<string>("employee_id");
                    var mood = emp.Value<int?>("mood");
                    if (!string.IsNullOrWhiteSpace(id) && mood.HasValue)
                    {
                        UpdateMood(id, mood.Value);
                    }
                }
            }
        }

        private void AnnotateMinibots()
        {
            var candidates = FindObjectsOfType<Transform>();
            var index = 0;
            foreach (var t in candidates)
            {
                if (t == null) continue;
                var nameLower = t.name.ToLowerInvariant();
                var isCandidate = false;
                foreach (var tag in employeeTags)
                {
                    if (nameLower.Contains(tag.ToLowerInvariant()))
                    {
                        isCandidate = true;
                        break;
                    }
                }

                if (!isCandidate)
                {
                    continue;
                }

                var id = $"employee_{index++}";
                var interactable = t.GetComponent<EmployeeInteractable>();
                if (interactable == null)
                {
                    try
                    {
                        interactable = t.gameObject.AddComponent<EmployeeInteractable>();
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogWarning($"EmployeeRegistry: cannot add interactable to {t.name}: {exception.Message}");
                        continue;
                    }
                }

                interactable.SetEmployeeId(id);

                var collider = t.GetComponentInChildren<Collider>();
                if (collider == null)
                {
                    try
                    {
                        var box = t.gameObject.AddComponent<BoxCollider>();
                        box.size = new Vector3(0.5f, 1f, 0.5f);
                        box.center = Vector3.up * 0.5f;
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogWarning($"EmployeeRegistry: cannot add collider to {t.name}: {exception.Message}");
                    }
                }

                var dot = t.GetComponentInChildren<MoodDot>();
                if (dot == null)
                {
                    var dotGo = new GameObject("MoodDot");
                    dotGo.transform.SetParent(t, false);
                    dotGo.transform.localPosition = Vector3.up * 1.2f;
                    var sr = dotGo.AddComponent<SpriteRenderer>();
                    if (dotSprite != null)
                    {
                        sr.sprite = dotSprite;
                    }

                    sr.transform.localScale = Vector3.one * 0.15f;
                    dot = dotGo.AddComponent<MoodDot>();
                }

                dot.SetEmployeeId(id);
                dots[id] = dot;
            }

            Debug.Log($"EmployeeRegistry: annotated {dots.Count} minibots.");
        }

        public void UpdateMood(string employeeId, int mood)
        {
            if (dots.TryGetValue(employeeId, out var dot))
            {
                dot.UpdateMood(mood);
            }
        }

        public IReadOnlyDictionary<string, MoodDot> Dots => dots;
    }
}
