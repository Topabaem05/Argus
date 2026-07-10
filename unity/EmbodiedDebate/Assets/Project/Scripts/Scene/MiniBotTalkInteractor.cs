using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArgusUnity.Locomotion;

namespace ArgusUnity.Scene
{
    public sealed class MiniBotTalkInteractor : MonoBehaviour
    {
        [SerializeField]
        private float talkRange = 1.5f;
        [SerializeField]
        private KeyCode talkKey = KeyCode.T;
        [SerializeField]
        private Color highlightColor = new Color(0.3f, 0.9f, 0.4f, 0.5f);

        [Header("Dialogue")]
        [SerializeField]
        private string[] placeholderLines = new[]
        {
            "안녕?",
            "반가워!",
            "요즘 어때?"
        };
        [SerializeField]
        private float lineDuration = 2f;
        [SerializeField]
        private float bubbleHeight = 2.2f;

        private AnimationHandler activeBot;
        private MiniBotSwitcher switcher;
        private Transform talkTarget;
        private GameObject talkHighlight;
        private bool isTalking;
        private bool wasTargetHandlerEnabled;

        private Canvas dialogueCanvas;
        private Text dialogueText;
        private float dialogueEndTime;
        private int placeholderIndex;

        private void Awake()
        {
            activeBot = GetComponent<AnimationHandler>();
            switcher = FindFirstObjectByType<MiniBotSwitcher>();
        }

        private void Update()
        {
            if (isTalking)
            {
                if (Input.GetKeyDown(talkKey))
                    EndTalk();

                UpdateDialoguePosition();
                TryHideExpiredDialogue();
                return;
            }

            talkTarget = FindNearestBot();
            UpdateTalkHighlight(talkTarget);

            if (Input.GetKeyDown(talkKey) && talkTarget != null)
                BeginTalk();
        }

        private Transform FindNearestBot()
        {
            if (switcher == null)
                return null;

            var botsField = typeof(MiniBotSwitcher).GetField("bots",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (botsField == null)
                return null;

            var list = botsField.GetValue(switcher) as List<MiniBotSwitcher.BotEntry>;
            if (list == null)
                return null;

            var nearestDist = talkRange;
            Transform nearest = null;
            var myPos = transform.position;
            foreach (var entry in list)
            {
                if (entry.root == null || entry.root == transform)
                    continue;
                if (!entry.root.gameObject.activeSelf)
                    continue;

                var d = Vector3.Distance(myPos, entry.root.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = entry.root;
                }
            }
            return nearest;
        }

        private void UpdateTalkHighlight(Transform target)
        {
            if (talkHighlight != null && (target == null || talkTarget != target))
            {
                Object.Destroy(talkHighlight);
                talkHighlight = null;
            }

            if (target == null)
                return;

            if (talkHighlight == null)
                talkHighlight = CreateOutline();

            if (talkHighlight == null)
                return;

            var lr = talkHighlight.GetComponent<LineRenderer>();
            if (lr != null)
                SetLineRendererBounds(lr, GetBounds(target));
        }

        private GameObject CreateOutline()
        {
            var go = new GameObject("TalkHighlight");
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.positionCount = 24;
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.numCapVertices = 0;
            lr.numCornerVertices = 0;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
                lr.material = new Material(shader) { color = highlightColor };

            return go;
        }

        private static void SetLineRendererBounds(LineRenderer lr, Bounds b)
        {
            var c = b.center;
            var e = b.extents;
            var corners = new Vector3[8];
            corners[0] = c + new Vector3(-e.x, -e.y, -e.z);
            corners[1] = c + new Vector3( e.x, -e.y, -e.z);
            corners[2] = c + new Vector3( e.x, -e.y,  e.z);
            corners[3] = c + new Vector3(-e.x, -e.y,  e.z);
            corners[4] = c + new Vector3(-e.x,  e.y, -e.z);
            corners[5] = c + new Vector3( e.x,  e.y, -e.z);
            corners[6] = c + new Vector3( e.x,  e.y,  e.z);
            corners[7] = c + new Vector3(-e.x,  e.y,  e.z);
            var edges = new int[]
            {
                0,1, 1,2, 2,3, 3,0,
                4,5, 5,6, 6,7, 7,4,
                0,4, 1,5, 2,6, 3,7
            };
            var positions = new Vector3[24];
            for (var i = 0; i < 24; i++)
                positions[i] = corners[edges[i]];
            lr.SetPositions(positions);
        }

        private static Bounds GetBounds(Transform target)
        {
            // ponytail: use skinned mesh bounds first so the outline hugs the
            // animated character model, not a collider debug visual or UI panel.
            var skinned = target.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinned.Length > 0)
            {
                var b = skinned[0].bounds;
                for (var i = 1; i < skinned.Length; i++)
                    b.Encapsulate(skinned[i].bounds);
                return b;
            }

            // Fallback to non-UI MeshRenderers only.
            var renderers = target.GetComponentsInChildren<MeshRenderer>();
            Bounds merged = default;
            var found = false;
            foreach (var r in renderers)
            {
                if (r.GetComponent<RectTransform>() != null)
                    continue;
                if (!found)
                {
                    merged = r.bounds;
                    found = true;
                }
                else
                {
                    merged.Encapsulate(r.bounds);
                }
            }

            if (found)
                return merged;

            return new Bounds(target.position, Vector3.one);
        }

