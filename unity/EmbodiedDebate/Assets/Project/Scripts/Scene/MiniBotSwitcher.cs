using System.Collections.Generic;
using UnityEngine;
using ArgusUnity.Locomotion;

namespace ArgusUnity.Scene
{
    public sealed class MiniBotSwitcher : MonoBehaviour
    {
        [System.Serializable]
        public sealed class BotEntry
        {
            public Transform root;
            public Color bodyColor = Color.white;
        }

        [SerializeField]
        private List<BotEntry> bots = new List<BotEntry>();
        [SerializeField]
        private int startIndex = 0;

        private int activeIndex = -1;

        private void Awake()
        {
            for (var i = 0; i < bots.Count; i++)
            {
                if (bots[i].root == null)
                    continue;
                ApplyColor(bots[i]);
            }
            activeIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, bots.Count - 1));
            for (var i = 0; i < bots.Count; i++)
            {
                if (bots[i].root == null)
                    continue;
                SetBotEnabled(bots[i].root, i == activeIndex);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTo(2);
        }

        private void SwitchTo(int index)
        {
            if (index < 0 || index >= bots.Count)
                return;
            if (index == activeIndex)
                return;

            for (var i = 0; i < bots.Count; i++)
            {
                if (bots[i].root == null)
                    continue;
                SetBotEnabled(bots[i].root, i == index);
            }

            activeIndex = index;
        }

        private static void SetBotEnabled(Transform root, bool enabled)
        {
            if (root.TryGetComponent<AnimationHandler>(out var anim))
                anim.enabled = enabled;

            if (root.TryGetComponent<MiniBotObjectInteractor>(out var interactor))
                interactor.enabled = enabled;

            if (root.TryGetComponent<MiniBotTalkInteractor>(out var talker))
                talker.enabled = enabled;
        }

        private static void ApplyColor(BotEntry entry)
        {
            if (entry.root == null)
                return;

            foreach (var renderer in entry.root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = new Material(renderer.sharedMaterial)
                {
                    color = entry.bodyColor
                };
            }
        }
    }
}
