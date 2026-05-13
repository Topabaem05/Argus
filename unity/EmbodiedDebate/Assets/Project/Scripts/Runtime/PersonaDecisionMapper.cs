namespace ArgusUnity.Runtime
{
    public readonly struct MinibotPersonaDecision
    {
        public MinibotPersonaDecision(
            string personaId,
            string archetype,
            MiniBotSocialPhase phase,
            string partnerId,
            string intent,
            string dialogueTone)
        {
            PersonaId = personaId;
            Archetype = archetype;
            Phase = phase;
            PartnerId = partnerId;
            Intent = intent;
            DialogueTone = dialogueTone;
        }

        public string PersonaId { get; }
        public string Archetype { get; }
        public MiniBotSocialPhase Phase { get; }
        public string PartnerId { get; }
        public string Intent { get; }
        public string DialogueTone { get; }
    }

    public readonly struct MinibotUnityAction
    {
        public MinibotUnityAction(
            string movement,
            string animation,
            string emotion,
            string gesture,
            string goal)
        {
            Movement = movement;
            Animation = animation;
            Emotion = emotion;
            Gesture = gesture;
            Goal = goal;
        }

        public string Movement { get; }
        public string Animation { get; }
        public string Emotion { get; }
        public string Gesture { get; }
        public string Goal { get; }
        public string DebugSummary => $"movement={Movement}, animation={Animation}, emotion={Emotion}, gesture={Gesture}, goal={Goal}";
    }

    public static class PersonaDecisionMapper
    {
        public static MinibotPersonaDecision FromSocialSnapshot(
            string agentId,
            string archetype,
            MiniBotSocialSnapshot snapshot)
        {
            var tone = snapshot.ActionLabel == "debate" || archetype == "skeptical"
                ? "careful"
                : snapshot.ActionLabel == "ask" || archetype == "curious"
                    ? "curious"
                    : "warm";
            return new MinibotPersonaDecision(
                agentId,
                archetype,
                snapshot.Phase,
                snapshot.PartnerId,
                snapshot.ActionLabel,
                tone);
        }

        public static MinibotUnityAction Map(MinibotPersonaDecision decision)
        {
            switch (decision.Phase)
            {
                case MiniBotSocialPhase.Approach:
                    return new MinibotUnityAction(
                        "approach_partner",
                        "walk_forward",
                        ResolveEmotion(decision),
                        "look_at_partner",
                        $"approach {decision.PartnerId}");
                case MiniBotSocialPhase.Chat:
                    return new MinibotUnityAction(
                        "hold_position",
                        "talk_idle",
                        ResolveEmotion(decision),
                        ResolveGesture(decision),
                        $"chat with {decision.PartnerId}");
                case MiniBotSocialPhase.React:
                    if (decision.Intent == "push")
                    {
                        return new MinibotUnityAction(
                            "hold_position",
                            "push_object",
                            "focused",
                            "push",
                            $"push {decision.PartnerId}");
                    }

                    if (decision.Intent == "pull")
                    {
                        return new MinibotUnityAction(
                            "hold_position",
                            "pull_object",
                            "focused",
                            "pull",
                            $"pull {decision.PartnerId}");
                    }

                    return new MinibotUnityAction(
                        "hold_position",
                        "react",
                        ResolveEmotion(decision),
                        ResolveGesture(decision),
                        $"react to {decision.PartnerId}");
                case MiniBotSocialPhase.Disperse:
                    return new MinibotUnityAction(
                        "walk_to_next_zone",
                        "walk_forward",
                        "calm",
                        "turn_away",
                        "resume wandering");
                default:
                    return new MinibotUnityAction(
                        "wander",
                        "walk_forward",
                        ResolveEmotion(decision),
                        "scan_room",
                        "free wander");
            }
        }

        private static string ResolveEmotion(MinibotPersonaDecision decision)
        {
            if (decision.Intent == "debate" || decision.Archetype == "skeptical")
            {
                return "focused";
            }

            if (decision.Intent == "ask" || decision.Archetype == "curious")
            {
                return "curious";
            }

            if (decision.Archetype == "cautious")
            {
                return "concerned";
            }

            if (decision.Archetype == "energetic")
            {
                return "bright";
            }

            return "friendly";
        }

        private static string ResolveGesture(MinibotPersonaDecision decision)
        {
            if (decision.Intent == "debate")
            {
                return "small_head_shake";
            }

            if (decision.Intent == "ask")
            {
                return "question_tilt";
            }

            return "nod";
        }
    }
}
