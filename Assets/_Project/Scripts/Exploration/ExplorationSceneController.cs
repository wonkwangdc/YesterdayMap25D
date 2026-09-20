using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YesterdayMap.CameraSystem;
using YesterdayMap.Core;
using YesterdayMap.UI;

namespace YesterdayMap.Exploration
{
    public sealed class ExplorationSceneController : MonoBehaviour
    {
        [SerializeField] private Button[] locationButtons;
        [SerializeField] private ExplorationLocationData[] locationData;
        [SerializeField] private Toggle takeGearToggle;
        [SerializeField] private ExplorationEquipmentSelector equipmentSelector;
        [SerializeField] private Text resultText;
        [SerializeField] private Text selectedLocationText;
        [SerializeField] private Button exploreButton;
        [SerializeField] private Font exploreButtonFont;
        [SerializeField] private Button returnButton;
        [SerializeField] private GameObject completionOverlay;
        [SerializeField] private Text completionText;
        [SerializeField] private GameObject dailyNotice;
        [SerializeField] private Text dailyNoticeText;
        [SerializeField] private ExplorationLoadingView loadingView;

        private ExplorationManager explorationManager;
        private SceneFader shelterFader;
        private bool configured;
        private bool uiEventsBound;
        private int selectedLocationIndex = -1;
        private ColorBlock[] defaultButtonColors;
        private bool waitingForCompletionDismiss;
        private string lastResultSummary = string.Empty;
        private bool isExplorationLoading;

        private void OnEnable()
        {
            // 탐사 지도는 마우스로 장소와 버튼을 선택하므로 커서를 자동으로 해제한다.
            IsometricCameraController.SetFirstPersonUiFocus(true);
        }

        private void OnDisable()
        {
            // 탐사 지도가 닫히면 벙커의 1인칭 마우스 조작으로 자동 복귀한다.
            IsometricCameraController.SetFirstPersonUiFocus(false);
        }

        public void Configure(Button[] buttons, Toggle gearToggle, Text result, Text selectionText,
            Button confirmExploreButton, Button back, GameObject overlay = null, Text overlayText = null)
        {
            locationButtons = buttons;
            takeGearToggle = gearToggle;
            resultText = result;
            selectedLocationText = selectionText;
            exploreButton = confirmExploreButton;
            returnButton = back;
            completionOverlay = overlay;
            completionText = overlayText;
        }

        public void ConfigureLocations(ExplorationLocationData[] locations)
        {
            locationData = locations;
        }

        public void ConfigureLoading(ExplorationLoadingView view)
        {
            loadingView = view;
        }

        public void ConfigureEquipment(ExplorationEquipmentSelector selector)
        {
            equipmentSelector = selector;
        }

        private void Start()
        {
            // Exploration 씬을 에디터에서 바로 실행해도 장소 선택 UI는 작동해야 한다.
            EnsureEventSystem();
            EnsureExploreButtonStyle();
            BindUiEventsIfNeeded();
            if (loadingView == null)
            {
                Debug.LogError(
                    "ExplorationLoadingView is not assigned. Run the exploration loading prefab installer.",
                    this);
            }
            UpdateSelectionView();
            equipmentSelector?.Refresh();
        }

