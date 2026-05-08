using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>Orients transform forward to face the main camera each frame.</summary>
    public sealed class BillboardToCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            transform.forward = cam.transform.forward;
        }
    }
}
