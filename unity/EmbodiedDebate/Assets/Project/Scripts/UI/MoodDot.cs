using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// World-space mood dot above a minibot. Hidden when off-screen.
    /// ponytail: simple sprite renderer, no world-space canvas overhead.
    /// </summary>
    public sealed class MoodDot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer dotRenderer;
        [SerializeField] private string employeeId;

        public string EmployeeId => employeeId;

        private void Start()
        {
            if (dotRenderer == null)
            {
                dotRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (dotRenderer != null)
            {
                dotRenderer.enabled = false;
            }
        }

        public void SetEmployeeId(string id)
        {
            employeeId = id;
        }

        public void UpdateMood(int mood)
        {
            if (dotRenderer == null)
            {
                return;
            }

            dotRenderer.color = UITheme.MoodColor(mood);
            dotRenderer.enabled = true;
        }

        private void OnBecameVisible()
        {
            if (dotRenderer != null)
            {
                dotRenderer.enabled = true;
            }
        }

        private void OnBecameInvisible()
        {
            if (dotRenderer != null)
            {
                dotRenderer.enabled = false;
            }
        }
    }
}
