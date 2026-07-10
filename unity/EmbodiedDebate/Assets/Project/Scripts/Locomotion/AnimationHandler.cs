using UnityEngine;
using ArgusUnity.Scene;

namespace ArgusUnity.Locomotion
{
    public class AnimationHandler : MonoBehaviour
    {
        [Header("The Character Animator")]
        [SerializeField]
        private Animator animator;

        [Header("Character Controller")]
        [SerializeField]
        private CharacterController controller;
        [SerializeField]
        private MiniBotObjectInteractor objectInteractor;
        [SerializeField]
        private float walkSpeed = 2f;
        [SerializeField]
        private float runSpeed = 3f;
        [SerializeField]
        private float mouseTurnSpeed = 120f;
        [SerializeField]
        private bool enableMouseLook = true;
        [SerializeField]
        private bool faceMoveDirection = true;
        [SerializeField]
        private float bodyRotationSpeed = 12f;
        [SerializeField]
        private float gravity = -9.81f;
        [SerializeField]
        private float interactionSpeedMultiplier = 0.45f;
        private float verticalVel;
        [SerializeField]
        private float headLookWeight = 0.7f;
        public bool FaceMoveDirection
        {
            get => faceMoveDirection;
            set => faceMoveDirection = value;
        }
        [SerializeField]
        private float headLookSpeed = 12f;
        [SerializeField]
        private float lookSmoothTime = 0.2f;
        private Camera mainCam;
        private float currentHeadWeight;
        private Vector3 smoothedLookTarget;
        private Vector3 lookTargetVel;
        private bool ikInit;
        [SerializeField]
        private Transform headBone;
        private Animator cachedParameterAnimator;
        private RuntimeAnimatorController cachedRuntimeController;
        private bool hasParamX;
        private bool hasParamY;
        private bool hasParamSpeed;
        private bool hasParamUpperSpeed;
        private bool hasParamTurn;
        private bool hasParamBlend;
        private bool hasParamIsRunning;
        private bool hasParamIsPushing;
        private bool hasParamIsPulling;
        private bool hasParamIsTalking;

        private float targetX = 0f;
        private float targetY = 0f;
        private Vector3 currentMoveDirection;
        private bool currentHasMoveInput;

        public Vector3 MoveDirection => currentMoveDirection;
        public bool HasMoveInput => currentHasMoveInput;
        public bool IsRunning => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public bool IsTalking { get; set; }

        private void Awake()
        {
            BindRig();
        }

        private void OnValidate()
        {
            BindRig();
        }

        private void BindRig()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (controller == null)
                controller = GetComponent<CharacterController>();
            if (objectInteractor == null)
                objectInteractor = GetComponent<MiniBotObjectInteractor>();
            if (animator != null && (headBone == null || !headBone.IsChildOf(animator.transform)))
                headBone = CanUseHumanoidIk() ? animator.GetBoneTransform(HumanBodyBones.Head) : FindHeadBone(animator.transform);
            RefreshAnimatorParameters();
        }

        // ponytail: nudge ungrabbed props when the bot walks into them.
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (objectInteractor != null && objectInteractor.IsInteracting)
                return;

            var pushable = hit.collider.GetComponentInParent<PushableObject>();
            if (pushable == null)
                return;

