using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace YesterdayMap.Exploration
{
    public sealed class LocationHoverPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform previewIcon;
        [SerializeField] private CanvasGroup previewCanvasGroup;
        [SerializeField] private ExplorationHoverTitlePanel titlePanel;
        [SerializeField] private string locationTitle;
        [SerializeField, Min(1f)] private float hoverScale = 1.18f;
        [SerializeField, Min(0.01f)] private float transitionSpeed = 12f;

        private bool isHovered;
        private bool isSelected;
        private Vector3 baseScale;
        private Action<bool> memoPreviewChanged;

        public void Configure(RectTransform icon, CanvasGroup canvasGroup)
        {
            previewIcon = icon;
            previewCanvasGroup = canvasGroup;
        }

        public void ConfigureTitle(ExplorationHoverTitlePanel panel, string title)
        {
            titlePanel = panel;
            locationTitle = title;
        }

        public void ConfigureMemoPreview(Action<bool> callback)
        {
            memoPreviewChanged = callback;
        }

        private void Awake()
        {
            if (previewIcon == null)
            {
                enabled = false;
                return;
            }

            baseScale = previewIcon.localScale;
            SetVisualState(false, true);
        }

        private void OnDisable()
        {
            isHovered = false;
            SetVisualState(false, true);
        }

        private void Update()
        {
            if (previewIcon == null || previewCanvasGroup == null)
                return;

            bool emphasized = isHovered || isSelected;
            Vector3 targetScale = baseScale * (emphasized ? hoverScale : 1f);
            float targetAlpha = emphasized ? 1f : 0f;
            float t = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);

            previewIcon.localScale = Vector3.Lerp(previewIcon.localScale, targetScale, t);
            previewCanvasGroup.alpha = Mathf.Lerp(previewCanvasGroup.alpha, targetAlpha, t);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            previewIcon.SetAsLastSibling();
            titlePanel?.Show(locationTitle);
            memoPreviewChanged?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            SetVisualState(isSelected, true);
            titlePanel?.Clear(locationTitle);
            memoPreviewChanged?.Invoke(false);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            SetVisualState(isHovered || isSelected, true);
        }

        public void ShowSelectedTitle()
        {
            titlePanel?.SetSelectedTitle(locationTitle);
        }

        private void SetVisualState(bool hovered, bool immediate)
        {
            if (previewIcon == null || previewCanvasGroup == null)
                return;

            float alpha = hovered ? 1f : 0f;
            Vector3 scale = baseScale * (hovered ? hoverScale : 1f);

            if (immediate)
            {
                previewCanvasGroup.alpha = alpha;
                previewIcon.localScale = scale;
            }
        }
    }
}