        private void EnsureExploreButtonStyle()
        {
            if (exploreButton == null) return;

            Image background = exploreButton.GetComponent<Image>();
            if (background != null)
                background.color = Color.clear;
            exploreButton.transition = Selectable.Transition.None;

            Text label = exploreButton.GetComponentInChildren<Text>(true);
            if (label == null) return;
            if (exploreButtonFont != null)
                label.font = exploreButtonFont;

            Color normalColor = new(0.18f, 0.12f, 0.08f, 1f);
            DialogueChoiceHoverEffect hover =
                exploreButton.GetComponent<DialogueChoiceHoverEffect>();
            if (hover == null)
                hover = exploreButton.gameObject.AddComponent<DialogueChoiceHoverEffect>();
            hover.ConfigureTextGlow(exploreButton, label, normalColor, normalColor);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public void Configure(ExplorationManager manager, SceneFader fader)
        {
            if (configured) return;
            explorationManager = manager;
            shelterFader = fader;
            configured = true;

            BindUiEventsIfNeeded();
            explorationManager.ResultResolved += ShowResult;
            resultText.text = $"{explorationManager.CurrentDay}일차";
            UpdateSelectionView();
        }

        private void BindUiEventsIfNeeded()
        {
            if (uiEventsBound || locationButtons == null) return;

            defaultButtonColors = new ColorBlock[locationButtons.Length];
            for (int i = 0; i < locationButtons.Length; i++)
            {
                if (locationButtons[i] == null) continue;
                int index = i;
                defaultButtonColors[i] = locationButtons[i].colors;
                locationButtons[i].onClick.AddListener(() => SelectLocation(index));
                LocationHoverPreview preview = locationButtons[i].GetComponent<LocationHoverPreview>();
                preview?.ConfigureMemoPreview(isHovered => PreviewLocation(index, isHovered));
            }

            exploreButton?.onClick.AddListener(ExploreSelectedLocation);
            returnButton?.onClick.AddListener(ReturnToShelter);
            uiEventsBound = true;
        }

        private void Update()
        {
            if (isExplorationLoading) return;
            if (!waitingForCompletionDismiss) return;
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (clicked || pressedSpace)
            {
                waitingForCompletionDismiss = false;
                ReturnToShelter();
            }
        }

        private void OnDestroy()
        {
            if (explorationManager != null)
                explorationManager.ResultResolved -= ShowResult;
        }

        private void SelectLocation(int index)
        {
            selectedLocationIndex = index;
            UpdateSelectionView();
        }

        private void PreviewLocation(int index, bool isHovered)
        {
            if (!isHovered)
            {
                UpdateSelectionView();
                return;
            }

            ExplorationLocationData location = GetLocation(index);
            if (location != null && selectedLocationText != null)
            {
                selectedLocationText.text = explorationManager != null
                    ? explorationManager.GetLocationMemoForCurrentState(location)
                    : ExplorationManager.GetLocationMemo(location);
            }
        }

        private void ExploreSelectedLocation()
        {
            if (selectedLocationIndex < 0 || isExplorationLoading) return;

            if (explorationManager == null)
            {
                if (resultText != null)
                    resultText.text = "은신처에서 탐사 화면으로 들어오면 탐사를 시작할 수 있습니다.";
                return;
            }

            ExplorationLocationData location = GetSelectedLocation();
            if (!explorationManager.CanExploreToday)
            {
                ShowExplorationBlocked(explorationManager.ExplorationBlockMessage);
                return;
            }

            bool takeGear = equipmentSelector != null
                ? equipmentSelector.UseEquipment
                : takeGearToggle != null && takeGearToggle.isOn;
            StartCoroutine(ExploreAfterLoading(location, takeGear));
        }

        private IEnumerator ExploreAfterLoading(
            ExplorationLocationData location,
            bool takeGear)
        {
            isExplorationLoading = true;
            SetExplorationControlsInteractable(false);

            bool loadingVisible = loadingView != null && loadingView.Show(location);
            float loadingDuration = loadingView != null ? loadingView.Duration : 0f;
            float elapsed = 0f;
            while (loadingVisible && elapsed < loadingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                loadingView.SetProgress(Mathf.Clamp01(elapsed / loadingDuration));
                yield return null;
            }

            if (loadingVisible)
            {
                loadingView.SetProgress(1f);
                loadingView.Hide();
            }

            bool started = explorationManager != null &&
                           explorationManager.StartExploration(location, takeGear);
            isExplorationLoading = false;
            SetExplorationControlsInteractable(true);
            if (!started)
            {
                string message = explorationManager != null
                    ? explorationManager.ExplorationBlockMessage
                    : "탐사를 시작할 수 없습니다.";
                ShowExplorationBlocked(message);
            }
            else ShowCompletion(location.LocationName);

            UpdateSelectionView();
        }

        private void ShowExplorationBlocked(string message)
        {
            if (resultText != null) resultText.text = message;
            StartCoroutine(ShowDailyNotice(message));
        }

        private void SetExplorationControlsInteractable(bool interactable)
        {
            if (locationButtons != null)
            {
                foreach (Button button in locationButtons)
                {
                    if (button != null) button.interactable = interactable;
                }
            }

            if (takeGearToggle != null)
                takeGearToggle.interactable = interactable;
            equipmentSelector?.SetInteractable(interactable);
            if (returnButton != null)
                returnButton.interactable = interactable;
            if (exploreButton != null)
                exploreButton.interactable = interactable &&
                                             selectedLocationIndex >= 0;
        }

        private void UpdateSelectionView()
        {
            ExplorationLocationData location = GetSelectedLocation();
            bool hasSelection = location != null;
            if (selectedLocationText != null)
            {
                selectedLocationText.text = hasSelection
                    ? explorationManager != null
                        ? explorationManager.GetLocationMemoForCurrentState(location)
                        : ExplorationManager.GetLocationMemo(location)
                    : "탐사 장소를\n선택하세요";
            }

            if (exploreButton != null)
            {
                exploreButton.interactable = hasSelection;
                Text label = exploreButton.GetComponentInChildren<Text>();
                if (label != null) label.text = "탐사하기";
            }

            if (defaultButtonColors == null) return;
            for (int i = 0; i < locationButtons.Length; i++)
            {
                ColorBlock colors = defaultButtonColors[i];
                if (i == selectedLocationIndex)
                    colors.normalColor = new Color(0.95f, 0.68f, 0.25f, 0.55f);
                locationButtons[i].colors = colors;
                LocationHoverPreview preview = locationButtons[i].GetComponent<LocationHoverPreview>();
                if (preview != null) preview.SetSelected(i == selectedLocationIndex);
            }
            if (selectedLocationIndex >= 0)
                locationButtons[selectedLocationIndex].GetComponent<LocationHoverPreview>()?.ShowSelectedTitle();
        }

        private ExplorationLocationData GetSelectedLocation()
        {
            if (selectedLocationIndex < 0) return null;

            return GetLocation(selectedLocationIndex);
        }

        private ExplorationLocationData GetLocation(int index)
        {
            if (index < 0) return null;

            if (explorationManager != null)
                return explorationManager.GetLocation(index);

            if (locationData == null || index >= locationData.Length)
                return null;

            return locationData[index];
        }

        private void ShowCompletion(string locationName)
        {
            completionText.text = $"{locationName} 탐사를 마치고 돌아왔다.\n오늘 있었던 일을 기록해두자.";
            completionOverlay.SetActive(true);
            completionOverlay.transform.SetAsLastSibling();
            waitingForCompletionDismiss = true;
        }

        private IEnumerator ShowDailyNotice(string message)
        {
            if (dailyNotice == null || dailyNoticeText == null) yield break;
            dailyNoticeText.text = message;
            dailyNotice.SetActive(true);
            dailyNotice.transform.SetAsLastSibling();
            yield return new WaitForSecondsRealtime(3f);
            dailyNotice.SetActive(false);
        }

        private void ShowResult(string summary)
        {
            lastResultSummary = summary ?? string.Empty;
            resultText.text = lastResultSummary;
        }

        private void ReturnToShelter()
        {
            returnButton.interactable = false;
            StartCoroutine(UnloadExploration());
        }

        private IEnumerator UnloadExploration()
        {
            Scene shelterScene = SceneManager.GetSceneByName("Shelter");
            if (shelterScene.IsValid()) SceneManager.SetActiveScene(shelterScene);

            explorationManager?.NotifyReturnedToShelter();
            shelterFader?.FadeBackIn();
            AsyncOperation unload = SceneManager.UnloadSceneAsync(gameObject.scene);
            while (unload != null && !unload.isDone) yield return null;
        }
    }
}
