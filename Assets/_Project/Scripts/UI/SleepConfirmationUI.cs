using System;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.CameraSystem;

namespace YesterdayMap.UI
{
    // Confirmation popup used before advancing the day from the bed.
    public sealed class SleepConfirmationUI : MonoBehaviour
    {
        private const string SleepFrameResourcePath = "UI/Sleep/SleepConfirmationFrame";
        private const string SleepFrameObjectName = "SleepReferenceFrame";

        [SerializeField] private GameObject panel;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action confirmed;
        private Text titleText;
        private Image boxImage;
        private Image panelDimImage;
        private Image sleepReferenceFrame;
        private Sprite runtimeSleepFrameSprite;
        private bool baseButtonVisualsCaptured;
        private ButtonVisualState baseConfirmVisual;
        private ButtonVisualState baseCancelVisual;
        private bool sleepTextLayoutCaptured;
        private TextLayout sleepTitleLayout;
        private TextLayout sleepMessageLayout;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private bool hasCapturedCursorState;

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Configure(GameObject root, Text message, Button confirm, Button cancel)
        {
            panel = root;
            messageText = message;
            confirmButton = confirm;
            cancelButton = cancel;
            BindButtons();
            Close();
        }

        private void Awake()
        {
            BindButtons();
        }

        private void OnDestroy()
        {
            RestoreCursorState();
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
            if (runtimeSleepFrameSprite != null)
            {
                Destroy(runtimeSleepFrameSprite);
            }
        }

        private void OnDisable()
        {
            RestoreCursorState();
        }

        public void Open(Action onConfirmed)
        {
            Open(onConfirmed, "잠을 자시겠습니까?");
        }

        public void Open(Action onConfirmed, string message)
        {
            Open(onConfirmed, message, "확인", "취소");
        }

        public void Open(
            Action onConfirmed,
            string message,
            string confirmLabel,
            string cancelLabel)
        {
            confirmed = onConfirmed;
            ApplySleepPresentation();
            SetMessage(message, "잠을 자시겠습니까?");
            SetButtonLabels(confirmLabel, cancelLabel);
            ShowPanel();
        }

        public void OpenNotice(
            Action onConfirmed,
            string title,
            string message)
        {
            confirmed = onConfirmed;
            ApplyNoticePresentation(title);
            SetMessage(message, string.Empty);
            SetButtonLabels("확인", "취소");
            ShowPanel();
        }

        private void ShowPanel()
        {
            if (panel == null)
            {
                return;
            }

            CaptureCursorState();
            IsometricCameraController.SetFirstPersonUiFocus(true);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            confirmed = null;
            if (panel != null)
            {
                panel.SetActive(false);
            }
            RestoreCursorState();
        }

        private void Confirm()
        {
            Action callback = confirmed;
            confirmed = null;
            Close();
            callback?.Invoke();
        }

        private void SetMessage(string message, string fallback)
        {
            if (messageText == null) return;
            messageText.text = string.IsNullOrWhiteSpace(message)
                ? fallback
                : message;
        }

        private void SetButtonLabels(
            string confirmLabel,
            string cancelLabel)
        {
            SetButtonLabel(confirmButton, confirmLabel, "확인");
            SetButtonLabel(cancelButton, cancelLabel, "취소");
        }

        private static void SetButtonLabel(
            Button button,
            string label,
            string fallback)
        {
            if (button == null)
            {
                return;
            }

            Text buttonText = button.GetComponentInChildren<Text>(true);
            if (buttonText != null)
            {
                buttonText.text = string.IsNullOrWhiteSpace(label)
                    ? fallback
                    : label;
            }
        }

