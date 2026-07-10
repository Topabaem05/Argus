using UnityEngine;

using ArgusUnity.Locomotion;
namespace ArgusUnity.Scene
{
    public sealed class MiniBotObjectInteractor : MonoBehaviour
    {
        [SerializeField]
        private CharacterController controller;
        [SerializeField]
        private float interactionRadius = 0.9f;
        [SerializeField]
        private KeyCode interactKey = KeyCode.Space;
        [SerializeField]
        private float faceObjectSpeed = 8f;
        [SerializeField]
        private float holdStandoff = 0.7f;
        private AnimationHandler animationHandler;
        private bool wasFacingMoveDirection;

        private PushableObject highlighted;
        private PushableObject held;
        private Vector3 lastBotPosition;
        private bool isPulling;

        public bool IsInteracting => held != null;
        public bool IsPulling => isPulling;
        public bool IsPushing => held != null && !isPulling;

        private void Awake()
        {
            if (controller == null)
                controller = GetComponent<CharacterController>();
            if (animationHandler == null)
                animationHandler = GetComponent<AnimationHandler>();
            lastBotPosition = transform.position;
        }

        private void Update()
        {
            if (held == null)
                HighlightNearest();

            if (Input.GetKeyDown(interactKey) && held == null && highlighted != null)
                BeginHold(highlighted);

            if (Input.GetKeyUp(interactKey) && held != null)
                EndHold();
        }

        private void FixedUpdate()
        {
            if (held == null)
            {
                lastBotPosition = transform.position;
                return;
            }

            FaceHeldObject();

            UpdateHeldObject();
            UpdateInteractionMode();

            lastBotPosition = transform.position;
        }

        private void OnDisable()
        {
            EndHold();
            SetHighlighted(null);
        }

        private void HighlightNearest()
        {
            var nearest = FindNearestPushable();
            SetHighlighted(nearest);
        }

        private PushableObject FindNearestPushable()
        {
            var origin = transform.position + Vector3.up * 0.45f;
            var nearestDistance = interactionRadius;
            PushableObject nearest = null;
            foreach (var candidate in FindObjectsOfType<PushableObject>())
            {
                foreach (var candidateCollider in candidate.Colliders)
                {
                    if (candidateCollider == null || !candidateCollider.enabled)
                        continue;

                    var closest = candidateCollider.ClosestPoint(origin);
                    var distance = Vector3.Distance(origin, closest);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = candidate;
                    }
                }
            }

            return nearest;
        }

        private void BeginHold(PushableObject target)
        {
            held = target;
            held.BeginHold();
            PreserveFacingOverride(true);
            SetIgnoredCollision(held, true);
            SetHighlighted(held);
            lastBotPosition = transform.position;
        }

        private void EndHold()
        {
            if (held == null)
                return;

            SetIgnoredCollision(held, false);
            PreserveFacingOverride(false);
            var releaseDir = lastBotPosition - transform.position;
            var releaseSpeed = animationHandler != null && animationHandler.HasMoveInput
                ? (animationHandler.IsRunning ? 3f : 2f)
                : releaseDir.magnitude / Time.fixedDeltaTime;
            held.EndHold(releaseDir, releaseSpeed);
            held = null;
            isPulling = false;
            lastBotPosition = transform.position;
        }

        private void UpdateInteractionMode()
        {
            if (held == null || animationHandler == null || !animationHandler.HasMoveInput)
            {
                isPulling = false;
                return;
            }

            var toObject = held.Position - transform.position;
            toObject.y = 0f;
            if (toObject.sqrMagnitude <= 0.0001f)
            {
                isPulling = false;
                return;
            }

            var moveDir = animationHandler.MoveDirection;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude <= 0.0001f)
            {
                isPulling = false;
                return;
            }

            var axisAmount = Vector3.Dot(moveDir.normalized, toObject.normalized);
            isPulling = axisAmount < -0.05f;
        }

        private void UpdateHeldObject()
        {
            if (held == null || animationHandler == null)
                return;

            // ponytail: free movement. Move the held object by the full input
            // vector (diagonals included); push/pull anim is driven only by the
            // sign of the bot->object dot in UpdateInteractionMode, not here.
            if (animationHandler.HasMoveInput)
            {
                var moveDir = animationHandler.MoveDirection;
                moveDir.y = 0f;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    var grabPoint = held.Position + Vector3.back * 0.3f + Vector3.up * 0.5f;
                    held.MoveHeld(held.Position + moveDir, grabPoint);
                }
            }

            KeepStandoff();
        }

        private void KeepStandoff()
        {
            // Recompute each frame: the object may have just moved.
            if (held == null)
                return;

            var toObject = held.Position - transform.position;
            toObject.y = 0f;
            if (toObject.sqrMagnitude <= 0.0001f)
                return;

            var objectForward = toObject.normalized;
            HoldAtStandoff(objectForward);
        }

        private void HoldAtStandoff(Vector3 objectForward)
        {
            var toObject = held.Position - transform.position;
            toObject.y = 0f;
            var distance = toObject.magnitude;
            if (distance <= 0.0001f)
                return;

            var desiredPosition = held.Position - objectForward * holdStandoff;
            desiredPosition.y = transform.position.y;
            if ((desiredPosition - transform.position).sqrMagnitude > 0.0001f)
                controller.Move(desiredPosition - transform.position);
        }

        private void FaceHeldObject()
        {
            if (held == null)
                return;

            var toObject = held.Position - transform.position;
            toObject.y = 0f;
            if (toObject.sqrMagnitude <= 0.0001f)
                return;

            var targetRotation = Quaternion.LookRotation(toObject.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                faceObjectSpeed * Time.fixedDeltaTime
            );
        }

        private void PreserveFacingOverride(bool interacting)
        {
            if (animationHandler == null)
                return;

            if (interacting)
            {
                wasFacingMoveDirection = animationHandler.FaceMoveDirection;
                animationHandler.FaceMoveDirection = false;
            }
            else
            {
                animationHandler.FaceMoveDirection = wasFacingMoveDirection;
            }
        }

        private void SetHighlighted(PushableObject target)
        {
            if (highlighted == target)
                return;

            if (highlighted != null && highlighted != held)
                highlighted.SetHighlighted(false);

            highlighted = target;
            if (highlighted != null)
                highlighted.SetHighlighted(true);
        }

        private void SetIgnoredCollision(PushableObject pushable, bool ignored)
        {
            if (controller == null || pushable == null)
                return;

            foreach (var candidateCollider in pushable.Colliders)
            {
                if (candidateCollider != null)
                    Physics.IgnoreCollision(controller, candidateCollider, ignored);
            }
        }
    }
}
