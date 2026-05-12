using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MiniBotKeyboardController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 1.05f;

        [SerializeField]
        private float acceleration = 5.4f;

        [SerializeField]
        private float deceleration = 7.2f;

        [SerializeField]
        private float maxTurnDegreesPerSecond = 320f;

        [SerializeField]
        private float steeringSharpness = 8f;

        private Rigidbody body;
        private bool inputLogged;
        private Vector3 smoothedMoveDirection;
        private bool hasSmoothedMoveDirection;

        private void Awake()
        {
            EnsureBody();
        }

        private void FixedUpdate()
        {
            EnsureBody();
            var rawDirection = ReadKeyboardDirection();
            var hasInput = rawDirection.sqrMagnitude > 0.001f;
            var direction = hasInput
                ? SmoothInputDirection(rawDirection, Time.fixedDeltaTime)
                : Vector3.zero;
            var targetVelocity = direction * moveSpeed;
            var currentVelocity = body.velocity;
            var currentPlanarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            var maxDelta = (hasInput ? acceleration : deceleration) * Time.fixedDeltaTime;
            var nextPlanarVelocity = Vector3.MoveTowards(currentPlanarVelocity, targetVelocity, maxDelta);
            body.velocity = new Vector3(nextPlanarVelocity.x, currentVelocity.y, nextPlanarVelocity.z);

            if (!hasInput)
            {
                hasSmoothedMoveDirection = false;
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                maxTurnDegreesPerSecond * Time.fixedDeltaTime));

            if (!inputLogged)
            {
                Debug.Log("MiniBotKeyboardController: keyboard control active for red test mini-bot.");
                inputLogged = true;
            }
        }

        private Vector3 SmoothInputDirection(Vector3 targetDirection, float dt)
        {
            var currentDirection = hasSmoothedMoveDirection
                ? smoothedMoveDirection
                : CurrentPlanarForward();
            smoothedMoveDirection = LocomotionMath.SmoothPlanarDirection(
                currentDirection,
                targetDirection,
                steeringSharpness,
                dt);
            hasSmoothedMoveDirection = true;
            return smoothedMoveDirection;
        }

        private Vector3 CurrentPlanarForward()
        {
            var forward = body != null ? body.rotation * Vector3.forward : transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        private void EnsureBody()
        {
            if (body != null)
            {
                return;
            }

            body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.isKinematic = false;
            body.useGravity = false;
            body.detectCollisions = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }

        private static Vector3 ReadKeyboardDirection()
        {
            var x = 0f;
            var z = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                x -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                x += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                z -= 1f;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                z += 1f;
            }

            var direction = new Vector3(x, 0f, z);
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }
    }
}
