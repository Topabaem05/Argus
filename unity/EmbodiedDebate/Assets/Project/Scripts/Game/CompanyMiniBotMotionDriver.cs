using System.Collections.Generic;
using UnityEngine;

namespace ArgusUnity.Game
{
    public enum MiniBotMotionState
    {
        Idle,
        Walk,
        Carry,
        Work,
        Talk,
        Cheer,
        Complain,
        Refuse,
        Gossip,
        Quit
    }

    /// <summary>
    /// Small compatibility layer between game actions and an optional Animator controller.
    /// When clips are unavailable, a procedural pose keeps state changes legible in the prototype.
    /// </summary>
    public sealed class CompanyMiniBotMotionDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float proceduralAmplitude = 0.035f;
        [SerializeField] private float proceduralFrequency = 5f;

        private readonly HashSet<int> parameterHashes = new();
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private MiniBotMotionState state = MiniBotMotionState.Idle;
        private float stateStartedAt;

        public MiniBotMotionState State => state;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (visualRoot == null)
            {
                var childRenderer = GetComponentInChildren<Renderer>();
                visualRoot = childRenderer != null ? childRenderer.transform : transform;
            }

            baseLocalPosition = visualRoot.localPosition;
            baseLocalRotation = visualRoot.localRotation;
            CacheAnimatorParameters();
            SetMotion(MiniBotMotionState.Idle, true);
        }

        private void LateUpdate()
        {
            if (visualRoot == null || HasRuntimeController())
            {
                return;
            }

            var elapsed = Time.time - stateStartedAt;
            var phase = elapsed * proceduralFrequency;
            var bob = Mathf.Sin(phase) * proceduralAmplitude;
            var tilt = 0f;

            switch (state)
            {
                case MiniBotMotionState.Walk:
                case MiniBotMotionState.Carry:
                    tilt = Mathf.Sin(phase * 0.5f) * 4f;
                    break;
                case MiniBotMotionState.Work:
                    tilt = 8f + Mathf.Sin(phase) * 3f;
                    break;
                case MiniBotMotionState.Cheer:
                    bob = Mathf.Abs(Mathf.Sin(phase)) * proceduralAmplitude * 4f;
                    break;
                case MiniBotMotionState.Complain:
                case MiniBotMotionState.Refuse:
                    tilt = Mathf.Sin(phase * 0.6f) * 7f;
                    break;
                case MiniBotMotionState.Gossip:
                    tilt = Mathf.Sin(phase) * 5f;
                    break;
                case MiniBotMotionState.Quit:
                    tilt = -12f;
                    break;
            }

            visualRoot.localPosition = baseLocalPosition + Vector3.up * bob;
            visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(tilt, 0f, 0f);
        }

        public void SetMotion(MiniBotMotionState nextState, bool force = false)
        {
            if (!force && state == nextState)
            {
                return;
            }

            state = nextState;
            stateStartedAt = Time.time;
            ApplyAnimatorState();
        }

        private void CacheAnimatorParameters()
        {
            parameterHashes.Clear();
            if (animator == null)
            {
                return;
            }

            foreach (var parameter in animator.parameters)
            {
                parameterHashes.Add(parameter.nameHash);
            }
        }

        private void ApplyAnimatorState()
        {
            if (!HasRuntimeController())
            {
                return;
            }

            SetFloatIfPresent("Speed", state is MiniBotMotionState.Walk or MiniBotMotionState.Carry ? 1f : 0f);
            SetBoolIfPresent("IsCarrying", state == MiniBotMotionState.Carry);
            SetBoolIfPresent("IsWorking", state == MiniBotMotionState.Work);

            var trigger = state switch
            {
                MiniBotMotionState.Talk => "Talk",
                MiniBotMotionState.Cheer => "Cheer",
                MiniBotMotionState.Complain => "Complain",
                MiniBotMotionState.Refuse => "Refuse",
                MiniBotMotionState.Gossip => "Gossip",
                MiniBotMotionState.Quit => "Quit",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(trigger))
            {
                SetTriggerIfPresent(trigger);
            }
        }

        private bool HasRuntimeController()
        {
            return animator != null && animator.runtimeAnimatorController != null;
        }

        private void SetFloatIfPresent(string parameterName, float value)
        {
            var hash = Animator.StringToHash(parameterName);
            if (parameterHashes.Contains(hash))
            {
                animator.SetFloat(hash, value);
            }
        }

        private void SetBoolIfPresent(string parameterName, bool value)
        {
            var hash = Animator.StringToHash(parameterName);
            if (parameterHashes.Contains(hash))
            {
                animator.SetBool(hash, value);
            }
        }

        private void SetTriggerIfPresent(string parameterName)
        {
            var hash = Animator.StringToHash(parameterName);
            if (parameterHashes.Contains(hash))
            {
                animator.SetTrigger(hash);
            }
        }
    }
}
