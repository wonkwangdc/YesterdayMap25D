using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    [DisallowMultipleComponent]
    public sealed class JoinEndingSequence : MonoBehaviour
    {
        [SerializeField] private RawImage artwork;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Texture[] survivorGroupImages;
        [SerializeField] private Texture[] redArmbandImages;
        [SerializeField] private Text captionText;
        [SerializeField] private Text continuePrompt;
        [SerializeField, Min(0.1f)] private float fadeDuration = 1f;
        [SerializeField, Min(0.1f)] private float imageDuration = 3.5f;
        [SerializeField, Min(0f)] private float blackPauseDuration = 0.65f;

        private Coroutine sequenceRoutine;
        private bool finalDiaryConfirmed;
        private EndingId activeEndingId = EndingId.None;

        public bool IsPlaying => sequenceRoutine != null;

        public void Configure(
            RawImage targetArtwork,
            CanvasGroup targetCanvasGroup,
            Texture[] survivorImages,
            Texture[] guardImages,
            Text caption,
            Text prompt)
        {
            artwork = targetArtwork;
            canvasGroup = targetCanvasGroup;
            survivorGroupImages = survivorImages;
            redArmbandImages = guardImages;
            captionText = caption;
            continuePrompt = prompt;
        }

        public bool Play(EndingId endingId)
        {
            Texture[] images = GetImages(endingId);
            if (!ValidateConfiguration(images))
            {
                Debug.LogError(
                    $"합류계열 엔딩 사진 연출 구성이 완전하지 않습니다. Ending: {endingId}",
                    this);
                return false;
            }

            if (sequenceRoutine != null)
                StopCoroutine(sequenceRoutine);

            activeEndingId = endingId;
            finalDiaryConfirmed = false;
            Time.timeScale = 1f;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            artwork.texture = null;
            artwork.color = Color.black;
            captionText.gameObject.SetActive(false);
            continuePrompt.gameObject.SetActive(false);
            sequenceRoutine = StartCoroutine(
                PlaySequence(images, GetCaptions(endingId)));
            return true;
        }

        private IEnumerator PlaySequence(
            Texture[] images,
            string[] captions)
        {
            for (int i = 0; i < images.Length; i++)
            {
                artwork.texture = images[i];
                yield return FadeArtwork(0f, 1f);
                captionText.text = captions[i];
                captionText.gameObject.SetActive(true);
                yield return FadeCaption(0f, 1f, 0.4f);
                yield return WaitRealtime(
                    Mathf.Max(0f, imageDuration - 0.8f));
                yield return FadeCaption(1f, 0f, 0.4f);
                captionText.gameObject.SetActive(false);
                yield return FadeArtwork(1f, 0f);
                yield return WaitRealtime(blackPauseDuration);
            }

            artwork.texture = null;
            artwork.color = Color.black;
            continuePrompt.text = "아무 키나 눌러 계속하세요";
            continuePrompt.gameObject.SetActive(true);

            yield return WaitForAnyKey();
            continuePrompt.gameObject.SetActive(false);

            LockPlayerControls();
            HideRegularShelterHud();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            yield return WaitRealtime(0.6f);

            DiaryUI diary = FindFirstObjectByType<DiaryUI>(
                FindObjectsInactive.Include);
            if (diary != null)
            {
                diary.OpenJoinEndingDiary(activeEndingId);
                yield return null;
                while (!finalDiaryConfirmed &&
                       !diary.IsFinalEndingConfirmed)
                {
                    yield return null;
                }
            }

            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.alpha = 1f;
            yield return WaitRealtime(0.45f);

            EndingId completedEndingId = activeEndingId;
            sequenceRoutine = null;
            activeEndingId = EndingId.None;
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.CompleteEnding(completedEndingId);
        }

        public void NotifyFinalDiaryConfirmed()
        {
            finalDiaryConfirmed = true;
        }

        private Texture[] GetImages(EndingId endingId)
        {
            return endingId switch
            {
                EndingId.WithStrangers => survivorGroupImages,
                EndingId.RedArmband => redArmbandImages,
                _ => null
            };
        }

        private static string[] GetCaptions(EndingId endingId)
        {
            return endingId switch
            {
                EndingId.WithStrangers => new[]
                {
                    "서로 믿지 못하는 건 마찬가지겠지. 그래도 혼자보다는 나을 거야.",
                    "내 물건이 아니라, 이제는 우리 물자라… 아직은 낯설다.",
                    "누군가와 음식을 나눠 먹는 게 이렇게 안심되는 일이었나."
                },
                EndingId.RedArmband => new[]
                {
                    "저 시선들… 여기서는 먼저 규칙부터 배워야 살아남겠군.",
                    "안전을 얻으려면 내 물건과 이름부터 넘겨야 하는 건가.",
                    "굶지는 않겠지. 하지만 이곳에서도 마음대로 나갈 수 있을까."
                },
                _ => null
            };
        }

        private bool ValidateConfiguration(Texture[] images)
        {
            if (artwork == null || canvasGroup == null ||
                captionText == null ||
                continuePrompt == null || images == null ||
                images.Length != 3)
            {
                return false;
            }

            foreach (Texture image in images)
            {
                if (image == null) return false;
            }

            return true;
        }

        private IEnumerator FadeCaption(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Color color = captionText.color;
                color.a = Mathf.Lerp(
                    from, to, Mathf.Clamp01(elapsed / duration));
                captionText.color = color;
                yield return null;
            }

            Color finalColor = captionText.color;
            finalColor.a = to;
            captionText.color = finalColor;
        }

        private IEnumerator FadeArtwork(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                artwork.color = new Color(
                    1f, 1f, 1f, Mathf.Lerp(from, to, progress));
                yield return null;
            }

            artwork.color = new Color(1f, 1f, 1f, to);
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(
                    from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private static IEnumerator WaitRealtime(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static IEnumerator WaitForAnyKey()
        {
            yield return null;
            while (Keyboard.current == null ||
                   !Keyboard.current.anyKey.wasPressedThisFrame)
            {
                yield return null;
            }
        }

        private static void LockPlayerControls()
        {
            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
            movement?.SetCinematicInputLocked(true);
            PlayerInteraction interaction =
                FindFirstObjectByType<PlayerInteraction>();
            if (interaction != null)
                interaction.enabled = false;

            InteractionPromptUI prompt =
                FindFirstObjectByType<InteractionPromptUI>();
            prompt?.SetPrompt(string.Empty);
        }

        private void HideRegularShelterHud()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child == transform ||
                    child.name == "DiaryButton" ||
                    child.name == "DiaryViewPanel" ||
                    child.name == "EndingPanel")
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (sequenceRoutine == null) return;
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
            activeEndingId = EndingId.None;
        }
    }
}
