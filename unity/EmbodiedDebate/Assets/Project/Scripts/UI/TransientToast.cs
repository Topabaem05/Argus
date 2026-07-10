using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Base toast surface that can stack messages and auto-dismiss.
    /// </summary>
    public sealed class TransientToast : MonoBehaviour
    {
        [SerializeField] private Transform toastContainer;
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private int maxVisible = 5;
        [SerializeField] private float displayDuration = 3f;

        private readonly Queue<GameObject> activeToasts = new Queue<GameObject>();

        public void Show(string title, string body)
        {
            if (toastContainer == null || toastPrefab == null)
            {
                return;
            }

            var go = Instantiate(toastPrefab, toastContainer);
            var titleText = go.transform.Find("Title")?.GetComponent<Text>();
            var bodyText = go.transform.Find("Body")?.GetComponent<Text>();
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;

            activeToasts.Enqueue(go);
            if (activeToasts.Count > maxVisible)
            {
                var oldest = activeToasts.Dequeue();
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }

            Destroy(go, displayDuration);
        }
    }
}
