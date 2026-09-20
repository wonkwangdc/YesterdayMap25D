using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.BranchOne;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.Core;
using YesterdayMap.Events;

namespace YesterdayMap.UI
{
    /// <summary>
    /// Temporary in-game HUD for visually checking quarter-one route scores.
    /// </summary>
    public sealed class Quarter1ScoreDebugHUD : MonoBehaviour
    {
        [SerializeField] private ShelterEventDialogueController eventDialogue;
        [SerializeField] private DayCycleManager dayCycle;
        [SerializeField] private Text scoreText;
        [SerializeField] private Button skipQuarter1Button;
        private Button skipToQuarter3SignalButton;
        private Button skipToQuarter3JoinButton;
        private Button skipToLastBunkerButton;
        private CanvasGroup adminVisibility;

        private int displayedDay = -1;
        private int displayedSignal = -1;
        private int displayedJoin = -1;
        private string displayedChange = string.Empty;
        private string displayedQuarter2 = string.Empty;

        public void Configure(
            ShelterEventDialogueController dialogue,
            DayCycleManager cycle,
            Text text)
        {
            eventDialogue = dialogue;
            dayCycle = cycle;
            scoreText = text;
            EnsureSkipButton();
            EnsureQuarter3Buttons();
            EnsureLastBunkerButton();
            BindSkipButton();
            BindQuarter3Buttons();
            BindLastBunkerButton();
            RefreshAdminAccess();
            Refresh(true);
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureSkipButton();
            EnsureQuarter3Buttons();
            EnsureLastBunkerButton();
            BindSkipButton();
            BindQuarter3Buttons();
            BindLastBunkerButton();
            RefreshAdminAccess();
            Refresh(true);
        }

        private void OnDestroy()
        {
            if (skipQuarter1Button != null)
            {
                skipQuarter1Button.onClick.RemoveListener(HandleSkipQuarter1);
            }

            if (skipToQuarter3SignalButton != null)
            {
                skipToQuarter3SignalButton.onClick.RemoveListener(
                    HandleSkipToQuarter3Signal);
            }

            if (skipToQuarter3JoinButton != null)
            {
                skipToQuarter3JoinButton.onClick.RemoveListener(
                    HandleSkipToQuarter3Join);
            }

            if (skipToLastBunkerButton != null)
            {
                skipToLastBunkerButton.onClick.RemoveListener(
                    HandleSkipToLastBunker);
            }
        }

        private void Update()
        {
            if (!RefreshAdminAccess())
            {
                return;
            }

            Refresh(false);
        }

        private bool RefreshAdminAccess()
        {
            return AdminDebugVisibility.Apply(
                gameObject,
                ref adminVisibility);
        }

        private void ResolveReferences()
        {
            if (eventDialogue == null)
            {
                eventDialogue = FindFirstObjectByType<ShelterEventDialogueController>(
                    FindObjectsInactive.Include);
            }

            if (dayCycle == null)
            {
                dayCycle = FindFirstObjectByType<DayCycleManager>(
                    FindObjectsInactive.Include);
            }

            if (scoreText == null)
            {
                Transform scoreTransform = transform.Find("ScoreText");
                scoreText = scoreTransform != null
                    ? scoreTransform.GetComponent<Text>()
                    : GetComponentInChildren<Text>(true);
            }
        }

