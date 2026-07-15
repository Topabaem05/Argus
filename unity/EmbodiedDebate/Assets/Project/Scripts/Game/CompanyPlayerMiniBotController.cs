using UnityEngine;

namespace ArgusUnity.Game
{
    /// <summary>
    /// Camera-relative WASD control for the local CEO minibot.
    /// Uses CharacterController so the prototype does not require a navigation package.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class CompanyPlayerMiniBotController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private CompanyMiniBotMotionDriver motionDriver;

        private CharacterController characterController;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (motionDriver == null)
            {
                motionDriver = GetComponent<CompanyMiniBotMotionDriver>();
            }

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        private void Update()
        {
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            var forward = Vector3.forward;
            var right = Vector3.right;
            var cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }

            var move = forward * input.y + right * input.x;
            if (move.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            var velocity = move * moveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
            motionDriver?.SetMotion(
                move.sqrMagnitude > 0.001f ? MiniBotMotionState.Walk : MiniBotMotionState.Idle);
        }
    }
}
