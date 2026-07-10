using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ArgusUnity.Bridge;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Central HUD coordinator. Owns panel references and routes bridge events to widgets.
    /// </summary>
    public sealed class HudManager : MonoBehaviour
    {
        public GameObject hudCanvas;
        public RectTransform topLeftPanel;
        public RectTransform topCenterPanel;
        public RectTransform topRightPanel;
        public RectTransform bottomCenterPanel;
        public RectTransform rightEdgePanel;
        public RectTransform centerOverlayPanel;

        public GameBridgeReceiver bridgeReceiver;

        public static HudManager Instance { get; private set; }

        public GameObject HudCanvas => hudCanvas;
        public RectTransform TopLeftPanel => topLeftPanel;
        public RectTransform TopCenterPanel => topCenterPanel;
        public RectTransform TopRightPanel => topRightPanel;
        public RectTransform BottomCenterPanel => bottomCenterPanel;
        public RectTransform RightEdgePanel => rightEdgePanel;
        public RectTransform CenterOverlayPanel => centerOverlayPanel;

        public string LocalPlayerId { get; set; } = "player_1";
        public event Action<string> OnEmployeeSelected;
        public event Action OnEmployeeDeselected;

        private string selectedEmployeeId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (hudCanvas == null)
            {
                Debug.LogWarning("HudManager: hudCanvas is not assigned. HUD will not render.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SelectEmployee(string employeeId)
        {
            selectedEmployeeId = employeeId;
            OnEmployeeSelected?.Invoke(employeeId);
        }

        public void DeselectEmployee()
        {
            selectedEmployeeId = null;
            OnEmployeeDeselected?.Invoke();
        }

        public string GetSelectedEmployeeId()
        {
            return selectedEmployeeId;
        }

        public GameBridgeReceiver GetBridgeReceiver()
        {
            return bridgeReceiver;
        }

        public void OpenDrawer(RectTransform drawer)
        {
            if (drawer == null)
            {
                return;
            }

            drawer.gameObject.SetActive(true);
            InputGate.IsMenuOpen = true;
        }

        public void CloseDrawer(RectTransform drawer)
        {
            if (drawer == null)
            {
                return;
            }

            drawer.gameObject.SetActive(false);
            InputGate.IsMenuOpen = AnyDrawerOpen();
        }

        private bool AnyDrawerOpen()
        {
            if (rightEdgePanel != null && rightEdgePanel.gameObject.activeSelf)
            {
                return true;
            }

            if (centerOverlayPanel != null && centerOverlayPanel.gameObject.activeSelf)
            {
                return true;
            }

            return false;
        }
    }
}
