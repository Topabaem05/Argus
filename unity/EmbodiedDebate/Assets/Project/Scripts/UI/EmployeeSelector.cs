using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Detects clicks on employee avatars and routes selection through HudManager.
    /// ponytail: single raycast path; camera drag is gated by InputGate.
    /// </summary>
    public sealed class EmployeeSelector : MonoBehaviour
    {
        [SerializeField] private LayerMask employeeLayers = ~0;

        private void Update()
        {
            if (!InputGate.AllowGameplayInput)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TrySelectAt(Input.mousePosition);
            }
        }

        private void TrySelectAt(Vector3 screenPosition)
        {
            var ray = Camera.main.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, employeeLayers))
            {
                return;
            }

            var interactable = hit.collider.GetComponentInParent<EmployeeInteractable>();
            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInChildren<EmployeeInteractable>();
            }

            if (interactable == null)
            {
                return;
            }

            HudManager.Instance?.SelectEmployee(interactable.EmployeeId);
        }
    }
}
