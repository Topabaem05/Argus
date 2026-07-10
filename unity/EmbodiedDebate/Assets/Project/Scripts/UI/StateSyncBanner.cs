using System.Collections;
using UnityEngine.UI;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Top-center banner for reconnects and round transitions.
    /// </summary>
    public sealed class StateSyncBanner : MonoBehaviour
    {
        private Text labelText => transform.Find("Label")?.GetComponent<Text>();
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float displayDuration = 3f;

        private Coroutine activeCoroutine;

        private void Start()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        public void Show(string text)
        {
            if (labelText != null)
            {
                labelText.text = text;
            }

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            activeCoroutine = StartCoroutine(AnimateInOut());
        }

        private IEnumerator AnimateInOut()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(displayDuration);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
    }
}
