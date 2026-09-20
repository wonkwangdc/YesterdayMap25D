using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YesterdayMap.Core
{
    /// <summary>
    /// Shows the house-to-bunker illustration while the Shelter scene loads.
    /// The overlay survives the scene swap and removes itself after fading out.
    /// </summary>
    public sealed class HouseToBunkerTransition : MonoBehaviour
    {
        private const string SpriteResourcePath =
            "UI/Transitions/HouseToBunker_Transition";
        private const float FadeInDuration = 0.35f;
        private const float MinimumDisplayDuration = 2.25f;
        private const float FadeOutDuration = 0.45f;

        private static bool isRunning;

        private CanvasGroup canvasGroup;
        private string destinationScene;

        public static bool TryBegin(string sceneName)
        {
            if (isRunning)
            {
                return true;
            }

            Sprite transitionSprite =
                UnityEngine.Resources.Load<Sprite>(SpriteResourcePath);
            if (transitionSprite == null)
            {
                Debug.LogError(
                    $"House-to-bunker transition sprite was not found at Resources/{SpriteResourcePath}.");
                return false;
            }

            isRunning = true;
            GameObject root = new("HouseToBunkerTransition");
            DontDestroyOnLoad(root);

            HouseToBunkerTransition transition =
                root.AddComponent<HouseToBunkerTransition>();
            transition.destinationScene = sceneName;
            transition.BuildOverlay(transitionSprite);
            transition.StartCoroutine(transition.Run());
            return true;
        }

        private void BuildOverlay(Sprite transitionSprite)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            Image background = CreateFullscreenImage("Background", transform);
            background.color = Color.black;

            Image illustration = CreateFullscreenImage("Illustration", transform);
            illustration.sprite = transitionSprite;
            illustration.color = Color.white;
            illustration.preserveAspect = true;
            illustration.raycastTarget = false;
        }

        private static Image CreateFullscreenImage(string objectName, Transform parent)
        {
            GameObject imageObject = new(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return imageObject.GetComponent<Image>();
        }

        private IEnumerator Run()
        {
            yield return null;
            yield return Fade(0f, 1f, FadeInDuration);

            float visibleSince = Time.unscaledTime;
            AsyncOperation load = SceneManager.LoadSceneAsync(destinationScene);
            if (load == null)
            {
                Finish();
                yield break;
            }

            load.allowSceneActivation = false;
            while (load.progress < 0.9f ||
                   Time.unscaledTime - visibleSince < MinimumDisplayDuration)
            {
                yield return null;
            }

            load.allowSceneActivation = true;
            while (!load.isDone)
            {
                yield return null;
            }

            yield return Fade(1f, 0f, FadeOutDuration);
            Finish();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private void Finish()
        {
            isRunning = false;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            isRunning = false;
        }
    }
}
