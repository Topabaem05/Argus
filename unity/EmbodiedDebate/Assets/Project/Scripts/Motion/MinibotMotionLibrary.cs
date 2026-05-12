using System;
using UnityEngine;

namespace ArgusUnity.Motion
{
    [CreateAssetMenu(menuName = "Argus/MiniBot Motion Library")]
    public sealed class MinibotMotionLibrary : ScriptableObject
    {
        [SerializeField]
        private MotionSlot[] slots = DefaultSlots();

        public MotionSlot[] Slots => slots;

        public static MotionSlot[] DefaultSlots()
        {
            var clips = MotionCatalog.All;
            var slots = new MotionSlot[clips.Count];
            for (var i = 0; i < clips.Count; i++)
            {
                slots[i] = new MotionSlot(
                    clips[i].ClipId.ToString(),
                    clips[i].ClipName,
                    clips[i].LoopTime,
                    clips[i].Category == MotionCategory.Locomotion);
            }

            return slots;
        }
    }

    [Serializable]
    public struct MotionSlot
    {
        public MotionSlot(string id, string clipName, bool loopTime, bool locomotion)
        {
            this.id = id;
            this.clipName = clipName;
            this.loopTime = loopTime;
            this.locomotion = locomotion;
        }

        [SerializeField]
        private string id;

        [SerializeField]
        private string clipName;

        [SerializeField]
        private bool loopTime;

        [SerializeField]
        private bool locomotion;

        public string Id => id;

        public string ClipName => clipName;

        public bool LoopTime => loopTime;

        public bool Locomotion => locomotion;
    }
}
