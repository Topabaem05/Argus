using System;
using System.Collections.Generic;
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

        public string CurrentChatText => currentChatText;
        public bool IsPaused => paused;

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
            interactions.Add(new SocialInteraction(
                firstAgentId,
                secondAgentId,
                RoomNavigationMath.ClampPlanarWithInset(meetingCenter, roomMin, roomMax, wallMargin),
                NormalizePlanarOrRight(meetingAxis),
                Mathf.Max(0f, startSeconds),
                Mathf.Max(0.1f, approachSeconds),
                Mathf.Max(0.1f, chatSeconds),
                Mathf.Max(0.1f, reactSeconds),
                Mathf.Max(0.1f, disperseSeconds),
                RoomNavigationMath.ClampPlanarWithInset(firstDisperseTarget, roomMin, roomMax, wallMargin),
                RoomNavigationMath.ClampPlanarWithInset(secondDisperseTarget, roomMin, roomMax, wallMargin),
                string.IsNullOrWhiteSpace(actionLabel) ? "chat" : actionLabel.Trim()));
        }

        private void Update()
        {
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
                ApplyPose(agent, position, facingDirection, false, sampleTime, true);
                StoreSnapshot(agent, MiniBotSocialPhase.Wander, position, facingDirection, string.Empty, "wander");
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
                StopAllWalkAnimations();
            }
        }

        public void TriggerGatherNow()
        {
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

            if (phase == MiniBotSocialPhase.Approach)
            {
                var startPosition = WanderPosition(agent, interaction.StartSeconds, out _);
                var t = Mathf.InverseLerp(
                    interaction.StartSeconds,
                    interaction.ChatStartSeconds,
                    sampleTime);
                position = Vector3.Lerp(startPosition, meetingPosition, Smooth01(t));
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
                var t = Mathf.InverseLerp(
                    interaction.DisperseStartSeconds,
                    interaction.EndSeconds,
                    sampleTime);
                position = Vector3.Lerp(
                    meetingPosition,
                    isFirst ? interaction.FirstDisperseTarget : interaction.SecondDisperseTarget,
                    Smooth01(t));
            }

            var facing = partner != null && partner.Agent != null
                ? partner.Agent.position - position
                : partnerMeetingPosition - position;
            facing.y = 0f;
            if (facing.sqrMagnitude <= 0.001f && phase == MiniBotSocialPhase.Disperse)
            {
                facing = (isFirst ? interaction.FirstDisperseTarget : interaction.SecondDisperseTarget) - position;
                facing.y = 0f;
            }

            var isWalkingPhase = phase == MiniBotSocialPhase.Approach || phase == MiniBotSocialPhase.Disperse;
            ApplyPose(
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

            StoreSnapshot(agent, phase, position, NormalizePlanarOrForward(facing), partner?.AgentId ?? string.Empty, interaction.ActionLabel);
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

        private void ApplyPose(
            SocialAgent agent,
            Vector3 position,
            Vector3 facingDirection,
            bool markerVisible,
            float sampleTime,
            bool allowWalkAnimation)
        {
            position = RoomNavigationMath.ClampPlanarWithInset(position, roomMin, roomMax, wallMargin);
            if (!previousPositions.TryGetValue(agent.AgentId, out var previousPosition))
            {
                previousPosition = agent.Agent.position;
            }
            var planarDelta = position - previousPosition;
            planarDelta.y = 0f;
            var distanceDelta = planarDelta.magnitude;
            var targetYaw = LocomotionMath.YawFromPlanarDirection(facingDirection);
            var currentYaw = agent.Agent.rotation.eulerAngles.y;
            var turnDegrees = LocomotionMath.SignedYawDelta(currentYaw, targetYaw);

            agent.Agent.position = position;
            if (facingDirection.sqrMagnitude > 0.001f)
            {
                agent.Agent.rotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
            }

            var animator = agent.Agent.GetComponent<MiniBotWalkAnimator>();
            if (animator != null)
            {
                var hasPreviousTime = previousSampleTimes.TryGetValue(agent.AgentId, out var previousTime);
                var deltaTime = Mathf.Max(1f / 30f, sampleTime - previousTime);
                var speed = Mathf.Clamp(distanceDelta / deltaTime, 0f, agent.WalkSpeedMetersPerSecond * 1.65f);
                var isMoving = allowWalkAnimation && distanceDelta > 0.006f;
                animator.SetMotionIntent(isMoving, turnDegrees, speed, isMoving ? distanceDelta : 0f);
                var walkedDistance = ResolveWalkedDistance(agent.AgentId, distanceDelta, isMoving, hasPreviousTime, sampleTime, previousTime);
                animator.SampleDistanceSyncedPose(walkedDistance, isMoving, turnDegrees);
            }

            previousPositions[agent.AgentId] = position;
            previousSampleTimes[agent.AgentId] = sampleTime;

            if (agent.EmotionMarker != null)
            {
                agent.EmotionMarker.gameObject.SetActive(markerVisible);
            }
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
