using UnityEngine;

namespace ArgusUnity.Scene
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PushableObject : MonoBehaviour
    {
        [SerializeField]
        private float pushAcceleration = 4f;
        [SerializeField]
        private float maxPlanarSpeed = 1.2f;
        [SerializeField]
        private float minPlanarSpeed = 0.3f;
        [SerializeField]
        private float pushAnimationHoldTime = 0.25f;
        [SerializeField]
        private Color highlightColor = new Color(0.25f, 0.9f, 1f, 1f);
        [SerializeField]
        private float highlightLineWidth = 0.025f;
        [SerializeField]
        private float highlightLightIntensity = 0.96f;
        [SerializeField]
        private float maxRigidbodySpeed = 2.5f;

        private Rigidbody body;
        private Collider[] colliders;
        private GameObject highlightRoot;
        private LineRenderer[] highlightEdges;
        private Light highlightLight;
        private Material highlightMaterial;
        private bool highlighted;
        private bool held;
        private float currentPlanarSpeed;
        private Vector3 lastMoveDir;
        private float lastMoveSpeed;

        public float PushAnimationHoldTime => pushAnimationHoldTime;
        public Collider[] Colliders => colliders ?? GetColliders();
        public Vector3 Position => transform.position;

        private void Awake()
        {
            ConfigureBody();
            GetColliders();
        }

        private void Reset()
        {
            ConfigureBody();
        }

        private void LateUpdate()
        {
            if (highlighted)
                UpdateHighlightFromCollider();

            if (body == null)
                return;
            var vh = body.velocity;
            vh.y = 0f;
            var hz = vh.magnitude;
            if (hz > maxRigidbodySpeed)
            {
                var clamped = vh.normalized * maxRigidbodySpeed;
                body.velocity = new Vector3(clamped.x, body.velocity.y, clamped.z);
            }
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
            EnsureHighlight();
            if (highlightRoot != null)
                highlightRoot.SetActive(value);
        }

        public void BeginHold()
        {
            held = true;
            currentPlanarSpeed = 0f;
            ApplyWeightSpeed();
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void ApplyWeightSpeed()
        {
            if (body == null)
                return;
            var t = Mathf.Clamp01((body.mass - 1f) / 34f);
            maxPlanarSpeed = Mathf.Lerp(Mathf.Max(minPlanarSpeed, 0.1f) * 4f, minPlanarSpeed, t);
        }

        public void EndHold(Vector3 releaseDir, float releaseSpeed)
        {
            held = false;
            currentPlanarSpeed = 0f;
            var speed = Mathf.Max(releaseSpeed, lastMoveSpeed);
            if (speed > 0.01f && releaseDir.sqrMagnitude > 0.0001f)
            {
                var dir = releaseDir;
                dir.y = 0f;
                dir.Normalize();
                body.velocity = dir * speed;
                var grabPoint = transform.position + dir * 0.3f + Vector3.up * 0.5f;
                body.AddForceAtPosition(dir * speed * body.mass * 3f, grabPoint, ForceMode.Impulse);
            }
        }

        public void Nudge(Vector3 worldDirection, float force, Vector3 worldPoint)
        {
            if (held || body == null || body.isKinematic)
                return;

            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <= 0.0001f)
                return;

            worldDirection.Normalize();
            body.AddForceAtPosition(worldDirection * force, worldPoint, ForceMode.Impulse);
        }

        public void MoveHeld(Vector3 targetPosition, Vector3 grabPoint)
        {
            if (body == null)
                return;

            var currentPosition = transform.position;
            targetPosition.y = currentPosition.y;
            var delta = targetPosition - currentPosition;
            delta.y = 0f;
            var distance = delta.magnitude;
            if (distance <= 0.0001f)
                return;

            var direction = delta / distance;
            currentPlanarSpeed = Mathf.MoveTowards(
                currentPlanarSpeed,
                maxPlanarSpeed,
                pushAcceleration * Time.fixedDeltaTime
            );
            var forceMag = currentPlanarSpeed * body.mass * 10f;
            if (currentPlanarSpeed > maxPlanarSpeed * 0.7f)
                forceMag *= 1f + (currentPlanarSpeed - maxPlanarSpeed * 0.7f) / maxPlanarSpeed * 1.5f;
            body.AddForceAtPosition(direction * forceMag, grabPoint, ForceMode.Force);
            lastMoveDir = direction;
            lastMoveSpeed = currentPlanarSpeed;
        }

        private void ConfigureBody()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body == null)
                return;

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.angularDrag = 5f;
            body.drag = 2f;
            body.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
        }

        private Collider[] GetColliders()
        {
            // ponytail: root-only. Children (Cube.005_1, locker_1, ...) carry no
            // collider/rigidbody by design; the top-level model is the body.
            colliders = GetComponents<Collider>();
            return colliders;
        }

        private void EnsureHighlight()
        {
            if (highlightRoot != null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                return;

            highlightMaterial = new Material(shader)
            {
                color = highlightColor
            };

            highlightRoot = new GameObject("Pushable Highlight");
            highlightRoot.transform.SetParent(transform, false);
            highlightEdges = new LineRenderer[12];
            for (var i = 0; i < highlightEdges.Length; i++)
            {
                var edge = new GameObject("Edge " + i);
                edge.transform.SetParent(highlightRoot.transform, false);
                var line = edge.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = highlightLineWidth;
                line.endWidth = highlightLineWidth;
                line.material = highlightMaterial;
                line.startColor = highlightColor;
                line.endColor = highlightColor;
                highlightEdges[i] = line;
            }

            var lightObject = new GameObject("Highlight Light");
            lightObject.transform.SetParent(highlightRoot.transform, false);
            highlightLight = lightObject.AddComponent<Light>();
            highlightLight.type = LightType.Point;
            highlightLight.color = highlightColor;
            highlightLight.intensity = highlightLightIntensity;
            highlightLight.range = 2.2f;
            highlightRoot.SetActive(false);
        }

        private void UpdateHighlightFromCollider()
        {
            if (highlightEdges == null)
                return;

            var box = GetPrimaryBoxCollider();
            if (box == null)
                return;

            // Build the box extents in the object's local space, then convert each
            // corner through the object's world transform so rotation/scale is handled.
            var center = box.center;
            var halfSize = box.size * 0.5f;
            var localCorners = new[]
            {
                new Vector3(center.x - halfSize.x, center.y - halfSize.y, center.z - halfSize.z),
                new Vector3(center.x + halfSize.x, center.y - halfSize.y, center.z - halfSize.z),
                new Vector3(center.x + halfSize.x, center.y - halfSize.y, center.z + halfSize.z),
                new Vector3(center.x - halfSize.x, center.y - halfSize.y, center.z + halfSize.z),
                new Vector3(center.x - halfSize.x, center.y + halfSize.y, center.z - halfSize.z),
                new Vector3(center.x + halfSize.x, center.y + halfSize.y, center.z - halfSize.z),
                new Vector3(center.x + halfSize.x, center.y + halfSize.y, center.z + halfSize.z),
                new Vector3(center.x - halfSize.x, center.y + halfSize.y, center.z + halfSize.z)
            };

            var worldCorners = new Vector3[8];
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (var i = 0; i < localCorners.Length; i++)
            {
                worldCorners[i] = transform.TransformPoint(localCorners[i]);
                min = Vector3.Min(min, worldCorners[i]);
                max = Vector3.Max(max, worldCorners[i]);
            }

            SetEdge(0, worldCorners[0], worldCorners[1]);
            SetEdge(1, worldCorners[1], worldCorners[2]);
            SetEdge(2, worldCorners[2], worldCorners[3]);
            SetEdge(3, worldCorners[3], worldCorners[0]);
            SetEdge(4, worldCorners[4], worldCorners[5]);
            SetEdge(5, worldCorners[5], worldCorners[6]);
            SetEdge(6, worldCorners[6], worldCorners[7]);
            SetEdge(7, worldCorners[7], worldCorners[4]);
            SetEdge(8, worldCorners[0], worldCorners[4]);
            SetEdge(9, worldCorners[1], worldCorners[5]);
            SetEdge(10, worldCorners[2], worldCorners[6]);
            SetEdge(11, worldCorners[3], worldCorners[7]);

            if (highlightLight != null)
            {
                highlightLight.transform.position = (min + max) * 0.5f;
                highlightLight.range = Mathf.Max(1f, ((max - min) * 0.5f).magnitude * 1.7f);
            }
        }

        private BoxCollider GetPrimaryBoxCollider()
        {
            var rootBox = GetComponent<BoxCollider>();
            if (rootBox != null && rootBox.enabled)
                return rootBox;

            foreach (var childBox in GetComponentsInChildren<BoxCollider>())
            {
                if (childBox != null && childBox.enabled)
                    return childBox;
            }

            return null;
        }

        private void SetEdge(int index, Vector3 start, Vector3 end)
        {
            highlightEdges[index].SetPosition(0, start);
            highlightEdges[index].SetPosition(1, end);
        }

        private Bounds GetRenderBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(transform.position, Vector3.one * 0.2f);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            bounds.Expand(0.04f);
            return bounds;
        }
    }
}
