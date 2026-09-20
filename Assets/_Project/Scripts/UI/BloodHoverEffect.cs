using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    public sealed class BloodHoverEffect :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Image overlay;
        [SerializeField] private float spreadDuration = 0.6f;
        [SerializeField] private float clearDuration = 0.18f;
        [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.78f;

        private Coroutine animationRoutine;
        private Vector2 lastButtonSize = new(-1f, -1f);
        private Vector2Int lastScreenSize = new(-1, -1);
        private float lastCanvasScaleFactor = -1f;

        public void Configure(
            Image bloodOverlay,
            float spreadSeconds = 0.6f,
            float clearSeconds = 0.18f,
            float alpha = 0.78f)
        {
            overlay = bloodOverlay;
            spreadDuration = Mathf.Max(0.01f, spreadSeconds);
            clearDuration = Mathf.Max(0.01f, clearSeconds);
            maximumAlpha = Mathf.Clamp01(alpha);
            ResetOverlay();
        }

        private void Awake() => ResetOverlay();

        private void OnEnable() => RefreshGeometry();

        private void LateUpdate()
        {
            if (GeometryChanged())
                PrepareOverlayGeometry();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshGeometry();
        }

        public void RefreshGeometry()
        {
            lastButtonSize = new Vector2(-1f, -1f);
            lastScreenSize = new Vector2Int(-1, -1);
            lastCanvasScaleFactor = -1f;
            if (overlay != null)
                PrepareOverlayGeometry();
        }

        private void OnDisable()
        {
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = null;
            ResetOverlay();
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            AnimateTo(maximumAlpha, spreadDuration);

        public void OnPointerExit(PointerEventData eventData) =>
            AnimateTo(0f, clearDuration);

        private void AnimateTo(float targetAlpha, float duration)
        {
            if (overlay == null) return;
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            overlay.fillAmount = 1f;
            overlay.enabled = true;
            animationRoutine = StartCoroutine(
                AnimateOverlay(Mathf.Clamp01(targetAlpha), duration));
        }

        private IEnumerator AnimateOverlay(float targetAlpha, float duration)
        {
            float startAlpha = overlay.color.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress * progress * (3f - 2f * progress);
                SetOverlayAlpha(Mathf.Lerp(startAlpha, targetAlpha, eased));
                yield return null;
            }

            SetOverlayAlpha(targetAlpha);
            overlay.enabled = targetAlpha > 0f;
            animationRoutine = null;
        }

        private void ResetOverlay()
        {
            if (overlay == null) return;
            PrepareOverlayGeometry();
            overlay.type = Image.Type.Simple;
            overlay.fillAmount = 1f;
            overlay.raycastTarget = false;
            SetOverlayAlpha(0f);
            overlay.enabled = false;
        }

        private void PrepareOverlayGeometry()
        {
            if (overlay == null || overlay.sprite == null) return;
            RectTransform buttonRect = transform as RectTransform;
            RectTransform overlayRect = overlay.rectTransform;
            if (buttonRect == null || overlayRect == null) return;

            float buttonWidth = Mathf.Abs(buttonRect.rect.width);
            float buttonHeight = Mathf.Abs(buttonRect.rect.height);
            float spriteWidth = overlay.sprite.rect.width;
            float spriteHeight = overlay.sprite.rect.height;
            if (buttonWidth <= 0f || buttonHeight <= 0f ||
                spriteWidth <= 0f || spriteHeight <= 0f)
                return;

            float spriteAspect = spriteWidth / spriteHeight;
            float buttonAspect = buttonWidth / buttonHeight;
            float overlayWidth;
            float overlayHeight;
            if (buttonAspect >= spriteAspect)
            {
                overlayWidth = buttonWidth;
                overlayHeight = buttonWidth / spriteAspect;
            }
            else
            {
                overlayHeight = buttonHeight;
                overlayWidth = buttonHeight * spriteAspect;
            }

            // The artwork's visible frame occupies roughly 73% of the sprite.
            // Enlarge it so that frame aligns with the actual button bounds.
            const float artworkFitScale = 1.36f;
            overlayWidth *= artworkFitScale;
            overlayHeight *= artworkFitScale;

            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = new Vector2(overlayWidth, overlayHeight);
            overlayRect.localScale = Vector3.one;

            lastButtonSize = new Vector2(buttonWidth, buttonHeight);
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            Canvas rootCanvas = overlay.canvas != null
                ? overlay.canvas.rootCanvas
                : null;
            lastCanvasScaleFactor = rootCanvas != null
                ? rootCanvas.scaleFactor
                : 1f;

            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();
        }

        private bool GeometryChanged()
        {
            if (overlay == null) return false;
            RectTransform buttonRect = transform as RectTransform;
            if (buttonRect == null) return false;

            Vector2 buttonSize = new(
                Mathf.Abs(buttonRect.rect.width),
                Mathf.Abs(buttonRect.rect.height));
            Canvas rootCanvas = overlay.canvas != null
                ? overlay.canvas.rootCanvas
                : null;
            float canvasScaleFactor = rootCanvas != null
                ? rootCanvas.scaleFactor
                : 1f;

            return lastScreenSize.x != Screen.width ||
                   lastScreenSize.y != Screen.height ||
                   Mathf.Abs(lastButtonSize.x - buttonSize.x) > 0.01f ||
                   Mathf.Abs(lastButtonSize.y - buttonSize.y) > 0.01f ||
                   Mathf.Abs(lastCanvasScaleFactor - canvasScaleFactor) > 0.001f;
        }

        private void SetOverlayAlpha(float alpha)
        {
            Color color = overlay.color;
            color.a = alpha;
            overlay.color = color;
        }
    }
}
