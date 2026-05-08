using UnityEngine;

namespace ArgusUnity.Runtime
{
    /// <summary>
    /// Lightweight keyboard controller for the isolated prototype space.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class MovablePrototypeController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 4.5f;

        [SerializeField]
        private float turnSpeed = 110f;

        [SerializeField]
        private float gravity = -18f;

        private CharacterController controller;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var turn = Input.GetAxisRaw("Horizontal");
            transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime);

            var forward = Input.GetAxisRaw("Vertical");
            var strafe = 0f;
            if (Input.GetKey(KeyCode.Q))
            {
                strafe -= 1f;
            }
            if (Input.GetKey(KeyCode.E))
            {
                strafe += 1f;
            }

            var planar = (transform.forward * forward) + (transform.right * strafe);
            if (planar.sqrMagnitude > 1f)
            {
                planar.Normalize();
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            var motion = (planar * moveSpeed) + (Vector3.up * verticalVelocity);
            controller.Move(motion * Time.deltaTime);
        }
    }
}
