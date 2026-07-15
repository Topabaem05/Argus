using System;
using System.Collections.Generic;
using ArgusUnity.Bridge;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Game
{
    /// <summary>
    /// Unity-side projection of the authoritative Python game state.
    /// Combines state/task/economy/rumor messages into one HUD and minibot-facing API.
    /// </summary>
    public sealed class CompanyGameCoordinator : MonoBehaviour
    {
        [SerializeField] private GameBridgeReceiver bridge;
        [SerializeField] private string localPlayerId = "p1";
        [SerializeField] private string localCompanyName = "AlphaCorp";
        [SerializeField] private int initialFunds = 10000;

        private readonly Dictionary<string, CompanyMiniBotActor> actorsById = new();

        public event Action StateChanged;

        public string LocalPlayerId => localPlayerId;
        public string LocalCompanyName => localCompanyName;
        public int Funds { get; private set; }
        public int CustomerSatisfaction { get; private set; } = 50;
        public int RoundNumber { get; private set; } = 1;
        public string Phase { get; private set; } = "morning";
        public string LatestEvent { get; private set; } = "도시가 영업 준비 중입니다.";
        public bool IsFinished { get; private set; }
        public bool IsConnected => bridge != null && bridge.IsConnected;

        private void Awake()
        {
            bridge ??= GetComponent<GameBridgeReceiver>();
            Funds = initialFunds;
            RebuildActorIndex();
        }

        private void OnEnable()
        {
            CompanyMiniBotActor.SelectionChanged += HandleSelectionChanged;
            if (bridge == null)
            {
                return;
            }

            bridge.OnPlayerCommand += HandlePlayerCommand;
            bridge.OnTaskUpdate += HandleTaskUpdate;
            bridge.OnRumorEvent += HandleRumorEvent;
            bridge.OnEconomyUpdate += HandleEconomyUpdate;
            bridge.OnGameStateSync += HandleGameStateSync;
        }

        private void OnDisable()
        {
            CompanyMiniBotActor.SelectionChanged -= HandleSelectionChanged;
            if (bridge == null)
            {
                return;
            }

            bridge.OnPlayerCommand -= HandlePlayerCommand;
            bridge.OnTaskUpdate -= HandleTaskUpdate;
            bridge.OnRumorEvent -= HandleRumorEvent;
            bridge.OnEconomyUpdate -= HandleEconomyUpdate;
            bridge.OnGameStateSync -= HandleGameStateSync;
        }

        public bool CanIssue(string action, CompanyMiniBotActor target)
        {
            if (IsFinished || string.IsNullOrWhiteSpace(action))
            {
                return false;
            }

            if (action is "hire" or "party")
            {
                return true;
            }

            if (target == null || target.Role != CompanyActorRole.Employee)
            {
                return false;
            }

            if (action is "gossip" or "scout")
            {
                return target.CompanyId != localPlayerId;
            }

            return target.CompanyId == localPlayerId;
        }

        public void SendSelectedCommand(string action)
        {
            var selected = CompanyMiniBotActor.Selected;
            if (!CanIssue(action, selected))
            {
                LatestEvent = "현재 선택으로는 이 명령을 실행할 수 없습니다.";
                StateChanged?.Invoke();
                return;
            }

            if (action is "hire" or "party")
            {
                SendCommand(action, null, null);
                return;
            }

            var targetPlayerId = action == "gossip" ? selected.CompanyId : null;
            SendCommand(action, selected.ActorId, targetPlayerId);
            selected.GetComponent<CompanyEmployeeAgentController>()?.PreviewAction(action);
        }

        public void SendCompanyCommand(string action)
        {
            if (!CanIssue(action, null))
            {
                return;
            }
            SendCommand(action, null, null);
        }

        private void SendCommand(string action, string targetEmployeeId, string targetPlayerId)
        {
            var payload = new JObject
            {
                ["command_id"] = Guid.NewGuid().ToString("N"),
                ["player_id"] = localPlayerId,
                ["action"] = action,
                ["round_number"] = Mathf.Max(1, RoundNumber)
            };
            if (!string.IsNullOrWhiteSpace(targetEmployeeId))
            {
                payload["target_employee_id"] = targetEmployeeId;
            }
            if (!string.IsNullOrWhiteSpace(targetPlayerId))
            {
                payload["target_player_id"] = targetPlayerId;
            }

            if (bridge != null)
            {
                bridge.SendPlayerCommand(payload);
                LatestEvent = $"명령 전송: {ActionLabel(action)}";
            }
            else
            {
                LatestEvent = $"오프라인 미리보기: {ActionLabel(action)}";
            }
            StateChanged?.Invoke();
        }

        private void HandleSelectionChanged(CompanyMiniBotActor selected)
        {
            StateChanged?.Invoke();
        }

        private void HandleGameStateSync(JObject payload)
        {
            RoundNumber = Mathf.Max(1, payload.Value<int?>("round_number") ?? RoundNumber);
            Phase = payload.Value<string>("phase") ?? Phase;
            IsFinished = payload.Value<bool?>("is_finished") ?? IsFinished;
            LatestEvent = IsFinished
                ? "최종 정산이 완료되었습니다."
                : $"라운드 {RoundNumber} · {PhaseLabel(Phase)}";

            if (payload["employees"] is JArray employees)
            {
                foreach (var token in employees)
                {
                    if (token is not JObject employee)
                    {
                        continue;
                    }

                    var actor = FindActor(employee.Value<string>("employee_id"));
                    if (actor == null)
                    {
                        continue;
                    }
                    actor.ApplySnapshot(
                        employee.Value<int?>("mood") ?? actor.Mood,
                        employee.Value<int?>("loyalty") ?? actor.Loyalty,
                        employee.Value<string>("current_task") ?? actor.CurrentTask);
                }
            }
            StateChanged?.Invoke();
        }

        private void HandleTaskUpdate(JObject payload)
        {
            var employeeId = payload.Value<string>("employee_id");
            var category = payload.Value<string>("category") ?? "업무";
            var status = payload.Value<string>("status") ?? "pending";
            var progress = Mathf.Clamp01(payload.Value<float?>("progress") ?? 0f);
            var actor = FindActor(employeeId);
            if (actor != null)
            {
                var label = status is "completed" or "failed"
                    ? "Idle"
                    : $"{category} {Mathf.RoundToInt(progress * 100f)}%";
                actor.SetCurrentTask(label);
                var motion = actor.GetComponent<CompanyMiniBotMotionDriver>();
                motion?.SetMotion(status switch
                {
                    "completed" => MiniBotMotionState.Cheer,
                    "failed" => MiniBotMotionState.Complain,
                    "in_progress" => MiniBotMotionState.Work,
                    _ => MiniBotMotionState.Talk
                });
            }

            LatestEvent = $"업무 {category}: {StatusLabel(status)} {Mathf.RoundToInt(progress * 100f)}%";
            StateChanged?.Invoke();
        }

        private void HandleRumorEvent(JObject payload)
        {
            var source = payload.Value<string>("source_player_id") ?? "?";
            var target = payload.Value<string>("target_player_id") ?? "?";
            var content = payload.Value<string>("content") ?? "새 소문";
            var credibility = Mathf.Clamp01(payload.Value<float?>("credibility") ?? 0f);
            LatestEvent = $"소문 {source}→{target}: {content} ({Mathf.RoundToInt(credibility * 100f)}%)";

            foreach (var actor in FindObjectsOfType<CompanyMiniBotActor>())
            {
                if (actor.Role != CompanyActorRole.Employee)
                {
                    continue;
                }
                if (actor.CompanyId == source)
                {
                    actor.GetComponent<CompanyMiniBotMotionDriver>()?.SetMotion(MiniBotMotionState.Gossip);
                }
                else if (actor.CompanyId == target)
                {
                    actor.GetComponent<CompanyMiniBotMotionDriver>()?.SetMotion(MiniBotMotionState.Complain);
                }
            }
            StateChanged?.Invoke();
        }

        private void HandleEconomyUpdate(JObject payload)
        {
            var playerId = payload.Value<string>("player_id");
            if (playerId == localPlayerId)
            {
                Funds = Mathf.Max(0, payload.Value<int?>("funds_after") ?? Funds);
                RoundNumber = Mathf.Max(1, payload.Value<int?>("round_number") ?? RoundNumber);
                var income = payload.Value<int?>("income") ?? 0;
                var expenses = payload.Value<int?>("expenses") ?? 0;
                LatestEvent = $"정산: +{income:N0} / -{expenses:N0} / 잔액 {Funds:N0}";
            }
            StateChanged?.Invoke();
        }

        private void HandlePlayerCommand(JObject payload)
        {
            var actor = FindActor(payload.Value<string>("target_employee_id"));
            if (actor == null)
            {
                return;
            }

            var action = payload.Value<string>("action") ?? "accept";
            var outcome = payload.Value<string>("outcome") ?? action;
            var efficiency = Mathf.Clamp01(payload.Value<float?>("efficiency") ?? 0.7f);
            var sideAction = payload.Value<string>("side_action");
            actor.GetComponent<CompanyEmployeeAgentController>()
                ?.ApplyDecision(outcome, efficiency, sideAction);
            LatestEvent = $"{actor.DisplayName}: {OutcomeLabel(outcome)}";
            StateChanged?.Invoke();
        }

        private CompanyMiniBotActor FindActor(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return null;
            }

            if (actorsById.TryGetValue(actorId, out var actor) && actor != null)
            {
                return actor;
            }

            RebuildActorIndex();
            return actorsById.TryGetValue(actorId, out actor) ? actor : null;
        }

        private void RebuildActorIndex()
        {
            actorsById.Clear();
            foreach (var actor in FindObjectsOfType<CompanyMiniBotActor>())
            {
                if (!string.IsNullOrWhiteSpace(actor.ActorId))
                {
                    actorsById[actor.ActorId] = actor;
                }
            }
        }

        private static string ActionLabel(string action) => action switch
        {
            "praise" => "칭찬",
            "scold" => "혼내기",
            "snack" => "간식",
            "raise" => "월급 인상",
            "bonus" => "보너스",
            "party" => "회식",
            "fire" => "해고",
            "hire" => "채용",
            "gossip" => "소문",
            "scout" => "스카우트",
            "assign_task" => "업무 배정",
            _ => action
        };

        private static string PhaseLabel(string phase) => phase switch
        {
            "morning" => "아침",
            "work" => "업무",
            "event" => "이벤트",
            "evening" => "저녁",
            _ => phase
        };

        private static string StatusLabel(string status) => status switch
        {
            "completed" => "완료",
            "failed" => "실패",
            "in_progress" => "진행",
            "assigned" => "배정",
            _ => status
        };

        private static string OutcomeLabel(string outcome) => outcome switch
        {
            "accept" => "수락",
            "reluctant_accept" => "불만 수행",
            "refuse" => "거부",
            "complain" => "불평",
            "quit" => "사직",
            _ => outcome
        };
    }
}
