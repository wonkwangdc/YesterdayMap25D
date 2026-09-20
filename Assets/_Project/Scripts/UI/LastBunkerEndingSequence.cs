using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using YesterdayMap.Character;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    [DisallowMultipleComponent]
    public sealed class LastBunkerEndingSequence : MonoBehaviour
    {
        [SerializeField] private RawImage artwork;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Texture[] timeLapseImages;
        [SerializeField] private Text continuePrompt;
        [SerializeField, Min(0.1f)] private float fadeDuration = 1f;
        [SerializeField, Min(0.1f)] private float imageDuration = 3.5f;
        [SerializeField, Min(0f)] private float blackPauseDuration = 0.65f;

        private Coroutine sequenceRoutine;
        private bool finalDiaryConfirmed;

        public bool IsPlaying => sequenceRoutine != null;

        public void Configure(
            RawImage targetArtwork,
            CanvasGroup targetCanvasGroup,
            Texture[] images,
            Text prompt)
        {
            artwork = targetArtwork;
            canvasGroup = targetCanvasGroup;
            timeLapseImages = images;
            continuePrompt = prompt;
        }

        public bool Play(Action atComplete = null)
        {
            if (!ValidateConfiguration())
            {
                Debug.LogError("마지막 벙커 엔딩 사진 연출 구성이 완전하지 않습니다.", this);
                return false;
            }

            if (sequenceRoutine != null)
                StopCoroutine(sequenceRoutine);

            finalDiaryConfirmed = false;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            artwork.color = Color.black;
            if (continuePrompt != null)
                continuePrompt.gameObject.SetActive(false);
            sequenceRoutine = StartCoroutine(PlaySequence(atComplete));
            return true;
        }

        private IEnumerator PlaySequence(Action atComplete)
        {
            for (int i = 0; i < timeLapseImages.Length; i++)
            {
                artwork.texture = timeLapseImages[i];
                yield return FadeArtwork(0f, 1f);
                yield return WaitRealtime(imageDuration);
                yield return FadeArtwork(1f, 0f);

                if (i < timeLapseImages.Length - 1 && blackPauseDuration > 0f)
                    yield return WaitRealtime(blackPauseDuration);
            }

            artwork.texture = null;
            artwork.color = Color.black;
            if (continuePrompt != null)
            {
                continuePrompt.text = "아무 키나 눌러 계속하세요";
                continuePrompt.gameObject.SetActive(true);
            }

            yield return WaitForAnyKey();
            if (continuePrompt != null)
                continuePrompt.gameObject.SetActive(false);

            LockFinalBunkerControls();
            HideRegularShelterHud();
            yield return FadeCanvas(1f, 0f, 1.4f);
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            yield return WaitRealtime(1.25f);

            DiaryUI diary = FindFirstObjectByType<DiaryUI>(
                FindObjectsInactive.Include);
            if (diary != null)
            {
                diary.OpenFinalEndingDiary();
                yield return null;
                while (!finalDiaryConfirmed &&
                       !diary.IsFinalEndingConfirmed)
                    yield return null;
            }

            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            yield return FadeCanvas(0f, 1f, 1f);
            yield return WaitRealtime(0.8f);

            sequenceRoutine = null;
            atComplete?.Invoke();
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.CompleteEnding(EndingId.LastBunker);
        }

        public void NotifyFinalDiaryConfirmed()
        {
            finalDiaryConfirmed = true;
            if (sequenceRoutine != null) return;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            artwork.texture = null;
            artwork.color = Color.black;
            if (continuePrompt != null)
                continuePrompt.gameObject.SetActive(false);
            sequenceRoutine = StartCoroutine(CompleteAfterFinalDiary());
        }

        private IEnumerator CompleteAfterFinalDiary()
        {
            yield return FadeCanvas(0f, 1f, 1f);
            yield return WaitRealtime(0.8f);
            sequenceRoutine = null;
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.CompleteEnding(EndingId.LastBunker);
        }

        private IEnumerator FadeArtwork(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                artwork.color = new Color(1f, 1f, 1f, Mathf.Lerp(from, to, progress));
                yield return null;
            }

            artwork.color = new Color(1f, 1f, 1f, to);
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

        private static void LockFinalBunkerControls()
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

        private bool ValidateConfiguration()
        {
            if (artwork == null || canvasGroup == null || continuePrompt == null ||
                timeLapseImages == null || timeLapseImages.Length != 3)
            {
                return false;
            }

            foreach (Texture image in timeLapseImages)
            {
                if (image == null) return false;
            }

            return true;
        }

        private void OnDisable()
        {
            if (sequenceRoutine == null) return;
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
    }
}
