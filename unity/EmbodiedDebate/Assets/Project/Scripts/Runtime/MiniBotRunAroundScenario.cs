using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    public enum MiniBotSocialPhase
    {
        Wander,
        Approach,
        Chat,
        React,
        Disperse,
    }

    public readonly struct MiniBotSocialSnapshot
    {
        public MiniBotSocialSnapshot(
            string agentId,
            MiniBotSocialPhase phase,
            Vector3 position,
            Vector3 facingDirection,
            string partnerId,
            string actionLabel)
        {
            AgentId = agentId;
            Phase = phase;
            Position = position;
            FacingDirection = facingDirection;
            PartnerId = partnerId;
            ActionLabel = actionLabel;
        }

        public string AgentId { get; }
        public MiniBotSocialPhase Phase { get; }
        public Vector3 Position { get; }
        public Vector3 FacingDirection { get; }
        public string PartnerId { get; }
        public string ActionLabel { get; }
        public bool IsInteracting => Phase != MiniBotSocialPhase.Wander;
    }

    public sealed class MiniBotRunAroundScenario : MonoBehaviour
    {
        private const float NaturalWalkSpeedMetersPerSecond = 0.55f;
        private const float FastWalkSpeedMetersPerSecond = 0.75f;
        private const float MinimumApproachSeconds = 2.5f;
        private const float MinimumDisperseSeconds = 1.8f;
        private const float VisibleWalkingStepMeters = 0.012f;
        private const string ReportDirectory = "reports/unity_dumps";
        private const string GaitTraceFileName = "minibot_gait_trace.jsonl";

        [SerializeField]
        private Vector2 roomMin = new Vector2(-8.1f, -8.1f);

        [SerializeField]
        private Vector2 roomMax = new Vector2(8.1f, 8.1f);

        [SerializeField]
        private float wallMargin = 0.8f;

        [SerializeField]
        private float waypointSegmentSeconds = 3.6f;

        [SerializeField]
        private List<SocialAgent> agents = new List<SocialAgent>();

        [SerializeField]
        private List<SocialInteraction> interactions = new List<SocialInteraction>();

        private readonly Dictionary<string, MiniBotSocialSnapshot> latestSnapshots =
            new Dictionary<string, MiniBotSocialSnapshot>();
        private readonly Dictionary<string, Vector3> previousPositions = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, float> previousSampleTimes = new Dictionary<string, float>();
        private readonly Dictionary<string, float> walkedDistances = new Dictionary<string, float>();
        private float lastAppliedTime;
        private bool paused;
        private float pausedTime;
        private string currentChatText = "Mini-bots are wandering freely.";
        private string currentActionMappingText = "Persona state maps to wandering locomotion.";
        private string gaitOutputDirectory;
        private bool gaitDumpInitialized;

        public string CurrentChatText => currentChatText;
        public string CurrentActionMappingText => currentActionMappingText;
        public bool IsPaused => paused;

        private void Awake()
        {
            InitializeGaitDump();
        }

        public void RegisterRunner(Transform agent, Vector3 center, float radius, float speed, float phase)
        {
            var points = new[]
            {
                center + new Vector3(-radius, 0f, -radius * 0.45f),
                center + new Vector3(radius, 0f, -radius * 0.35f),
                center + new Vector3(radius * 0.65f, 0f, radius),
                center + new Vector3(-radius * 0.85f, 0f, radius * 0.7f),
            };
            RegisterSocialAgent(agent, agent != null ? agent.name : string.Empty, "runner", points, speed, phase, null);
        }

        public void RegisterSocialAgent(
            Transform agent,
            string agentId,
            string archetype,
            Vector3[] waypoints,
            float walkSpeedMetersPerSecond,
            float phaseOffsetSeconds,
            Transform emotionMarker)
        {
            if (agent == null || waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            agents.Add(new SocialAgent(
                agent,
                string.IsNullOrWhiteSpace(agentId) ? agent.name : agentId.Trim(),
                string.IsNullOrWhiteSpace(archetype) ? "calm" : archetype.Trim(),
                ClampWaypoints(waypoints),
                Mathf.Max(0.1f, walkSpeedMetersPerSecond),
                Mathf.Max(0f, phaseOffsetSeconds),
                emotionMarker));
        }

        public void RegisterInteraction(
            string firstAgentId,
            string secondAgentId,
            Vector3 meetingCenter,
            Vector3 meetingAxis,
            float startSeconds,
            float approachSeconds,
            float chatSeconds,
            float reactSeconds,
            float disperseSeconds,
            Vector3 firstDisperseTarget,
            Vector3 secondDisperseTarget,
            string actionLabel)
        {
            var meetingAxisPlanar = NormalizePlanarOrRight(meetingAxis);
            var meetingCenterPlanar = RoomNavigationMath.ClampPlanarWithInset(meetingCenter, roomMin, roomMax, wallMargin);
            var firstMeetingPosition = meetingCenterPlanar - meetingAxisPlanar * 0.52f;
            var secondMeetingPosition = meetingCenterPlanar + meetingAxisPlanar * 0.52f;
            var resolvedApproachSeconds = ResolveApproachSeconds(
                firstAgentId,
                secondAgentId,
                startSeconds,
                firstMeetingPosition,
                secondMeetingPosition,
                approachSeconds);
            var resolvedDisperseSeconds = ResolveDisperseSeconds(
                firstMeetingPosition,
                secondMeetingPosition,
                firstDisperseTarget,
                secondDisperseTarget,
                disperseSeconds);

            interactions.Add(new SocialInteraction(
                firstAgentId,
                secondAgentId,
                meetingCenterPlanar,
                meetingAxisPlanar,
                Mathf.Max(0f, startSeconds),
                resolvedApproachSeconds,
                Mathf.Max(0.1f, chatSeconds),
                Mathf.Max(0.1f, reactSeconds),
                resolvedDisperseSeconds,
                RoomNavigationMath.ClampPlanarWithInset(firstDisperseTarget, roomMin, roomMax, wallMargin),
                RoomNavigationMath.ClampPlanarWithInset(secondDisperseTarget, roomMin, roomMax, wallMargin),
                string.IsNullOrWhiteSpace(actionLabel) ? "chat" : actionLabel.Trim()));
        }

        private void Update()
        {
            if (Environment.GetEnvironmentVariable("ARGUS_UNITY_VIDEO_CAPTURE") == "1")
            {
                return;
            }

            ApplyAtTime(Time.time);
        }

        public void ApplyAtTime(float sampleTime)
        {
            if (paused)
            {
                sampleTime = pausedTime;
            }

            lastAppliedTime = sampleTime;
            currentChatText = "Mini-bots are wandering freely.";
            currentActionMappingText = "Persona state maps to wandering locomotion.";
            latestSnapshots.Clear();
            for (var i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                if (agent.Agent == null)
                {
                    continue;
                }

                if (TryResolveInteraction(agent, sampleTime, out var interaction, out var partner, out var phase))
                {
                    ApplyInteraction(agent, partner, interaction, phase, sampleTime);
                    continue;
                }

                var position = WanderPosition(agent, sampleTime, out var facingDirection);
                var appliedFacing = ApplyPose(agent, position, facingDirection, false, sampleTime, true);
                StoreSnapshot(agent, MiniBotSocialPhase.Wander, position, appliedFacing, string.Empty, "wander");
            }

            RefreshChatTextFromSnapshots();
        }

        public void SetPaused(bool value)
        {
            if (paused == value)
            {
                return;
            }

            paused = value;
            if (paused)
            {
                pausedTime = lastAppliedTime;
                currentChatText = "Paused.";
                currentActionMappingText = "Simulation paused; movement and animation intent are held.";
                StopAllWalkAnimations();
            }
        }

        public void TriggerGatherNow()
        {
            interactions.Clear();
            RegisterInteraction(
                "A01",
                "C01",
                Vector3.zero,
                Vector3.right,
                lastAppliedTime,
                0.9f,
                1.6f,
                0.6f,
                1.0f,
                new Vector3(-3.9f, 0f, -2.2f),
                new Vector3(4.0f, 0f, -2.0f),
                "gather");
            currentChatText = "Gather: friendly and calm mini-bots meet at the plaza.";
        }

        public void TriggerChatNow()
        {
            interactions.Clear();
            RegisterInteraction(
                "A02",
                "B02",
                new Vector3(0.4f, 0f, 0.8f),
                Vector3.forward,
                lastAppliedTime,
                0.8f,
                2.0f,
                0.6f,
                1.0f,
                new Vector3(-4.5f, 0f, 2.6f),
                new Vector3(3.9f, 0f, 2.4f),
                "chat");
            currentChatText = "Chat: curious asks skeptical about the idea.";
        }

        public void TriggerScatterNow()
        {
            interactions.Clear();
            currentChatText = "Scatter: everyone resumes free wandering.";
        }

        public bool TryGetCurrentSnapshot(string agentId, out MiniBotSocialSnapshot snapshot)
        {
            if (!string.IsNullOrWhiteSpace(agentId) &&
                latestSnapshots.TryGetValue(agentId.Trim(), out snapshot))
            {
                return true;
            }

            snapshot = default;
            return false;
        }

        public int CountAgentsInPhase(MiniBotSocialPhase phase)
        {
            var count = 0;
            foreach (var snapshot in latestSnapshots.Values)
            {
                if (snapshot.Phase == phase)
                {
                    count++;
                }
            }

            return count;
        }

        private void ApplyInteraction(
            SocialAgent agent,
            SocialAgent partner,
            SocialInteraction interaction,
            MiniBotSocialPhase phase,
            float sampleTime)
        {
            var isFirst = agent.AgentId == interaction.FirstAgentId;
            var meetingPosition = interaction.MeetingCenter +
                                  interaction.MeetingAxis * (isFirst ? -0.52f : 0.52f);
            var partnerMeetingPosition = interaction.MeetingCenter +
                                         interaction.MeetingAxis * (isFirst ? 0.52f : -0.52f);
            var position = meetingPosition;
            var walkingFacing = Vector3.zero;

            if (phase == MiniBotSocialPhase.Approach)
            {
                var startPosition = WanderPosition(agent, interaction.StartSeconds, out _);
                var t = Mathf.InverseLerp(
                    interaction.StartSeconds,
                    interaction.ChatStartSeconds,
                    sampleTime);
                position = Vector3.Lerp(startPosition, meetingPosition, Smooth01(t));
                walkingFacing = meetingPosition - startPosition;
            }
            else if (phase == MiniBotSocialPhase.React)
            {
                var reactT = Mathf.InverseLerp(
                    interaction.ReactStartSeconds,
                    interaction.DisperseStartSeconds,
                    sampleTime);
                var bob = Mathf.Sin(reactT * Mathf.PI * 2f) * 0.08f;
                position = meetingPosition + Vector3.up * Mathf.Max(0f, bob);
            }
            else if (phase == MiniBotSocialPhase.Disperse)
            {
                var disperseTarget = isFirst ? interaction.FirstDisperseTarget : interaction.SecondDisperseTarget;
                var t = Mathf.InverseLerp(
                    interaction.DisperseStartSeconds,
                    interaction.EndSeconds,
                    sampleTime);
                position = Vector3.Lerp(
                    meetingPosition,
                    disperseTarget,
                    Smooth01(t));
                walkingFacing = disperseTarget - meetingPosition;
            }

            var facing = partner != null && partner.Agent != null
                ? partner.Agent.position - position
                : partnerMeetingPosition - position;
            facing.y = 0f;
            if ((phase == MiniBotSocialPhase.Approach || phase == MiniBotSocialPhase.Disperse) &&
                walkingFacing.sqrMagnitude > 0.001f)
            {
                facing = walkingFacing;
                facing.y = 0f;
            }
            else if (facing.sqrMagnitude <= 0.001f && phase == MiniBotSocialPhase.Disperse)
            {
                facing = (isFirst ? interaction.FirstDisperseTarget : interaction.SecondDisperseTarget) - position;
                facing.y = 0f;
            }

            var isWalkingPhase = phase == MiniBotSocialPhase.Approach || phase == MiniBotSocialPhase.Disperse;
            var appliedFacing = ApplyPose(
                agent,
                position,
                NormalizePlanarOrForward(facing),
                phase == MiniBotSocialPhase.Chat || phase == MiniBotSocialPhase.React,
                sampleTime,
                isWalkingPhase);
            if (phase == MiniBotSocialPhase.Chat || phase == MiniBotSocialPhase.React)
            {
                currentChatText = $"{agent.AgentId} {interaction.ActionLabel} with {partner?.AgentId ?? "neighbor"}.";
            }

            StoreSnapshot(agent, phase, position, appliedFacing, partner?.AgentId ?? string.Empty, interaction.ActionLabel);
        }

        private bool TryResolveInteraction(
            SocialAgent agent,
            float sampleTime,
            out SocialInteraction interaction,
            out SocialAgent partner,
            out MiniBotSocialPhase phase)
        {
            for (var i = 0; i < interactions.Count; i++)
            {
                interaction = interactions[i];
                if (!interaction.Contains(agent.AgentId) ||
                    sampleTime < interaction.StartSeconds ||
                    sampleTime >= interaction.EndSeconds)
                {
                    continue;
                }

                partner = FindAgent(interaction.OtherAgentId(agent.AgentId));
                phase = interaction.PhaseAt(sampleTime);
                return true;
            }

            interaction = null;
            partner = null;
            phase = MiniBotSocialPhase.Wander;
            return false;
        }

        private SocialAgent FindAgent(string agentId)
        {
            for (var i = 0; i < agents.Count; i++)
            {
                if (agents[i].AgentId == agentId)
                {
                    return agents[i];
                }
            }

            return null;
        }

        private Vector3 WanderPosition(SocialAgent agent, float sampleTime, out Vector3 facingDirection)
        {
            if (agent.Waypoints.Count == 1)
            {
                facingDirection = agent.Agent != null ? agent.Agent.forward : Vector3.forward;
                return agent.Waypoints[0];
            }

            var localTime = Mathf.Max(0f, (sampleTime + agent.PhaseOffsetSeconds) * agent.WalkSpeedMetersPerSecond);
            var scaled = localTime / Mathf.Max(0.1f, waypointSegmentSeconds);
            var segment = Mathf.FloorToInt(scaled);
            var t = Smooth01(scaled - segment);
            var from = agent.Waypoints[segment % agent.Waypoints.Count];
            var to = agent.Waypoints[(segment + 1) % agent.Waypoints.Count];
            facingDirection = NormalizePlanarOrForward(to - from);
            return Vector3.Lerp(from, to, t);
        }

        private Vector3 ApplyPose(
            SocialAgent agent,
            Vector3 position,
            Vector3 facingDirection,
            bool markerVisible,
            float sampleTime,
            bool allowWalkAnimation)
        {
            position = RoomNavigationMath.ClampPlanarWithInset(position, roomMin, roomMax, wallMargin);
            var hasPreviousPosition = previousPositions.TryGetValue(agent.AgentId, out var previousPosition);
            if (!hasPreviousPosition)
            {
                previousPosition = agent.Agent.position;
            }
            var planarDelta = position - previousPosition;
            planarDelta.y = 0f;
            var distanceDelta = planarDelta.magnitude;
            var appliedFacingDirection = NormalizePlanarOrForward(facingDirection);
            var hasVisibleResidualMovement = hasPreviousPosition && distanceDelta > VisibleWalkingStepMeters;
            var shouldAnimateLocomotion = allowWalkAnimation || hasVisibleResidualMovement;
            if (shouldAnimateLocomotion && planarDelta.sqrMagnitude > 0.000001f)
            {
                appliedFacingDirection = planarDelta.normalized;
            }

            var targetYaw = LocomotionMath.YawFromPlanarDirection(appliedFacingDirection);
            var currentYaw = agent.Agent.rotation.eulerAngles.y;
            var turnDegrees = LocomotionMath.SignedYawDelta(currentYaw, targetYaw);

            var movement = EnsureMovementController(agent.Agent.gameObject);
            movement.ApplyKinematicPose(
                position,
                appliedFacingDirection,
                sampleTime,
                agent.WalkSpeedMetersPerSecond);
            var appliedPosition = movement.LastAppliedPosition;
            var appliedDelta = appliedPosition - previousPosition;
            appliedDelta.y = 0f;
            distanceDelta = appliedDelta.magnitude;
            turnDegrees = movement.LastSignedTurnDegrees;

            var animator = agent.Agent.GetComponent<MiniBotWalkAnimator>();
            if (animator != null)
            {
                var hasPreviousTime = previousSampleTimes.TryGetValue(agent.AgentId, out var previousTime);
                var deltaTime = Mathf.Max(1f / 30f, sampleTime - previousTime);
                var speed = Mathf.Clamp(
                    movement.LastPlanarSpeed > 0f ? movement.LastPlanarSpeed : distanceDelta / deltaTime,
                    0f,
                    agent.WalkSpeedMetersPerSecond * 1.65f);
                var isMoving = shouldAnimateLocomotion && distanceDelta > 0.006f;
                animator.SetMotionIntent(isMoving, turnDegrees, speed, isMoving ? distanceDelta : 0f);
                var walkedDistance = ResolveWalkedDistance(agent.AgentId, distanceDelta, isMoving, hasPreviousTime, sampleTime, previousTime);
                animator.SampleDistanceSyncedPose(walkedDistance, isMoving, turnDegrees);
            }

            var headingAlignmentDegrees = ResolveHeadingAlignmentDegrees(appliedDelta, appliedFacingDirection);
            AppendGaitTrace(agent, animator, movement, distanceDelta, headingAlignmentDegrees, sampleTime);

            previousPositions[agent.AgentId] = appliedPosition;
            previousSampleTimes[agent.AgentId] = sampleTime;

            if (agent.EmotionMarker != null)
            {
                agent.EmotionMarker.gameObject.SetActive(markerVisible);
            }

            return NormalizePlanarOrForward(agent.Agent.forward);
        }

        private float ResolveWalkedDistance(
            string agentId,
            float distanceDelta,
            bool isMoving,
            bool hasPreviousTime,
            float sampleTime,
            float previousTime)
        {
            walkedDistances.TryGetValue(agentId, out var walkedDistance);
            if (!isMoving)
            {
                walkedDistances[agentId] = walkedDistance;
                return walkedDistance;
            }

            if (hasPreviousTime && sampleTime + 0.0001f >= previousTime)
            {
                walkedDistance += distanceDelta;
            }
            else
            {
                walkedDistance = 0f;
            }

            walkedDistances[agentId] = walkedDistance;
            return walkedDistance;
        }

        private void StopAllWalkAnimations()
        {
            for (var i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                if (agent.Agent == null)
                {
                    continue;
                }

                var animator = agent.Agent.GetComponent<MiniBotWalkAnimator>();
                if (animator != null)
                {
                    animator.SetMotionIntent(false, 0f, 0f, 0f);
                }
            }
        }

        private void StoreSnapshot(
            SocialAgent agent,
            MiniBotSocialPhase phase,
            Vector3 position,
            Vector3 facingDirection,
            string partnerId,
            string actionLabel)
        {
            latestSnapshots[agent.AgentId] = new MiniBotSocialSnapshot(
                agent.AgentId,
                phase,
                position,
                facingDirection,
                partnerId,
                actionLabel);
            UpdateBlackboard(agent, latestSnapshots[agent.AgentId]);
        }

        private void UpdateBlackboard(SocialAgent agent, MiniBotSocialSnapshot snapshot)
        {
            var blackboard = agent.Agent.GetComponent<MinibotBlackboard>();
            if (blackboard == null)
            {
                blackboard = agent.Agent.gameObject.AddComponent<MinibotBlackboard>();
            }

            var decision = PersonaDecisionMapper.FromSocialSnapshot(agent.AgentId, agent.Archetype, snapshot);
            var unityAction = PersonaDecisionMapper.Map(decision);
            var movement = agent.Agent.GetComponent<MinibotMovementController>();
            blackboard.ApplySocialState(
                agent.AgentId,
                agent.Archetype,
                snapshot,
                unityAction,
                movement != null ? movement.LastMovementTarget : snapshot.Position,
                movement != null ? movement.LastPlanarSpeed : 0f,
                movement != null ? movement.LastSignedTurnDegrees : 0f);

            if (snapshot.Phase == MiniBotSocialPhase.Chat || snapshot.Phase == MiniBotSocialPhase.React)
            {
                currentActionMappingText = $"{agent.AgentId}: {unityAction.DebugSummary}";
                return;
            }

            if (snapshot.Phase == MiniBotSocialPhase.Approach &&
                currentActionMappingText == "Persona state maps to wandering locomotion.")
            {
                currentActionMappingText = $"{agent.AgentId}: {unityAction.DebugSummary}";
            }
        }

        private static MinibotMovementController EnsureMovementController(GameObject agent)
        {
            var movement = agent.GetComponent<MinibotMovementController>();
            return movement != null ? movement : agent.AddComponent<MinibotMovementController>();
        }

        private float ResolveApproachSeconds(
            string firstAgentId,
            string secondAgentId,
            float startSeconds,
            Vector3 firstMeetingPosition,
            Vector3 secondMeetingPosition,
            float fallbackSeconds)
        {
            var firstDistance = ResolveAgentDistanceAt(firstAgentId, startSeconds, firstMeetingPosition);
            var secondDistance = ResolveAgentDistanceAt(secondAgentId, startSeconds, secondMeetingPosition);
            var longestDistance = Mathf.Max(firstDistance, secondDistance);
            return Mathf.Max(
                MinimumApproachSeconds,
                longestDistance / NaturalWalkSpeedMetersPerSecond,
                fallbackSeconds);
        }

        private float ResolveDisperseSeconds(
            Vector3 firstMeetingPosition,
            Vector3 secondMeetingPosition,
            Vector3 firstDisperseTarget,
            Vector3 secondDisperseTarget,
            float fallbackSeconds)
        {
            var firstDistance = PlanarDistance(
                firstMeetingPosition,
                RoomNavigationMath.ClampPlanarWithInset(firstDisperseTarget, roomMin, roomMax, wallMargin));
            var secondDistance = PlanarDistance(
                secondMeetingPosition,
                RoomNavigationMath.ClampPlanarWithInset(secondDisperseTarget, roomMin, roomMax, wallMargin));
            var longestDistance = Mathf.Max(firstDistance, secondDistance);
            return Mathf.Max(
                MinimumDisperseSeconds,
                longestDistance / FastWalkSpeedMetersPerSecond,
                fallbackSeconds);
        }

        private float ResolveAgentDistanceAt(string agentId, float sampleTime, Vector3 targetPosition)
        {
            var agent = FindAgent(agentId);
            if (agent == null)
            {
                return 0f;
            }

            return PlanarDistance(WanderPosition(agent, sampleTime, out _), targetPosition);
        }

        private static float PlanarDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        private static float ResolveHeadingAlignmentDegrees(Vector3 planarDelta, Vector3 facingDirection)
        {
            planarDelta.y = 0f;
            facingDirection.y = 0f;
            if (planarDelta.sqrMagnitude <= 0.000001f || facingDirection.sqrMagnitude <= 0.000001f)
            {
                return 0f;
            }

            return Vector3.Angle(planarDelta.normalized, facingDirection.normalized);
        }

        private void InitializeGaitDump()
        {
            if (gaitDumpInitialized)
            {
                return;
            }

            gaitDumpInitialized = true;
            var outputDirectory = EnsureGaitOutputDirectory();
            File.WriteAllText(Path.Combine(outputDirectory, GaitTraceFileName), string.Empty);
            WriteGaitJson("gait_dump.json", BuildGaitDump());
            WriteGaitJson("video_gait_review.json", BuildVideoGaitReview());
        }

        private JObject BuildGaitDump()
        {
            return new JObject
            {
                ["gait_system"] = "MiniBotWalkAnimator",
                ["movement_source"] = "timeline_showcase_speed_limited",
                ["meters_per_walk_cycle"] = 0.75f,
                ["minimum_walk_cycle_seconds"] = 1.0f,
                ["max_visual_cycle_rate_hz"] = 1.0f,
                ["normal_walk_speed_range_mps"] = new JArray(0.4f, 0.65f),
                ["fast_walk_speed_range_mps"] = new JArray(0.65f, 0.85f),
                ["run_requires_run_clip"] = true,
                ["warnings"] = new JArray(
                    "MiniBotRunAroundScenario samples timeline targets, then MinibotMovementController speed-limits actual movement.",
                    "Interaction approach and disperse durations are distance-based for natural walk cadence.",
                    "Externally sampled gait poses are authoritative for the rendered frame to avoid double-advancing the walk cycle.",
                    "Stride warnings in minibot_gait_trace.jsonl indicate speed or cycle rates outside walk range.")
            };
        }

        private static JObject BuildVideoGaitReview()
        {
            return new JObject
            {
                ["video"] = "tmp/mini_bot_run_working.mp4",
                ["duration_seconds"] = 8.0f,
                ["fps"] = 30,
                ["observed_issue"] = "feet can cycle independently from floor cadence if timeline targets exceed natural walk speed",
                ["diagnosis"] = "timeline speed and gait cycle mismatch",
                ["primary_fix"] = "speed-limit actual transform movement and increase interaction durations",
                ["secondary_fix"] = "calibrate metersPerWalkCycle from 0.62 to 0.75"
            };
        }

        private void AppendGaitTrace(
            SocialAgent agent,
            MiniBotWalkAnimator animator,
            MinibotMovementController movement,
            float distanceDelta,
            float headingAlignmentDegrees,
            float sampleTime)
        {
            if (!gaitDumpInitialized || movement == null)
            {
                return;
            }

            var cycleMeters = animator != null ? animator.MetersPerWalkCycle : 0.75f;
            var speed = movement.LastPlanarSpeed;
            var cycleRate = speed / Mathf.Max(0.01f, cycleMeters);
            var visualCycleRate = animator != null ? animator.LastVisualCycleRateHz : cycleRate;
            var visualPhaseAdvance = animator != null ? animator.LastPhaseAdvance : 0f;
            var warning = string.Empty;
            if (movement.LastSpeedLimitExceeded)
            {
                warning = "timeline_target_exceeded_speed_limit";
            }
            else if (visualCycleRate > 1.05f)
            {
                warning = "visual_cycle_restarted_too_fast";
            }
            else if (movement.LastActualStepMeters > VisibleWalkingStepMeters && headingAlignmentDegrees > 35f)
            {
                warning = "body_facing_sideways_while_walking";
            }
            else if (speed > 0.85f || cycleRate > 2.0f)
            {
                warning = "too_fast_for_walk";
            }

            var row = new JObject
            {
                ["frame"] = Time.frameCount,
                ["sample_time"] = sampleTime,
                ["agent_id"] = agent.AgentId,
                ["actual_speed_mps"] = speed,
                ["walk_speed_limit_mps"] = movement.LastSpeedLimitMetersPerSecond,
                ["distance_delta"] = distanceDelta,
                ["allowed_step_meters"] = movement.LastAllowedStepMeters,
                ["actual_step_meters"] = movement.LastActualStepMeters,
                ["meters_per_cycle"] = cycleMeters,
                ["cycle_rate_hz"] = cycleRate,
                ["visual_cycle_rate_hz"] = visualCycleRate,
                ["visual_phase_advance"] = visualPhaseAdvance,
                ["visual_phase"] = animator != null ? animator.LastNormalizedPhase : 0f,
                ["externally_sampled_pose"] = animator != null && animator.LastPoseWasExternallySampled,
                ["heading_alignment_degrees"] = headingAlignmentDegrees,
                ["stride_warning"] = warning
            };
            File.AppendAllText(
                Path.Combine(EnsureGaitOutputDirectory(), GaitTraceFileName),
                row.ToString(Formatting.None) + Environment.NewLine);
        }

        private void WriteGaitJson(string fileName, JObject data)
        {
            File.WriteAllText(
                Path.Combine(EnsureGaitOutputDirectory(), fileName),
                data.ToString(Formatting.Indented));
        }

        private string EnsureGaitOutputDirectory()
        {
            if (!string.IsNullOrEmpty(gaitOutputDirectory))
            {
                return gaitOutputDirectory;
            }

            gaitOutputDirectory = Path.Combine(FindRepositoryRoot(), ReportDirectory);
            Directory.CreateDirectory(gaitOutputDirectory);
            return gaitOutputDirectory;
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(Application.dataPath);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
        }

        private void RefreshChatTextFromSnapshots()
        {
            if (paused)
            {
                currentChatText = "Paused.";
                return;
            }

            foreach (var snapshot in latestSnapshots.Values)
            {
                if (snapshot.Phase == MiniBotSocialPhase.Chat || snapshot.Phase == MiniBotSocialPhase.React)
                {
                    currentChatText = $"{snapshot.AgentId} {snapshot.ActionLabel} with {snapshot.PartnerId}.";
                    return;
                }
            }

            foreach (var snapshot in latestSnapshots.Values)
            {
                if (snapshot.Phase == MiniBotSocialPhase.Approach)
                {
                    currentChatText = $"{snapshot.AgentId} approaches {snapshot.PartnerId}.";
                    return;
                }
            }
        }

        private IReadOnlyList<Vector3> ClampWaypoints(Vector3[] waypoints)
        {
            var result = new List<Vector3>(waypoints.Length);
            for (var i = 0; i < waypoints.Length; i++)
            {
                result.Add(RoomNavigationMath.ClampPlanarWithInset(waypoints[i], roomMin, roomMax, wallMargin));
            }

            return result;
        }

        private static Vector3 NormalizePlanarOrRight(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
        }

        private static Vector3 NormalizePlanarOrForward(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        [Serializable]
        private sealed class SocialAgent
        {
            public SocialAgent(
                Transform agent,
                string agentId,
                string archetype,
                IReadOnlyList<Vector3> waypoints,
                float walkSpeedMetersPerSecond,
                float phaseOffsetSeconds,
                Transform emotionMarker)
            {
                this.agent = agent;
                this.agentId = agentId;
                this.archetype = archetype;
                this.waypoints = new List<Vector3>(waypoints);
                this.walkSpeedMetersPerSecond = walkSpeedMetersPerSecond;
                this.phaseOffsetSeconds = phaseOffsetSeconds;
                this.emotionMarker = emotionMarker;
            }

            [SerializeField]
            private Transform agent;

            [SerializeField]
            private string agentId;

            [SerializeField]
            private string archetype;

            [SerializeField]
            private List<Vector3> waypoints;

            [SerializeField]
            private float walkSpeedMetersPerSecond;

            [SerializeField]
            private float phaseOffsetSeconds;

            [SerializeField]
            private Transform emotionMarker;

            public Transform Agent => agent;
            public string AgentId => agentId;
            public string Archetype => archetype;
            public IReadOnlyList<Vector3> Waypoints => waypoints;
            public float WalkSpeedMetersPerSecond => walkSpeedMetersPerSecond;
            public float PhaseOffsetSeconds => phaseOffsetSeconds;
            public Transform EmotionMarker => emotionMarker;
        }

        [Serializable]
        private sealed class SocialInteraction
        {
            public SocialInteraction(
                string firstAgentId,
                string secondAgentId,
                Vector3 meetingCenter,
                Vector3 meetingAxis,
                float startSeconds,
                float approachSeconds,
                float chatSeconds,
                float reactSeconds,
                float disperseSeconds,
                Vector3 firstDisperseTarget,
                Vector3 secondDisperseTarget,
                string actionLabel)
            {
                this.firstAgentId = firstAgentId;
                this.secondAgentId = secondAgentId;
                this.meetingCenter = meetingCenter;
                this.meetingAxis = meetingAxis;
                this.startSeconds = startSeconds;
                this.approachSeconds = approachSeconds;
                this.chatSeconds = chatSeconds;
                this.reactSeconds = reactSeconds;
                this.disperseSeconds = disperseSeconds;
                this.firstDisperseTarget = firstDisperseTarget;
                this.secondDisperseTarget = secondDisperseTarget;
                this.actionLabel = actionLabel;
            }

            [SerializeField]
            private string firstAgentId;

            [SerializeField]
            private string secondAgentId;

            [SerializeField]
            private Vector3 meetingCenter;

            [SerializeField]
            private Vector3 meetingAxis;

            [SerializeField]
            private float startSeconds;

            [SerializeField]
            private float approachSeconds;

            [SerializeField]
            private float chatSeconds;

            [SerializeField]
            private float reactSeconds;

            [SerializeField]
            private float disperseSeconds;

            [SerializeField]
            private Vector3 firstDisperseTarget;

            [SerializeField]
            private Vector3 secondDisperseTarget;

            [SerializeField]
            private string actionLabel;

            public string FirstAgentId => firstAgentId;
            public string SecondAgentId => secondAgentId;
            public Vector3 MeetingCenter => meetingCenter;
            public Vector3 MeetingAxis => meetingAxis;
            public float StartSeconds => startSeconds;
            public float ChatStartSeconds => startSeconds + approachSeconds;
            public float ReactStartSeconds => ChatStartSeconds + chatSeconds;
            public float DisperseStartSeconds => ReactStartSeconds + reactSeconds;
            public float EndSeconds => DisperseStartSeconds + disperseSeconds;
            public Vector3 FirstDisperseTarget => firstDisperseTarget;
            public Vector3 SecondDisperseTarget => secondDisperseTarget;
            public string ActionLabel => actionLabel;

            public bool Contains(string agentId)
            {
                return agentId == firstAgentId || agentId == secondAgentId;
            }

            public string OtherAgentId(string agentId)
            {
                return agentId == firstAgentId ? secondAgentId : firstAgentId;
            }

            public MiniBotSocialPhase PhaseAt(float sampleTime)
            {
                if (sampleTime < ChatStartSeconds)
                {
                    return MiniBotSocialPhase.Approach;
                }

                if (sampleTime < ReactStartSeconds)
                {
                    return MiniBotSocialPhase.Chat;
                }

                return sampleTime < DisperseStartSeconds
                    ? MiniBotSocialPhase.React
                    : MiniBotSocialPhase.Disperse;
            }
        }
    }
}
