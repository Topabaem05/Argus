using UnityEngine;

namespace ArgusUnity.Motion
{
    /// <summary>
    /// Procedural leg animation via raycast ground detection, stride-length triggers,
    /// and parabolic foot interpolation. Head/body rotation is untouched; only foot
    /// target Transforms are moved. Implements the procedural IK approach (방식 B).
    /// </summary>
    public sealed class ProceduralLegController : MonoBehaviour
    {
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private Transform footRaycastOrigin;

        [SerializeField] private float strideLength = 0.6f;
        [SerializeField] private float stepHeight = 0.12f;
        [SerializeField] private float stepSpeed = 4.0f;
        [SerializeField] private float maxStepDistance = 1.2f;
        [SerializeField] private float raycastDownDistance = 2.0f;
        [SerializeField] private float footSpacing = 0.18f;
        [SerializeField] private LayerMask groundLayer = ~0;

        public enum FootState { Planted, Stepping }

        private FootState _leftState = FootState.Planted;
        private FootState _rightState = FootState.Planted;
        private float _leftStepProgress;
        private float _rightStepProgress;
        private Vector3 _leftStepStart;
        private Vector3 _leftStepEnd;
        private Vector3 _rightStepStart;
        private Vector3 _rightStepEnd;

        private Vector3 _moveDirection = Vector3.forward;
        private float _moveSpeed;
        private Vector3 _lastGroundPoint = Vector3.zero;
        private bool _hasGround;

        /// <summary>Planar direction the character is moving (normalized).</summary>
        public void SetMoveDirection(Vector3 planarDir)
        {
            planarDir.y = 0f;
            if (planarDir.sqrMagnitude > 0.0001f)
            {
                _moveDirection = planarDir.normalized;
            }
        }

        /// <summary>Current planar movement speed in m/s.</summary>
        public void SetMoveSpeed(float mps) => _moveSpeed = Mathf.Max(0f, mps);

        public FootState GetFootState(bool left) => left ? _leftState : _rightState;

        public float GetStepProgress(bool left) => left ? _leftStepProgress : _rightStepProgress;

        /// <summary>Sets a fake ground point for testing without raycasting.</summary>
        public void SetGroundPointForTesting(Vector3 point)
        {
            _lastGroundPoint = point;
            _hasGround = true;
        }

        public void Tick()
        {
            UpdateInternal();
        }

        private void Update() => UpdateInternal();

        private void UpdateInternal()
        {
            if (footRaycastOrigin != null)
            {
                UpdateGroundPoint();
            }

            if (!_hasGround)
            {
                return;
            }

            var forward = _moveSpeed > 0.001f ? _moveDirection : transform.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            var leftIdeal = _lastGroundPoint - right * footSpacing + forward * 0.05f;
            var rightIdeal = _lastGroundPoint + right * footSpacing + forward * 0.05f;

            TryTriggerStep(leftFoot, leftIdeal, true);
            TryTriggerStep(rightFoot, rightIdeal, false);

            UpdateSteppingFoot(leftFoot, true);
            UpdateSteppingFoot(rightFoot, false);
        }

        private void UpdateGroundPoint()
        {
            var origin = footRaycastOrigin.position;
            if (Physics.Raycast(origin, Vector3.down, out var hit, raycastDownDistance, groundLayer))
            {
                _lastGroundPoint = hit.point;
                _hasGround = true;
            }
        }

        private void TryTriggerStep(Transform foot, Vector3 idealPosition, bool isLeft)
        {
            var state = isLeft ? _leftState : _rightState;
            if (state == FootState.Stepping)
            {
                return;
            }

            var otherStepping = isLeft ? _rightState == FootState.Stepping : _leftState == FootState.Stepping;
            if (otherStepping)
            {
                return;
            }

            if (foot == null)
            {
                return;
            }

            var distance = Vector3.Distance(
                new Vector3(foot.position.x, 0f, foot.position.z),
                new Vector3(idealPosition.x, 0f, idealPosition.z));

            if (distance >= strideLength)
            {
                var clampedEnd = Vector3.MoveTowards(foot.position, idealPosition, maxStepDistance);
                if (isLeft)
                {
                    _leftStepStart = foot.position;
                    _leftStepEnd = clampedEnd;
                    _leftStepProgress = 0f;
                    _leftState = FootState.Stepping;
                }
                else
                {
                    _rightStepStart = foot.position;
                    _rightStepEnd = clampedEnd;
                    _rightStepProgress = 0f;
                    _rightState = FootState.Stepping;
                }
            }
        }

        private void UpdateSteppingFoot(Transform foot, bool isLeft)
        {
            var state = isLeft ? _leftState : _rightState;
            if (state != FootState.Stepping || foot == null)
            {
                return;
            }

            var progress = isLeft ? _leftStepProgress : _rightStepProgress;
            var start = isLeft ? _leftStepStart : _rightStepStart;
            var end = isLeft ? _leftStepEnd : _rightStepEnd;

            progress += stepSpeed * Time.deltaTime;
            progress = Mathf.Clamp01(progress);

            var horizontal = Vector3.Lerp(start, end, progress);
            var y = start.y + stepHeight * Mathf.Sin(progress * Mathf.PI);
            foot.position = new Vector3(horizontal.x, y, horizontal.z);

            if (progress >= 1f)
            {
                foot.position = end;
                if (isLeft)
                {
                    _leftState = FootState.Planted;
                    _leftStepProgress = 0f;
                }
                else
                {
                    _rightState = FootState.Planted;
                    _rightStepProgress = 0f;
                }
            }
            else
            {
                if (isLeft)
                {
                    _leftStepProgress = progress;
                }
                else
                {
                    _rightStepProgress = progress;
                }
            }
        }
    }
}
