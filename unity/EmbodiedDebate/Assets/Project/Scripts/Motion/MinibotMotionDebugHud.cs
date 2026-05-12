using System.Text;
using UnityEngine;

namespace ArgusUnity.Motion
{
    public sealed class MinibotMotionDebugHud : MonoBehaviour
    {
        [SerializeField]
        private bool visible = true;

        [SerializeField]
        private MinibotMotionController controller;

        private readonly StringBuilder builder = new StringBuilder(256);

        private void Awake()
        {
            controller = controller ?? GetComponent<MinibotMotionController>();
        }

        private void OnGUI()
        {
            if (!visible || controller == null || controller.Motor == null)
            {
                return;
            }

            var intent = controller.CurrentIntent;
            builder.Length = 0;
            builder.Append("MiniBot Motion\n");
            builder.Append("Agent: ").Append(controller.AgentId).Append('\n');
            builder.Append("State: ").Append(controller.State).Append('\n');
            builder.Append("Speed: ").Append(controller.Motor.CurrentVelocity.magnitude.ToString("0.00")).Append(" m/s\n");
            builder.Append("Distance: ").Append(controller.Motor.DistanceToTarget.ToString("0.00")).Append('\n');
            builder.Append("Emotion: ").Append(intent.Emotion).Append('\n');
            builder.Append("Gesture: ").Append(intent.Gesture).Append('\n');
            builder.Append("Action: ").Append(intent.Action).Append('\n');
            builder.Append("Stuck/Obstacle: ").Append(controller.Motor.ObstacleAhead);

            GUI.Box(new Rect(16f, 280f, 250f, 165f), builder.ToString());
        }
    }
}
