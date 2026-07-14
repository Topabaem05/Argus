using UnityEngine;

namespace ArgusUnity.Game
{
    /// <summary>
    /// Lightweight employee locomotion/action controller driven by server task and SLM decisions.
    /// This is intentionally navigation-backend agnostic; NavMesh can replace destination movement later.
    /// </summary>
    public sealed class CompanyEmployeeAgentController : MonoBehaviour
    {
        [SerializeField] private float baseMoveSpeed = 2.8f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private float arrivalDistance = 0.25f;
        [SerializeField] private CompanyMiniBotActor actor;
        [SerializeField] private CompanyMiniBotMotionDriver motionDriver;

        private Vector3 destination;
        private bool hasDestination;
        private float efficiency = 1f;
        private MiniBotMotionState arrivalMotion = MiniBotMotionState.Work;

        public bool HasDestination => hasDestination;

        private void Awake()
        {
            actor ??= GetComponent<CompanyMiniBotActor>();
            motionDriver ??= GetComponent<CompanyMiniBotMotionDriver>();

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        private void Update()
        {
            if (!hasDestination)
            {
                return;
            }

            var delta = destination - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= arrivalDistance * arrivalDistance)
            {
                hasDestination = false;
                motionDriver?.SetMotion(arrivalMotion);
                return;
            }

            var direction = delta.normalized;
            transform.position += direction * (baseMoveSpeed * efficiency * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                turnSpeed * Time.deltaTime);
            motionDriver?.SetMotion(MiniBotMotionState.Walk);
        }

        public void AssignDestination(
            Vector3 worldDestination,
            string taskLabel,
            float decisionEfficiency = 1f,
            MiniBotMotionState motionOnArrival = MiniBotMotionState.Work)
        {
            destination = worldDestination;
            hasDestination = true;
            efficiency = Mathf.Clamp(decisionEfficiency, 0.15f, 1f);
            arrivalMotion = motionOnArrival;
            actor?.SetCurrentTask(taskLabel);
        }

        public void ClearTask(string status = "Idle")
        {
            hasDestination = false;
            actor?.SetCurrentTask(status);
            motionDriver?.SetMotion(MiniBotMotionState.Idle);
        }

        public void ApplyDecision(string action, float decisionEfficiency, string sideAction)
        {
            efficiency = Mathf.Clamp(decisionEfficiency, 0.15f, 1f);
            var next = action switch
            {
                "accept" => MiniBotMotionState.Talk,
                "reluctant_accept" => MiniBotMotionState.Complain,
                "complain" => MiniBotMotionState.Complain,
                "refuse" => MiniBotMotionState.Refuse,
                "quit" => MiniBotMotionState.Quit,
                _ => MiniBotMotionState.Idle
            };

            if (sideAction == "gossip")
            {
                next = MiniBotMotionState.Gossip;
            }
            else if (sideAction == "consider_quit")
            {
                next = MiniBotMotionState.Quit;
            }

            motionDriver?.SetMotion(next);
        }

        public void PreviewAction(string command)
        {
            var next = command switch
            {
                "praise" or "snack" or "raise" or "bonus" or "party" => MiniBotMotionState.Cheer,
                "scold" => MiniBotMotionState.Complain,
                "fire" => MiniBotMotionState.Quit,
                "gossip" => MiniBotMotionState.Gossip,
                "assign_task" => MiniBotMotionState.Work,
                _ => MiniBotMotionState.Talk
            };
            motionDriver?.SetMotion(next);
        }
    }
}
