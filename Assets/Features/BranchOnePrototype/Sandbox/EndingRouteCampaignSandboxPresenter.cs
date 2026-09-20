using System;
using System.Globalization;
using UnityEngine;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Diary;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Flow;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.BranchOne.Sandbox
{
    public sealed class EndingRouteCampaignSandboxPresenter : MonoBehaviour
    {
        private const double MaximumRandomRoll = 0.999999d;

        [SerializeField] private EndingRouteCampaignSandboxView view;
        [SerializeField] private bool useFixedRandomRoll;
        [SerializeField] private double fixedRandomRoll;

        private BranchEventDefinition pendingQuarter1Event;
        private int pendingQuarter1EventDay;
        private string quarter1ResultText = string.Empty;
        private string quarter2ResultText = string.Empty;
        private string quarter3ResultText = string.Empty;
        private string randomValidationText = string.Empty;
        private bool quarter1DecisionRequested;
        private int testCannedFoodCount = 2;
        private int testWaterCount = 2;
        private int currentJoinSupportDay = 1;
        private Quarter3JoinSupportTarget selectedSupportTarget =
            Quarter3JoinSupportTarget.SurvivorGroup;
        private Quarter3JoinSupportResource selectedSupportResource =
            Quarter3JoinSupportResource.CannedFood;
        private string recentSupportResult = string.Empty;
        private string recentSupportClue = string.Empty;

        public BranchPrototypeSettings Settings { get; private set; }
        public BranchOneFlowController Quarter1Controller { get; private set; }
        public Quarter2FlowController Quarter2Controller { get; private set; }
        public Quarter3FlowController Quarter3Controller { get; private set; }
        public EndingRouteCampaignController CampaignController { get; private set; }
        public BranchEventDefinition PendingQuarter1Event =>
            pendingQuarter1Event;
        public bool UseFixedRandomRoll => useFixedRandomRoll;
        public double FixedRandomRoll => fixedRandomRoll;
        public int Quarter1DecisionResolutionCount { get; private set; }

        private void Awake()
        {
            if (view == null || !view.HasRequiredReferences())
            {
                Debug.LogError(
                    "EndingRouteCampaignSandboxPresenter requires a fully configured view.",
                    this);
                enabled = false;
                return;
            }

            fixedRandomRoll = NormalizeRandomRoll(fixedRandomRoll);
            CreateCore();
            CampaignController.StateChanged += HandleCampaignStateChanged;
            view.Bind(
                PreviewQuarter1Event,
                ChooseQuarter1Act,
                ChooseQuarter1Decline,
                AdvanceQuarter1Day,
                StartQuarter2,
                ChooseQuarter2Progress,
                ChooseQuarter2Reject,
                AdvanceQuarter2Step,
                StartQuarter3,
                AcquireQuarter3SourceAClue,
                AcquireQuarter3SourceBClue,
                OpenQuarter3FinalChoice,
                ChooseQuarter3FinalOptionA,
                ChooseQuarter3FinalOptionB,
                SelectSurvivorSupportTarget,
                SelectRedArmbandSupportTarget,
                SelectCannedFoodSupportResource,
                SelectWaterSupportResource,
                AddTestCannedFood,
                RemoveTestCannedFood,
                ZeroTestCannedFood,
                AddTestWater,
                RemoveTestWater,
                ZeroTestWater,
                ZeroAllTestResources,
                AttemptCurrentJoinSupport,
                AdvanceJoinSupportDay,
                RestartCampaign,
                SetUseFixedRandomRoll,
                SetFixedRandomRollText);
        }

        private void Start()
        {
            if (enabled)
            {
                RestartCampaign();
            }
        }

        private void OnDestroy()
        {
            if (CampaignController != null)
            {
                CampaignController.StateChanged -= HandleCampaignStateChanged;
            }

            view?.Unbind();
        }

        public void Configure(EndingRouteCampaignSandboxView sandboxView)
        {
            view = sandboxView;
        }

        public void PreviewQuarter1Event(int eventNumber)
        {
            BranchRuntimeState runtime = Quarter1Controller.RuntimeState;
            if (CampaignController.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter1Running ||
                runtime.Phase != BranchFlowPhase.WaitingForEvent ||
                runtime.CurrentDay >= Settings.DecisionDay ||
                runtime.IsTodayEventCompleted)
            {
                quarter1ResultText = "지금은 새 1분기 이벤트를 선택할 수 없습니다.";
                RefreshView();
                return;
            }

            if (pendingQuarter1Event != null)
            {
                quarter1ResultText =
                    "이미 선택한 이벤트의 행동을 먼저 결정해야 합니다.";
                RefreshView();
                return;
            }

            if (!Quarter1Controller.EventCatalog.TryGetByNumber(
                    eventNumber,
                    out BranchEventDefinition definition) ||
                !definition.IsAvailableOnDay(runtime.CurrentDay))
            {
                quarter1ResultText =
                    "현재 날짜에 사용할 수 없는 이벤트입니다.";
                RefreshView();
                return;
            }

            pendingQuarter1Event = definition;
            pendingQuarter1EventDay = runtime.CurrentDay;
            quarter1ResultText = "행동 여부를 선택해 주세요.";
            RefreshView();
        }

        public void ChooseQuarter1Act()
        {
            SubmitQuarter1Choice(
                pendingQuarter1Event == null
                    ? string.Empty
                    : BranchSandboxEventProvider.CreateActChoiceId(
                        pendingQuarter1Event.EventNumber));
        }

        public void ChooseQuarter1Decline()
        {
            SubmitQuarter1Choice(
                pendingQuarter1Event == null
                    ? string.Empty
                    : BranchSandboxEventProvider.CreateDeclineChoiceId(
                        pendingQuarter1Event.EventNumber));
        }

        public void AdvanceQuarter1Day()
        {
            if (!Quarter1Controller.AdvanceDay())
            {
                quarter1ResultText =
                    $"하루 종료 실패: {Quarter1Controller.LastFailureReason}";
                RefreshView();
                return;
            }

            ClearPendingQuarter1Event();
            quarter1ResultText =
                Quarter1Controller.RuntimeState.Phase ==
                BranchFlowPhase.ResolvingBranch
                    ? "7일 차 계열 판정을 진행합니다."
                    : "다음 날짜가 시작되었습니다.";
            ResolveQuarter1DecisionIfReady();
            RefreshView();
        }

        public void StartQuarter2()
        {
            EndingRouteCampaignTransitionResult result =
                CampaignController.AdvanceToQuarter2();
            quarter2ResultText = result.IsSuccess
                ? "2분기를 시작했습니다."
                : $"2분기 시작 실패: {result.FailureReason}";
            RefreshView();
        }

        public void ChooseQuarter2Progress()
        {
            SubmitQuarter2Decision(Quarter2Decision.Progress);
        }

        public void ChooseQuarter2Reject()
        {
            SubmitQuarter2Decision(Quarter2Decision.Reject);
        }

        public void AdvanceQuarter2Step()
        {
            EndingRouteCampaignTransitionResult result =
                CampaignController.AdvanceQuarter2Step();
            quarter2ResultText = result.IsSuccess
                ? CampaignController.Quarter2Phase ==
                  Quarter2FlowPhase.WaitingForChoice
                    ? "다음 2분기 이벤트를 준비했습니다."
                    : result.Message
                : $"2분기 진행 실패: {result.FailureReason}";
            RefreshView();
        }

        public void StartQuarter3()
        {
            EndingRouteCampaignTransitionResult result =
                CampaignController.AdvanceToQuarter3();
            quarter3ResultText = result.IsSuccess
                ? "3분기를 시작했습니다."
                : $"3분기 시작 실패: {result.FailureReason}";
            RefreshView();
        }

        public void AcquireQuarter3SourceAClue()
        {
            AcquireQuarter3ClueFromSource(
                Quarter3SandboxDataProvider.SourceA);
        }

        public void AcquireQuarter3SourceBClue()
        {
            AcquireQuarter3ClueFromSource(
                Quarter3SandboxDataProvider.SourceB);
        }

        public void OpenQuarter3FinalChoice()
        {
            EndingRouteCampaignTransitionResult result =
                CampaignController.OpenQuarter3FinalChoice();
            quarter3ResultText = result.IsSuccess
                ? "최종 선택을 개방했습니다."
                : $"최종 선택 개방 실패: {result.FailureReason}";
            RefreshView();
        }

        public void ChooseQuarter3FinalOptionA()
        {
            SelectQuarter3FinalChoice(0);
        }

        public void ChooseQuarter3FinalOptionB()
        {
            SelectQuarter3FinalChoice(1);
        }

        public void SelectSurvivorSupportTarget()
        { selectedSupportTarget = Quarter3JoinSupportTarget.SurvivorGroup; RefreshView(); }
        public void SelectRedArmbandSupportTarget()
        { selectedSupportTarget = Quarter3JoinSupportTarget.RedArmband; RefreshView(); }
        public void SelectCannedFoodSupportResource()
        { selectedSupportResource = Quarter3JoinSupportResource.CannedFood; RefreshView(); }
        public void SelectWaterSupportResource()
        { selectedSupportResource = Quarter3JoinSupportResource.Water; RefreshView(); }
        public void AddTestCannedFood() { testCannedFoodCount++; RefreshView(); }
        public void RemoveTestCannedFood()
        { testCannedFoodCount = Math.Max(0, testCannedFoodCount - 1); RefreshView(); }
        public void ZeroTestCannedFood() { testCannedFoodCount = 0; RefreshView(); }
        public void AddTestWater() { testWaterCount++; RefreshView(); }
        public void RemoveTestWater()
        { testWaterCount = Math.Max(0, testWaterCount - 1); RefreshView(); }
        public void ZeroTestWater() { testWaterCount = 0; RefreshView(); }
        public void ZeroAllTestResources()
        { testCannedFoodCount = 0; testWaterCount = 0; RefreshView(); }

        public void AttemptCurrentJoinSupport()
        {
            Quarter3JoinSupportAttemptResult result =
                CampaignController.AttemptQuarter3JoinSupport(
                    currentJoinSupportDay, selectedSupportTarget,
                    selectedSupportResource,
                    new Quarter3JoinSupportInventorySnapshot(
                        testCannedFoodCount, testWaterCount));
            recentSupportResult = result.Outcome.ToString();
            recentSupportClue = result.AcquiredClueId;
            if (result.IsSuccess && result.ConsumeAmount > 0 &&
                result.ConsumeResource.HasValue)
            {
                if (result.ConsumeResource.Value ==
                    Quarter3JoinSupportResource.CannedFood)
                    testCannedFoodCount = Math.Max(
                        0, testCannedFoodCount - result.ConsumeAmount);
                else
                    testWaterCount = Math.Max(
                        0, testWaterCount - result.ConsumeAmount);
            }
            quarter3ResultText = result.IsSuccess
                ? $"지원 성공: {result.AcquiredClueId}"
                : $"지원 결과: {result.Outcome} {result.FailureReason}";
            RefreshView();
        }

        public void AdvanceJoinSupportDay()
        {
            if (currentJoinSupportDay > 4) return;
            Quarter3JoinSupportDayStatus status =
                CampaignController.GetQuarter3JoinSupportDayStatus(
                    currentJoinSupportDay);
            if (status == Quarter3JoinSupportDayStatus.Open)
            {
                recentSupportResult = "현재 일차를 먼저 처리해야 합니다.";
            }
            else
            {
                currentJoinSupportDay++;
                recentSupportResult = currentJoinSupportDay > 4
                    ? "지원 4일차가 종료되었습니다."
                    : $"{currentJoinSupportDay}일차 지원을 시작합니다.";
                recentSupportClue = string.Empty;
            }
            RefreshView();
        }

        public void RestartCampaign()
        {
            ClearPendingQuarter1Event();
            quarter1DecisionRequested = false;
            Quarter1DecisionResolutionCount = 0;
            quarter1ResultText = "이벤트를 선택해 주세요.";
            quarter2ResultText = "2분기 시작 전입니다.";
            quarter3ResultText = "3분기 시작 전입니다.";
            randomValidationText = string.Empty;
            testCannedFoodCount = 2;
            testWaterCount = 2;
            currentJoinSupportDay = 1;
            selectedSupportTarget = Quarter3JoinSupportTarget.SurvivorGroup;
            selectedSupportResource = Quarter3JoinSupportResource.CannedFood;
            recentSupportResult = string.Empty;
            recentSupportClue = string.Empty;

            CampaignController.ResetCampaign();
            EndingRouteCampaignTransitionResult result =
                CampaignController.StartQuarter1();
            if (!result.IsSuccess)
            {
                quarter1ResultText =
                    $"캠페인 시작 실패: {result.FailureReason}";
            }

            RefreshView();
        }

        public void SetUseFixedRandomRoll(bool enabled)
        {
            useFixedRandomRoll = enabled;
            randomValidationText = enabled
                ? $"고정 randomRoll {fixedRandomRoll:0.######} 사용"
                : "UnityEngine.Random.value를 외부 randomRoll로 사용";
            RefreshView();
        }

        public void SetFixedRandomRollText(string text)
        {
            if (!TryParseRandomRoll(text, out double parsed))
            {
                randomValidationText =
                    "randomRoll 입력 오류: 0 이상 1 미만의 숫자를 입력하세요.";
                RefreshView();
                return;
            }

            double normalized = NormalizeRandomRoll(parsed);
            fixedRandomRoll = normalized;
            randomValidationText = Math.Abs(normalized - parsed) > double.Epsilon
                ? $"범위를 보정했습니다: {normalized:0.######}"
                : $"고정 randomRoll 설정: {normalized:0.######}";
            RefreshView();
        }

        private void CreateCore()
        {
            Settings = BranchSandboxSettings.Create(true);
            BranchEventCatalog quarter1Catalog =
                new BranchSandboxEventProvider().CreateCatalog();
            Quarter1Controller = new BranchOneFlowController(
                quarter1Catalog,
                Settings,
                new BranchRuntimeState(),
                new BranchScoreState(),
                new BranchDiaryRepository(),
                new BranchDecisionResolver());
            Quarter2Controller = new Quarter2FlowController(
                new Quarter2SandboxEventProvider().CreateCatalog());
            Quarter3SandboxDataProvider quarter3Provider = new();
            Quarter3Controller = new Quarter3FlowController(
                quarter3Provider.CreateCampaignClueCatalog(),
                quarter3Provider.CreateFinalChoiceCatalog());
            Quarter3JoinSupportController supportController = new(
                quarter3Provider.CreateJoinSupportCatalog());
            CampaignController = new EndingRouteCampaignController(
                Quarter1Controller,
                Quarter2Controller,
                Quarter3Controller,
                quarter3JoinSupportController: supportController);
        }

        private void SubmitQuarter1Choice(string choiceId)
        {
            BranchRuntimeState runtime = Quarter1Controller.RuntimeState;
            if (pendingQuarter1Event == null ||
                pendingQuarter1EventDay != runtime.CurrentDay ||
                CampaignController.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter1Running ||
                runtime.Phase != BranchFlowPhase.WaitingForEvent ||
                runtime.IsTodayEventCompleted)
            {
                quarter1ResultText = "선택할 수 있는 1분기 행동이 없습니다.";
                RefreshView();
                return;
            }

            BranchEventSelectionResult result =
                Quarter1Controller.SelectEvent(
                    pendingQuarter1Event.EventId,
                    choiceId);
            quarter1ResultText = result.IsSuccess
                ? $"{result.Choice.Label}: {result.Choice.ResultText}\n" +
                  $"Signal {FormatDelta(result.AppliedSignalScoreDelta)}, " +
                  $"Join {FormatDelta(result.AppliedJoinScoreDelta)}"
                : $"행동 선택 실패: {result.FailureReason}";
            RefreshView();
        }

        private void SubmitQuarter2Decision(Quarter2Decision decision)
        {
            Quarter2SelectionResult result =
                CampaignController.SelectQuarter2Decision(decision);
            quarter2ResultText = result.IsSuccess
                ? $"{result.EventDefinition.Title}: {FormatDecision(decision)}\n" +
                  $"Progress {result.ProgressCount}, Reject {result.RejectCount}, " +
                  $"Phase {result.Phase}"
                : $"2분기 선택 실패: {result.FailureReason}";
            RefreshView();
        }

        private void AcquireQuarter3ClueFromSource(string sourceId)
        {
            var availableClues =
                CampaignController.GetQuarter3AvailableClues(sourceId);
            if (availableClues.Count == 0)
            {
                quarter3ResultText =
                    $"{sourceId}: 획득 가능한 단서가 없습니다.";
                RefreshView();
                return;
            }

            Quarter3ClueAcquisitionResult result =
                CampaignController.AcquireQuarter3Clue(
                    availableClues[0].ClueId);
            quarter3ResultText = result.IsSuccess
                ? $"단서 획득: {result.AcquiredClueId}"
                : $"단서 획득 실패: {result.FailureReason}";
            RefreshView();
        }

        private void SelectQuarter3FinalChoice(int optionIndex)
        {
            var options =
                CampaignController.GetQuarter3FinalChoiceOptions();
            if (optionIndex < 0 || optionIndex >= options.Count)
            {
                quarter3ResultText =
                    "선택할 수 있는 3분기 최종 방향이 없습니다.";
                RefreshView();
                return;
            }

            Quarter3FinalSelectionResult result =
                CampaignController.SelectQuarter3FinalChoice(
                    options[optionIndex].FinalChoiceId);
            quarter3ResultText = result.IsSuccess
                ? $"최종 방향 선택: {result.SelectedFinalChoiceId}"
                : $"최종 방향 선택 실패: {result.FailureReason}";
            RefreshView();
        }

        private void ResolveQuarter1DecisionIfReady()
        {
            if (quarter1DecisionRequested ||
                Quarter1Controller.DecisionResult != null ||
                Quarter1Controller.RuntimeState.Phase !=
                    BranchFlowPhase.ResolvingBranch)
            {
                return;
            }

            quarter1DecisionRequested = true;
            Quarter1DecisionResolutionCount++;
            double randomRoll = useFixedRandomRoll
                ? fixedRandomRoll
                : NormalizeRandomRoll(UnityEngine.Random.value);
            BranchDecisionResult result =
                Quarter1Controller.ResolveDecision(randomRoll);
            quarter1ResultText = result.IsSuccess
                ? $"1분기 판정 완료: {result.DecidedRoute}"
                : $"1분기 판정 실패: {result.FailureReason}";
        }

        private void HandleCampaignStateChanged()
        {
            if (CampaignController.CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter1Running)
            {
                ClearPendingQuarter1Event();
            }

            RefreshView();
        }

        private void RefreshView()
        {
            view.Render(
                CampaignController,
                pendingQuarter1Event,
                quarter1ResultText,
                quarter2ResultText,
                quarter3ResultText,
                currentJoinSupportDay,
                testCannedFoodCount,
                testWaterCount,
                selectedSupportTarget,
                selectedSupportResource,
                recentSupportResult,
                recentSupportClue,
                useFixedRandomRoll,
                fixedRandomRoll,
                randomValidationText);
        }

        private void ClearPendingQuarter1Event()
        {
            pendingQuarter1Event = null;
            pendingQuarter1EventDay = 0;
        }

        private static bool TryParseRandomRoll(
            string text,
            out double value)
        {
            if (double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value) ||
                double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    out value))
            {
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }

            value = 0d;
            return false;
        }

        private static double NormalizeRandomRoll(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                return 0d;
            }

            return value >= 1d ? MaximumRandomRoll : value;
        }

        private static string FormatDelta(int delta)
        {
            return delta >= 0 ? $"+{delta}" : delta.ToString();
        }

        private static string FormatDecision(Quarter2Decision decision)
        {
            return decision == Quarter2Decision.Progress
                ? "진행한다"
                : "거절한다";
        }
    }
}
