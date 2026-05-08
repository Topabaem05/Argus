using System;
using System.Collections.Generic;

namespace ArgusUnity.Robots
{
    public sealed class AnimationClipMap
    {
        public string Idle { get; set; } = "Idle";
        public string Walk { get; set; } = "Walk";
        public string Run { get; set; } = "Run";
        public string Speak { get; set; } = "Speak";
        public string Argue { get; set; } = "Argue";
        public string Push { get; set; } = "Push";
        public string Block { get; set; } = "Block";
        public string Stumble { get; set; } = "Stumble";
        public string Fall { get; set; } = "Fall";
        public string Recover { get; set; } = "Recover";
    }

    public sealed class AnimationMappingResult
    {
        public string Action { get; set; }
        public string ClipName { get; set; }
        public bool UsedFallback { get; set; }
        public string Warning { get; set; }
    }

    public sealed class AnimationStateMapper
    {
        private readonly AnimationClipMap clipMap;
        private readonly Dictionary<string, Func<string>> clipsByAction;

        public AnimationStateMapper(AnimationClipMap clipMap = null)
        {
            this.clipMap = clipMap ?? new AnimationClipMap();
            clipsByAction = new Dictionary<string, Func<string>>
            {
                ["idle"] = () => this.clipMap.Idle,
                ["walk"] = () => this.clipMap.Walk,
                ["run"] = () => this.clipMap.Run,
                ["speak"] = () => this.clipMap.Speak,
                ["argue"] = () => this.clipMap.Argue,
                ["push"] = () => this.clipMap.Push,
                ["block"] = () => this.clipMap.Block,
                ["stumble"] = () => this.clipMap.Stumble,
                ["fall"] = () => this.clipMap.Fall,
                ["recover"] = () => this.clipMap.Recover
            };
        }

        public AnimationMappingResult Resolve(string action)
        {
            var normalized = string.IsNullOrWhiteSpace(action)
                ? string.Empty
                : action.Trim().ToLowerInvariant();
            if (clipsByAction.TryGetValue(normalized, out var clipName))
            {
                return new AnimationMappingResult
                {
                    Action = normalized,
                    ClipName = clipName(),
                    UsedFallback = false,
                    Warning = null
                };
            }

            return new AnimationMappingResult
            {
                Action = normalized,
                ClipName = clipMap.Idle,
                UsedFallback = true,
                Warning = string.IsNullOrEmpty(normalized)
                    ? "Animation action was empty; falling back to idle."
                    : $"Unknown animation action '{action}'; falling back to idle."
            };
        }
    }
}
