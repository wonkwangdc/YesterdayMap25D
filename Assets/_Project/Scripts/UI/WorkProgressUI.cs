using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    public sealed class WorkProgressUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider slider;
        [SerializeField] private Text label;
        private bool working;

        public void Configure(GameObject root, Slider progress, Text workLabel)
        { panel = root; slider = progress; label = workLabel; panel.SetActive(false); }

        public bool Begin(string workName, float duration, Action completed)
        {
            if (working) return false;
            StartCoroutine(Run(workName, Mathf.Max(0.1f, duration), completed));
            return true;
        }

        private IEnumerator Run(string workName, float duration, Action completed)
        {
            working = true; panel.SetActive(true); label.text = workName;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime; slider.value = elapsed / duration; yield return null;
            }
            panel.SetActive(false); working = false; completed?.Invoke();
        }
    }
}
