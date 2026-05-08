using System.Collections.Generic;
using UnityEngine;

namespace ArgusUnity.Runtime
{
    public sealed class MiniBotPersonaInteractionScenario : MonoBehaviour
    {
        private readonly List<Transform> agents = new List<Transform>();
        private readonly List<Vector3> starts = new List<Vector3>();
        private readonly List<Vector3> targets = new List<Vector3>();

        public void RegisterAgent(Transform agent, Vector3 target)
        {
            agents.Add(agent);
            starts.Add(agent.position);
            targets.Add(target);
        }

        private void Update()
        {
            var blend = 0.5f + Mathf.Sin(Time.time * 0.75f) * 0.5f;
            for (var i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                var target = targets[i];
                agent.position = Vector3.Lerp(starts[i], target, blend * 0.35f);

                var look = Vector3.zero - agent.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.001f)
                {
                    agent.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
                }
            }
        }
    }
}
