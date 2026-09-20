using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.UI;

namespace YesterdayMap.Core
{
    public sealed class PrologueController : MonoBehaviour
    {
        [SerializeField] private Text storyText;
        [SerializeField] private SceneFader sceneFader;
        [SerializeField] private GameObject nameEntryPanel;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Text nameValidationText;
        [SerializeField] private PrologueCinematicSequence cinematicSequence;
        private readonly string[] lines =
        {
            "그날 아침, 도시의 모든 경보가 동시에 울렸다.",
            "뉴스에서는 집 밖으로 나오지 말라고 했다.",
            "나는 이 날을 오래전부터 준비해 왔다.",
            "하지만 영화와 현실은 전혀 달랐다."
        };
        private int index;
        private bool nameConfirmed;
        private bool nameConfirmationPending;
        private bool cinematicPlaying;
        private int nameConfirmedFrame = -1;
        private string latestImeComposition = string.Empty;
        private int latestImeCompositionCaret;
        private int latestImeCompositionFrame = -1;
        private string pendingImeComposition = string.Empty;
        private int pendingImeCompositionCaret;

        public void Configure(Text text, SceneFader fader) { storyText = text; sceneFader = fader; }
        public void ConfigureCinematic(PrologueCinematicSequence sequence)
        {
            cinematicSequence = sequence;
        }
        private void Start()
        {
            BuildNameEntryUI();
            SetStoryVisible(false);
            nameEntryPanel.SetActive(true);
            nameInput.text = string.Empty;
            nameInput.Select();
            nameInput.ActivateInputField();
            SetKoreanImeEnabled(true);
        }

        private void OnDestroy()
        {
            SetKoreanImeEnabled(false);
        }

        private void Update()
        {
            if (!nameConfirmed)
            {
                TrackImeComposition();
                if (Keyboard.current != null &&
                    Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    RequestNameConfirmation();
                }
                return;
            }

            if (Time.frameCount == nameConfirmedFrame) return;
            if (cinematicPlaying) return;
            bool advance = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            advance |= Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (!advance) return;
            index++;
            if (index >= lines.Length) sceneFader.LoadScene("Scavenge");
            else storyText.text = lines[index];
        }

        private void RequestNameConfirmation()
        {
            if (nameConfirmationPending) return;

            TrackImeComposition();
            bool hasRecentComposition = latestImeCompositionFrame >= 0 &&
                Time.frameCount - latestImeCompositionFrame <= 1;
            pendingImeComposition = hasRecentComposition
                ? latestImeComposition
                : string.Empty;
            pendingImeCompositionCaret = latestImeCompositionCaret;
            StartCoroutine(ConfirmNameAfterImeCommit());
        }

        private IEnumerator ConfirmNameAfterImeCommit()
        {
            nameConfirmationPending = true;
            yield return null;
            CommitPendingImeComposition();
            nameConfirmationPending = false;
            ConfirmName();
        }

        private void TrackImeComposition()
        {
            if (nameInput == null || !nameInput.isFocused) return;

            string composition = UnityEngine.Input.compositionString;
            if (string.IsNullOrEmpty(composition)) return;

            latestImeComposition = composition;
            latestImeCompositionCaret = Mathf.Clamp(
                nameInput.caretPosition - composition.Length,
                0,
                nameInput.text.Length);
            latestImeCompositionFrame = Time.frameCount;
        }

        private void CommitPendingImeComposition()
        {
            if (nameInput == null || string.IsNullOrEmpty(pendingImeComposition))
                return;

            string currentText = nameInput.text ?? string.Empty;
            int caret = Mathf.Clamp(
                pendingImeCompositionCaret,
                0,
                currentText.Length);
            bool alreadyCommitted = caret + pendingImeComposition.Length <=
                                    currentText.Length &&
                string.CompareOrdinal(
                    currentText,
                    caret,
                    pendingImeComposition,
                    0,
                    pendingImeComposition.Length) == 0;

            if (!alreadyCommitted)
            {
                string committedText = currentText.Insert(
                    caret,
                    pendingImeComposition);
                if (committedText.Length > GameSession.MaxPlayerNameLength)
                {
                    committedText = committedText.Substring(
                        0,
                        GameSession.MaxPlayerNameLength);
                }

                nameInput.text = committedText;
                nameInput.caretPosition = Mathf.Min(
                    caret + pendingImeComposition.Length,
                    committedText.Length);
            }

            pendingImeComposition = string.Empty;
        }

