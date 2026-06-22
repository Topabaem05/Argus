using UnityEngine;

namespace ArgusUnity.Motion
{
    /// <summary>
    /// Fixes the head bone's yaw to a target direction during locomotion.
    /// Runs in LateUpdate (after Animator) so it overrides animation-driven head rotation.
    /// Body/root rotation is untouched; only the head bone is adjusted.
    /// </summary>
    public sealed class HeadFixConstraint : MonoBehaviour
    {
        [SerializeField] private Transform headBone;
        [SerializeField] private bool fixYaw = true;
        [SerializeField] private bool fixPitch = false;
        [SerializeField] private bool fixRoll = false;
        [SerializeField] private float targetYaw = 0f;
        [SerializeField] private float yawStiffness = 10.0f;
        [SerializeField] private float snapThreshold = 0.5f;

        private bool _targetInitialized;

        public void SetTargetYaw(float degrees) => targetYaw = degrees;

        public float GetTargetYaw() => targetYaw;

        public void SetFixYaw(bool enabled) => fixYaw = enabled;

        public float GetCurrentYawDelta()
        {
            if (headBone == null)
            {
                return 0f;
            }

            return Mathf.Abs(Mathf.DeltaAngle(headBone.eulerAngles.y, targetYaw));
        }

        private void Awake()
        {
            if (headBone != null && !_targetInitialized)
            {
                targetYaw = headBone.eulerAngles.y;
                _targetInitialized = true;
            }
        }

        public void Tick()
        {
            ApplyHeadFix();
        }

        private void LateUpdate() => ApplyHeadFix();

        private void ApplyHeadFix()
        {
            if (headBone == null)
            {
                return;
            }

            var currentEuler = headBone.eulerAngles;

            if (fixYaw)
            {
                var delta = Mathf.DeltaAngle(currentEuler.y, targetYaw);
                var newYaw = Mathf.Abs(delta) <= snapThreshold
                    ? targetYaw
                    : Mathf.MoveTowardsAngle(currentEuler.y, targetYaw, yawStiffness * Time.deltaTime * 90f);
                currentEuler.y = newYaw;
            }

            if (fixPitch)
            {
                currentEuler.x = 0f;
            }

            if (fixRoll)
            {
                currentEuler.z = 0f;
            }

            headBone.eulerAngles = currentEuler;
        }
    }
}
