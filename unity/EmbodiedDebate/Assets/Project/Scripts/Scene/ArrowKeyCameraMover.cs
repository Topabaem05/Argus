using UnityEngine;

namespace ArgusUnity.Scene
{
    public sealed class ArrowKeyCameraMover : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 4f;
        [SerializeField]
        private bool useUnscaledTime = true;

        private void Update()
        {
            if (!ArgusUnity.UI.InputGate.AllowGameplayInput)
            {
                return;
            }

            var input = Vector3.zero;
            if (Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
            if (Input.GetKey(KeyCode.UpArrow)) input.z += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) input.z -= 1f;
            if (input.sqrMagnitude <= 0.0001f)
                return;

            input.Normalize();
            var right = transform.right;
            var forward = transform.forward;
            right.y = 0f;
            forward.y = 0f;
            if (right.sqrMagnitude > 0.0001f) right.Normalize();
            if (forward.sqrMagnitude > 0.0001f) forward.Normalize();

            var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.position += (right * input.x + forward * input.z) * (moveSpeed * deltaTime);
        }
    }
}
