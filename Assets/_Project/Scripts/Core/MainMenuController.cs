using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YesterdayMap.UI;

namespace YesterdayMap.Core
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button recordsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SceneFader sceneFader;
        [SerializeField] private GameSettingsPanelUI settingsPanel;
        [SerializeField] private MainMenuRecordsPanelUI recordsPanel;

        private Material referenceCompositeMaterial;

        private void OnEnable()
        {
            RestoreMenuCursor();
        }

        public void Configure(
            Button newGame,
            Button continueGame,
            Button records,
            Button settings,
            Button quit,
            SceneFader fader)
        {
            newGameButton = newGame;
            continueButton = continueGame;
            recordsButton = records;
            settingsButton = settings;
            quitButton = quit;
            sceneFader = fader;
        }

        private void Awake()
        {
            RestoreMenuCursor();
            NormalizeReferenceComposite();
            ConfigureReferenceCompositeMaterial();
        }

        private void Start()
        {
            RestoreMenuCursor();

            if (settingsPanel == null)
            {
                settingsPanel = FindFirstObjectByType<GameSettingsPanelUI>(
                    FindObjectsInactive.Include);
            }

            if (recordsPanel == null)
            {
                recordsPanel = FindFirstObjectByType<MainMenuRecordsPanelUI>(
                    FindObjectsInactive.Include);
            }

            BindButton(newGameButton, StartNewGame);
            BindButton(continueButton, ContinueGame);
            BindButton(recordsButton, OpenRecords);
            BindButton(settingsButton, OpenSettings);
            BindButton(quitButton, QuitGame);

            if (continueButton != null)
                continueButton.interactable = true;
            if (recordsButton != null)
                recordsButton.interactable = true;
            if (settingsButton != null)
                settingsButton.interactable = true;

            ConfigureKeyboardNavigation();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                RestoreMenuCursor();
        }

        private static void RestoreMenuCursor()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ConfigureKeyboardNavigation()
        {
            Button[] menuButtons =
            {
                newGameButton,
                continueButton,
                recordsButton,
                settingsButton,
                quitButton
            };

            for (int index = 0; index < menuButtons.Length; index++)
            {
                Button current = menuButtons[index];
                if (current == null) continue;

                Navigation navigation = current.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = FindNextInteractableButton(
                    menuButtons, index, -1);
                navigation.selectOnDown = FindNextInteractableButton(
                    menuButtons, index, 1);
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                navigation.wrapAround = false;
                current.navigation = navigation;
            }

            if (EventSystem.current != null)
            {
                Button initialButton = FindNextInteractableButton(
                    menuButtons, menuButtons.Length - 1, 1);
                EventSystem.current.SetSelectedGameObject(
                    initialButton != null ? initialButton.gameObject : null);
            }
        }

        private static Button FindNextInteractableButton(
            Button[] buttons,
            int currentIndex,
            int direction)
        {
            if (buttons == null || buttons.Length == 0) return null;

            for (int offset = 1; offset <= buttons.Length; offset++)
            {
                int index = (currentIndex + direction * offset) % buttons.Length;
                if (index < 0) index += buttons.Length;

                Button candidate = buttons[index];
                if (candidate != null && candidate.IsInteractable() &&
                    candidate.gameObject.activeInHierarchy)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            UnbindButton(newGameButton, StartNewGame);
            UnbindButton(continueButton, ContinueGame);
            UnbindButton(recordsButton, OpenRecords);
            UnbindButton(settingsButton, OpenSettings);
            UnbindButton(quitButton, QuitGame);

            if (referenceCompositeMaterial != null)
                Destroy(referenceCompositeMaterial);
        }

        private void StartNewGame()
        {
            Time.timeScale = 1f;
            GameSession.Instance?.ResetSession();
            sceneFader.LoadScene("Prologue");
        }

        private static void ContinueGame()
        {
            SaveGameManager.LoadLatestGame();
        }

        private void OpenRecords()
        {
            settingsPanel?.Close();
            recordsPanel?.Open();
        }

        private void OpenSettings()
        {
            recordsPanel?.Close();
            settingsPanel?.Open();
        }

        private static void BindButton(Button button, UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void UnbindButton(Button button, UnityAction action)
        {
            if (button != null && action != null)
                button.onClick.RemoveListener(action);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void NormalizeReferenceComposite()
        {
            GameObject overlayObject =
                GameObject.Find("MainMenuUI/ReferenceMenuOverlay");
            if (overlayObject == null) return;

            RectTransform overlay =
                overlayObject.transform as RectTransform;
            RectTransform composite =
                overlayObject.transform.Find("ReferenceCompositeImage")
                    as RectTransform;
            StretchToParent(overlay);
            StretchToParent(composite);
        }

        private void ConfigureReferenceCompositeMaterial()
        {
            GameObject overlayObject =
                GameObject.Find("MainMenuUI/ReferenceMenuOverlay");
            Transform compositeTransform = overlayObject != null
                ? overlayObject.transform.Find("ReferenceCompositeImage")
                : null;
            RawImage compositeImage = compositeTransform != null
                ? compositeTransform.GetComponent<RawImage>()
                : null;
            if (compositeImage == null)
                return;

            Shader shader = UnityEngine.Resources.Load<Shader>(
                "Shaders/MainMenuDifferenceOverlay");
            if (shader == null)
            {
                Debug.LogWarning(
                    "Main menu difference overlay shader was not found.",
                    this);
                return;
            }

            referenceCompositeMaterial = new Material(shader)
            {
                name = "Main Menu Difference Overlay (Runtime)"
            };
            referenceCompositeMaterial.SetFloat("_SolidUntil", 0.36f);
            referenceCompositeMaterial.SetFloat("_FadeUntil", 0.46f);
            compositeImage.material = referenceCompositeMaterial;

            RectMask2D mask = overlayObject.GetComponent<RectMask2D>();
            if (mask != null)
                mask.enabled = false;
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}

