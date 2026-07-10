using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Adjusts the root RectTransform to respect Screen.safeArea on mobile devices.
    /// </summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (rectTransform == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            lastSafeArea = safeArea;

            var screenSize = new Vector2(Screen.width, Screen.height);
            var anchorMin = safeArea.position / screenSize;
            var anchorMax = (safeArea.position + safeArea.size) / screenSize;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }
}
