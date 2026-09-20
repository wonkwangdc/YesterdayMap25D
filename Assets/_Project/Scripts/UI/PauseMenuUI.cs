using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Dropdown saveSlotDropdown;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text statusText;
        [SerializeField] private GameSettingsPanelUI settingsPanel;
        [SerializeField] private GameObject[] blockingPanels;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject slotSelectionPanel;
        [SerializeField] private Text slotSelectionTitle;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Text[] slotLabels;
        [SerializeField] private Button slotBackButton;
        [SerializeField] private RawImage slotSelectionArtwork;
        [SerializeField] private Texture saveSlotArtwork;
        [SerializeField] private Texture loadSlotArtwork;
        [SerializeField] private GameObject slotConfirmationPanel;
        [SerializeField] private Text slotConfirmationMessage;
        [SerializeField] private Button slotConfirmButton;
        [SerializeField] private Button slotCancelButton;

        private float previousTimeScale = 1f;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private bool blockingPanelWasOpen;
        private SlotAction slotAction;
        private SlotAction pendingSlotAction;
        private int pendingSlot;
        private UnityAction[] slotButtonListeners;

        private enum SlotAction
        {
            None,
            Save,
            Load
        }

        public bool IsOpen => menuRoot != null && menuRoot.activeSelf;
        public bool BlocksGameplayInput => IsOpen || IsBlockingPanelOpen();

        public void SetBlockingPanels(GameObject[] blockers)
        {
            blockingPanels = blockers;
            blockingPanelWasOpen = IsBlockingPanelOpen();
        }

        public void Configure(
            GameObject root,
            Button resume,
            Button save,
            Button load,
            Dropdown saveSlot,
            Button settings,
            Button mainMenu,
            Button quit,
            Text status,
            GameSettingsPanelUI settingsUi,
            GameObject[] blockers)
        {
            menuRoot = root;
            continueButton = resume;
            saveButton = save;
            loadButton = load;
            saveSlotDropdown = saveSlot;
            settingsButton = settings;
            mainMenuButton = mainMenu;
            quitButton = quit;
            statusText = status;
            settingsPanel = settingsUi;
            blockingPanels = blockers;
            BindButtons();
            CloseImmediate();
        }

        public void ConfigureSlotSelection(
            GameObject primaryPanel,
            GameObject selectionPanel,
            Text selectionTitle,
            Button[] buttons,
            Text[] labels,
            Button backButton)
        {
            mainPanel = primaryPanel;
            slotSelectionPanel = selectionPanel;
            slotSelectionTitle = selectionTitle;
            slotButtons = buttons;
            slotLabels = labels;
            slotBackButton = backButton;
            BindSlotButtons();
            CloseSlotSelection();
        }

        public void ConfigureSlotArtwork(
            RawImage artwork,
            Texture saveArtwork,
            Texture loadArtwork)
        {
            slotSelectionArtwork = artwork;
            saveSlotArtwork = saveArtwork;
            loadSlotArtwork = loadArtwork;
        }

        public void ConfigureSlotConfirmation(
            GameObject panel,
            Text message,
            Button confirm,
            Button cancel)
        {
            slotConfirmationPanel = panel;
            slotConfirmationMessage = message;
            slotConfirmButton = confirm;
            slotCancelButton = cancel;
            BindConfirmationButtons();
            CloseSlotConfirmation();
        }

        private void Awake()
        {
            BindButtons();
            CloseImmediate();
        }

        private void OnEnable()
        {
            SaveGameManager.StatusChanged -= HandleStatusChanged;
            SaveGameManager.StatusChanged += HandleStatusChanged;
        }

        private void OnDisable()
        {
            SaveGameManager.StatusChanged -= HandleStatusChanged;
            if (IsOpen) Close();
        }

        private void OnDestroy()
        {
            saveSlotDropdown?.onValueChanged.RemoveListener(HandleSaveSlotChanged);
            UnbindConfirmationButtons();
            UnbindSlotButtons();
            UnbindButtons();
        }

        private void Update()
        {
            bool blockingNow = IsBlockingPanelOpen();
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (slotConfirmationPanel != null &&
                    slotConfirmationPanel.activeSelf)
                    CloseSlotConfirmation();
                else if (settingsPanel != null && settingsPanel.IsOpen)
                    settingsPanel.Close();
                else if (slotSelectionPanel != null && slotSelectionPanel.activeSelf)
                    CloseSlotSelection();
                else if (IsOpen)
                    Close();
                else if (!blockingNow && !blockingPanelWasOpen)
                    Open();
            }
            blockingPanelWasOpen = blockingNow;
        }

        public void Open()
        {
            if (menuRoot == null || IsOpen) return;

            previousTimeScale = Time.timeScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            menuRoot.SetActive(true);
            menuRoot.transform.SetAsLastSibling();
            settingsPanel?.Close();
            CloseSlotSelection();
            RefreshButtons();
        }

        public void Close()
        {
            if (!IsOpen) return;
            CloseImmediate();
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }

        private void Save()
        {
            if (slotSelectionPanel == null)
            {
                SaveGameManager.SaveGame();
                RefreshButtons();
                return;
            }

            OpenSlotSelection(SlotAction.Save);
        }

        private void Load()
        {
            if (slotSelectionPanel != null)
            {
                OpenSlotSelection(SlotAction.Load);
                return;
            }

            if (!SaveGameManager.LoadGame())
            {
                RefreshButtons();
                return;
            }
            Close();
        }

        private void OpenSettings() => settingsPanel?.Open();

        private void ReturnToMainMenu()
        {
            CloseImmediate();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private static void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RefreshButtons()
        {
            RefreshSaveSlots();
            if (saveButton != null)
                saveButton.interactable = SceneManager.GetActiveScene().name == "Shelter";
            if (loadButton != null)
                loadButton.interactable = SaveGameManager.HasAnySave;
            HandleStatusChanged(SaveGameManager.LastMessage);
        }

        private void RefreshSaveSlots()
        {
            if (saveSlotDropdown == null) return;

            List<string> labels = new(SaveGameManager.SlotCount);
            for (int slot = 1; slot <= SaveGameManager.SlotCount; slot++)
                labels.Add(SaveGameManager.GetSlotSummary(slot));

            saveSlotDropdown.ClearOptions();
            saveSlotDropdown.AddOptions(labels);
            saveSlotDropdown.SetValueWithoutNotify(SaveGameManager.CurrentSlot - 1);
            saveSlotDropdown.RefreshShownValue();
        }

        private void OpenSlotSelection(SlotAction action)
        {
            slotAction = action;
            if (mainPanel != null) mainPanel.SetActive(false);
            slotSelectionPanel.SetActive(true);
            slotSelectionPanel.transform.SetAsLastSibling();
            if (slotSelectionArtwork != null)
                slotSelectionArtwork.texture = action == SlotAction.Save
                    ? saveSlotArtwork
                    : loadSlotArtwork;
            if (slotSelectionTitle != null)
                slotSelectionTitle.text = action == SlotAction.Save
                    ? "저장할 슬롯 선택"
                    : "불러올 슬롯 선택";
            RefreshSlotButtons();
        }

        private void CloseSlotSelection()
        {
            CloseSlotConfirmation();
            slotAction = SlotAction.None;
            if (slotSelectionPanel != null) slotSelectionPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        private void RefreshSlotButtons()
        {
            if (slotButtons == null || slotLabels == null) return;
            int count = Mathf.Min(
                SaveGameManager.SlotCount,
                Mathf.Min(slotButtons.Length, slotLabels.Length));
            for (int index = 0; index < count; index++)
            {
                int slot = index + 1;
                if (slotLabels[index] != null)
                    slotLabels[index].text = SaveGameManager.GetSlotSummary(slot);
                if (slotButtons[index] != null)
                    slotButtons[index].interactable = slotAction == SlotAction.Save ||
                        SaveGameManager.HasSaveInSlot(slot);
            }
        }

        private void SelectSlot(int slot)
        {
            if (slotAction == SlotAction.Save)
            {
                OpenSlotConfirmation(
                    slot,
                    SaveGameManager.HasSaveInSlot(slot)
                        ? "이미 저장된 파일이 있습니다.\n덮어씌우겠습니까?"
                        : "저장하시겠습니까?");
                return;
            }

            if (slotAction == SlotAction.Load &&
                SaveGameManager.HasSaveInSlot(slot))
                OpenSlotConfirmation(slot, "불러오시겠습니까?");
            else
                RefreshSlotButtons();
        }

        private void OpenSlotConfirmation(int slot, string message)
        {
            pendingSlot = slot;
            pendingSlotAction = slotAction;
            if (slotConfirmationMessage != null)
                slotConfirmationMessage.text = message;
            if (slotConfirmationPanel == null)
            {
                ConfirmSlotAction();
                return;
            }

            slotConfirmationPanel.SetActive(true);
            slotConfirmationPanel.transform.SetAsLastSibling();
        }

        private void ConfirmSlotAction()
        {
            int slot = pendingSlot;
            SlotAction action = pendingSlotAction;
            CloseSlotConfirmation();
            if (slot < 1 || action == SlotAction.None) return;

            SaveGameManager.SetCurrentSlot(slot);
            if (action == SlotAction.Save)
            {
                if (SaveGameManager.SaveGame())
                {
                    CloseSlotSelection();
                    RefreshButtons();
                }
                else
                    RefreshSlotButtons();
                return;
            }

            if (action == SlotAction.Load && SaveGameManager.LoadGame())
                Close();
            else
                RefreshSlotButtons();
        }

        private void CloseSlotConfirmation()
        {
            pendingSlot = 0;
            pendingSlotAction = SlotAction.None;
            if (slotConfirmationPanel != null)
                slotConfirmationPanel.SetActive(false);
        }

        private void HandleSaveSlotChanged(int index)
        {
            SaveGameManager.SetCurrentSlot(index + 1);
            RefreshButtons();
        }

        private void HandleStatusChanged(string message)
        {
            if (statusText != null)
                statusText.text = string.IsNullOrWhiteSpace(message)
                    ? "벙커에서 현재 진행을 저장할 수 있습니다."
                    : message;
        }

        private bool IsBlockingPanelOpen()
        {
            if (blockingPanels == null) return false;
            foreach (GameObject panel in blockingPanels)
            {
                if (panel != null && panel.activeInHierarchy)
                    return true;
            }
            return false;
        }

        private void BindButtons()
        {
            UnbindButtons();
            saveSlotDropdown?.onValueChanged.RemoveListener(HandleSaveSlotChanged);
            saveSlotDropdown?.onValueChanged.AddListener(HandleSaveSlotChanged);
            continueButton?.onClick.AddListener(Close);
            saveButton?.onClick.AddListener(Save);
            loadButton?.onClick.AddListener(Load);
            settingsButton?.onClick.AddListener(OpenSettings);
            mainMenuButton?.onClick.AddListener(ReturnToMainMenu);
            quitButton?.onClick.AddListener(QuitGame);
            BindSlotButtons();
            BindConfirmationButtons();
        }

        private void BindConfirmationButtons()
        {
            UnbindConfirmationButtons();
            slotConfirmButton?.onClick.AddListener(ConfirmSlotAction);
            slotCancelButton?.onClick.AddListener(CloseSlotConfirmation);
        }

        private void UnbindConfirmationButtons()
        {
            slotConfirmButton?.onClick.RemoveListener(ConfirmSlotAction);
            slotCancelButton?.onClick.RemoveListener(CloseSlotConfirmation);
        }

        private void BindSlotButtons()
        {
            UnbindSlotButtons();
            if (slotButtons != null)
            {
                slotButtonListeners = new UnityAction[slotButtons.Length];
                for (int index = 0; index < slotButtons.Length; index++)
                {
                    int slot = index + 1;
                    slotButtonListeners[index] = () => SelectSlot(slot);
                    slotButtons[index]?.onClick.AddListener(slotButtonListeners[index]);
                }
            }
            slotBackButton?.onClick.AddListener(CloseSlotSelection);
        }

        private void UnbindSlotButtons()
        {
            if (slotButtons != null && slotButtonListeners != null)
            {
                int count = Mathf.Min(slotButtons.Length, slotButtonListeners.Length);
                for (int index = 0; index < count; index++)
                    slotButtons[index]?.onClick.RemoveListener(slotButtonListeners[index]);
            }
            slotButtonListeners = null;
            slotBackButton?.onClick.RemoveListener(CloseSlotSelection);
        }

        private void UnbindButtons()
        {
            saveSlotDropdown?.onValueChanged.RemoveListener(HandleSaveSlotChanged);
            continueButton?.onClick.RemoveListener(Close);
            saveButton?.onClick.RemoveListener(Save);
            loadButton?.onClick.RemoveListener(Load);
            settingsButton?.onClick.RemoveListener(OpenSettings);
            mainMenuButton?.onClick.RemoveListener(ReturnToMainMenu);
            quitButton?.onClick.RemoveListener(QuitGame);
        }

        private void CloseImmediate()
        {
            settingsPanel?.Close();
            CloseSlotSelection();
            if (menuRoot != null)
                menuRoot.SetActive(false);
        }
    }
}