        private void ApplySleepPresentation()
        {
            ResolveVisualReferences();
            CaptureSleepTextLayout();
            bool hasReferenceFrame = EnsureSleepReferenceFrame();
            if (sleepReferenceFrame != null)
            {
                sleepReferenceFrame.gameObject.SetActive(hasReferenceFrame);
            }

            if (boxImage != null)
            {
                boxImage.gameObject.SetActive(!hasReferenceFrame);
            }

            if (panelDimImage != null)
            {
                panelDimImage.color = new Color(0.01f, 0.008f, 0.006f, 0.56f);
            }

            if (titleText != null)
            {
                titleText.text = "수면";
                StyleText(titleText, 44, FontStyle.Bold);
                SetCenteredRect(titleText, new Vector2(0f, 248f), new Vector2(465f, 84f));
            }

            if (messageText != null)
            {
                StyleText(messageText, 32, FontStyle.Normal);
                messageText.resizeTextForBestFit = true;
                messageText.resizeTextMinSize = 20;
                messageText.resizeTextMaxSize = 32;
                messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
                messageText.verticalOverflow = VerticalWrapMode.Truncate;
                SetCenteredRect(messageText, new Vector2(0f, 51f), new Vector2(735f, 128f));
            }

            ApplySleepButtonStyle(confirmButton, new Vector2(-203f, -138f));
            ApplySleepButtonStyle(cancelButton, new Vector2(203f, -138f));

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(true);
            }
        }

        private void ApplyNoticePresentation(string title)
        {
            ResolveVisualReferences();
            CaptureSleepTextLayout();
            if (sleepReferenceFrame != null)
            {
                sleepReferenceFrame.gameObject.SetActive(false);
            }

            if (boxImage != null)
            {
                boxImage.gameObject.SetActive(true);
            }

            if (panelDimImage != null)
            {
                panelDimImage.color = new Color(0.015f, 0.012f, 0.01f, 0.72f);
            }

            RestoreButtonVisual(confirmButton, baseConfirmVisual);
            RestoreButtonVisual(cancelButton, baseCancelVisual);

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(title)
                    ? "시스템 안내"
                    : title;
                titleText.fontSize = 34;
                titleText.alignment = TextAnchor.MiddleCenter;
            }

