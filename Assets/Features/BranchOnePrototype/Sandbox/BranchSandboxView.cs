using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Diary;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Flow;

namespace YesterdayMap.BranchOne.Sandbox
{
    public sealed class BranchSandboxView : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text scoreText;

        [Header("Event Detail")]
        [SerializeField] private Text selectedEventTitleText;
        [SerializeField] private Text selectedEventBodyText;
        [SerializeField] private Text selectionResultText;

        [Header("Diary")]
        [SerializeField] private Text diaryDayText;
        [SerializeField] private Text mainDiaryText;
        [SerializeField] private Text eventDiaryText;
        [SerializeField] private Text explorationDiaryText;

        [Header("Decision")]
        [SerializeField] private GameObject decisionPanel;
        [SerializeField] private Text decisionDetailsText;

        [Header("Buttons")]
        [SerializeField] private Button[] eventButtons = Array.Empty<Button>();
        [SerializeField] private Button actButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Button advanceDayButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button previousDiaryButton;
        [SerializeField] private Button nextDiaryButton;

        private Font runtimeKoreanFont;
        private bool isBound;

        public bool KoreanFontReady { get; private set; }
        public string KoreanFontName =>
            runtimeKoreanFont != null ? runtimeKoreanFont.name : string.Empty;

        private void Awake()
        {
            ApplyKoreanFont();
        }

        private void OnDestroy()
        {
            Unbind();
            if (runtimeKoreanFont != null)
            {
                Destroy(runtimeKoreanFont);
            }
        }

        public void Configure(
            Text status,
            Text score,
            Text selectedEventTitle,
            Text selectedEventBody,
            Text selectionResult,
            Text diaryDay,
            Text mainDiary,
            Text eventDiary,
            Text explorationDiary,
            GameObject decision,
            Text decisionDetails,
            Button[] eventSelectionButtons,
            Button act,
            Button decline,
            Button advance,
            Button reset,
            Button previousDiary,
            Button nextDiary)
        {
            statusText = status;
            scoreText = score;
            selectedEventTitleText = selectedEventTitle;
            selectedEventBodyText = selectedEventBody;
            selectionResultText = selectionResult;
            diaryDayText = diaryDay;
            mainDiaryText = mainDiary;
            eventDiaryText = eventDiary;
            explorationDiaryText = explorationDiary;
            decisionPanel = decision;
            decisionDetailsText = decisionDetails;
            eventButtons = eventSelectionButtons ?? Array.Empty<Button>();
            actButton = act;
            declineButton = decline;
            advanceDayButton = advance;
            resetButton = reset;
            previousDiaryButton = previousDiary;
            nextDiaryButton = nextDiary;
        }

        public bool HasRequiredReferences()
        {
            return statusText != null &&
                   scoreText != null &&
                   selectedEventTitleText != null &&
                   selectedEventBodyText != null &&
                   selectionResultText != null &&
                   diaryDayText != null &&
                   mainDiaryText != null &&
                   eventDiaryText != null &&
                   explorationDiaryText != null &&
                   decisionPanel != null &&
                   decisionDetailsText != null &&
                   eventButtons != null &&
                   eventButtons.Length == 10 &&
                   eventButtons.All(button => button != null) &&
                   actButton != null &&
                   declineButton != null &&
                   advanceDayButton != null &&
                   resetButton != null &&
                   previousDiaryButton != null &&
                   nextDiaryButton != null;
        }

        public void Bind(
            Action<int> onEventPreviewed,
            Action onAct,
            Action onDecline,
            Action onAdvanceDay,
            Action onReset,
            Action onPreviousDiary,
            Action onNextDiary)
        {
            if (onEventPreviewed == null ||
                onAct == null ||
                onDecline == null ||
                onAdvanceDay == null ||
                onReset == null ||
                onPreviousDiary == null ||
                onNextDiary == null)
            {
                throw new ArgumentNullException("Sandbox UI callbacks cannot be null.");
            }

            Unbind();

            for (int index = 0; index < eventButtons.Length; index++)
            {
                int eventNumber = index + 1;
                eventButtons[index].onClick.AddListener(
                    () => onEventPreviewed(eventNumber));
            }

            actButton.onClick.AddListener(() => onAct());
            declineButton.onClick.AddListener(() => onDecline());
            advanceDayButton.onClick.AddListener(() => onAdvanceDay());
            resetButton.onClick.AddListener(() => onReset());
            previousDiaryButton.onClick.AddListener(() => onPreviousDiary());
            nextDiaryButton.onClick.AddListener(() => onNextDiary());
            isBound = true;
        }

        public void Unbind()
        {
            if (!isBound)
            {
                return;
            }

            foreach (Button button in eventButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }

            actButton?.onClick.RemoveAllListeners();
            declineButton?.onClick.RemoveAllListeners();
            advanceDayButton?.onClick.RemoveAllListeners();
            resetButton?.onClick.RemoveAllListeners();
            previousDiaryButton?.onClick.RemoveAllListeners();
            nextDiaryButton?.onClick.RemoveAllListeners();
            isBound = false;
        }

