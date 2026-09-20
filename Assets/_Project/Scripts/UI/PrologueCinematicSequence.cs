using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YesterdayMap.Core;

namespace YesterdayMap.UI
{
    /// <summary>
    /// Presents the four-image opening cinematic after player-name entry.
    /// The sequence owns only presentation; PrologueController owns scene flow.
    /// </summary>
    public sealed class PrologueCinematicSequence : MonoBehaviour
    {
        [SerializeField] private CanvasGroup screenGroup;
        [SerializeField] private Image artwork;
        [SerializeField] private GameObject captionPanel;
        [SerializeField] private Text captionText;
        [SerializeField] private GameObject breakingNewsPanel;
        [SerializeField] private Text breakingNewsText;
        [SerializeField] private GameObject emergencyAlertPanel;
        [SerializeField] private Text emergencyAlertText;
        [SerializeField] private Text advanceHint;
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.55f;

        private Coroutine routine;
        private Action completed;
        private float acceptInputAfter;

        public bool IsPlaying => routine != null;
        public bool IsConfigured =>
            screenGroup != null && artwork != null &&
            captionPanel != null && captionText != null &&
            breakingNewsPanel != null && breakingNewsText != null &&
            emergencyAlertPanel != null && emergencyAlertText != null &&
            frames != null && frames.Length >= 4 &&
            frames[0] != null && frames[1] != null &&
            frames[2] != null && frames[3] != null;

        public void Configure(
            CanvasGroup group,
            Image image,
            GameObject captionRoot,
            Text caption,
            GameObject newsRoot,
            Text news,
            GameObject alertRoot,
            Text alert,
            Text hint,
            Sprite[] cinematicFrames)
        {
            screenGroup = group;
            artwork = image;
            captionPanel = captionRoot;
            captionText = caption;
            breakingNewsPanel = newsRoot;
            breakingNewsText = news;
            emergencyAlertPanel = alertRoot;
            emergencyAlertText = alert;
            advanceHint = hint;
            frames = cinematicFrames ?? Array.Empty<Sprite>();
        }

        public void Play(Action onCompleted)
        {
            if (routine != null) return;
            if (!IsConfigured)
            {
                Debug.LogError(
                    "프롤로그 사진 연출에 필요한 이미지 또는 UI 참조가 없습니다.",
                    this);
                onCompleted?.Invoke();
                return;
            }

            completed = onCompleted;
            gameObject.SetActive(true);
            acceptInputAfter = Time.unscaledTime + 0.35f;
            routine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            HideAllText();
            screenGroup.alpha = 0f;
            screenGroup.blocksRaycasts = true;
            screenGroup.interactable = false;
            if (advanceHint != null)
            {
                advanceHint.text = "Space / 좌클릭 : 다음";
                advanceHint.gameObject.SetActive(true);
            }

            SetFrame(0);
            yield return Fade(0f, 1f);
            ShowCaption(
                "뉴스 앵커",
                "시민 여러분께서는 확인되지 않은 소문을 퍼뜨리지 마시고,\n" +
                "가급적 외출을 자제해 주시기 바랍니다.");
            yield return Hold(4.2f);

            HideAllText();
            breakingNewsPanel.SetActive(true);
            string[] headlines =
            {
                "군, 외곽 지역에 임시 집결지 설치",
                "통신 장애 속 비인가 대피 방송 확산",
                "외곽 상점가에서 무장 집단 목격",
                "주민 대피 지원 민간인 단체, 자체 집결지 운영"
            };
            foreach (string headline in headlines)
            {
                breakingNewsText.text = headline;
                yield return Hold(1.8f);
            }

            ShowCaption(
                GameSession.CurrentPlayerName,
                "또 과장된 뉴스겠지. 그래도 문은 잠가 둬야겠네.");
            yield return Hold(3.2f);
            HideAllText();
            yield return Fade(1f, 0f);

            SetFrame(1);
            yield return Fade(0f, 1f);
            ShowEmergencyAlert(
                "긴급재난문자\n\n" +
                "공격성을 보이는 사람과 접촉하지 마십시오.\n" +
                "모든 출입문과 창문을 잠그고\n" +
                "안전한 장소에서 대기하십시오.");
            yield return Hold(5.2f);

            ShowCaption(
                "긴급 방송",
                "현재 도심…… 통제 불가능……\n" +
                "생존자는 가까운…… 대피…….");
            yield return Hold(3.8f);
            ShowCaption(string.Empty, "치직—");
            yield return Hold(1.4f);
            HideAllText();
            yield return Fade(1f, 0f);

            SetFrame(2);
            yield return Fade(0f, 1f);
            ShowCaption(string.Empty, "쾅! 쾅! 쾅!");
            yield return Hold(1.6f);
            ShowCaption(GameSession.CurrentPlayerName, "……뭐야?");
            yield return Hold(2.0f);
            ShowBreakingNews("방송 중단");
            yield return Hold(2.0f);
            ShowEmergencyAlert("휴대전화\n\n통신 연결 안 됨");
            yield return Hold(2.0f);
            ShowCaption(
                GameSession.CurrentPlayerName,
                "어제 그 뉴스가…… 진짜였어.");
            yield return Hold(3.0f);
            HideAllText();
            yield return Fade(1f, 0f, 0.35f);

            SetFrame(3);
            yield return Fade(0f, 1f, 0.35f);
            ShowCaption(GameSession.CurrentPlayerName, "……시간 없어.");
            yield return Hold(2.5f);
            HideAllText();
            yield return Fade(1f, 0f, 0.25f);

            routine = null;
            Action callback = completed;
            completed = null;
            callback?.Invoke();
            gameObject.SetActive(false);
        }

        private void SetFrame(int index)
        {
            artwork.sprite = frames[index];
            artwork.preserveAspect = true;
            artwork.color = Color.white;
        }

        private void ShowCaption(string speaker, string body)
        {
            HideAllText();
            captionPanel.SetActive(true);
            captionText.text = string.IsNullOrWhiteSpace(speaker)
                ? body
                : $"<color=#D6B36A>{speaker}</color>\n{body}";
        }

        private void ShowBreakingNews(string message)
        {
            HideAllText();
            breakingNewsPanel.SetActive(true);
            breakingNewsText.text = message;
        }

        private void ShowEmergencyAlert(string message)
        {
            HideAllText();
            emergencyAlertPanel.SetActive(true);
            emergencyAlertText.text = message;
        }

        private void HideAllText()
        {
            captionPanel?.SetActive(false);
            breakingNewsPanel?.SetActive(false);
            emergencyAlertPanel?.SetActive(false);
        }

        private IEnumerator Hold(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (Time.unscaledTime >= acceptInputAfter && WasAdvancePressed())
                {
                    acceptInputAfter = Time.unscaledTime + 0.15f;
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator Fade(float from, float to, float duration = -1f)
        {
            float actualDuration = duration > 0f ? duration : fadeDuration;
            float elapsed = 0f;
            screenGroup.alpha = from;
            while (elapsed < actualDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                screenGroup.alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / actualDuration));
                yield return null;
            }

            screenGroup.alpha = to;
        }

        private static bool WasAdvancePressed()
        {
            bool pressed = Keyboard.current != null &&
                           (Keyboard.current.spaceKey.wasPressedThisFrame ||
                            Keyboard.current.enterKey.wasPressedThisFrame);
            return pressed ||
                   (Mouse.current != null &&
                    Mouse.current.leftButton.wasPressedThisFrame);
        }
    }
}
