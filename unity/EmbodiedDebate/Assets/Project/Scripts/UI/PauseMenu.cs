using UnityEngine;
using UnityEngine.UI;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Pause menu overlay with resume, scoreboard, rumor log, and quit buttons.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button scoreboardButton;
        [SerializeField] private Button rumorLogButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private GameObject rumorLogPanel;

        public void SetScoreboardPanel(GameObject panel)
        {
            scoreboardPanel = panel;
        }

        public void SetRumorLogPanel(GameObject panel)
        {
            rumorLogPanel = panel;
        }

        private void Start()
        {
            gameObject.SetActive(false);

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(Hide);
            }

            if (scoreboardButton != null)
            {
                scoreboardButton.onClick.AddListener(() => TogglePanel(scoreboardPanel));
            }

            if (rumorLogButton != null)
            {
                rumorLogButton.onClick.AddListener(() => TogglePanel(rumorLogPanel));
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(() => Application.Quit());
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (gameObject.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            InputGate.IsMenuOpen = true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
            if (rumorLogPanel != null) rumorLogPanel.SetActive(false);
            InputGate.IsMenuOpen = false;
        }

        private static void TogglePanel(GameObject panel)
        {
            if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
            }
        }
    }
}
