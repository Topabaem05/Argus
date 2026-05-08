using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MiniBotFootstepPreview : MonoBehaviour
    {
        [SerializeField]
        private float stepCycleSeconds = 0.85f;

        [SerializeField]
        private float walkSpeedMetersPerSecond = 0.42f;

        private Transform leftUpLeg;
        private Transform rightUpLeg;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftFoot;
        private Transform rightFoot;
        private Vector3 startPosition;
        private Vector3 leftFootStart;
        private Vector3 rightFootStart;

        private void Awake()
        {
            startPosition = transform.position;
            leftUpLeg = FindBone("LeftUpLeg");
            rightUpLeg = FindBone("RightUpLeg");
            leftLeg = FindBone("LeftLeg");
            rightLeg = FindBone("RightLeg");
            leftFoot = FindBone("LeftFoot");
            rightFoot = FindBone("RightFoot");
            leftFootStart = leftFoot != null ? leftFoot.localPosition : Vector3.zero;
            rightFootStart = rightFoot != null ? rightFoot.localPosition : Vector3.zero;

            Debug.Log(
                "MiniBotFootstepPreview: rig bones " +
                $"leftFoot={leftFoot != null}, rightFoot={rightFoot != null}, " +
                $"leftLeg={leftLeg != null}, rightLeg={rightLeg != null}.");
        }

        private void Update()
        {
            var t = Time.time / Mathf.Max(0.01f, stepCycleSeconds);
            var leftPhase = Mathf.Sin(t * Mathf.PI * 2f);
            var rightPhase = Mathf.Sin((t + 0.5f) * Mathf.PI * 2f);

            transform.position = startPosition + Vector3.forward * (Time.time * walkSpeedMetersPerSecond);
            transform.rotation = Quaternion.Euler(0f, 18f, 0f);

            ApplyLegPose(leftUpLeg, leftLeg, leftFoot, leftFootStart, leftPhase);
            ApplyLegPose(rightUpLeg, rightLeg, rightFoot, rightFootStart, rightPhase);
        }

        private static void ApplyLegPose(
            Transform upperLeg,
            Transform lowerLeg,
            Transform foot,
            Vector3 footStart,
            float phase)
        {
            if (upperLeg != null)
            {
                upperLeg.localRotation = Quaternion.Euler(phase * 22f, 0f, 0f);
            }

            if (lowerLeg != null)
            {
                lowerLeg.localRotation = Quaternion.Euler(Mathf.Max(0f, -phase) * 28f, 0f, 0f);
            }

            if (foot != null)
            {
                foot.localRotation = Quaternion.Euler(-phase * 18f, 0f, 0f);
                var lift = Mathf.Max(0f, phase) * 0.04f;
                foot.localPosition = footStart + new Vector3(0f, lift, Mathf.Max(0f, phase) * 0.01f);
            }
        }

        private Transform FindBone(string suffix)
        {
            foreach (var child in GetComponentsInChildren<Transform>())
            {
                if (child.name.EndsWith(suffix, System.StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }
    }
}
