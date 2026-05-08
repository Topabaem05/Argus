using UnityEngine;

namespace ArgusUnity.Robots
{
    /// <summary>
    /// Builds a compact, friendly robot silhouette from built-in primitives at <see cref="Awake"/>.
    /// For a rigged Sketchfab / Asset Store avatar, import under <c>Assets/ThirdParty/Robots</c> per docs
    /// and wire a prefab instead; this stays a safe, redistributable default.
    /// </summary>
    public sealed class CuteRobotPrefabInitializer : MonoBehaviour
    {
        private void Awake()
        {
            if (transform.childCount > 0)
            {
                return;
            }

            StripRootBuiltInMesh();
            BuildVisuals();
        }

        private void StripRootBuiltInMesh()
        {
            foreach (var col in GetComponents<Collider>())
            {
                Destroy(col);
            }

            foreach (var mf in GetComponents<MeshFilter>())
            {
                Destroy(mf);
            }

            foreach (var mr in GetComponents<MeshRenderer>())
            {
                Destroy(mr);
            }
        }

        private void BuildVisuals()
        {
            var bodyColor = new Color(0.52f, 0.74f, 0.92f);
            var headColor = new Color(0.84f, 0.92f, 0.99f);
            var accent = new Color(1f, 0.55f, 0.5f);
            var visorColor = new Color(0.13f, 0.18f, 0.26f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.54f, 0.4f, 0.54f);
            ApplyColorAndDropColliders(body, bodyColor);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 0.94f, 0f);
            head.transform.localScale = Vector3.one * 0.5f;
            ApplyColorAndDropColliders(head, headColor);

            var antenna = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            antenna.name = "Antenna";
            antenna.transform.SetParent(head.transform, false);
            antenna.transform.localPosition = new Vector3(0.12f, 0.52f, 0f);
            antenna.transform.localScale = Vector3.one * 0.16f;
            ApplyColorAndDropColliders(antenna, accent);

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0f, 0.38f);
            visor.transform.localScale = new Vector3(0.58f, 0.14f, 0.12f);
            ApplyColorAndDropColliders(visor, visorColor);
        }

        private static void ApplyColorAndDropColliders(GameObject go, Color color)
        {
            foreach (var c in go.GetComponents<Collider>())
            {
                Object.Destroy(c);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }
}
