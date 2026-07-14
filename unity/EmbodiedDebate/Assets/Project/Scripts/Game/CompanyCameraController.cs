using UnityEngine;

namespace ArgusUnity.Game
{
    /// <summary>
    /// Compact quarter-view camera controls: middle-drag pan, wheel zoom, Q/E rotate, F focus.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CompanyCameraController : MonoBehaviour
    {
        [SerializeField] private float panSpeed = 0.025f;
        [SerializeField] private float zoomSpeed = 1.6f;
        [SerializeField] private float rotateSpeed = 70f;
        [SerializeField] private float minOrthographicSize = 8f;
        [SerializeField] private float maxOrthographicSize = 30f;

        private Camera controlledCamera;
        private Vector3 lastMousePosition;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
            HandleRotation();
            HandleFocus();
        }

        private void HandlePan()
        {
            if (Input.GetMouseButtonDown(2))
            {
                lastMousePosition = Input.mousePosition;
            }

            if (!Input.GetMouseButton(2))
            {
                return;
            }

            var delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            var right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            transform.position -= (right * delta.x + forward * delta.y) * panSpeed;
        }

        private void HandleZoom()
        {
            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f)
            {
                return;
            }

            if (controlledCamera.orthographic)
            {
                controlledCamera.orthographicSize = Mathf.Clamp(
                    controlledCamera.orthographicSize - wheel * zoomSpeed,
                    minOrthographicSize,
                    maxOrthographicSize);
            }
            else
            {
                transform.position += transform.forward * wheel * zoomSpeed;
            }
        }

        private void HandleRotation()
        {
            var axis = 0f;
            if (Input.GetKey(KeyCode.Q))
            {
                axis -= 1f;
            }
            if (Input.GetKey(KeyCode.E))
            {
                axis += 1f;
            }
            if (Mathf.Abs(axis) < 0.01f)
            {
                return;
            }

            transform.RotateAround(Vector3.zero, Vector3.up, axis * rotateSpeed * Time.deltaTime);
        }

        private void HandleFocus()
        {
            if (!Input.GetKeyDown(KeyCode.F))
            {
                return;
            }

            var players = FindObjectsOfType<CompanyMiniBotActor>();
            foreach (var actor in players)
            {
                if (actor.Role != CompanyActorRole.Player || actor.CompanyId != "p1")
                {
                    continue;
                }

                var offset = transform.position - Vector3.zero;
                transform.position = actor.transform.position + offset;
                return;
            }
        }
    }
}
