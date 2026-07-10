using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Marks a minibot as selectable by the HUD employee selector.
    /// </summary>
    public sealed class EmployeeInteractable : MonoBehaviour
    {
        [SerializeField] private string employeeId;
        [SerializeField] private string displayName;

        public string EmployeeId => employeeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? employeeId : displayName;

        public void SetEmployeeId(string id)
        {
            employeeId = id;
        }
    }
}
