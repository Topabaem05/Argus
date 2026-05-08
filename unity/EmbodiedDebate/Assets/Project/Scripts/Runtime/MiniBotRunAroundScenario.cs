using System.Collections.Generic;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MiniBotRunAroundScenario : MonoBehaviour
    {
        [SerializeField]
        private Vector2 roomMin = new Vector2(-8.1f, -8.1f);

        [SerializeField]
        private Vector2 roomMax = new Vector2(8.1f, 8.1f);

        [SerializeField]
        private float wallMargin = 0.8f;

        private readonly List<Runner> runners = new List<Runner>();

        public void RegisterRunner(Transform agent, Vector3 center, float radius, float speed, float phase)
        {
            runners.Add(new Runner(agent, center, radius, speed, phase));
        }

        private void Update()
        {
            ApplyAtTime(Time.time);
        }

        public void ApplyAtTime(float sampleTime)
        {
            for (var i = 0; i < runners.Count; i++)
            {
                var runner = runners[i];
                var angle = runner.Phase + sampleTime * runner.Speed;
                var position = runner.Center + new Vector3(Mathf.Cos(angle) * runner.Radius, 0f, Mathf.Sin(angle) * runner.Radius);
                var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                var clamped = RoomNavigationMath.ClampPlanarWithInset(position, roomMin, roomMax, wallMargin);
                if ((clamped - position).sqrMagnitude > 0.0001f)
                {
                    tangent = RoomNavigationMath.WallContactRecoveryDirection(
                        position,
                        tangent,
                        roomMin,
                        roomMax,
                        wallMargin,
                        i % 2 == 0);
                    position = clamped;
                }

                runner.Agent.position = position;
                if (tangent.sqrMagnitude > 0.001f)
                {
                    runner.Agent.rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
                }
            }
        }

        private readonly struct Runner
        {
            public Runner(Transform agent, Vector3 center, float radius, float speed, float phase)
            {
                Agent = agent;
                Center = center;
                Radius = radius;
                Speed = speed;
                Phase = phase;
            }

            public Transform Agent { get; }
            public Vector3 Center { get; }
            public float Radius { get; }
            public float Speed { get; }
            public float Phase { get; }
        }
    }
}
