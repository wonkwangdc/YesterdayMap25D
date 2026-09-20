using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YesterdayMap.Exploration;

namespace YesterdayMap.Core
{
    public sealed class SceneFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeCanvas;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.45f;
        private bool transitionInProgress;

        public void Configure(CanvasGroup canvasGroup) => fadeCanvas = canvasGroup;
        public void Configure(CanvasGroup canvasGroup, float duration)
        {
            fadeCanvas = canvasGroup;
            fadeDuration = Mathf.Max(0.1f, duration);
        }

        private void Start()
        {
            if (fadeCanvas != null) StartCoroutine(Fade(1f, 0f));
        }

        public void LoadScene(string sceneName)
        {
            if (transitionInProgress) return;
            transitionInProgress = true;
            StartCoroutine(FadeAndLoad(sceneName));
        }

        public void LoadExplorationScene(ExplorationManager explorationManager)
        {
            if (transitionInProgress) return;
            transitionInProgress = true;
            StartCoroutine(FadeAndLoadExploration(explorationManager));
        }

        public void FadeBackIn() => StartCoroutine(FadeBackInRoutine());

        public void FadeOutAndBack(Action atDarkest = null)
        {
            if (transitionInProgress) return;
            transitionInProgress = true;
            StartCoroutine(FadeOutAndBackRoutine(atDarkest));
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            yield return Fade(0f, 1f);
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator FadeAndLoadExploration(ExplorationManager explorationManager)
        {
            yield return Fade(0f, 1f);
            AsyncOperation load = SceneManager.LoadSceneAsync("Exploration", LoadSceneMode.Additive);
            while (load != null && !load.isDone) yield return null;

            Scene explorationScene = SceneManager.GetSceneByName("Exploration");
            if (explorationScene.IsValid()) SceneManager.SetActiveScene(explorationScene);

            ExplorationSceneController controller = FindFirstObjectByType<ExplorationSceneController>();
            if (controller != null) controller.Configure(explorationManager, this);
            else
            {
                Debug.LogError("ExplorationSceneController를 Exploration 씬에서 찾을 수 없습니다.");
                FadeBackIn();
            }
        }

        private IEnumerator FadeBackInRoutine()
        {
            yield return Fade(1f, 0f);
            transitionInProgress = false;
        }

        private IEnumerator FadeOutAndBackRoutine(Action atDarkest)
        {
            yield return Fade(0f, 1f);
            atDarkest?.Invoke();
            yield return new WaitForSecondsRealtime(0.35f);
            yield return Fade(1f, 0f);
            transitionInProgress = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeCanvas == null) yield break;
            fadeCanvas.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvas.alpha = to;
            fadeCanvas.blocksRaycasts = to > 0.9f;
        }
    }
}
