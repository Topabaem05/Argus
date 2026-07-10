using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Modal list for choosing a target employee, player, or task before sending a command.
    /// </summary>
    public sealed class TargetPicker : MonoBehaviour
    {
        private Text titleText => transform.Find("Title")?.GetComponent<Text>();
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject itemPrefab;

        private string currentAction;
        private string sourceEmployeeId;
        Action<string, string, string> onSelected;

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void Open(string action, string sourceEmployeeId)
        {
            currentAction = action;
            this.sourceEmployeeId = sourceEmployeeId;
            gameObject.SetActive(true);
            InputGate.IsMenuOpen = true;

            if (titleText != null)
            {
                titleText.text = $"Choose target for {action}";
                titleText.color = UITheme.TextPrimary;
                titleText.fontSize = UITheme.FontL;
            }

            BuildItems(action);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            InputGate.IsMenuOpen = false;
        }

        private void BuildItems(string action)
        {
            if (itemContainer == null)
            {
                return;
            }

            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }

            var items = GetItems(action);
            foreach (var item in items)
            {
                var go = Instantiate(itemPrefab, itemContainer);
                var image = go.GetComponent<Image>();
                if (image != null)
                {
                    image.color = UITheme.BgSecondary;
                }

                var label = go.GetComponentInChildren<Text>();
                var button = go.GetComponent<Button>();
                if (label != null)
                {
                    label.text = item.label;
                    label.color = UITheme.TextPrimary;
                    label.fontSize = UITheme.FontS;
                }

                var value = item.value;
                if (button != null)
                {
                    button.onClick.AddListener(() => SelectTarget(value));
                }
            }

            var cancelGo = Instantiate(itemPrefab, itemContainer);
            var cancelImage = cancelGo.GetComponent<Image>();
            if (cancelImage != null)
            {
                cancelImage.color = UITheme.BgTertiary;
            }

            var cancelLabel = cancelGo.GetComponentInChildren<Text>();
            var cancelButton = cancelGo.GetComponent<Button>();
            if (cancelLabel != null)
            {
                cancelLabel.text = "Cancel";
                cancelLabel.color = UITheme.TextSecondary;
                cancelLabel.fontSize = UITheme.FontS;
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Close);
            }
        }

        private List<(string label, string value)> GetItems(string action)
        {
            // ponytail: placeholder data; real data should come from state sync cache.
            if (action == "gossip" || action == "scout")
            {
                return new List<(string, string)>
                {
                    ("Rival A", "player_2"),
                    ("Rival B", "player_3"),
                    ("Rival C", "player_4")
                };
            }

            if (action == "assign_task")
            {
                return new List<(string, string)>
                {
                    ("Task #1", "task_1"),
                    ("Task #2", "task_2"),
                    ("Task #3", "task_3")
                };
            }

            return new List<(string, string)>();
        }

        private void SelectTarget(string value)
        {
            var bar = FindObjectOfType<CommandActionBar>();
            if (bar != null)
            {
                string targetPlayerId = null;
                string taskId = null;

                if (currentAction == "gossip" || currentAction == "scout")
                {
                    targetPlayerId = value;
                }
                else if (currentAction == "assign_task")
                {
                    taskId = value;
                }

                bar.SendCommand(currentAction, sourceEmployeeId, targetPlayerId, taskId, null);
            }

            Close();
        }
    }
}
