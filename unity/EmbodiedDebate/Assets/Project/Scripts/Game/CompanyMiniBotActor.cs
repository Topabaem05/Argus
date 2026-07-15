using System;
using UnityEngine;

namespace ArgusUnity.Game
{
    public enum CompanyActorRole
    {
        Player,
        Employee
    }

    /// <summary>
    /// Stable game identity and presentation state attached to every player/employee minibot.
    /// Server identifiers live here so scene object names never become part of the network protocol.
    /// </summary>
    public sealed class CompanyMiniBotActor : MonoBehaviour
    {
        [SerializeField] private string actorId = string.Empty;
        [SerializeField] private string companyId = string.Empty;
        [SerializeField] private string displayName = "Minibot";
        [SerializeField] private CompanyActorRole role = CompanyActorRole.Employee;
        [SerializeField, Range(0, 100)] private int mood = 50;
        [SerializeField, Range(-100, 100)] private int loyalty;
        [SerializeField] private string currentTask = "Idle";

        public static CompanyMiniBotActor Selected { get; private set; }
        public static event Action<CompanyMiniBotActor> SelectionChanged;

        public string ActorId => actorId;
        public string CompanyId => companyId;
        public string DisplayName => displayName;
        public CompanyActorRole Role => role;
        public int Mood => mood;
        public int Loyalty => loyalty;
        public string CurrentTask => currentTask;
        public bool IsSelected => Selected == this;

        private void Awake()
        {
            EnsureSelectionCollider();
        }

        private void OnValidate()
        {
            mood = Mathf.Clamp(mood, 0, 100);
            loyalty = Mathf.Clamp(loyalty, -100, 100);
        }

        private void OnMouseDown()
        {
            Select();
        }

        private void OnDestroy()
        {
            if (Selected != this)
            {
                return;
            }

            Selected = null;
            SelectionChanged?.Invoke(null);
        }

        public void Configure(
            string newActorId,
            string newCompanyId,
            string newDisplayName,
            CompanyActorRole newRole)
        {
            actorId = string.IsNullOrWhiteSpace(newActorId) ? name : newActorId;
            companyId = newCompanyId ?? string.Empty;
            displayName = string.IsNullOrWhiteSpace(newDisplayName) ? actorId : newDisplayName;
            role = newRole;
        }

        public void ApplySnapshot(int newMood, int newLoyalty, string newTask)
        {
            mood = Mathf.Clamp(newMood, 0, 100);
            loyalty = Mathf.Clamp(newLoyalty, -100, 100);
            currentTask = string.IsNullOrWhiteSpace(newTask) ? "Idle" : newTask;
        }

        public void SetCurrentTask(string task)
        {
            currentTask = string.IsNullOrWhiteSpace(task) ? "Idle" : task;
        }

        public void Select()
        {
            if (Selected == this)
            {
                return;
            }

            Selected = this;
            SelectionChanged?.Invoke(this);
        }

        public static void ClearSelection()
        {
            if (Selected == null)
            {
                return;
            }

            Selected = null;
            SelectionChanged?.Invoke(null);
        }

        private void EnsureSelectionCollider()
        {
            // OnMouseDown is delivered to scripts on the collider's GameObject. A collider that
            // exists only on a model child is therefore not sufficient for this root actor.
            if (GetComponent<Collider>() != null)
            {
                return;
            }

            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.75f, 0f);
            capsule.height = 1.5f;
            capsule.radius = 0.45f;
        }
    }
}
