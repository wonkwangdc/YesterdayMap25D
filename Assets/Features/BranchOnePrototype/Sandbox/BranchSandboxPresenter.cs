using UnityEngine;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Diary;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Flow;

namespace YesterdayMap.BranchOne.Sandbox
{
    public sealed class BranchSandboxPresenter : MonoBehaviour
    {
        [SerializeField] private BranchSandboxView view;
        [SerializeField] private bool weightedRandomDecision = true;

        private int openedDiaryDay;
        private string selectionResultText = string.Empty;
        private bool decisionResolutionRequested;
        private BranchEventDefinition pendingEvent;
        private int pendingEventDay;

        public BranchPrototypeSettings Settings { get; private set; }
        public BranchRuntimeState RuntimeState { get; private set; }
        public BranchScoreState ScoreState { get; private set; }
        public BranchDiaryRepository DiaryRepository { get; private set; }
        public BranchEventCatalog EventCatalog { get; private set; }
        public BranchDecisionResolver DecisionResolver { get; private set; }
        public BranchOneFlowController FlowController { get; private set; }
        public int DecisionResolutionCount { get; private set; }
        public int OpenedDiaryDay => openedDiaryDay;
        public BranchEventDefinition PendingEvent => pendingEvent;
        public bool HasPendingEvent => pendingEvent != null;

        private void Awake()
        {
            if (view == null || !view.HasRequiredReferences())
            {
                Debug.LogError(
                    "BranchSandboxPresenter requires a fully configured BranchSandboxView.",
                    this);
                enabled = false;
                return;
            }

            CreateCore();
            SubscribeCoreEvents();
            view.Bind(
                PreviewEvent,
                ChooseAct,
                ChooseDecline,
                AdvanceDay,
                RestartTest,
                ShowPreviousDiary,
                ShowNextDiary);
        }

        private void Start()
        {
            if (enabled)
            {
                RestartTest();
            }
        }

        private void OnDestroy()
        {
            if (FlowController != null)
            {
                FlowController.StateChanged -= HandleStateChanged;
                FlowController.EventSelected -= HandleEventSelected;
                FlowController.DecisionResolved -= HandleDecisionResolved;
            }

            view?.Unbind();
        }

        public void Configure(BranchSandboxView sandboxView, bool useWeightedRandomDecision)
        {
            view = sandboxView;
            weightedRandomDecision = useWeightedRandomDecision;
        }

        public void PreviewEvent(int eventNumber)
        {
            if (RuntimeState.Phase != BranchFlowPhase.WaitingForEvent ||
                RuntimeState.CurrentDay >= Settings.DecisionDay ||
                RuntimeState.IsTodayEventCompleted)
            {
                selectionResultText = "지금은 새 이벤트를 선택할 수 없습니다.";
                RefreshView();
                return;
            }

            if (pendingEvent != null)
            {
                selectionResultText = "이미 선택한 이벤트의 행동을 결정해야 합니다.";
                RefreshView();
                return;
            }

            if (!EventCatalog.TryGetByNumber(
                    eventNumber,
                    out BranchEventDefinition definition) ||
                !definition.IsAvailableOnDay(RuntimeState.CurrentDay))
            {
                selectionResultText = "현재 날짜에 사용할 수 없는 이벤트입니다.";
                RefreshView();
                return;
            }

            pendingEvent = definition;
            pendingEventDay = RuntimeState.CurrentDay;
            selectionResultText = "행동을 할지 결정해 주세요.";
            RefreshView();
        }

        public void ChooseAct()
        {
            SubmitPendingChoice(
                pendingEvent == null
                    ? string.Empty
                    : BranchSandboxEventProvider.CreateActChoiceId(
                        pendingEvent.EventNumber));
        }

        public void ChooseDecline()
        {
            SubmitPendingChoice(
                pendingEvent == null
                    ? string.Empty
                    : BranchSandboxEventProvider.CreateDeclineChoiceId(
                        pendingEvent.EventNumber));
        }

        public void AdvanceDay()
        {
            if (!FlowController.AdvanceDay())
            {
                selectionResultText = $"하루 종료 실패: {FlowController.LastFailureReason}";
                RefreshView();
            }
        }

        public void RestartTest()
        {
            decisionResolutionRequested = false;
            DecisionResolutionCount = 0;
            ClearPendingEvent();
            selectionResultText = "이벤트를 선택해 주세요.";
            openedDiaryDay = Settings.StartDay;
            FlowController.StartTest();
        }

