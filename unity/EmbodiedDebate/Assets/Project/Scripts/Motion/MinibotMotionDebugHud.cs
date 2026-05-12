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
            var debug = controller.DebugState;
            builder.Length = 0;
            builder.Append("MiniBot Motion\n");
            builder.Append("Agent: ").Append(controller.AgentId).Append('\n');
            builder.Append("State: ").Append(controller.State).Append('\n');
            builder.Append("Intent: ").Append(intent.Type).Append('\n');
            builder.Append("Selected Base: ").Append(debug.SelectedBaseClipName).Append('\n');
            builder.Append("Selected Overlay: ").Append(debug.SelectedOverlayClipName).Append('\n');
            builder.Append("Applied Base: ").Append(debug.CurrentBaseClipName).Append('\n');
            builder.Append("Applied Overlay: ").Append(debug.CurrentOverlayClipName).Append('\n');
            builder.Append("Speed: ").Append(controller.Motor.CurrentVelocity.magnitude.ToString("0.00")).Append(" m/s\n");
            builder.Append("Turn: ").Append(debug.CurrentTurn.ToString("0.00")).Append('\n');
            builder.Append("Distance: ").Append(controller.Motor.DistanceToTarget.ToString("0.00")).Append('\n');
            builder.Append("Emotion: ").Append(intent.Emotion).Append('\n');
            builder.Append("Gesture: ").Append(intent.Gesture).Append('\n');
            builder.Append("Action: ").Append(intent.Action).Append('\n');
            builder.Append("Stuck/Obstacle: ").Append(debug.IsStuck || controller.Motor.ObstacleAhead).Append('\n');
            builder.Append("Last recovery: ").Append(debug.LastRecoveryTime.ToString("0.00"));

            GUI.Box(new Rect(16f, 280f, 320f, 265f), builder.ToString());
        }
    }
}
