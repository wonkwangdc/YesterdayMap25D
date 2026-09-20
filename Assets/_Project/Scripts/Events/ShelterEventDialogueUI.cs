using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.CameraSystem;
using YesterdayMap.UI;

namespace YesterdayMap.Events
{
    // Shows one shelter branch event as a visual-novel style dialogue panel.
    public sealed class ShelterEventDialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RawImage paperBackground;
        [SerializeField] private RawImage leftPortrait;
        [SerializeField] private RawImage rightPortrait;
        [SerializeField] private Text dayText;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text resultText;
        [SerializeField] private Button firstChoiceButton;
        [SerializeField] private Button secondChoiceButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text firstChoiceText;
        [SerializeField] private Text secondChoiceText;
        [SerializeField] private GameObject subtitlesRoot;

        private string firstChoiceId = string.Empty;
        private string secondChoiceId = string.Empty;
        private bool subtitlesWereActive;
        private bool subtitlesStateCaptured;

        public event Action<string> ChoiceRequested;
        public event Action Closed;
        public bool IsOpen => root != null && root.activeSelf;

        public void Configure(
            GameObject panelRoot,
            RawImage background,
            RawImage left,
            RawImage right,
            Text day,
            Text speaker,
            Text title,
            Text body,
            Text result,
            Button firstButton,
            Button secondButton,
            Button close,
            Text firstText,
            Text secondText,
            GameObject subtitles)
        {
            root = panelRoot;
            paperBackground = background;
            leftPortrait = left;
            rightPortrait = right;
            dayText = day;
            speakerText = speaker;
            titleText = title;
            bodyText = body;
            resultText = result;
            firstChoiceButton = firstButton;
            secondChoiceButton = secondButton;
            closeButton = close;
            firstChoiceText = firstText;
            secondChoiceText = secondText;
            subtitlesRoot = subtitles;
            BindButtons();
            EnsureChoiceHoverEffects();
            if (root != null)
            {
                root.SetActive(false);
            }

            if (subtitlesRoot != null)
            {
                subtitlesRoot.SetActive(true);
            }

            subtitlesStateCaptured = false;
        }

        private void Awake()
        {
            BindButtons();
            EnsureChoiceHoverEffects();
        }