            var pushDir = hit.moveDirection;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude <= 0.01f)
                return;

            pushDir.Normalize();
            pushable.Nudge(pushDir, 1.5f, hit.point);
        }

        void Update()
        {
            if (mainCam == null) mainCam = Camera.main;

            bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (IsTalking)
            {
                targetX = 0f;
                targetY = 0f;
                currentHasMoveInput = false;
                currentMoveDirection = Vector3.zero;
                if (animator != null)
                {
                    EnsureAnimatorParameters();
                    if (hasParamX) animator.SetFloat("x", 0f);
                    if (hasParamY) animator.SetFloat("y", 0f);
                    if (hasParamSpeed) animator.SetFloat("speed", 0f);
                    if (hasParamIsTalking) animator.SetBool("isTalking", true);
                    animator.speed = 1f;
                }
                return;
            }

            targetX = 0f;
            targetY = 0f;

            if (Input.GetKey(KeyCode.W))
            {
                targetY = isRunning ? 1f : 0.5f;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                targetY = isRunning ? -1f : -0.5f;
            }

            if (Input.GetKey(KeyCode.A))
            {
                targetX = isRunning ? -1f : -0.5f;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                targetX = isRunning ? 1f : 0.5f;
            }

            var hasMoveInput = new Vector2(targetX, targetY).sqrMagnitude > 0.0001f;
            var inputAmount = hasMoveInput ? Mathf.Clamp01(new Vector2(targetX, targetY).magnitude) : 0f;
            currentHasMoveInput = hasMoveInput;

            Vector3 moveDir = Vector3.zero;
            if (hasMoveInput)
            {
                var camRight = mainCam != null ? mainCam.transform.right : transform.right;
                var camFwd = mainCam != null ? mainCam.transform.forward : transform.forward;
                camRight.y = 0f;
                camFwd.y = 0f;
                if (camRight.sqrMagnitude > 0.01f) camRight.Normalize();
                if (camFwd.sqrMagnitude > 0.01f) camFwd.Normalize();
                moveDir = (camRight * targetX + camFwd * targetY);
                if (moveDir.sqrMagnitude > 0.0001f)
                    moveDir.Normalize();
            }
            currentMoveDirection = moveDir;

            if (faceMoveDirection && hasMoveInput && moveDir.sqrMagnitude > 0.0001f)
            {
                var targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    bodyRotationSpeed * Time.deltaTime
                );
            }

            var mouseDelta = Input.GetAxis("Mouse X");
            if (enableMouseLook && Input.GetMouseButton(1) && Mathf.Abs(mouseDelta) > 0.01f)
            {
                transform.Rotate(Vector3.up, mouseDelta * mouseTurnSpeed * Time.deltaTime);
            }

            if (controller != null)
            {
                // While holding an object, the interactor drives the body so the
                // CharacterController does not fight the standoff logic.
                var interacting = objectInteractor != null && objectInteractor.IsInteracting;
                if (controller.isGrounded)
                    verticalVel = -0.5f;
                else
                    verticalVel += gravity * Time.deltaTime;

                var speed = isRunning ? runSpeed : walkSpeed;
                if (interacting)
                    speed *= interactionSpeedMultiplier;
                var horizontal = interacting ? Vector3.zero : moveDir * speed;
                controller.Move((horizontal + Vector3.up * verticalVel) * Time.deltaTime);
            }

            if (animator != null)
            {
                EnsureAnimatorParameters();
                var interacting = objectInteractor != null && objectInteractor.IsInteracting;
                // When the body faces the move direction, the animation should always read
                // as forward motion regardless of how far the rotation has caught up.
                var animationInputAmount = interacting ? 0f : inputAmount;
                if (hasParamX) animator.SetFloat("x", 0f);
                if (hasParamY) animator.SetFloat("y", animationInputAmount);
                if (hasParamSpeed) animator.SetFloat("speed", animationInputAmount);
                if (hasParamUpperSpeed) animator.SetFloat("Speed", animationInputAmount);
                if (hasParamTurn) animator.SetFloat("Turn", 0f);
                if (hasParamBlend) animator.SetFloat("Blend", animationInputAmount);
               if (hasParamIsRunning) animator.SetBool("isRunning", isRunning && hasMoveInput);
                // isPushing and isPulling must be mutually exclusive: the Animator's
                // Push->Pull transition requires isPushing==false, and the Locomotion->Push
                // transition is listed first, so if both are true the bot gets stuck in Push
                // and the Pull clip never plays. Use the interactor's exclusive flags.
                var pushing = interacting && objectInteractor != null && objectInteractor.IsPushing;
                var pulling = interacting && objectInteractor != null && objectInteractor.IsPulling;
                if (hasParamIsPushing) animator.SetBool("isPushing", pushing);
                if (hasParamIsPulling) animator.SetBool("isPulling", pulling);
                if (hasParamIsTalking) animator.SetBool("isTalking", IsTalking);
               animator.speed = 1f;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || mainCam == null) return;
            if (!CanUseHumanoidIk())
            {
                currentHeadWeight = 0f;
                return;
            }

            // While holding an object, look at the held object so the head does
            // not twist through the pushed/pulled prop toward the camera.
            var interacting = objectInteractor != null && objectInteractor.IsInteracting;
            Vector3 lookTargetPos;
            float targetWeight;
            if (interacting)
            {
                lookTargetPos = objectInteractor.transform.position + Vector3.up * 1.2f;
                targetWeight = headLookWeight;
            }
            else
            {
                var camDir = mainCam.transform.position - transform.position;
                camDir.y = 0f;
                if (camDir.sqrMagnitude <= 0.01f)
                {
                    currentHeadWeight = Mathf.MoveTowards(currentHeadWeight, 0f, headLookSpeed * Time.deltaTime);
                    animator.SetLookAtWeight(currentHeadWeight, 0f, 1f, 1f, 0.5f);
                    return;
                }
                lookTargetPos = transform.position + camDir.normalized * 5f + Vector3.up * 1.2f;
                targetWeight = headLookWeight;
            }
            currentHeadWeight = Mathf.MoveTowards(currentHeadWeight, targetWeight, headLookSpeed * Time.deltaTime);

            animator.SetLookAtWeight(currentHeadWeight, 0f, 1f, 1f, 0.5f);
            if (!ikInit)
            {
                smoothedLookTarget = lookTargetPos;
                ikInit = true;
            }
            smoothedLookTarget = Vector3.SmoothDamp(smoothedLookTarget, lookTargetPos, ref lookTargetVel, lookSmoothTime);
            animator.SetLookAtPosition(smoothedLookTarget);
        }

        private void LateUpdate()
        {
            if (animator == null || CanUseHumanoidIk() || headBone == null || mainCam == null) return;

            // Non-humanoid fallback: same held-object rule as the IK pass.
            var interacting = objectInteractor != null && objectInteractor.IsInteracting;
            var target = interacting
                ? objectInteractor.transform.position
                : mainCam.transform.position;
            target.y = headBone.position.y;
            var dir = target - headBone.position;
            if (dir.sqrMagnitude <= 0.01f) return;

            headBone.rotation = Quaternion.Slerp(
                headBone.rotation,
                Quaternion.LookRotation(dir.normalized, Vector3.up),
                headLookSpeed * Time.deltaTime
            );
        }

        private static Transform FindHeadBone(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                if (t.name.ToLowerInvariant().Contains("head"))
                    return t;
            }

            return null;
        }

        private bool CanUseHumanoidIk()
        {
            return animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid;
        }

        private void EnsureAnimatorParameters()
        {
            if (cachedParameterAnimator == animator && cachedRuntimeController == animator.runtimeAnimatorController)
                return;

            RefreshAnimatorParameters();
        }

        private void RefreshAnimatorParameters()
        {
            cachedParameterAnimator = animator;
            cachedRuntimeController = animator != null ? animator.runtimeAnimatorController : null;
            hasParamX = false;
            hasParamY = false;
            hasParamSpeed = false;
            hasParamUpperSpeed = false;
            hasParamTurn = false;
            hasParamBlend = false;
            hasParamIsRunning = false;
            hasParamIsPushing = false;
            hasParamIsPulling = false;
            hasParamIsTalking = false;

            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasParamX |= parameter.name == "x";
                    hasParamY |= parameter.name == "y";
                    hasParamSpeed |= parameter.name == "speed";
                    hasParamUpperSpeed |= parameter.name == "Speed";
                    hasParamTurn |= parameter.name == "Turn";
                    hasParamBlend |= parameter.name == "Blend";
                }
                else if (parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasParamIsRunning |= parameter.name == "isRunning";
                    hasParamIsPushing |= parameter.name == "isPushing";
                    hasParamIsPulling |= parameter.name == "isPulling";
                    hasParamIsTalking |= parameter.name == "isTalking";
                }
            }
        }
    }
}
