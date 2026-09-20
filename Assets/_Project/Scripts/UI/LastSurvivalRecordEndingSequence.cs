using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Events;

namespace YesterdayMap.UI
{
    [DisallowMultipleComponent]
    public sealed class LastSurvivalRecordEndingSequence : MonoBehaviour
    {
        [SerializeField] private Image blackScreen;
        [SerializeField] private RawImage deathArtwork;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Texture starvationImage;
        [SerializeField] private Texture dehydrationImage;
        [SerializeField, Min(0.1f)] private float slowWakeDuration = 2f;
        [SerializeField, Range(0.05f, 1f)] private float slowMoveMultiplier = 0.3f;
        [SerializeField, Min(0.1f)] private float dialogueHoldDuration = 2.5f;
        [SerializeField, Min(0.1f)] private float blackFadeDuration = 1f;
        [SerializeField, Min(0.1f)] private float artworkFadeDuration = 1f;
        [SerializeField, Min(0.1f)] private float artworkHoldDuration = 4f;
        private Coroutine routine;
        private bool finalDiaryConfirmed;
        private bool finalDialogueConfirmed;

        public bool IsPlaying => routine != null;

        public void Configure(
            Image screen,
            RawImage artwork,
            CanvasGroup group,
            Texture starvation,
            Texture dehydration)
        {
            blackScreen = screen;
            deathArtwork = artwork;
            canvasGroup = group;
            starvationImage = starvation;
            dehydrationImage = dehydration;
        }

        public bool Play(string deathReason)
        {
            Texture selectedImage = ResolveDeathImage(deathReason);
            if (blackScreen == null || deathArtwork == null ||
                canvasGroup == null || selectedImage == null || routine != null)
                return false;

            finalDiaryConfirmed = false;
            finalDialogueConfirmed = false;
            Time.timeScale = 1f;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            deathArtwork.texture = selectedImage;
            deathArtwork.color = new Color(1f, 1f, 1f, 0f);
            routine = StartCoroutine(PlayRoutine(deathReason));
            return true;
        }

        public void NotifyFinalDiaryConfirmed()
        {
            finalDiaryConfirmed = true;
        }

        private IEnumerator PlayRoutine(string deathReason)
        {
            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
            if (movement != null)
            {
                movement.SetCinematicInputLocked(false);
                movement.SetCinematicSpeedMultiplier(slowMoveMultiplier);
            }

            PlayerInteraction interaction = FindFirstObjectByType<PlayerInteraction>();
            if (interaction != null) interaction.enabled = false;
            FindFirstObjectByType<InteractionPromptUI>()?.SetPrompt(string.Empty);

            yield return WaitRealtime(slowWakeDuration);

            if (movement != null)
            {
                movement.SetCinematicSpeedMultiplier(1f);
                movement.SetCinematicInputLocked(true);
            }

            ShelterEventDialogueController dialogueController =
                FindFirstObjectByType<ShelterEventDialogueController>(
                    FindObjectsInactive.Include);
            bool dialogueOpened = dialogueController != null &&
                dialogueController.ShowLastSurvivalEndingDialogue(
                    GetFinalDialogue(deathReason),
                    () => finalDialogueConfirmed = true);
            if (dialogueOpened)
            {
                while (!finalDialogueConfirmed)
                {
                    yield return null;
                }
            }
            else
            {
                yield return WaitRealtime(dialogueHoldDuration);
            }

            LockControls();
            HideRegularShelterHud();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            yield return Fade(0f, 1f, blackFadeDuration);
            yield return WaitRealtime(0.35f);
            yield return FadeArtwork(0f, 1f, artworkFadeDuration);
            yield return WaitRealtime(artworkHoldDuration);
            yield return FadeArtwork(1f, 0f, artworkFadeDuration);
            deathArtwork.texture = null;
            yield return WaitRealtime(0.5f);
            yield return Fade(1f, 0f, 1.2f);
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            yield return WaitRealtime(1f);

            DiaryUI diary = FindFirstObjectByType<DiaryUI>(
                FindObjectsInactive.Include);
            if (diary != null)
            {
                diary.OpenLastSurvivalRecordDiary(deathReason);
                while (!finalDiaryConfirmed &&
                       !diary.IsFinalEndingConfirmed)
                {
                    yield return null;
                }
            }

            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            yield return Fade(0f, 1f, 1f);
            yield return WaitRealtime(0.8f);
            routine = null;
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.CompleteEnding(EndingId.LastSurvivalRecord);
        }

        private Texture ResolveDeathImage(string deathReason)
        {
            return deathReason switch
            {
                "Starvation" => starvationImage,
                "Dehydration" => dehydrationImage,
                _ => null
            };
        }

        private static string GetFinalDialogue(string deathReason)
        {
            return deathReason switch
            {
                "Starvation" => "조금만…… 더 버틸 수 있을 줄 알았는데.",
                "Dehydration" => "물…….",
                _ => string.Empty
            };
        }

        private IEnumerator FadeArtwork(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(
                    from, to, Mathf.Clamp01(elapsed / duration));
                deathArtwork.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            deathArtwork.color = new Color(1f, 1f, 1f, to);
        }

        private static void LockControls()
        {
            FindFirstObjectByType<PlayerMovement>()?.SetCinematicInputLocked(true);
            PlayerInteraction interaction = FindFirstObjectByType<PlayerInteraction>();
            if (interaction != null) interaction.enabled = false;
            FindFirstObjectByType<InteractionPromptUI>()?.SetPrompt(string.Empty);
        }

        private void HideRegularShelterHud()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child == transform || child.name == "DiaryButton" ||
                    child.name == "DiaryViewPanel" || child.name == "EndingPanel")
                    continue;
                child.gameObject.SetActive(false);
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
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
    }
}
