using UnityEngine;

namespace ArgusUnity.Game
{
    /// <summary>
    /// Runtime-generated selection ring and mood dot. Final art can replace the meshes without
    /// changing gameplay code, while the vertical slice stays fully visible with no sprite pack.
    /// </summary>
    [RequireComponent(typeof(CompanyMiniBotActor))]
    public sealed class CompanyMiniBotIndicator : MonoBehaviour
    {
        [SerializeField] private float ringRadius = 0.75f;
        [SerializeField] private float moodHeight = 1.9f;

        private CompanyMiniBotActor actor;
        private GameObject selectionRing;
        private GameObject moodDot;
        private Material ringMaterial;
        private Material moodMaterial;
        private int lastMood = -1;

        private void Awake()
        {
            actor = GetComponent<CompanyMiniBotActor>();
            BuildIndicators();
            RefreshSelection(CompanyMiniBotActor.Selected);
            RefreshMood(true);
        }

        private void OnEnable()
        {
            CompanyMiniBotActor.SelectionChanged += RefreshSelection;
        }

        private void OnDisable()
        {
            CompanyMiniBotActor.SelectionChanged -= RefreshSelection;
        }

        private void Update()
        {
            RefreshMood(false);
            if (moodDot != null && Camera.main != null)
            {
                moodDot.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            Destroy(ringMaterial);
            Destroy(moodMaterial);
        }

        private void BuildIndicators()
        {
            selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            selectionRing.name = "SelectionRing";
            selectionRing.transform.SetParent(transform, false);
            selectionRing.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            selectionRing.transform.localScale = new Vector3(ringRadius, 0.012f, ringRadius);
            RemoveCollider(selectionRing);
            ringMaterial = CreateMaterial(new Color(0.30f, 0.79f, 0.94f, 0.9f));
            selectionRing.GetComponent<Renderer>().sharedMaterial = ringMaterial;

            moodDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moodDot.name = "MoodDot";
            moodDot.transform.SetParent(transform, false);
            moodDot.transform.localPosition = new Vector3(0f, moodHeight, 0f);
            moodDot.transform.localScale = Vector3.one * 0.22f;
            RemoveCollider(moodDot);
            moodMaterial = CreateMaterial(Color.white);
            moodDot.GetComponent<Renderer>().sharedMaterial = moodMaterial;
            moodDot.SetActive(actor.Role == CompanyActorRole.Employee);
        }

        private void RefreshSelection(CompanyMiniBotActor selected)
        {
            if (selectionRing != null)
            {
                selectionRing.SetActive(selected == actor);
            }
            if (moodDot != null)
            {
                moodDot.SetActive(actor.Role == CompanyActorRole.Employee && selected != actor);
            }
        }

        private void RefreshMood(bool force)
        {
            if (actor == null || moodMaterial == null || (!force && lastMood == actor.Mood))
            {
                return;
            }

            lastMood = actor.Mood;
            moodMaterial.color = actor.Mood switch
            {
                >= 80 => new Color(0.56f, 0.75f, 0.43f),
                >= 40 => new Color(0.98f, 0.78f, 0.31f),
                >= 20 => new Color(0.97f, 0.59f, 0.12f),
                _ => new Color(0.98f, 0.25f, 0.27f)
            };
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader)
            {
                color = color,
                name = "ArgusRuntimeIndicator"
            };
            return material;
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}
