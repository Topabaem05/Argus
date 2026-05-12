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
            return new[]
            {
                new MotionSlot("IdleNeutral", "Standing Idle", true, true),
                new MotionSlot("IdleBreathing", "Breathing Idle", true, true),
                new MotionSlot("IdleThinking", "Thinking-2", true, false),
                new MotionSlot("WalkForward", "Walking-3", true, true),
                new MotionSlot("RunForward", "Running-2", true, true),
                new MotionSlot("WalkBackward", "Walking Backward", true, true),
                new MotionSlot("StrafeLeft", "Left Strafe Walking", true, true),
                new MotionSlot("StrafeRight", "Right Strafe Walking", true, true),
                new MotionSlot("TurnLeft", "Left Turn", false, false),
                new MotionSlot("TurnRight", "Right Turn", false, false),
                new MotionSlot("Stop", "Stop Walking", false, false),
                new MotionSlot("Talk", "Talking", false, false),
                new MotionSlot("TalkAlt", "Talking-2", false, false),
                new MotionSlot("Nod", "Thoughtful Head Nod", false, false),
                new MotionSlot("ShakeNo", "Shaking Head No", false, false),
                new MotionSlot("LookAround", "Look Around", false, false),
                new MotionSlot("StepBack", "Step Backward", false, false),
                new MotionSlot("Hit", "Zombie Reaction Hit", false, false),
                new MotionSlot("Fall", "Falling Flat Impact", false, false),
                new MotionSlot("GetUp", "Getting Up", false, false)
            };
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