        private void OnDestroy()
        {
            if (firstChoiceButton != null) firstChoiceButton.onClick.RemoveListener(ChooseFirst);
            if (secondChoiceButton != null) secondChoiceButton.onClick.RemoveListener(ChooseSecond);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void ShowEvent(
            BranchEventDefinition definition,
            int day,
            Texture leftPortraitTexture,
            Texture rightPortraitTexture)
        {
            if (definition == null)
            {
                return;
            }

            string firstId = definition.Choices.Count > 0
                ? definition.Choices[0].ChoiceId
                : string.Empty;
            string firstLabel = definition.Choices.Count > 0
                ? definition.Choices[0].Label
                : string.Empty;
            string secondId = definition.Choices.Count > 1
                ? definition.Choices[1].ChoiceId
                : string.Empty;
            string secondLabel = definition.Choices.Count > 1
                ? definition.Choices[1].Label
                : string.Empty;

            ShowChoice(
                day,
                "문 너머의 목소리",
                definition.Title,
                definition.Body,
                firstId,
                firstLabel,
                secondId,
                secondLabel,
                leftPortraitTexture,
                rightPortraitTexture);
        }

        public void ShowChoice(
            int day,
            string speaker,
            string title,
            string body,
            string firstId,
            string firstLabel,
            string secondId,
            string secondLabel,
            Texture leftPortraitTexture,
            Texture rightPortraitTexture)
        {
            if (root == null)
            {
                return;
            }

            bool wasAlreadyOpen = root.activeSelf;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            BindButtons();
            IsometricCameraController.SetFirstPersonUiFocus(true);
            if (subtitlesRoot != null && !wasAlreadyOpen)
            {
                subtitlesWereActive = subtitlesRoot.activeSelf;
                subtitlesStateCaptured = true;
                subtitlesRoot.SetActive(false);
            }

            if (leftPortrait != null)
            {
                leftPortrait.rectTransform.localScale = Vector3.one;
                leftPortrait.texture = leftPortraitTexture;
                leftPortrait.gameObject.SetActive(leftPortraitTexture != null);
            }

            if (rightPortrait != null)
            {
                rightPortrait.rectTransform.localScale = Vector3.one;
                rightPortrait.texture = rightPortraitTexture;
                rightPortrait.gameObject.SetActive(rightPortraitTexture != null);
            }

            if (paperBackground != null)
            {
                paperBackground.color = Color.white;
            }

            if (dayText != null) dayText.text = $"{day}일차";
            if (speakerText != null)
            {
                speakerText.text = string.Empty;
                speakerText.gameObject.SetActive(false);
            }
            if (titleText != null)
            {
                titleText.text = !string.IsNullOrWhiteSpace(title)
                    ? title
                    : speaker ?? string.Empty;
            }
            if (bodyText != null) bodyText.text = body ?? string.Empty;
            if (resultText != null) resultText.text = string.Empty;

            ApplyChoice(firstId, firstLabel, firstChoiceButton, firstChoiceText, out firstChoiceId);
            ApplyChoice(secondId, secondLabel, secondChoiceButton, secondChoiceText, out secondChoiceId);
        }

        public void ShowDialogueLine(
            int day,
            string speaker,
            string body,
            string nextChoiceId,
            Texture leftPortraitTexture,
            Texture rightPortraitTexture,
            float rightPortraitScale = 1f)
        {
            ShowChoice(
                day,
                speaker,
                speaker,
                body,
                nextChoiceId,
                "다음",
                string.Empty,
                string.Empty,
                leftPortraitTexture,
                rightPortraitTexture);

            if (rightPortrait != null && rightPortraitTexture != null)
            {
                float safeScale = Mathf.Max(0.1f, rightPortraitScale);
                rightPortrait.rectTransform.localScale =
                    Vector3.one * safeScale;
            }
        }

        public void ShowResult(BranchEventSelectionResult result)
        {
            if (result == null || !result.IsSuccess)
            {
                if (resultText != null)
                {
                    resultText.text = result == null
                        ? "이벤트 처리에 실패했습니다."
                        : result.FailureReason;
                }

                return;
            }

            if (speakerText != null) speakerText.text = "기록";
            if (resultText != null) resultText.text = result.Choice.ResultText;
            SetButtonInteractable(firstChoiceButton, false);
            SetButtonInteractable(secondChoiceButton, false);
        }

        public void ShowResultText(string speaker, string result)
        {
            if (speakerText != null) speakerText.text = speaker ?? "기록";
            if (resultText != null) resultText.text = result ?? string.Empty;
            SetButtonInteractable(firstChoiceButton, false);
            SetButtonInteractable(secondChoiceButton, false);
        }

        public void ShowDiaryResultPrompt()
        {
            SetButtonInteractable(firstChoiceButton, false);
            SetButtonInteractable(secondChoiceButton, false);
            DiaryUI.NotifyUnreadResultAvailable();
            StartCoroutine(CloseAfterChoiceClick());
        }

        private IEnumerator CloseAfterChoiceClick()
        {
            yield return null;
            Close();
        }

        public void Close()
        {
            bool wasOpen = root != null && root.activeSelf;
            if (root != null)
            {
                root.SetActive(false);
            }

            if (wasOpen)
            {
                IsometricCameraController.SetFirstPersonUiFocus(false);
                if (subtitlesRoot != null && subtitlesStateCaptured)
                {
                    subtitlesRoot.SetActive(subtitlesWereActive);
                    subtitlesStateCaptured = false;
                }

                Closed?.Invoke();
            }
        }

        private void BindButtons()
        {
            if (firstChoiceButton != null)
            {
                firstChoiceButton.onClick.RemoveListener(ChooseFirst);
                firstChoiceButton.onClick.AddListener(ChooseFirst);
            }

            if (secondChoiceButton != null)
            {
                secondChoiceButton.onClick.RemoveListener(ChooseSecond);
                secondChoiceButton.onClick.AddListener(ChooseSecond);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void EnsureChoiceHoverEffects()
        {
            EnsureChoiceHoverEffect(
                firstChoiceButton,
                firstChoiceText);
            EnsureChoiceHoverEffect(
                secondChoiceButton,
                secondChoiceText);
        }

        private static void EnsureChoiceHoverEffect(
            Button button,
            Text label)
        {
            if (button == null)
            {
                return;
            }

            DialogueChoiceHoverEffect effect =
                button.GetComponent<DialogueChoiceHoverEffect>();
            if (effect == null)
            {
                effect =
                    button.gameObject.AddComponent<DialogueChoiceHoverEffect>();
            }

            effect.Configure(button, label);
        }

        private static void ApplyChoice(
            string id,
            string choiceLabel,
            Button button,
            Text label,
            out string choiceId)
        {
            bool hasChoice = !string.IsNullOrWhiteSpace(id);
            choiceId = hasChoice ? id : string.Empty;

            if (hasChoice)
            {
                if (label != null) label.text = choiceLabel ?? string.Empty;
            }

            SetButtonInteractable(button, hasChoice);
            if (button != null) button.gameObject.SetActive(hasChoice);
        }

        private void ChooseFirst()
        {
            if (!string.IsNullOrWhiteSpace(firstChoiceId))
            {
                ChoiceRequested?.Invoke(firstChoiceId);
            }
        }

        private void ChooseSecond()
        {
            if (!string.IsNullOrWhiteSpace(secondChoiceId))
            {
                ChoiceRequested?.Invoke(secondChoiceId);
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
                DialogueChoiceHoverEffect effect =
                    button.GetComponent<DialogueChoiceHoverEffect>();
                if (effect != null)
                {
                    effect.SetInteractable(interactable);
                }
            }
        }
    }
}
