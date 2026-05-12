using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MinibotConversationCameraRig : MonoBehaviour
    {
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private Vector3 overviewPosition = new Vector3(7.4f, 5.2f, -7.7f);

        [SerializeField]
        private Vector3 conversationOffset = new Vector3(3.9f, 2.45f, -4.2f);

        [SerializeField]
        private float overviewFov = 42f;

        [SerializeField]
        private float conversationFov = 34f;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            RefreshImmediate();
        }

        public void Initialize(Camera camera)
        {
            targetCamera = camera;
        }

        public void RefreshImmediate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (TryFindActivePair(out var center, out var span))
            {
                var distanceBoost = Mathf.Clamp(span * 0.35f, 0f, 1.4f);
                ApplyCamera(center + conversationOffset + Vector3.back * distanceBoost, center + Vector3.up * 0.9f, conversationFov);
                return;
            }

            ApplyCamera(overviewPosition, Vector3.up * 0.75f, overviewFov);
        }

        private void ApplyCamera(Vector3 position, Vector3 target, float fov)
        {
            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, position, 0.35f);
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                Quaternion.LookRotation(target - targetCamera.transform.position, Vector3.up),
                0.45f);
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, fov, 0.3f);
        }

        private static bool TryFindActivePair(out Vector3 center, out float span)
        {
            if (TryFindPairInPhase(MiniBotSocialPhase.Chat, out center, out span) ||
                TryFindPairInPhase(MiniBotSocialPhase.React, out center, out span) ||
                TryFindPairInPhase(MiniBotSocialPhase.Approach, out center, out span))
            {
                return true;
            }

            center = Vector3.zero;
            span = 0f;
            return false;
        }

        private static bool TryFindPairInPhase(MiniBotSocialPhase phase, out Vector3 center, out float span)
        {
            foreach (var blackboard in FindObjectsOfType<MinibotBlackboard>())
            {
                if (blackboard.SocialPhase != phase)
                {
                    continue;
                }

                var partner = FindAgent(blackboard.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                center = Vector3.Lerp(blackboard.transform.position, partner.position, 0.5f);
                span = Vector3.Distance(blackboard.transform.position, partner.position);
                return true;
            }

            center = Vector3.zero;
            span = 0f;
            return false;
        }

        private static Transform FindAgent(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                return null;
            }

            foreach (var blackboard in FindObjectsOfType<MinibotBlackboard>())
            {
                if (blackboard.AgentId == agentId)
                {
                    return blackboard.transform;
                }
            }

            return null;
        }
    }
}