        private void ConfirmName()
        {
            nameInput?.ForceLabelUpdate();
            if (!GameSession.TrySetPlayerName(nameInput != null ? nameInput.text : string.Empty))
            {
                if (nameValidationText != null)
                    nameValidationText.text = "이름을 입력해주세요.";
                nameInput?.ActivateInputField();
                return;
            }

            nameConfirmed = true;
            nameConfirmedFrame = Time.frameCount;
            SetKoreanImeEnabled(false);
            nameEntryPanel.SetActive(false);
            if (cinematicSequence != null && cinematicSequence.IsConfigured)
            {
                cinematicPlaying = true;
                SetStoryVisible(false);
                cinematicSequence.Play(CompleteCinematic);
                return;
            }

            SetStoryVisible(true);
            storyText.text = lines[0];
        }

        private void CompleteCinematic()
        {
            cinematicPlaying = false;
            sceneFader.LoadScene("Scavenge");
        }

        private static void SetKoreanImeEnabled(bool enabled)
        {
            if (Keyboard.current != null)
            {
                Keyboard.current.SetIMEEnabled(enabled);
            }
        }

        private void SetStoryVisible(bool visible)
        {
            if (storyText != null) storyText.gameObject.SetActive(visible);
            Transform hint = storyText != null
                ? storyText.transform.parent.Find("ContinueHint")
                : null;
            if (hint != null) hint.gameObject.SetActive(visible);
        }

        private void BuildNameEntryUI()
        {
            if (nameEntryPanel != null && nameInput != null) return;
            Transform parent = storyText != null ? storyText.transform.parent : transform;
            Font font = DefaultUIFont.Get();

            nameEntryPanel = CreateUIObject("NameEntryPanel", parent, typeof(Image));
            RectTransform panelRect = nameEntryPanel.GetComponent<RectTransform>();
            SetRect(panelRect, new Vector2(0.29f, 0.31f), new Vector2(0.71f, 0.69f));
            Image panelImage = nameEntryPanel.GetComponent<Image>();
            panelImage.color = new Color(0.035f, 0.03f, 0.025f, 0.96f);

            Text prompt = CreateText("NamePrompt", nameEntryPanel.transform, font, 30);
            prompt.text = "주인공의 이름을 입력해주세요";
            SetRect(prompt.rectTransform, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.91f));

            GameObject inputObject = CreateUIObject("NameInput", nameEntryPanel.transform, typeof(Image), typeof(InputField));
            SetRect(inputObject.GetComponent<RectTransform>(), new Vector2(0.14f, 0.42f), new Vector2(0.86f, 0.64f));
            inputObject.GetComponent<Image>().color = new Color(0.12f, 0.11f, 0.10f, 1f);
            nameInput = inputObject.GetComponent<InputField>();
            nameInput.characterLimit = GameSession.MaxPlayerNameLength;
            nameInput.lineType = InputField.LineType.SingleLine;

            Text inputText = CreateText("Text", inputObject.transform, font, 28);
            inputText.alignment = TextAnchor.MiddleCenter;
            inputText.color = Color.white;
            SetRect(inputText.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            nameInput.textComponent = inputText;

            Text placeholder = CreateText("Placeholder", inputObject.transform, font, 24);
            placeholder.text = "최대 8글자";
            placeholder.alignment = TextAnchor.MiddleCenter;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            SetRect(placeholder.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            nameInput.placeholder = placeholder;

            GameObject confirmObject = CreateUIObject("NameConfirmButton", nameEntryPanel.transform, typeof(Image), typeof(Button));
            SetRect(confirmObject.GetComponent<RectTransform>(), new Vector2(0.34f, 0.18f), new Vector2(0.66f, 0.34f));
            confirmObject.GetComponent<Image>().color = new Color(0.28f, 0.24f, 0.18f, 1f);
            confirmObject.GetComponent<Button>().onClick.AddListener(RequestNameConfirmation);
            Text confirmText = CreateText("Label", confirmObject.transform, font, 24);
            confirmText.text = "확인";
            SetRect(confirmText.rectTransform, Vector2.zero, Vector2.one);

            nameValidationText = CreateText("NameValidation", nameEntryPanel.transform, font, 18);
            nameValidationText.color = new Color(0.95f, 0.55f, 0.48f, 1f);
            SetRect(nameValidationText.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.15f));
        }

        private static GameObject CreateUIObject(string objectName, Transform parent, params System.Type[] components)
        {
            GameObject result = new(objectName, typeof(RectTransform), typeof(CanvasRenderer));
            result.transform.SetParent(parent, false);
            foreach (System.Type component in components)
                if (result.GetComponent(component) == null) result.AddComponent(component);
            return result;
        }

        private static Text CreateText(string objectName, Transform parent, Font font, int size)
        {
            GameObject owner = CreateUIObject(objectName, parent, typeof(Text));
            Text text = owner.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