        private void BeginTalk()
        {
            isTalking = true;
            if (activeBot != null)
                activeBot.IsTalking = true;

            var targetHandler = talkTarget.GetComponent<AnimationHandler>();
            if (targetHandler != null)
            {
                wasTargetHandlerEnabled = targetHandler.enabled;
                targetHandler.enabled = true;
                targetHandler.IsTalking = true;
            }

            FaceEachOther(transform, talkTarget);
            ShowNextPlaceholderLine();
        }

        private void EndTalk()
        {
            isTalking = false;
            if (activeBot != null)
                activeBot.IsTalking = false;

            if (talkTarget != null)
            {
                var targetHandler = talkTarget.GetComponent<AnimationHandler>();
                if (targetHandler != null)
                {
                    targetHandler.IsTalking = false;
                    targetHandler.enabled = wasTargetHandlerEnabled;
                }
            }

            HideDialogue();
        }

        private void ShowNextPlaceholderLine()
        {
            if (placeholderLines == null || placeholderLines.Length == 0)
                return;

            var line = placeholderLines[placeholderIndex % placeholderLines.Length];
            placeholderIndex++;
            ShowDialogue(line, lineDuration);
        }

        private void ShowDialogue(string text, float duration)
        {
            EnsureDialogueCanvas();
            if (dialogueText == null)
                return;

            dialogueText.text = text;
            dialogueEndTime = Time.time + Mathf.Max(duration, 0.5f);
            dialogueCanvas.enabled = true;
            UpdateDialoguePosition();
        }

        private void HideDialogue()
        {
            if (dialogueCanvas != null)
                dialogueCanvas.enabled = false;
        }

        private void TryHideExpiredDialogue()
        {
            if (dialogueCanvas != null && dialogueCanvas.enabled && Time.time >= dialogueEndTime)
                dialogueCanvas.enabled = false;
        }

        private void EnsureDialogueCanvas()
        {
            if (dialogueCanvas != null)
                return;

            var go = new GameObject("TalkDialogue");
            go.transform.SetParent(talkTarget, false);

            dialogueCanvas = go.AddComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.WorldSpace;
            dialogueCanvas.sortingOrder = 100;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2.4f, 0.7f);
            rect.localPosition = Vector3.up * bubbleHeight;

            var textGo = new GameObject("DialogueText");
            textGo.transform.SetParent(go.transform, false);
            dialogueText = textGo.AddComponent<Text>();
            dialogueText.font = Resources.Load<Font>("Fonts/YesMyungjo-Regular");
            dialogueText.fontSize = 42;
            dialogueText.resizeTextForBestFit = true;
            dialogueText.resizeTextMinSize = 8;
            dialogueText.resizeTextMaxSize = 64;
            dialogueText.color = Color.black;
            dialogueText.alignment = TextAnchor.MiddleCenter;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            HideDialogue();
        }

        private void UpdateDialoguePosition()
        {
            if (dialogueCanvas == null || Camera.main == null)
                return;

            var t = dialogueCanvas.transform;
            t.rotation = Quaternion.LookRotation(t.position - Camera.main.transform.position, Vector3.up);
        }

        private static void FaceEachOther(Transform a, Transform b)
        {
            var dir = b.position - a.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                a.rotation = Quaternion.LookRotation(dir, Vector3.up);
                b.rotation = Quaternion.LookRotation(-dir, Vector3.up);
            }
        }

        private void OnDisable()
        {
            EndTalk();
            if (talkHighlight != null)
            {
                Object.Destroy(talkHighlight);
                talkHighlight = null;
            }
            DestroyDialogueCanvas();
        }

        private void DestroyDialogueCanvas()
        {
            if (dialogueCanvas != null)
            {
                Object.Destroy(dialogueCanvas.gameObject);
                dialogueCanvas = null;
                dialogueText = null;
            }
        }
    }
}
