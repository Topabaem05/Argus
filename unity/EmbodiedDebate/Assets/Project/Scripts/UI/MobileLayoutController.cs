using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Swaps desktop and mobile HUD variants based on viewport width.
    /// ponytail: threshold-based layout swap; no duplicated canvases.
    /// </summary>
    public sealed class MobileLayoutController : MonoBehaviour
    {
        [SerializeField] private RectTransform bottomCenterPanel;
        [SerializeField] private RectTransform rightEdgePanel;
        [SerializeField] private RectTransform topLeftPanel;
        [SerializeField] private RectTransform topCenterPanel;
        [SerializeField] private RectTransform topRightPanel;
        [SerializeField] private float mobileThresholdWidth = 900f;

        private bool wasMobile;

        public void SetPanels(RectTransform topLeft, RectTransform topCenter, RectTransform topRight, RectTransform bottomCenter, RectTransform rightEdge)
        {
            topLeftPanel = topLeft;
            topCenterPanel = topCenter;
            topRightPanel = topRight;
            bottomCenterPanel = bottomCenter;
            rightEdgePanel = rightEdge;
        }

        private void Update()
        {
            var isMobile = Screen.width < mobileThresholdWidth;
            if (isMobile == wasMobile)
            {
                return;
            }

            wasMobile = isMobile;
            ApplyLayout(isMobile);
        }

        private void ApplyLayout(bool isMobile)
        {
            if (bottomCenterPanel != null)
            {
                var bottom = bottomCenterPanel;
                bottom.anchorMin = isMobile ? new Vector2(0, 0) : new Vector2(0.35f, 0);
                bottom.anchorMax = isMobile ? new Vector2(1, 0.22f) : new Vector2(0.65f, 0.12f);
                bottom.anchoredPosition = Vector2.zero;
                bottom.sizeDelta = Vector2.zero;
            }

            if (rightEdgePanel != null)
            {
                var right = rightEdgePanel;
                right.anchorMin = isMobile ? Vector2.zero : new Vector2(1, 0);
                right.anchorMax = isMobile ? Vector2.one : new Vector2(1, 1);
                right.anchoredPosition = isMobile ? Vector2.zero : new Vector2(-right.sizeDelta.x * 0.5f, 0);
                right.pivot = isMobile ? new Vector2(0.5f, 0.5f) : new Vector2(1, 0.5f);
            }

            if (topLeftPanel != null)
            {
                var panel = topLeftPanel;
                panel.gameObject.SetActive(true);
            }

            if (topCenterPanel != null)
            {
                var panel = topCenterPanel;
                panel.gameObject.SetActive(!isMobile);
            }

            if (topRightPanel != null)
            {
                var panel = topRightPanel;
                panel.gameObject.SetActive(!isMobile);
            }
        }
    }
}
