using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    /// <summary>
    /// Gives dialogue choices the same restrained scale feedback used by the
    /// exploration map without recoloring the underlying button artwork.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueChoiceHoverEffect :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private float hoverDuration = 0.12f;
        [SerializeField] private float clearDuration = 0.09f;
        [SerializeField, Min(1f)] private float hoverScale = 1.05f;
        [SerializeField, Range(0.9f, 1f)] private float pressedScale = 0.985f;
        [SerializeField] private Text glowLabel;
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color hoverTextColor = Color.white;
        [SerializeField] private bool useTextGlow;

        private Vector3 normalScale;
        private Coroutine animationRoutine;
        private Outline textGlow;
        private bool pointerInside;
        private bool configured;

        public void Configure(Button targetButton, Text targetLabel)
        {
            button = targetButton != null
                ? targetButton
                : GetComponent<Button>();

            if (button != null)
            {
                // Prevent the Button color transition from tinting the artwork.
                button.transition = Selectable.Transition.None;
            }

            normalScale = transform.localScale;
            configured = true;
            ResetVisuals();
        }

        public void ConfigureTextGlow(
            Button targetButton,
            Text targetLabel,
            Color normalColor,
            Color hoverColor)
        {
            Configure(targetButton, targetLabel);
            glowLabel = targetLabel;
            normalTextColor = normalColor;
            hoverTextColor = hoverColor;
            useTextGlow = glowLabel != null;
            hoverScale = 1f;
            pressedScale = 1f;

            if (useTextGlow)
            {
                textGlow = glowLabel.GetComponent<Outline>();
                if (textGlow == null)
                    textGlow = glowLabel.gameObject.AddComponent<Outline>();
                textGlow.effectColor = new Color(1f, 1f, 1f, 0.8f);
                textGlow.effectDistance = new Vector2(1.5f, -1.5f);
                textGlow.useGraphicAlpha = true;
            }

            ApplyTextHover(false);
        }

        private void Awake()
        {
            if (!configured)
            {
                Configure(
                    GetComponent<Button>(),
                    GetComponentInChildren<Text>(true));
            }
        }

        private void OnEnable()
        {
            pointerInside = false;
            if (configured)
            {
                ResetVisuals();
            }
        }

        private void OnDisable()
        {
            pointerInside = false;
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            if (configured)
            {
                ResetVisuals();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            if (CanInteract())
            {
                ApplyTextHover(true);
                AnimateTo(
                    normalScale * hoverScale,
                    hoverDuration);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            ApplyTextHover(false);
            AnimateToNormal();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanInteract())
            {
                return;
            }

            AnimateTo(
                normalScale * hoverScale * pressedScale,
                0.055f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pointerInside && CanInteract())
            {
                AnimateTo(
                    normalScale * hoverScale,
                    0.07f);
            }
            else
            {
                AnimateToNormal();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            pointerInside = false;
            ApplyTextHover(false);
            AnimateToNormal();
        }

        public void SetInteractable(bool interactable)
        {
            if (!interactable)
            {
                pointerInside = false;
                ResetVisuals();
            }
        }

        private bool CanInteract()
        {
            return button != null && button.IsInteractable();
        }

        private void AnimateToNormal()
        {
            if (!configured)
            {
                return;
            }

            AnimateTo(
                normalScale,
                clearDuration);
        }

        private void AnimateTo(
            Vector3 targetScale,
            float duration)
        {
            if (!configured)
            {
                return;
            }

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            // A choice click can hide its button before this pointer callback
            // finishes. Inactive behaviours cannot start coroutines, so apply
            // the final visual state immediately in that case.
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                animationRoutine = null;
                transform.localScale = targetScale;
                return;
            }

            animationRoutine = StartCoroutine(
                AnimateScale(
                    targetScale,
                    Mathf.Max(0.01f, duration)));
        }

        private IEnumerator AnimateScale(
            Vector3 targetScale,
            float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress * progress * (3f - 2f * progress);
                transform.localScale =
                    Vector3.Lerp(startScale, targetScale, eased);
                yield return null;
            }

            transform.localScale = targetScale;
            animationRoutine = null;
        }

        private void ResetVisuals()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            transform.localScale = normalScale;
            ApplyTextHover(false);
        }

        private void ApplyTextHover(bool hovered)
        {
            if (!useTextGlow || glowLabel == null) return;
            glowLabel.color = hovered ? hoverTextColor : normalTextColor;
            if (textGlow != null) textGlow.enabled = hovered;
        }
    }
}
