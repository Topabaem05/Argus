using System;
using UnityEngine;

namespace ArgusUnity.Motion
{
    [Serializable]
    public sealed class PersonaMotionProfile
    {
        [SerializeField]
        private string agentId = string.Empty;

        [SerializeField]
        private int seed = 1;

        [SerializeField]
        [Range(0f, 1f)]
        private float energy = 0.45f;

        [SerializeField]
        [Range(0f, 1f)]
        private float confidence = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        private float aggression = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        private float curiosity = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        private float anxiety = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        private float friendliness = 0.5f;

        public string AgentId
        {
            get => agentId;
            set => agentId = value ?? string.Empty;
        }

        public int Seed
        {
            get => seed;
            set => seed = value == 0 ? 1 : value;
        }

        public float Energy
        {
            get => energy;
            set => energy = Mathf.Clamp01(value);
        }

        public float Confidence
        {
            get => confidence;
            set => confidence = Mathf.Clamp01(value);
        }

        public float Aggression
        {
            get => aggression;
            set => aggression = Mathf.Clamp01(value);
        }

        public float Curiosity
        {
            get => curiosity;
            set => curiosity = Mathf.Clamp01(value);
        }

        public float Anxiety
        {
            get => anxiety;
            set => anxiety = Mathf.Clamp01(value);
        }

        public float Friendliness
        {
            get => friendliness;
            set => friendliness = Mathf.Clamp01(value);
        }

        public static PersonaMotionProfile FromAgentId(string nextAgentId, int deterministicSeed)
        {
            var normalizedSeed = deterministicSeed == 0 ? StableHash(nextAgentId) : deterministicSeed;
            var profile = new PersonaMotionProfile
            {
                AgentId = nextAgentId ?? string.Empty,
                Seed = normalizedSeed
            };

            var positive = Mathf.Abs(normalizedSeed);
            profile.Energy = 0.25f + ((positive >> 1) & 15) / 30f;
            profile.Confidence = 0.25f + ((positive >> 5) & 15) / 30f;
            profile.Aggression = 0.10f + ((positive >> 9) & 15) / 40f;
            profile.Curiosity = 0.30f + ((positive >> 13) & 15) / 28f;
            profile.Anxiety = 0.10f + ((positive >> 17) & 15) / 42f;
            profile.Friendliness = 0.30f + ((positive >> 21) & 15) / 30f;
            return profile;
        }

        public float WeightFor(MotionClipId clipId)
        {
            switch (clipId)
            {
                case MotionClipId.Running2:
                case MotionClipId.Excited:
                case MotionClipId.Waving:
                    return 0.75f + Energy * 1.15f + Friendliness * 0.35f;
                case MotionClipId.Yelling:
                case MotionClipId.HardHeadNod:
                case MotionClipId.Charge:
                case MotionClipId.Push:
                    return 0.65f + Aggression * 1.45f + Confidence * 0.35f - Friendliness * 0.25f;
                case MotionClipId.LookAround:
                case MotionClipId.Thinking2:
                case MotionClipId.PickingUp:
                    return 0.70f + Curiosity * 1.35f;
                case MotionClipId.StepBackward:
                case MotionClipId.Dodging:
                case MotionClipId.Surprised:
                    return 0.70f + Anxiety * 1.35f - Aggression * 0.20f;
                case MotionClipId.Clapping:
                case MotionClipId.Talking:
                case MotionClipId.Talking2:
                    return 0.85f + Friendliness * 1.10f;
                case MotionClipId.ShakingHeadNo:
                case MotionClipId.ThoughtfulHeadNod:
                case MotionClipId.SadIdle:
                    return 0.70f + (1f - Confidence) * 0.95f;
                default:
                    return 1f;
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 5381;
                if (value != null)
                {
                    for (var i = 0; i < value.Length; i++)
                    {
                        hash = ((hash << 5) + hash) ^ value[i];
                    }
                }

                return hash == 0 ? 1 : hash;
            }
        }
    }
}
