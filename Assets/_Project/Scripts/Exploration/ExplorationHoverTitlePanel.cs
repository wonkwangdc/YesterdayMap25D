using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.Exploration
{
    public sealed class ExplorationHoverTitlePanel : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private CanvasGroup canvasGroup;
        // 탐사 지도가 열린 직후에도 장소 이름을 바로 확인할 수 있도록 짧게 표시한다.
        [SerializeField, Min(0.01f)] private float initialFadeDuration = 0.2f;

        private string currentTitle;
        private string selectedTitle;

        public void Configure(Text text, CanvasGroup group)
        {
            titleText = text;
            canvasGroup = group;
            ClearImmediately();
        }

        private void Awake()
        {
            ClearImmediately();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                StartCoroutine(FadeInOnce());
            }
        }

        public void Show(string title)
        {
            currentTitle = title;
            transform.SetAsLastSibling();
            if (titleText != null)
                titleText.text = title;
        }

        public void Clear(string title)
        {
            if (currentTitle != title)
                return;
            Show(selectedTitle);
        }

        public void SetSelectedTitle(string title)
        {
            selectedTitle = title ?? string.Empty;
            Show(selectedTitle);
        }

        private void ClearImmediately()
        {
            currentTitle = string.Empty;
            if (titleText != null)
                titleText.text = string.Empty;
        }

        private IEnumerator FadeInOnce()
        {
            float elapsed = 0f;
            while (elapsed < initialFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / initialFadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }
    }
}