        public void ShowPreviousDiary()
        {
            TryOpenDiary(openedDiaryDay - 1);
        }

        public void ShowNextDiary()
        {
            TryOpenDiary(openedDiaryDay + 1);
        }

        private void CreateCore()
        {
            Settings = BranchSandboxSettings.Create(weightedRandomDecision);
            RuntimeState = new BranchRuntimeState();
            ScoreState = new BranchScoreState();
            DiaryRepository = new BranchDiaryRepository();
            EventCatalog = new BranchSandboxEventProvider().CreateCatalog();
            DecisionResolver = new BranchDecisionResolver();
            FlowController = new BranchOneFlowController(
                EventCatalog,
                Settings,
                RuntimeState,
                ScoreState,
                DiaryRepository,
                DecisionResolver);
        }

        private void SubscribeCoreEvents()
        {
            FlowController.StateChanged += HandleStateChanged;
            FlowController.EventSelected += HandleEventSelected;
            FlowController.DecisionResolved += HandleDecisionResolved;
        }

        private void HandleStateChanged()
        {
            if (RuntimeState.Phase == BranchFlowPhase.WaitingForEvent &&
                pendingEventDay != RuntimeState.CurrentDay)
            {
                ClearPendingEvent();
                selectionResultText = "이벤트를 선택해 주세요.";
            }

            if (RuntimeState.CurrentDay >= Settings.DecisionDay)
            {
                ClearPendingEvent();
            }

            if (RuntimeState.Phase == BranchFlowPhase.ResolvingBranch)
            {
                ResolveDecisionExactlyOnce();
            }

            if (RuntimeState.CurrentDay >= Settings.StartDay &&
                RuntimeState.CurrentDay <= Settings.LastEventDay)
            {
                openedDiaryDay = RuntimeState.CurrentDay;
            }

            RefreshView();
        }

        private void HandleEventSelected(BranchEventSelectionResult result)
        {
            selectionResultText =
                $"{result.Choice.Label}: {result.Choice.ResultText}\n" +
                $"적용 점수: Signal {FormatDelta(result.AppliedSignalScoreDelta)}, " +
                $"Join {FormatDelta(result.AppliedJoinScoreDelta)}";
            RefreshView();
        }

        private void HandleDecisionResolved(BranchDecisionResult result)
        {
            selectionResultText = result.IsSuccess
                ? $"7일 차 계열 판정 완료: {result.DecidedRoute}"
                : $"7일 차 계열 판정 실패: {result.FailureReason}";
            RefreshView();
        }

        private void ResolveDecisionExactlyOnce()
        {
            if (decisionResolutionRequested || FlowController.DecisionResult != null)
            {
                return;
            }

            decisionResolutionRequested = true;
            DecisionResolutionCount++;
            double randomRoll = Random.value;
            FlowController.ResolveDecision(randomRoll);
        }

        private void TryOpenDiary(int day)
        {
            if (!DiaryRepository.TryGetRecord(day, out _))
            {
                selectionResultText = $"{day}일 차 일기 기록은 아직 없습니다.";
                RefreshView();
                return;
            }

            openedDiaryDay = day;
            RefreshView();
        }

        private void RefreshView()
        {
            view.Render(
                FlowController,
                openedDiaryDay,
                pendingEvent,
                selectionResultText);
        }

        private void SubmitPendingChoice(string choiceId)
        {
            if (pendingEvent == null ||
                pendingEventDay != RuntimeState.CurrentDay ||
                RuntimeState.Phase != BranchFlowPhase.WaitingForEvent ||
                RuntimeState.IsTodayEventCompleted)
            {
                selectionResultText = "선택할 수 있는 행동이 없습니다.";
                RefreshView();
                return;
            }

            BranchEventSelectionResult result =
                FlowController.SelectEvent(pendingEvent.EventId, choiceId);
            if (!result.IsSuccess)
            {
                selectionResultText = $"행동 선택 실패: {result.FailureReason}";
                RefreshView();
            }
        }

        private void ClearPendingEvent()
        {
            pendingEvent = null;
            pendingEventDay = 0;
        }

        private static string FormatDelta(int delta)
        {
            return delta >= 0 ? $"+{delta}" : delta.ToString();
        }
    }
}
