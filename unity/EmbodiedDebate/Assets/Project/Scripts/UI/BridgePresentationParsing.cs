using System;
using System.Collections.Generic;
using ArgusUnity.Bridge;
using Newtonsoft.Json.Linq;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Extracts dialogue and emotion payloads from validated bridge envelopes.
    /// Kept allocation-light for Unity main-thread use and covered by EditMode tests.
    /// </summary>
    public static class BridgePresentationParsing
    {
        public static bool TryGetDialogue(BridgeEnvelope envelope, out DialogueViewModel vm)
        {
            vm = default;
            if (envelope == null || envelope.Type != "agent.dialogue" || envelope.Payload == null)
            {
                return false;
            }

            var speaker = envelope.Payload["speaker_id"]?.ToObject<string>();
            var text = envelope.Payload["text"]?.ToObject<string>();
            if (string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var durationMs = ReadNonNegativeInt(envelope.Payload["duration_ms"], 4000);

            vm = new DialogueViewModel(speaker.Trim(), text.Trim(), durationMs);
            return true;
        }

        public static bool TryGetAgentEmotion(BridgeEnvelope envelope, out AgentEmotionViewModel vm)
        {
            vm = default;
            if (envelope == null || envelope.Payload == null)
            {
                return false;
            }

            string agentId;
            string label;
            JToken intensityToken;
            if (envelope.Type == "agent.emotion")
            {
                agentId = envelope.Payload["agent_id"]?.ToObject<string>();
                label = envelope.Payload["label"]?.ToObject<string>();
                intensityToken = envelope.Payload["intensity"];
            }
            else if (envelope.Type == "agent.behavior")
            {
                agentId = envelope.Payload["agent_id"]?.ToObject<string>();
                var emotion = envelope.Payload["emotion"] as JObject;
                label = emotion?["label"]?.ToObject<string>();
                intensityToken = emotion?["intensity"];
            }
            else
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            float intensity = 1f;
            if (intensityToken != null && intensityToken.Type != JTokenType.Null &&
                intensityToken.Type != JTokenType.Undefined)
            {
                try
                {
                    intensity = intensityToken.Value<float>();
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (InvalidCastException)
                {
                    return false;
                }

                if (float.IsNaN(intensity) || float.IsInfinity(intensity))
                {
                    return false;
                }

                intensity = Clamp01(intensity);
            }

            vm = new AgentEmotionViewModel(agentId.Trim(), label.Trim(), intensity);
            return true;
        }

        public static bool TryGetGroupUpdate(BridgeEnvelope envelope, out GroupUpdateViewModel vm)
        {
            vm = default;
            if (envelope == null || envelope.Type != "group.update" || envelope.Payload == null)
            {
                return false;
            }

            var groupId = envelope.Payload["group_id"]?.ToObject<string>();
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return false;
            }

            if (!TryReadAgentIdList(envelope.Payload["member_agent_ids"], out var members))
            {
                return false;
            }

            var badge = envelope.Payload["badge_label"]?.ToObject<string>();
            var badgeDisplay = string.IsNullOrWhiteSpace(badge) ? groupId.Trim() : badge.Trim();

            vm = new GroupUpdateViewModel(groupId.Trim(), members, badgeDisplay);
            return true;
        }

        public static bool TryGetConflictUpdate(BridgeEnvelope envelope, out ConflictUpdateViewModel vm)
        {
            vm = default;
            if (envelope == null || envelope.Type != "conflict.update" || envelope.Payload == null)
            {
                return false;
            }

            var conflictId = envelope.Payload["conflict_id"]?.ToObject<string>();
            if (string.IsNullOrWhiteSpace(conflictId))
            {
                return false;
            }

            if (!TryReadAgentIdList(envelope.Payload["participant_ids"], out var participants))
            {
                return false;
            }

            var summary = envelope.Payload["public_summary"]?.ToObject<string>();
            if (string.IsNullOrWhiteSpace(summary))
            {
                return false;
            }

            var stage = envelope.Payload["stage"]?.ToObject<string>();
            if (string.IsNullOrWhiteSpace(stage))
            {
                return false;
            }

            var intensityToken = envelope.Payload["intensity"];
            float intensity = 0.5f;
            if (intensityToken != null && intensityToken.Type != JTokenType.Null)
            {
                try
                {
                    intensity = intensityToken.Value<float>();
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (InvalidCastException)
                {
                    return false;
                }

                if (float.IsNaN(intensity) || float.IsInfinity(intensity))
                {
                    return false;
                }

                intensity = Clamp01(intensity);
            }

            vm = new ConflictUpdateViewModel(
                conflictId.Trim(),
                participants,
                intensity,
                stage.Trim().ToLowerInvariant(),
                summary.Trim());
            return true;
        }

        private static bool TryReadAgentIdList(JToken token, out string[] ids)
        {
            ids = Array.Empty<string>();
            if (token is not JArray arr)
            {
                return false;
            }

            var list = new List<string>();
            foreach (var item in arr)
            {
                var s = item?.Type == JTokenType.String ? item.ToObject<string>() : null;
                if (!string.IsNullOrWhiteSpace(s))
                {
                    list.Add(s.Trim());
                }
            }

            if (list.Count == 0)
            {
                return false;
            }

            ids = list.ToArray();
            return true;
        }

        /// <summary>Shortens unreadably long captions for billboard display.</summary>
        public static string TruncateDialogue(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || maxChars <= 0)
            {
                return string.Empty;
            }

            return text.Length <= maxChars ? text : text.Substring(0, maxChars).TrimEnd() + "…";
        }

        private static int ReadNonNegativeInt(JToken token, int fallback)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
            {
                return fallback;
            }

            try
            {
                var value = token.Value<long>();
                if (value <= 0)
                {
                    return fallback;
                }

                return value > int.MaxValue ? int.MaxValue : (int)value;
            }
            catch (FormatException)
            {
                return fallback;
            }
            catch (InvalidCastException)
            {
                return fallback;
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }

    public readonly struct DialogueViewModel
    {
        public DialogueViewModel(string speakerId, string text, int durationMs)
        {
            SpeakerId = speakerId ?? string.Empty;
            Text = text ?? string.Empty;
            DurationMs = durationMs;
        }

        public string SpeakerId { get; }

        public string Text { get; }

        public int DurationMs { get; }
    }

    public readonly struct AgentEmotionViewModel
    {
        public AgentEmotionViewModel(string agentId, string label, float intensity)
        {
            AgentId = agentId ?? string.Empty;
            Label = label ?? string.Empty;
            Intensity = intensity;
        }

        public string AgentId { get; }

        public string Label { get; }

        public float Intensity { get; }
    }

    public readonly struct GroupUpdateViewModel
    {
        public GroupUpdateViewModel(string groupId, string[] memberAgentIds, string badgeDisplay)
        {
            GroupId = groupId ?? string.Empty;
            MemberAgentIds = memberAgentIds ?? Array.Empty<string>();
            BadgeDisplay = badgeDisplay ?? string.Empty;
        }

        public string GroupId { get; }

        public string[] MemberAgentIds { get; }

        public string BadgeDisplay { get; }
    }

    public readonly struct ConflictUpdateViewModel
    {
        public ConflictUpdateViewModel(
            string conflictId,
            string[] participantIds,
            float intensity,
            string stage,
            string publicSummary)
        {
            ConflictId = conflictId ?? string.Empty;
            ParticipantIds = participantIds ?? Array.Empty<string>();
            Intensity = intensity;
            Stage = stage ?? string.Empty;
            PublicSummary = publicSummary ?? string.Empty;
        }

        public string ConflictId { get; }

        public string[] ParticipantIds { get; }

        public float Intensity { get; }

        public string Stage { get; }

        public string PublicSummary { get; }
    }
}
