using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    public sealed class GameSettingsPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Toggle fullScreenToggle;
        [SerializeField] private Dropdown displayModeDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Dropdown movementPresetDropdown;
        [SerializeField] private Toggle reduceFlashingToggle;
        [SerializeField] private Button closeButton;

        private Coroutine responsiveRefreshRoutine;
        private bool layoutPrepared;
        private readonly List<GameObject> temporarilyHiddenUi = new();

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Configure(
            GameObject root,
            Slider volume,
            Slider mouseSensitivity,
            Toggle fullScreen,
            Dropdown resolution,
            Dropdown movementPreset,
            Button close,
            Toggle reduceFlashing = null)
        {
            panelRoot = root;
            volumeSlider = volume;
            mouseSensitivitySlider = mouseSensitivity;
            fullScreenToggle = fullScreen;
            resolutionDropdown = resolution;
            movementPresetDropdown = movementPreset;
            reduceFlashingToggle = reduceFlashing;
            closeButton = close;
            PrepareSettingsLayout();
            BindControls();
            Close();
        }

        public void Configure(
            GameObject root,
            Slider volume,
            Slider mouseSensitivity,
            Dropdown displayMode,
            Dropdown resolution,
            Dropdown movementPreset,
            Button close,
            Toggle reduceFlashing = null)
        {
            panelRoot = root;
            volumeSlider = volume;
            mouseSensitivitySlider = mouseSensitivity;
            fullScreenToggle = null;
            displayModeDropdown = displayMode;
            resolutionDropdown = resolution;
            movementPresetDropdown = movementPreset;
            reduceFlashingToggle = reduceFlashing;
            closeButton = close;
            PrepareSettingsLayout();
            BindControls();
            Close();
        }

        private void Awake()
        {
            PrepareSettingsLayout();
            BindControls();
            Close();
        }

        private void OnDestroy()
        {
            RestoreCompetingHud();
            if (responsiveRefreshRoutine != null)
                StopCoroutine(responsiveRefreshRoutine);
            if (volumeSlider != null)
                volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
            if (mouseSensitivitySlider != null)
                mouseSensitivitySlider.onValueChanged.RemoveListener(HandleMouseSensitivityChanged);
            if (fullScreenToggle != null)
                fullScreenToggle.onValueChanged.RemoveListener(HandleFullScreenChanged);
            if (displayModeDropdown != null)
                displayModeDropdown.onValueChanged.RemoveListener(HandleDisplayModeChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
            if (movementPresetDropdown != null)
                movementPresetDropdown.onValueChanged.RemoveListener(HandleMovementPresetChanged);
            if (reduceFlashingToggle != null)
                reduceFlashingToggle.onValueChanged.RemoveListener(HandleReduceFlashingChanged);
            closeButton?.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
            PrepareSettingsLayout();
            PromoteSettingsCanvas();
            HideCompetingHud();
            RefreshControls();
            RefreshResponsiveLayout();
            transform.SetAsLastSibling();
            panelRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            RestoreCompetingHud();
        }

        private void BindControls()
        {
            NormalizeSliderVisual(volumeSlider);
            NormalizeSliderVisual(mouseSensitivitySlider);

            if (resolutionDropdown != null && resolutionDropdown.options.Count == 0)
            {
                List<string> labels = new();
                foreach (Vector2Int resolution in GameSettings.Resolutions)
                    labels.Add($"{resolution.x} x {resolution.y}");
                resolutionDropdown.AddOptions(labels);
            }

            if (displayModeDropdown != null && displayModeDropdown.options.Count == 0)
                displayModeDropdown.AddOptions(new List<string>
                {
                    "전체화면",
                    "창모드",
                    "테두리 없는 창모드"
                });

            if (movementPresetDropdown != null && movementPresetDropdown.options.Count == 0)
                movementPresetDropdown.AddOptions(new List<string> { "WASD", "방향키" });

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
                volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
            }
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.minValue = GameSettings.MinimumMouseSensitivity;
                mouseSensitivitySlider.maxValue = GameSettings.MaximumMouseSensitivity;
                mouseSensitivitySlider.onValueChanged.RemoveListener(HandleMouseSensitivityChanged);
                mouseSensitivitySlider.onValueChanged.AddListener(HandleMouseSensitivityChanged);
            }
            if (fullScreenToggle != null)
            {
                fullScreenToggle.onValueChanged.RemoveListener(HandleFullScreenChanged);
                fullScreenToggle.onValueChanged.AddListener(HandleFullScreenChanged);
            }
            if (displayModeDropdown != null)
            {
                displayModeDropdown.onValueChanged.RemoveListener(HandleDisplayModeChanged);
                displayModeDropdown.onValueChanged.AddListener(HandleDisplayModeChanged);
            }
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
                resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);
            }
            if (movementPresetDropdown != null)
            {
                movementPresetDropdown.onValueChanged.RemoveListener(HandleMovementPresetChanged);
                movementPresetDropdown.onValueChanged.AddListener(HandleMovementPresetChanged);
            }
            if (reduceFlashingToggle != null)
            {
                reduceFlashingToggle.onValueChanged.RemoveListener(HandleReduceFlashingChanged);
                reduceFlashingToggle.onValueChanged.AddListener(HandleReduceFlashingChanged);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void RefreshControls()
        {
            volumeSlider?.SetValueWithoutNotify(GameSettings.Volume);
            mouseSensitivitySlider?.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            fullScreenToggle?.SetIsOnWithoutNotify(GameSettings.FullScreen);
            displayModeDropdown?.SetValueWithoutNotify(GameSettings.DisplayMode);
            resolutionDropdown?.SetValueWithoutNotify(GameSettings.ResolutionIndex);
            movementPresetDropdown?.SetValueWithoutNotify(GameSettings.MovementPreset);
            reduceFlashingToggle?.SetIsOnWithoutNotify(GameSettings.ReduceFlashing);
        }

        private static void HandleVolumeChanged(float value) =>
            GameSettings.SetVolume(value);

        private static void HandleMouseSensitivityChanged(float value) =>
            GameSettings.SetMouseSensitivity(value);

        private void HandleFullScreenChanged(bool value)
        {
            GameSettings.SetFullScreen(value);
            QueueResponsiveLayoutRefresh();
        }

        private void HandleDisplayModeChanged(int value)
        {
            GameSettings.SetDisplayMode(value);
            QueueResponsiveLayoutRefresh();
        }

        private void HandleResolutionChanged(int value)
        {
            GameSettings.SetResolution(value);
            QueueResponsiveLayoutRefresh();
        }

        private static void HandleMovementPresetChanged(int value) =>
            GameSettings.SetMovementPreset(value);

        private static void HandleReduceFlashingChanged(bool value) =>
            GameSettings.SetReduceFlashing(value);

        private void PrepareSettingsLayout()
        {
            if (layoutPrepared || panelRoot == null) return;

            RectTransform artwork =
                panelRoot.transform.Find("SettingsArtwork") as RectTransform;
            if (artwork == null) return;

            layoutPrepared = true;

            if (panelRoot.TryGetComponent(out Image backdrop))
                backdrop.color = Color.black;

            PromoteSettingsCanvas();

            artwork.anchorMin = new Vector2(0.5f, 0.5f);
            artwork.anchorMax = new Vector2(0.5f, 0.5f);
            artwork.pivot = new Vector2(0.5f, 0.5f);
            artwork.anchoredPosition = Vector2.zero;
            artwork.sizeDelta = new Vector2(1680f, 945f);

            if (artwork.TryGetComponent(out RawImage artworkImage))
                artworkImage.uvRect = new Rect(0f, 0f, 1f, 1f);

            AspectRatioFitter fitter = artwork.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                fitter.enabled = true;
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 1680f / 945f;
            }

            RectTransform window =
                artwork.Find("SettingsWindow") as RectTransform;
            if (window == null) return;

            // Cover the controls painted into the source artwork with one clean,
            // continuous surface. The old three independent masks were visible as
            // mismatched black rectangles and let fragments of the old slider
            // handles peek through.
            RectTransform contentSurface =
                window.Find("SettingsLowerMask") as RectTransform;
            if (contentSurface != null)
            {
                SetCenteredRect(contentSurface, new Vector2(0f, 10f),
                    new Vector2(760f, 560f));
                contentSurface.SetAsFirstSibling();

                if (contentSurface.TryGetComponent(out Image surfaceImage))
                    surfaceImage.color = new Color(0.055f, 0.05f, 0.043f, 1f);

                Outline outline = contentSurface.GetComponent<Outline>();
                if (outline == null)
                    outline = contentSurface.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.22f, 0.14f, 0.055f, 0.72f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            SetActive(window.Find("VolumeMask"), false);
            SetActive(window.Find("MouseSensitivityMask"), false);

            Text labelTemplate =
                window.Find("DisplayModeLabel")?.GetComponent<Text>();
            Text volumeLabel = EnsureLabel(window, labelTemplate,
                "VolumeLabel", "\uC804\uCCB4 \uC74C\uB7C9");
            Text sensitivityLabel = EnsureLabel(window, labelTemplate,
                "MouseSensitivityLabel", "\uB9C8\uC6B0\uC2A4 \uAC10\uB3C4");
            Text displayLabel = window.Find("DisplayModeLabel")?.GetComponent<Text>();
            Text resolutionLabel = window.Find("ResolutionLabel")?.GetComponent<Text>();
            Text movementLabel = window.Find("MovementPresetLabel")?.GetComponent<Text>();
            Text flashingLabel = window.Find("ReduceFlashingLabel")?.GetComponent<Text>();

            ConfigureLabel(volumeLabel, "\uC804\uCCB4 \uC74C\uB7C9", 220f, 230f);
            ConfigureLabel(sensitivityLabel, "\uB9C8\uC6B0\uC2A4 \uAC10\uB3C4", 145f, 230f);
            ConfigureLabel(displayLabel, "\uD654\uBA74 \uBAA8\uB4DC", 55f, 230f);
            ConfigureLabel(resolutionLabel, "\uD574\uC0C1\uB3C4", -35f, 230f);
            ConfigureLabel(movementLabel, "\uC774\uB3D9 \uD0A4", -125f, 230f);
            ConfigureLabel(flashingLabel, "\uD654\uBA74 \uAE5C\uBE61\uC784 \uAC10\uC18C", -215f, 300f);

            SetCenteredRect(volumeSlider?.transform as RectTransform,
                new Vector2(145f, 220f), new Vector2(402f, 48f));
            SetCenteredRect(mouseSensitivitySlider?.transform as RectTransform,
                new Vector2(145f, 145f), new Vector2(402f, 48f));
            SetCenteredRect(displayModeDropdown?.transform as RectTransform,
                new Vector2(145f, 55f), new Vector2(402f, 60f));
            SetCenteredRect(resolutionDropdown?.transform as RectTransform,
                new Vector2(145f, -35f), new Vector2(402f, 60f));
            SetCenteredRect(movementPresetDropdown?.transform as RectTransform,
                new Vector2(145f, -125f), new Vector2(402f, 60f));
            SetCenteredRect(reduceFlashingToggle?.transform as RectTransform,
                new Vector2(-38f, -215f), new Vector2(56f, 48f));
            SetCenteredRect(closeButton?.transform as RectTransform,
                new Vector2(0f, -333f), new Vector2(410f, 78f));

            NormalizeSliderVisual(volumeSlider);
            NormalizeSliderVisual(mouseSensitivitySlider);
        }

        private static Text EnsureLabel(
            RectTransform window,
            Text template,
            string name,
            string label)
        {
            Transform existing = window.Find(name);
            if (existing != null)
                return existing.GetComponent<Text>();
            if (template == null) return null;

            GameObject clone = Instantiate(template.gameObject, window, false);
            clone.name = name;
            Text text = clone.GetComponent<Text>();
            text.text = label;
            return text;
        }

        private static void ConfigureLabel(
            Text label,
            string text,
            float y,
            float width)
        {
            if (label == null) return;
            label.text = text;
            label.alignment = TextAnchor.MiddleLeft;
            SetCenteredRect(label.rectTransform,
                new Vector2(-280f, y), new Vector2(width, 46f));
        }

        private static void SetCenteredRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null)
                target.gameObject.SetActive(active);
        }

        private void PromoteSettingsCanvas()
        {
            if (panelRoot == null) return;

            Canvas overlayCanvas = panelRoot.GetComponent<Canvas>();
            if (overlayCanvas == null)
                overlayCanvas = panelRoot.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 32767;
            if (panelRoot.GetComponent<GraphicRaycaster>() == null)
                panelRoot.AddComponent<GraphicRaycaster>();
        }

        private void HideCompetingHud()
        {
            RestoreCompetingHud();

            Text[] texts = UnityEngine.Resources.FindObjectsOfTypeAll<Text>();
            foreach (Text text in texts)
            {
                if (text == null || text.name != "DiaryShortcutHint") continue;
                GameObject target = text.gameObject;
                if (!target.scene.IsValid() || !target.activeInHierarchy) continue;
                temporarilyHiddenUi.Add(target);
                target.SetActive(false);
            }
        }

        private void RestoreCompetingHud()
        {
            foreach (GameObject target in temporarilyHiddenUi)
            {
                if (target != null)
                    target.SetActive(true);
            }
            temporarilyHiddenUi.Clear();
        }

        private static void NormalizeSliderVisual(Slider slider)
        {
            if (slider == null || slider.handleRect == null) return;

            RectTransform handle = slider.handleRect;
            RectTransform handleArea = handle.parent as RectTransform;
            if (handleArea != null)
            {
                handleArea.anchorMin = new Vector2(0f, 0.5f);
                handleArea.anchorMax = new Vector2(1f, 0.5f);
                handleArea.pivot = new Vector2(0.5f, 0.5f);
                handleArea.offsetMin = new Vector2(10f, -12f);
                handleArea.offsetMax = new Vector2(-10f, 12f);
            }

            // Slider drives the handle's vertical anchors to stretch. A zero
            // height delta therefore keeps it at the fixed 24px handle-area height.
            handle.sizeDelta = new Vector2(12f, 0f);
            handle.anchoredPosition = new Vector2(handle.anchoredPosition.x, 0f);
        }

        private void QueueResponsiveLayoutRefresh()
        {
            if (!isActiveAndEnabled) return;
            if (responsiveRefreshRoutine != null)
                StopCoroutine(responsiveRefreshRoutine);
            responsiveRefreshRoutine = StartCoroutine(
                RefreshResponsiveLayoutAfterDisplayChange());
        }

        private IEnumerator RefreshResponsiveLayoutAfterDisplayChange()
        {
            // Screen.SetResolution can settle over more than one rendered frame.
            // Refresh once after each of the next two frames so hover artwork never
            // keeps geometry from the previous display mode.
            for (int frame = 0; frame < 2; frame++)
            {
                yield return new WaitForEndOfFrame();
                RefreshResponsiveLayout();
            }

            responsiveRefreshRoutine = null;
        }

        private void RefreshResponsiveLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (panelRoot != null &&
                panelRoot.transform is RectTransform panelRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
                PositionSettingsArtwork(panelRect);
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform scope = parentCanvas != null
                ? parentCanvas.transform
                : transform.root;
            BloodHoverEffect[] effects =
                scope.GetComponentsInChildren<BloodHoverEffect>(true);
            foreach (BloodHoverEffect effect in effects)
                effect.RefreshGeometry();
        }

        private void PositionSettingsArtwork(RectTransform panelRect)
        {
            RectTransform artwork =
                panelRoot.transform.Find("SettingsArtwork") as RectTransform;
            if (artwork == null) return;

            // The supplied settings artwork is a complete 16:9 screen, not a
            // side panel. Keeping it centered prevents the pause artwork from
            // showing through as a second, offset window.
            artwork.anchoredPosition = Vector2.zero;
        }
    }
}