            if (messageText != null)
            {
                messageText.fontSize = 23;
                messageText.alignment = TextAnchor.UpperLeft;
                messageText.supportRichText = true;
                messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
                messageText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            SetAnchors(boxImage, new Vector2(0.20f, 0.13f), new Vector2(0.80f, 0.87f));
            SetAnchors(titleText, new Vector2(0.24f, 0.76f), new Vector2(0.76f, 0.84f));
            SetAnchors(messageText, new Vector2(0.25f, 0.29f), new Vector2(0.75f, 0.74f));
            SetAnchors(confirmButton, new Vector2(0.43f, 0.17f), new Vector2(0.57f, 0.25f));
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
            }
        }

        private void ResolveVisualReferences()
        {
            if (panel == null) return;
            panelDimImage ??= panel.GetComponent<Image>();
            if (titleText == null)
            {
                titleText = panel.transform
                    .Find("SleepConfirmTitle")
                    ?.GetComponent<Text>();
            }

            if (boxImage == null)
            {
                boxImage = panel.transform
                    .Find("SleepConfirmBox")
                    ?.GetComponent<Image>();
            }

            if (!baseButtonVisualsCaptured)
            {
                baseConfirmVisual = ButtonVisualState.Capture(confirmButton);
                baseCancelVisual = ButtonVisualState.Capture(cancelButton);
                baseButtonVisualsCaptured = true;
            }
        }

        private bool EnsureSleepReferenceFrame()
        {
            if (panel == null)
            {
                return false;
            }

            if (sleepReferenceFrame == null)
            {
                Transform existing = panel.transform.Find(SleepFrameObjectName);
                if (existing != null)
                {
                    sleepReferenceFrame = existing.GetComponent<Image>();
                }
            }

            if (sleepReferenceFrame == null)
            {
                GameObject frameObject = new GameObject(
                    SleepFrameObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                frameObject.transform.SetParent(panel.transform, false);
                frameObject.transform.SetAsFirstSibling();
                sleepReferenceFrame = frameObject.GetComponent<Image>();
                sleepReferenceFrame.raycastTarget = false;
            }

            if (runtimeSleepFrameSprite == null)
            {
                Texture2D texture = UnityEngine.Resources.Load<Texture2D>(SleepFrameResourcePath);
                if (texture == null)
                {
                    sleepReferenceFrame.gameObject.SetActive(false);
                    return false;
                }

                runtimeSleepFrameSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0u,
                    SpriteMeshType.FullRect);
                runtimeSleepFrameSprite.name = "SleepConfirmationFrame_Runtime";
            }

            sleepReferenceFrame.sprite = runtimeSleepFrameSprite;
            sleepReferenceFrame.color = Color.white;
            sleepReferenceFrame.type = Image.Type.Simple;
            sleepReferenceFrame.preserveAspect = true;
            SetCenteredRect(sleepReferenceFrame, Vector2.zero, new Vector2(1152f, 768f));
            return true;
        }

        private static void ApplySleepButtonStyle(Button button, Vector2 position)
        {
            if (button == null)
            {
                return;
            }

            SetCenteredRect(button, position, new Vector2(366f, 104f));
            Image image = button.image;
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.01f);
            colors.highlightedColor = new Color(1f, 0.86f, 0.58f, 0.16f);
            colors.selectedColor = new Color(1f, 0.86f, 0.58f, 0.12f);
            colors.pressedColor = new Color(0.62f, 0.45f, 0.25f, 0.28f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.18f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                StyleText(label, 34, FontStyle.Normal);
                SetAnchors(label, Vector2.zero, Vector2.one);
            }
        }

        private static void StyleText(Text text, int fontSize, FontStyle fontStyle)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.91f, 0.78f, 1f);
            text.raycastTarget = false;

            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0f, 0f, 0f, 0.88f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        private static void RestoreButtonVisual(Button button, ButtonVisualState state)
        {
            if (button == null || !state.IsValid)
            {
                return;
            }

            button.colors = state.Colors;
            Image image = button.image;
            if (image == null)
            {
                return;
            }

            image.sprite = state.Sprite;
            image.color = state.Color;
            image.type = state.Type;
            image.preserveAspect = state.PreserveAspect;
        }

        private static void SetCenteredRect(Component component, Vector2 position, Vector2 size)
        {
            if (component == null || component.transform is not RectTransform rect)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private void CaptureSleepTextLayout()
        {
            if (sleepTextLayoutCaptured) return;
            sleepTitleLayout = TextLayout.Capture(titleText);
            sleepMessageLayout = TextLayout.Capture(messageText);
            sleepTextLayoutCaptured = true;
        }

        private static void RestoreTextLayout(Text text, TextLayout layout)
        {
            if (text == null || !layout.IsValid) return;
            text.fontSize = layout.FontSize;
            text.alignment = layout.Alignment;
            if (text.transform is RectTransform rect)
            {
                rect.anchorMin = layout.AnchorMin;
                rect.anchorMax = layout.AnchorMax;
                rect.offsetMin = layout.OffsetMin;
                rect.offsetMax = layout.OffsetMax;
            }
        }

        private struct TextLayout
        {
            public bool IsValid;
            public int FontSize;
            public TextAnchor Alignment;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 OffsetMin;
            public Vector2 OffsetMax;

            public static TextLayout Capture(Text text)
            {
                if (text == null || text.transform is not RectTransform rect)
                {
                    return default;
                }

                return new TextLayout
                {
                    IsValid = true,
                    FontSize = text.fontSize,
                    Alignment = text.alignment,
                    AnchorMin = rect.anchorMin,
                    AnchorMax = rect.anchorMax,
                    OffsetMin = rect.offsetMin,
                    OffsetMax = rect.offsetMax
                };
            }
        }

        private struct ButtonVisualState
        {
            public bool IsValid;
            public Sprite Sprite;
            public Color Color;
            public Image.Type Type;
            public bool PreserveAspect;
            public ColorBlock Colors;

            public static ButtonVisualState Capture(Button button)
            {
                Image image = button != null ? button.image : null;
                if (button == null || image == null)
                {
                    return default;
                }

                return new ButtonVisualState
                {
                    IsValid = true,
                    Sprite = image.sprite,
                    Color = image.color,
                    Type = image.type,
                    PreserveAspect = image.preserveAspect,
                    Colors = button.colors
                };
            }
        }

        private static void SetAnchors(
            Component component,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            if (component == null ||
                component.transform is not RectTransform rect)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void BindButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
                confirmButton.onClick.AddListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Close);
                cancelButton.onClick.AddListener(Close);
            }
        }

        private void CaptureCursorState()
        {
            if (hasCapturedCursorState)
            {
                return;
            }

            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            hasCapturedCursorState = true;
        }

        private void RestoreCursorState()
        {
            if (!hasCapturedCursorState)
            {
                return;
            }

            IsometricCameraController.SetFirstPersonUiFocus(false);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            hasCapturedCursorState = false;
        }
    }
}
