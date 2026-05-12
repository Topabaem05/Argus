using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MinibotBlackboard : MonoBehaviour
    {
        [SerializeField]
        private string agentId;

        [SerializeField]
        private string archetype;

        [SerializeField]
        private MiniBotSocialPhase socialPhase;

        [SerializeField]
        private string partnerId;

        [SerializeField]
        private string currentEmotion;

        [SerializeField]
        private string currentGoal;

        [SerializeField]
        private string mappedUnityAction;

        [SerializeField]
        private Vector3 movementTarget;

        [SerializeField]
        private float speedMetersPerSecond;

        [SerializeField]
        private float turnDegrees;

        public string AgentId => agentId;
        public string Archetype => archetype;
        public MiniBotSocialPhase SocialPhase => socialPhase;
        public string PartnerId => partnerId;
        public string CurrentEmotion => currentEmotion;
        public string CurrentGoal => currentGoal;
        public string MappedUnityAction => mappedUnityAction;
        public Vector3 MovementTarget => movementTarget;
        public float SpeedMetersPerSecond => speedMetersPerSecond;
        public float TurnDegrees => turnDegrees;

        public void ApplySocialState(
            string nextAgentId,
            string nextArchetype,
            MiniBotSocialSnapshot snapshot,
            MinibotUnityAction unityAction,
            Vector3 nextMovementTarget,
            float nextSpeedMetersPerSecond,
            float nextTurnDegrees)
        {
            agentId = nextAgentId;
            archetype = nextArchetype;
            socialPhase = snapshot.Phase;
            partnerId = snapshot.PartnerId;
            currentEmotion = unityAction.Emotion;
            currentGoal = unityAction.Goal;
            mappedUnityAction = unityAction.DebugSummary;
            movementTarget = nextMovementTarget;
            speedMetersPerSecond = nextSpeedMetersPerSecond;
            turnDegrees = nextTurnDegrees;
        }
    }
}