        private void EnsureSkipButton()
        {
            if (skipQuarter1Button == null)
            {
                Transform existing = transform.Find("SkipQuarter1Button");
                if (existing != null)
                {
                    skipQuarter1Button = existing.GetComponent<Button>();
                }
            }

            if (skipQuarter1Button == null)
            {
                GameObject buttonObject = new(
                    "SkipQuarter1Button",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(transform, false);

                Image image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.58f, 0.24f, 0.18f, 0.96f);
                skipQuarter1Button = buttonObject.GetComponent<Button>();
                skipQuarter1Button.targetGraphic = image;

                GameObject labelObject = new(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                Text label = labelObject.GetComponent<Text>();
                label.font = DefaultUIFont.Get();
                label.fontSize = 15;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.text = "1분기 → 2분기";
                label.raycastTarget = false;
            }

            Text existingLabel =
                skipQuarter1Button.GetComponentInChildren<Text>(true);
            if (existingLabel != null)
            {
                existingLabel.text = "1분기 → 2분기";
            }

            if (scoreText != null)
            {
                RectTransform scoreRect = scoreText.rectTransform;
                scoreRect.offsetMin = new Vector2(14f, 52f);
                scoreRect.offsetMax = new Vector2(-14f, -10f);
            }
        }

        private void EnsureQuarter3Buttons()
        {
            skipToQuarter3SignalButton = EnsureRouteButton(
                "SkipToQuarter3SignalButton",
                "3분기 구조신호",
                new Color(0.16f, 0.38f, 0.58f, 0.96f),
                new Vector2(0.365f, 0.04f),
                new Vector2(0.67f, 0.25f));
            skipToQuarter3JoinButton = EnsureRouteButton(
                "SkipToQuarter3JoinButton",
                "3분기 합류",
                new Color(0.25f, 0.48f, 0.28f, 0.96f),
                new Vector2(0.685f, 0.04f),
                new Vector2(0.96f, 0.25f));
        }

        private void EnsureLastBunkerButton()
        {
            skipToLastBunkerButton = EnsureRouteButton(
                "SkipToLastBunkerButton",
                "마지막 벙커 진입",
                new Color(0.38f, 0.29f, 0.18f, 0.96f),
                new Vector2(0.04f, 0.28f),
                new Vector2(0.96f, 0.49f));
        }

        private Button EnsureRouteButton(
            string objectName,
            string labelText,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Transform existing = transform.Find(objectName);
            Button button = existing != null
                ? existing.GetComponent<Button>()
                : null;
            if (button == null)
            {
                GameObject buttonObject = new(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(transform, false);

                Image image = buttonObject.GetComponent<Image>();
                image.color = color;
                button = buttonObject.GetComponent<Button>();
                button.targetGraphic = image;

                GameObject labelObject = new(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect =
                    labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                Text label = labelObject.GetComponent<Text>();
                label.font = DefaultUIFont.Get();
                label.fontSize = 13;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.text = labelText;
                label.raycastTarget = false;
            }

            Text existingLabel = button.GetComponentInChildren<Text>(true);
            if (existingLabel != null)
            {
                existingLabel.text = labelText;
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            return button;
        }

        private void LayoutButtons(bool showQuarter1)
        {
            if (skipQuarter1Button != null)
            {
                SetButtonAnchors(
                    skipQuarter1Button,
                    new Vector2(0.04f, 0.04f),
                    new Vector2(0.35f, 0.20f));
            }

            SetButtonAnchors(
                skipToQuarter3SignalButton,
                showQuarter1
                    ? new Vector2(0.365f, 0.04f)
                    : new Vector2(0.06f, 0.04f),
                showQuarter1
                    ? new Vector2(0.67f, 0.20f)
                    : new Vector2(0.49f, 0.25f));
            SetButtonAnchors(
                skipToQuarter3JoinButton,
                showQuarter1
                    ? new Vector2(0.685f, 0.04f)
                    : new Vector2(0.34f, 0.04f),
                showQuarter1
                    ? new Vector2(0.96f, 0.20f)
                    : new Vector2(0.62f, 0.25f));

            if (!showQuarter1)
            {
                SetButtonAnchors(
                    skipToQuarter3SignalButton,
                    new Vector2(0.04f, 0.04f),
                    new Vector2(0.32f, 0.25f));
            }

            SetButtonAnchors(
                skipToLastBunkerButton,
                showQuarter1
                    ? new Vector2(0.04f, 0.24f)
                    : new Vector2(0.64f, 0.04f),
                showQuarter1
                    ? new Vector2(0.96f, 0.40f)
                    : new Vector2(0.96f, 0.25f));
        }

        private static void SetButtonAnchors(
            Button button,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            if (button == null)
            {
                return;
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
        }

        private void BindSkipButton()
        {
            if (skipQuarter1Button == null)
            {
                return;
            }

            skipQuarter1Button.onClick.RemoveListener(HandleSkipQuarter1);
            skipQuarter1Button.onClick.AddListener(HandleSkipQuarter1);
        }

        private void BindQuarter3Buttons()
        {
            if (skipToQuarter3SignalButton != null)
            {
                skipToQuarter3SignalButton.onClick.RemoveListener(
                    HandleSkipToQuarter3Signal);
                skipToQuarter3SignalButton.onClick.AddListener(
                    HandleSkipToQuarter3Signal);
            }

            if (skipToQuarter3JoinButton != null)
            {
                skipToQuarter3JoinButton.onClick.RemoveListener(
                    HandleSkipToQuarter3Join);
                skipToQuarter3JoinButton.onClick.AddListener(
                    HandleSkipToQuarter3Join);
            }
        }

        private void BindLastBunkerButton()
        {
            if (skipToLastBunkerButton == null)
            {
                return;
            }

            skipToLastBunkerButton.onClick.RemoveListener(
                HandleSkipToLastBunker);
            skipToLastBunkerButton.onClick.AddListener(
                HandleSkipToLastBunker);
        }

        private void HandleSkipQuarter1()
        {
            if (!GameSession.HasAdminAccess) return;

            ResolveReferences();
            eventDialogue?.SkipQuarter1ForTesting();
            Refresh(true);
        }

        private void HandleSkipToQuarter3Signal()
        {
            if (!GameSession.HasAdminAccess) return;

            ResolveReferences();
            eventDialogue?.SkipToQuarter3ForTesting(BranchRoute.Signal);
            Refresh(true);
        }

        private void HandleSkipToQuarter3Join()
        {
            if (!GameSession.HasAdminAccess) return;

            ResolveReferences();
            eventDialogue?.SkipToQuarter3ForTesting(BranchRoute.Join);
            Refresh(true);
        }

        private void HandleSkipToLastBunker()
        {
            if (!GameSession.HasAdminAccess) return;

            ResolveReferences();
            eventDialogue?.SkipToLastBunkerForTesting();
            Refresh(true);
        }

        private void Refresh(bool force)
        {
            if (scoreText == null)
            {
                return;
            }

            int day = dayCycle != null ? dayCycle.CurrentDay : 1;
            int signal = eventDialogue != null
                ? eventDialogue.Quarter1SignalScore
                : 0;
            int join = eventDialogue != null
                ? eventDialogue.Quarter1JoinScore
                : 0;
            string change = eventDialogue != null
                ? eventDialogue.LastQuarter1ScoreChange
                : "이벤트 시스템 연결 대기";
            string quarter2Text = BuildQuarter2Text(day);

            if (!force &&
                day == displayedDay &&
                signal == displayedSignal &&
                join == displayedJoin &&
                change == displayedChange &&
                quarter2Text == displayedQuarter2)
            {
                return;
            }

            displayedDay = day;
            displayedSignal = signal;
            displayedJoin = join;
            displayedChange = change;
            displayedQuarter2 = quarter2Text;

            bool showQuarter1 =
                eventDialogue == null ||
                eventDialogue.CampaignPhase == EndingRouteCampaignPhase.NotStarted ||
                eventDialogue.CampaignPhase == EndingRouteCampaignPhase.Quarter1Running;
            scoreText.text = showQuarter1
                ? "[테스트] 1분기 계열 점수\n" +
                  $"{day}일차\n" +
                  $"구조신호  {signal}점\n" +
                  $"합류      {join}점\n" +
                  change
                : quarter2Text;

            if (skipQuarter1Button != null)
            {
                skipQuarter1Button.gameObject.SetActive(showQuarter1);
                skipQuarter1Button.interactable =
                    eventDialogue != null &&
                    eventDialogue.CanSkipQuarter1ForTesting;
            }

            LayoutButtons(showQuarter1);

            if (skipToQuarter3SignalButton != null)
            {
                skipToQuarter3SignalButton.interactable =
                    eventDialogue != null;
            }

            if (skipToQuarter3JoinButton != null)
            {
                skipToQuarter3JoinButton.interactable =
                    eventDialogue != null;
            }

            if (skipToLastBunkerButton != null)
            {
                skipToLastBunkerButton.interactable =
                    eventDialogue != null;
            }

            RectTransform scoreRect = scoreText.rectTransform;
            scoreRect.anchorMin = new Vector2(
                0f,
                showQuarter1 ? 0.46f : 0.29f);
            scoreRect.anchorMax = Vector2.one;
            scoreRect.offsetMin = new Vector2(14f, 6f);
            scoreRect.offsetMax = new Vector2(-14f, -10f);
        }

        private string BuildQuarter2Text(int day)
        {
            if (eventDialogue == null)
            {
                return "[테스트] 2분기 진행 결과\n이벤트 시스템 연결 대기";
            }

            string route = eventDialogue.Quarter2CurrentRoute switch
            {
                BranchRoute.Signal => "구조신호",
                BranchRoute.Join => "합류",
                _ => "결정 전"
            };
            int eventOrder = eventDialogue.Quarter2CurrentEventOrder;
            string eventPosition = eventOrder > 0
                ? $"{eventOrder}/3"
                : "-";

            return
                "[테스트] 2분기 진행 결과\n" +
                $"{day}일차 · 현재 {route} {eventPosition}\n" +
                $"구조신호  진행 {eventDialogue.Quarter2SignalProgressCount} / 거부 {eventDialogue.Quarter2SignalRejectCount}\n" +
                $"합류      진행 {eventDialogue.Quarter2JoinProgressCount} / 거부 {eventDialogue.Quarter2JoinRejectCount}\n" +
                eventDialogue.LastQuarter2DecisionResult;
        }
    }
}
