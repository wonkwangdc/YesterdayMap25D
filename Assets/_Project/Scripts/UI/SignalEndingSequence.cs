using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    [DisallowMultipleComponent]
    public sealed class SignalEndingSequence : MonoBehaviour
    {
        [SerializeField] private RawImage artwork;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Texture[] militaryFrequencyImages;
        [SerializeField] private Texture[] secondFrequencyImages;
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
            Texture[] militaryImages,
            Texture[] badRadioImages,
            Text caption,
            Text prompt)
        {
            artwork = targetArtwork;
            canvasGroup = targetCanvasGroup;
            militaryFrequencyImages = militaryImages;
            secondFrequencyImages = badRadioImages;
            captionText = caption;
            continuePrompt = prompt;
        }

        public bool Play(EndingId endingId)
        {
            Texture[] images = GetImages(endingId);
            if (!ValidateConfiguration(images))
            {
                Debug.LogError(
                    $"구조신호 엔딩 사진 연출 구성이 완전하지 않습니다. Ending: {endingId}",
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
                diary.OpenSignalEndingDiary(activeEndingId);
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
                EndingId.LastFrequency => militaryFrequencyImages,
                EndingId.DoorOpenedNight => secondFrequencyImages,
                _ => null
            };
        }

        private static string[] GetCaptions(EndingId endingId)
        {
            return endingId switch
            {
                EndingId.LastFrequency => new[]
                {
                    "여기까지 왔으니… 이번 신호만큼은 믿어보자.",
                    "아무도 없네. 또 너무 늦은 건가.",
                    "저 불빛… 정말 나를 찾으러 온 건가?"
                },
                EndingId.DoorOpenedNight => new[]
                {
                    "가까운 곳이야. 이번에는 헛걸음이 아니었으면 좋겠는데.",
                    "이 물건들… 사람들이 스스로 두고 간 건 아닌 것 같아.",
                    "한두 명이 아니야. 처음부터 나를 기다리고 있었던 건가."
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
