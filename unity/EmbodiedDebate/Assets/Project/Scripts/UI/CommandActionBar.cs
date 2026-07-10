using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;
using ArgusUnity.Bridge;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Bottom action bar shown when an employee is selected.
    /// </summary>
    public sealed class CommandActionBar : MonoBehaviour
    {
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private TargetPicker targetPicker;

        private readonly string[] commands = new[]
        {
            "praise", "scold", "snack", "raise", "bonus", "party",
            "fire", "hire", "gossip", "scout", "assign_task"
        };

        private readonly HashSet<string> targetCommands = new HashSet<string>
        {
            "gossip", "scout", "assign_task"
        };

        private void Start()
        {
            if (buttonContainer == null || buttonPrefab == null)
            {
                return;
            }

            foreach (var command in commands)
            {
                var go = Instantiate(buttonPrefab, buttonContainer);
                var button = go.GetComponent<Button>();
                var label = go.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = command;
                }

                var captured = command;
                if (button != null)
                {
                    button.onClick.AddListener(() => OnCommandClicked(captured));
                }
            }

            gameObject.SetActive(false);

            if (HudManager.Instance != null)
            {
                HudManager.Instance.OnEmployeeSelected += _ => gameObject.SetActive(true);
                HudManager.Instance.OnEmployeeDeselected += () => gameObject.SetActive(false);
            }
        }

        private void OnCommandClicked(string command)
        {
            var employeeId = HudManager.Instance?.GetSelectedEmployeeId();
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return;
            }

            if (targetCommands.Contains(command) && targetPicker != null)
            {
                targetPicker.Open(command, employeeId);
                return;
            }

            SendCommand(command, employeeId, null, null, null);
        }

        public void SendCommand(string action, string targetEmployeeId, string targetPlayerId, string taskId, JObject extraPayload)
        {
            var receiver = HudManager.Instance?.GetBridgeReceiver();
            if (receiver == null)
            {
                Debug.LogWarning("CommandActionBar: no GameBridgeReceiver available.");
                return;
            }

            var payload = new JObject
            {
                ["command_id"] = Guid.NewGuid().ToString("N"),
                ["player_id"] = HudManager.Instance?.LocalPlayerId ?? "player_1",
                ["action"] = action,
                ["target_employee_id"] = targetEmployeeId,
                ["target_player_id"] = targetPlayerId,
                ["task_id"] = taskId,
                ["round_number"] = 1
            };

            if (extraPayload != null)
            {
                foreach (var pair in extraPayload)
                {
                    payload[pair.Key] = pair.Value;
                }
            }

            receiver.SendPlayerCommand(payload);
        }
    }
}