        public void ApplyKoreanFont()
        {
            if (runtimeKoreanFont == null)
            {
                runtimeKoreanFont = ResolveProjectDefaultFont();
            }

            KoreanFontReady =
                runtimeKoreanFont != null && runtimeKoreanFont.HasCharacter('한');
            if (runtimeKoreanFont == null)
            {
                return;
            }

            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                text.font = runtimeKoreanFont;
            }
        }

        private static Font ResolveProjectDefaultFont()
        {
            ScriptableObject settings =
                UnityEngine.Resources.Load<ScriptableObject>("UI/DefaultUIFontSettings");
            return settings?.GetType().GetProperty("DefaultFont")
                ?.GetValue(settings) as Font;
        }

        public void Render(
            BranchOneFlowController flow,
            int openedDiaryDay,
            BranchEventDefinition pendingEvent,
            string selectionResult)
        {
            if (flow == null)
            {
                throw new ArgumentNullException(nameof(flow));
            }

            BranchRuntimeState runtime = flow.RuntimeState;
            statusText.text =
                $"현재 날짜: {runtime.CurrentDay}일 차\n" +
                $"현재 단계: {runtime.Phase}\n" +
                $"오늘 선택 이벤트: {FormatSelectedEvent(runtime.TodaySelectedEventId)}\n" +
                $"오늘 이벤트 완료: {(runtime.IsTodayEventCompleted ? "예" : "아니오")}";

            scoreText.text =
                $"구조 신호 점수: {flow.ScoreState.SignalScore}\n" +
                $"합류 계열 점수: {flow.ScoreState.JoinScore}";

            selectedEventTitleText.text = pendingEvent == null
                ? "선택된 이벤트: 아직 선택하지 않았습니다."
                : $"선택된 이벤트: {pendingEvent.Title}";
            selectedEventBodyText.text = pendingEvent == null
                ? "이벤트 버튼을 누르면 제목과 본문이 여기에 표시됩니다."
                : pendingEvent.Body;
            selectionResultText.text =
                $"선택 결과: {(string.IsNullOrWhiteSpace(selectionResult) ? "아직 선택하지 않았습니다." : selectionResult)}";

            RenderButtons(flow, openedDiaryDay, pendingEvent);
            RenderDiary(flow.DiaryRepository, openedDiaryDay);
            RenderDecision(flow.DecisionResult, runtime.IsDecisionCompleted);
        }

        private void RenderButtons(
            BranchOneFlowController flow,
            int openedDiaryDay,
            BranchEventDefinition pendingEvent)
        {
            BranchRuntimeState runtime = flow.RuntimeState;
            bool waitingForEvent =
                runtime.Phase == BranchFlowPhase.WaitingForEvent &&
                runtime.CurrentDay < flow.Settings.DecisionDay;
            HashSet<int> availableNumbers = new(
                flow.GetAvailableEventsForCurrentDay()
                    .Select(definition => definition.EventNumber));

            for (int index = 0; index < eventButtons.Length; index++)
            {
                eventButtons[index].interactable =
                    waitingForEvent &&
                    pendingEvent == null &&
                    availableNumbers.Contains(index + 1);
            }

            bool waitingForAction =
                waitingForEvent &&
                pendingEvent != null &&
                !runtime.IsTodayEventCompleted;
            actButton.interactable = waitingForAction;
            declineButton.interactable = waitingForAction;
            advanceDayButton.interactable =
                runtime.Phase == BranchFlowPhase.ReadyToAdvance &&
                runtime.CurrentDay < flow.Settings.DecisionDay;

            IReadOnlyList<DiaryDayRecord> records = flow.DiaryRepository.GetRecords();
            int minimumDay = records.Count > 0 ? records[0].Day : flow.Settings.StartDay;
            int maximumDay = records.Count > 0
                ? records[records.Count - 1].Day
                : flow.Settings.StartDay;
            previousDiaryButton.interactable = openedDiaryDay > minimumDay;
            nextDiaryButton.interactable = openedDiaryDay < maximumDay;
            resetButton.interactable = true;
        }

        private void RenderDiary(BranchDiaryRepository repository, int openedDiaryDay)
        {
            diaryDayText.text = $"현재 열어본 일기: {openedDiaryDay}일 차";

            if (!repository.TryGetRecord(openedDiaryDay, out DiaryDayRecord record))
            {
                mainDiaryText.text = "메인 일기\n(해당 날짜 기록 없음)";
                eventDiaryText.text = "이벤트 일기\n(해당 날짜 기록 없음)";
                explorationDiaryText.text = "탐사 기록\n(해당 날짜 기록 없음)";
                return;
            }

            mainDiaryText.text =
                $"메인 일기\n{FormatDiarySection(record.MainDiary)}";
            eventDiaryText.text =
                $"이벤트 일기\n{FormatDiarySection(record.EventRecords)}";
            explorationDiaryText.text =
                $"탐사 기록\n{FormatDiarySection(record.ExplorationRecords)}";
        }

        private void RenderDecision(BranchDecisionResult result, bool isDecisionCompleted)
        {
            bool shouldShow = isDecisionCompleted && result != null;
            decisionPanel.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            decisionDetailsText.text =
                $"최종 Signal 점수: {result.FinalSignalScore}\n" +
                $"최종 Join 점수: {result.FinalJoinScore}\n" +
                $"Signal 비율: {result.SignalRatio:P1}\n" +
                $"Join 비율: {result.JoinRatio:P1}\n" +
                $"사용한 randomRoll: {result.RandomRoll:F3}\n" +
                $"최종 BranchRoute: {result.DecidedRoute}\n" +
                $"판정 성공 여부: {(result.IsSuccess ? "성공" : "실패")}\n" +
                $"실패 사유: {(string.IsNullOrWhiteSpace(result.FailureReason) ? "없음" : result.FailureReason)}";
        }

        private static string FormatSelectedEvent(string eventId)
        {
            return string.IsNullOrWhiteSpace(eventId) ? "없음" : eventId;
        }

        private static string FormatDiarySection(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "(기록 없음)" : text;
        }

        private static string FormatDiarySection(IReadOnlyList<string> entries)
        {
            return entries == null || entries.Count == 0
                ? "(기록 없음)"
                : string.Join("\n", entries);
        }
    }
}
