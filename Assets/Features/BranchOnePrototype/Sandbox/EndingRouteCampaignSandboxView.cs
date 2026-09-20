using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Flow;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.BranchOne.Sandbox
{
    public sealed class EndingRouteCampaignSandboxView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject quarter1Panel;
        [SerializeField] private GameObject quarter1ResultPanel;
        [SerializeField] private GameObject quarter2Panel;
        [SerializeField] private GameObject quarter2RouteStatusPanel;
        [SerializeField] private GameObject quarter3Panel;
        [SerializeField] private GameObject campaignResultPanel;
        [SerializeField] private GameObject controlPanel;

        [Header("Header")]
        [SerializeField] private Text headerText;

        [Header("Quarter 1")]
        [SerializeField] private Text quarter1EventTitleText;
        [SerializeField] private Text quarter1EventBodyText;
        [SerializeField] private Text quarter1SelectionResultText;
        [SerializeField] private Button[] quarter1EventButtons =
            Array.Empty<Button>();
        [SerializeField] private Button quarter1ActButton;
        [SerializeField] private Button quarter1DeclineButton;
        [SerializeField] private Button quarter1AdvanceDayButton;

        [Header("Quarter 1 Result")]
        [SerializeField] private Text quarter1DecisionText;
        [SerializeField] private Button startQuarter2Button;

        [Header("Quarter 2")]
        [SerializeField] private Text quarter2EventText;
        [SerializeField] private Text quarter2SelectionResultText;
        [SerializeField] private Button quarter2ProgressButton;
        [SerializeField] private Button quarter2RejectButton;
        [SerializeField] private Button quarter2AdvanceButton;

        [Header("Quarter 2 Route Status")]
        [SerializeField] private Text quarter2RouteStatusText;

        [Header("Quarter 3")]
        [SerializeField] private Text quarter3StatusText;
        [SerializeField] private Text quarter3SourceProgressText;
        [SerializeField] private Text quarter3ResultText;
        [SerializeField] private Button quarter3SourceAButton;
        [SerializeField] private Button quarter3SourceBButton;
        [SerializeField] private Button quarter3OpenFinalChoiceButton;
        [SerializeField] private Button quarter3FinalOptionAButton;
        [SerializeField] private Button quarter3FinalOptionBButton;
        private GameObject joinSupportPanel;
        private Text joinSupportStatusText;
        private Button survivorTargetButton;
        private Button redArmbandTargetButton;
        private Button cannedFoodButton;
        private Button waterButton;
        private Button cannedFoodAddButton;
        private Button cannedFoodRemoveButton;
        private Button cannedFoodZeroButton;
        private Button waterAddButton;
        private Button waterRemoveButton;
        private Button waterZeroButton;
        private Button bothZeroButton;
        private Button attemptSupportButton;
        private Button nextSupportDayButton;

        [Header("Campaign Result")]
        [SerializeField] private Text campaignResultText;
        [SerializeField] private Button startQuarter3Button;

        [Header("Control")]
        [SerializeField] private Toggle useFixedRandomRollToggle;
        [SerializeField] private InputField fixedRandomRollInput;
        [SerializeField] private Text randomRollStatusText;
        [SerializeField] private Button resetCampaignButton;

        private Font runtimeKoreanFont;
        private bool isBound;

        public bool Quarter1PanelVisible =>
            quarter1Panel != null && quarter1Panel.activeSelf;
        public bool Quarter1ResultPanelVisible =>
            quarter1ResultPanel != null && quarter1ResultPanel.activeSelf;
        public bool Quarter2PanelVisible =>
            quarter2Panel != null && quarter2Panel.activeSelf;
        public bool Quarter3PanelVisible =>
            quarter3Panel != null && quarter3Panel.activeSelf;
        public bool CampaignResultPanelVisible =>
            campaignResultPanel != null && campaignResultPanel.activeSelf;
        public bool StartQuarter2Interactable =>
            startQuarter2Button != null && startQuarter2Button.interactable;
        public bool Quarter2ProgressInteractable =>
            quarter2ProgressButton != null &&
            quarter2ProgressButton.interactable;
        public bool Quarter2RejectInteractable =>
            quarter2RejectButton != null && quarter2RejectButton.interactable;
        public bool Quarter2AdvanceInteractable =>
            quarter2AdvanceButton != null &&
            quarter2AdvanceButton.interactable;
        public bool StartQuarter3Interactable =>
            startQuarter3Button != null && startQuarter3Button.interactable;
        public bool Quarter3OpenFinalChoiceInteractable =>
            quarter3OpenFinalChoiceButton != null &&
            quarter3OpenFinalChoiceButton.interactable;
        public string Quarter2AdvanceButtonLabel =>
            GetButtonLabel(quarter2AdvanceButton);
        public string HeaderDisplayText =>
            headerText != null ? headerText.text : string.Empty;
        public string Quarter2EventDisplayText =>
            quarter2EventText != null ? quarter2EventText.text : string.Empty;
        public string CampaignResultDisplayText =>
            campaignResultText != null ? campaignResultText.text : string.Empty;
        public bool KoreanFontReady { get; private set; }

        private void Awake()
        {
            EnsureJoinSupportUi();
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
            GameObject q1Panel,
            GameObject q1ResultPanel,
            GameObject q2Panel,
            GameObject q2StatusPanel,
            GameObject q3Panel,
            GameObject resultPanel,
            GameObject controls,
            Text header,
            Text q1EventTitle,
            Text q1EventBody,
            Text q1SelectionResult,
            Button[] q1EventButtons,
            Button q1Act,
            Button q1Decline,
            Button q1AdvanceDay,
            Text q1Decision,
            Button startQ2,
            Text q2Event,
            Text q2SelectionResult,
            Button q2Progress,
            Button q2Reject,
            Button q2Advance,
            Text q2RouteStatus,
            Text q3Status,
            Text q3SourceProgress,
            Text q3Result,
            Button q3SourceA,
            Button q3SourceB,
            Button q3OpenFinalChoice,
            Button q3FinalOptionA,
            Button q3FinalOptionB,
            Text result,
            Button startQ3,
            Toggle fixedRollToggle,
            InputField fixedRollInput,
            Text rollStatus,
            Button reset)
        {
            quarter1Panel = q1Panel;
            quarter1ResultPanel = q1ResultPanel;
            quarter2Panel = q2Panel;
            quarter2RouteStatusPanel = q2StatusPanel;
            quarter3Panel = q3Panel;
            campaignResultPanel = resultPanel;
            controlPanel = controls;
            headerText = header;
            quarter1EventTitleText = q1EventTitle;
            quarter1EventBodyText = q1EventBody;
            quarter1SelectionResultText = q1SelectionResult;
            quarter1EventButtons =
                q1EventButtons ?? Array.Empty<Button>();
            quarter1ActButton = q1Act;
            quarter1DeclineButton = q1Decline;
            quarter1AdvanceDayButton = q1AdvanceDay;
            quarter1DecisionText = q1Decision;
            startQuarter2Button = startQ2;
            quarter2EventText = q2Event;
            quarter2SelectionResultText = q2SelectionResult;
            quarter2ProgressButton = q2Progress;
            quarter2RejectButton = q2Reject;
            quarter2AdvanceButton = q2Advance;
            quarter2RouteStatusText = q2RouteStatus;
            quarter3StatusText = q3Status;
            quarter3SourceProgressText = q3SourceProgress;
            quarter3ResultText = q3Result;
            quarter3SourceAButton = q3SourceA;
            quarter3SourceBButton = q3SourceB;
            quarter3OpenFinalChoiceButton = q3OpenFinalChoice;
            quarter3FinalOptionAButton = q3FinalOptionA;
            quarter3FinalOptionBButton = q3FinalOptionB;
            campaignResultText = result;
            startQuarter3Button = startQ3;
            useFixedRandomRollToggle = fixedRollToggle;
            fixedRandomRollInput = fixedRollInput;
            randomRollStatusText = rollStatus;
            resetCampaignButton = reset;
        }

        public bool HasRequiredReferences()
        {
            return quarter1Panel != null &&
                   quarter1ResultPanel != null &&
                   quarter2Panel != null &&
                   quarter2RouteStatusPanel != null &&
                   quarter3Panel != null &&
                   campaignResultPanel != null &&
                   controlPanel != null &&
                   headerText != null &&
                   quarter1EventTitleText != null &&
                   quarter1EventBodyText != null &&
                   quarter1SelectionResultText != null &&
                   quarter1EventButtons != null &&
                   quarter1EventButtons.Length == 10 &&
                   quarter1EventButtons.All(button => button != null) &&
                   quarter1ActButton != null &&
                   quarter1DeclineButton != null &&
                   quarter1AdvanceDayButton != null &&
                   quarter1DecisionText != null &&
                   startQuarter2Button != null &&
                   quarter2EventText != null &&
                   quarter2SelectionResultText != null &&
                   quarter2ProgressButton != null &&
                   quarter2RejectButton != null &&
                   quarter2AdvanceButton != null &&
                   quarter2RouteStatusText != null &&
                   quarter3StatusText != null &&
                   quarter3SourceProgressText != null &&
                   quarter3ResultText != null &&
                   quarter3SourceAButton != null &&
                   quarter3SourceBButton != null &&
                   quarter3OpenFinalChoiceButton != null &&
                   quarter3FinalOptionAButton != null &&
                   quarter3FinalOptionBButton != null &&
                   campaignResultText != null &&
                   startQuarter3Button != null &&
                   useFixedRandomRollToggle != null &&
                   fixedRandomRollInput != null &&
                   randomRollStatusText != null &&
                   resetCampaignButton != null;
        }

        public void Bind(
            Action<int> onQuarter1EventPreview,
            Action onQuarter1Act,
            Action onQuarter1Decline,
            Action onQuarter1AdvanceDay,
            Action onStartQuarter2,
            Action onQuarter2Progress,
            Action onQuarter2Reject,
            Action onQuarter2Advance,
            Action onStartQuarter3,
            Action onQuarter3SourceA,
            Action onQuarter3SourceB,
            Action onQuarter3OpenFinalChoice,
            Action onQuarter3FinalOptionA,
            Action onQuarter3FinalOptionB,
            Action onSurvivorTarget,
            Action onRedArmbandTarget,
            Action onCannedFood,
            Action onWater,
            Action onCannedFoodAdd,
            Action onCannedFoodRemove,
            Action onCannedFoodZero,
            Action onWaterAdd,
            Action onWaterRemove,
            Action onWaterZero,
            Action onBothZero,
            Action onAttemptSupport,
            Action onNextSupportDay,
            Action onReset,
            Action<bool> onUseFixedRollChanged,
            Action<string> onFixedRollSubmitted)
        {
            if (onQuarter1EventPreview == null ||
                onQuarter1Act == null ||
                onQuarter1Decline == null ||
                onQuarter1AdvanceDay == null ||
                onStartQuarter2 == null ||
                onQuarter2Progress == null ||
                onQuarter2Reject == null ||
                onQuarter2Advance == null ||
                onStartQuarter3 == null ||
                onQuarter3SourceA == null ||
                onQuarter3SourceB == null ||
                onQuarter3OpenFinalChoice == null ||
                onQuarter3FinalOptionA == null ||
                onQuarter3FinalOptionB == null ||
                onSurvivorTarget == null ||
                onRedArmbandTarget == null ||
                onCannedFood == null ||
                onWater == null ||
                onCannedFoodAdd == null ||
                onCannedFoodRemove == null ||
                onCannedFoodZero == null ||
                onWaterAdd == null ||
                onWaterRemove == null ||
                onWaterZero == null ||
                onBothZero == null ||
                onAttemptSupport == null ||
                onNextSupportDay == null ||
                onReset == null ||
                onUseFixedRollChanged == null ||
                onFixedRollSubmitted == null)
            {
                throw new ArgumentNullException(
                    "Campaign Sandbox callbacks cannot be null.");
            }

            Unbind();
            for (int index = 0; index < quarter1EventButtons.Length; index++)
            {
                int eventNumber = index + 1;
                quarter1EventButtons[index].onClick.AddListener(
                    () => onQuarter1EventPreview(eventNumber));
            }

            quarter1ActButton.onClick.AddListener(
                () => onQuarter1Act());
            quarter1DeclineButton.onClick.AddListener(
                () => onQuarter1Decline());
            quarter1AdvanceDayButton.onClick.AddListener(
                () => onQuarter1AdvanceDay());
            startQuarter2Button.onClick.AddListener(
                () => onStartQuarter2());
            quarter2ProgressButton.onClick.AddListener(
                () => onQuarter2Progress());
            quarter2RejectButton.onClick.AddListener(
                () => onQuarter2Reject());
            quarter2AdvanceButton.onClick.AddListener(
                () => onQuarter2Advance());
            startQuarter3Button.onClick.AddListener(
                () => onStartQuarter3());
            quarter3SourceAButton.onClick.AddListener(
                () => onQuarter3SourceA());
            quarter3SourceBButton.onClick.AddListener(
                () => onQuarter3SourceB());
            quarter3OpenFinalChoiceButton.onClick.AddListener(
                () => onQuarter3OpenFinalChoice());
            quarter3FinalOptionAButton.onClick.AddListener(
                () => onQuarter3FinalOptionA());
            quarter3FinalOptionBButton.onClick.AddListener(
                () => onQuarter3FinalOptionB());
            survivorTargetButton.onClick.AddListener(() => onSurvivorTarget());
            redArmbandTargetButton.onClick.AddListener(() => onRedArmbandTarget());
            cannedFoodButton.onClick.AddListener(() => onCannedFood());
            waterButton.onClick.AddListener(() => onWater());
            cannedFoodAddButton.onClick.AddListener(() => onCannedFoodAdd());
            cannedFoodRemoveButton.onClick.AddListener(() => onCannedFoodRemove());
            cannedFoodZeroButton.onClick.AddListener(() => onCannedFoodZero());
            waterAddButton.onClick.AddListener(() => onWaterAdd());
            waterRemoveButton.onClick.AddListener(() => onWaterRemove());
            waterZeroButton.onClick.AddListener(() => onWaterZero());
            bothZeroButton.onClick.AddListener(() => onBothZero());
            attemptSupportButton.onClick.AddListener(() => onAttemptSupport());
            nextSupportDayButton.onClick.AddListener(() => onNextSupportDay());
            resetCampaignButton.onClick.AddListener(
                () => onReset());
            useFixedRandomRollToggle.onValueChanged.AddListener(
                value => onUseFixedRollChanged(value));
            fixedRandomRollInput.onEndEdit.AddListener(
                value => onFixedRollSubmitted(value));
            isBound = true;
        }

        public void Unbind()
        {
            if (!isBound)
            {
                return;
            }

            foreach (Button button in quarter1EventButtons)
            {
                button?.onClick.RemoveAllListeners();
            }

            quarter1ActButton?.onClick.RemoveAllListeners();
            quarter1DeclineButton?.onClick.RemoveAllListeners();
            quarter1AdvanceDayButton?.onClick.RemoveAllListeners();
            startQuarter2Button?.onClick.RemoveAllListeners();
            quarter2ProgressButton?.onClick.RemoveAllListeners();
            quarter2RejectButton?.onClick.RemoveAllListeners();
            quarter2AdvanceButton?.onClick.RemoveAllListeners();
            startQuarter3Button?.onClick.RemoveAllListeners();
            quarter3SourceAButton?.onClick.RemoveAllListeners();
            quarter3SourceBButton?.onClick.RemoveAllListeners();
            quarter3OpenFinalChoiceButton?.onClick.RemoveAllListeners();
            quarter3FinalOptionAButton?.onClick.RemoveAllListeners();
            quarter3FinalOptionBButton?.onClick.RemoveAllListeners();
            survivorTargetButton?.onClick.RemoveAllListeners();
            redArmbandTargetButton?.onClick.RemoveAllListeners();
            cannedFoodButton?.onClick.RemoveAllListeners();
            waterButton?.onClick.RemoveAllListeners();
            cannedFoodAddButton?.onClick.RemoveAllListeners();
            cannedFoodRemoveButton?.onClick.RemoveAllListeners();
            cannedFoodZeroButton?.onClick.RemoveAllListeners();
            waterAddButton?.onClick.RemoveAllListeners();
            waterRemoveButton?.onClick.RemoveAllListeners();
            waterZeroButton?.onClick.RemoveAllListeners();
            bothZeroButton?.onClick.RemoveAllListeners();
            attemptSupportButton?.onClick.RemoveAllListeners();
            nextSupportDayButton?.onClick.RemoveAllListeners();
            resetCampaignButton?.onClick.RemoveAllListeners();
            useFixedRandomRollToggle?.onValueChanged.RemoveAllListeners();
            fixedRandomRollInput?.onEndEdit.RemoveAllListeners();
            isBound = false;
        }

        public void Render(
            EndingRouteCampaignController campaign,
            BranchEventDefinition pendingQuarter1Event,
            string quarter1Result,
            string quarter2Result,
            string quarter3Result,
            int supportDay,
            int cannedFoodCount,
            int waterCount,
            Quarter3JoinSupportTarget supportTarget,
            Quarter3JoinSupportResource supportResource,
            string recentSupportResult,
            string recentSupportClue,
            bool useFixedRandomRoll,
            double fixedRandomRoll,
            string randomValidation)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            RenderHeader(campaign);
            RenderPanels(campaign.CampaignState.Phase);
            RenderQuarter1(
                campaign,
                pendingQuarter1Event,
                quarter1Result);
            RenderQuarter1Decision(campaign);
            RenderQuarter2(campaign, quarter2Result);
            RenderQuarter3(campaign, quarter3Result, supportDay,
                cannedFoodCount, waterCount, supportTarget, supportResource,
                recentSupportResult, recentSupportClue);
            RenderCampaignResult(campaign);
            RenderRandomControl(
                campaign,
                useFixedRandomRoll,
                fixedRandomRoll,
                randomValidation);
        }

        public void ApplyKoreanFont()
        {
            if (runtimeKoreanFont == null)
            {
                runtimeKoreanFont = ResolveProjectDefaultFont();
            }

            KoreanFontReady =
                runtimeKoreanFont != null &&
                runtimeKoreanFont.HasCharacter('한');
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

        private void RenderHeader(EndingRouteCampaignController campaign)
        {
            BranchRuntimeState quarter1 =
                campaign.Quarter1Controller.RuntimeState;
            headerText.text =
                $"CampaignPhase: {campaign.CampaignState.Phase}    " +
                $"1분기 날짜: {quarter1.CurrentDay}일    " +
                $"Signal: {campaign.Quarter1Controller.ScoreState.SignalScore}    " +
                $"Join: {campaign.Quarter1Controller.ScoreState.JoinScore}\n" +
                $"1분기 결정: {campaign.CampaignState.Quarter1Route}    " +
                $"2분기 현재: {campaign.Quarter2CurrentRoute}    " +
                $"2분기 Phase: {campaign.Quarter2Phase}    " +
                $"이벤트: {campaign.Quarter2CurrentEventOrder} / 3    " +
                $"통과: {campaign.Quarter2PassedRoute}    " +
                $"양쪽 실패: {FormatBool(campaign.AreAllBranchesFailed)}\n" +
                $"3분기 계열: {campaign.Quarter3CurrentRoute}    " +
                $"3분기 Phase: {campaign.Quarter3Phase}    " +
                $"획득 단서: {campaign.Quarter3AcquiredClueIds.Count}    " +
                $"최종 선택: " +
                $"{FormatText(campaign.Quarter3SelectedFinalChoiceId, "없음")}";
        }

        private void RenderPanels(EndingRouteCampaignPhase phase)
        {
            bool quarter1Visible =
                phase == EndingRouteCampaignPhase.Quarter1Running ||
                phase == EndingRouteCampaignPhase.Quarter1Resolved;
            bool quarter2Running =
                phase == EndingRouteCampaignPhase.Quarter2Running;
            bool quarter2StatusVisible =
                quarter2Running ||
                phase == EndingRouteCampaignPhase.Quarter2Passed ||
                phase == EndingRouteCampaignPhase.AllBranchesFailed;
            bool quarter3Running =
                phase == EndingRouteCampaignPhase.Quarter3Running;

            quarter1Panel.SetActive(quarter1Visible);
            quarter1ResultPanel.SetActive(
                phase == EndingRouteCampaignPhase.Quarter1Resolved);
            quarter2Panel.SetActive(quarter2Running);
            quarter2RouteStatusPanel.SetActive(quarter2StatusVisible);
            quarter3Panel.SetActive(quarter3Running);
            campaignResultPanel.SetActive(
                phase == EndingRouteCampaignPhase.Quarter2Passed ||
                phase == EndingRouteCampaignPhase.AllBranchesFailed ||
                phase == EndingRouteCampaignPhase.Quarter3Resolved);
            controlPanel.SetActive(true);
        }

        private void RenderQuarter1(
            EndingRouteCampaignController campaign,
            BranchEventDefinition pendingEvent,
            string selectionResult)
        {
            BranchOneFlowController flow = campaign.Quarter1Controller;
            BranchRuntimeState runtime = flow.RuntimeState;
            quarter1EventTitleText.text = pendingEvent == null
                ? "선택된 이벤트: 아직 선택하지 않았습니다."
                : $"선택된 이벤트: {pendingEvent.Title}";
            quarter1EventBodyText.text = pendingEvent == null
                ? "이벤트 버튼을 누르면 제목과 본문을 미리 볼 수 있습니다."
                : pendingEvent.Body;
            quarter1SelectionResultText.text =
                $"선택 결과: {FormatText(selectionResult, "아직 선택하지 않았습니다.")}";

            bool waitingForEvent =
                campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.Quarter1Running &&
                runtime.Phase == BranchFlowPhase.WaitingForEvent &&
                runtime.CurrentDay < flow.Settings.DecisionDay;
            HashSet<int> availableNumbers = new(
                flow.GetAvailableEventsForCurrentDay()
                    .Select(definition => definition.EventNumber));
            for (int index = 0; index < quarter1EventButtons.Length; index++)
            {
                quarter1EventButtons[index].interactable =
                    waitingForEvent &&
                    pendingEvent == null &&
                    availableNumbers.Contains(index + 1);
            }

            bool waitingForAction =
                waitingForEvent &&
                pendingEvent != null &&
                !runtime.IsTodayEventCompleted;
            quarter1ActButton.interactable = waitingForAction;
            quarter1DeclineButton.interactable = waitingForAction;
            quarter1AdvanceDayButton.interactable =
                campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.Quarter1Running &&
                runtime.Phase == BranchFlowPhase.ReadyToAdvance;
        }

        private void RenderQuarter1Decision(
            EndingRouteCampaignController campaign)
        {
            BranchDecisionResult result =
                campaign.CampaignState.Quarter1DecisionResult;
            quarter1DecisionText.text = result == null
                ? "1분기 판정 결과 대기 중"
                : $"최종 Signal: {result.FinalSignalScore}\n" +
                  $"최종 Join: {result.FinalJoinScore}\n" +
                  $"Signal 확률: {result.SignalRatio:P1}\n" +
                  $"Join 확률: {result.JoinRatio:P1}\n" +
                  $"사용 randomRoll: {result.RandomRoll:0.######}\n" +
                  $"결정 계열: {result.DecidedRoute}\n" +
                  $"성공: {FormatBool(result.IsSuccess)}";
            startQuarter2Button.interactable =
                campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.Quarter1Resolved &&
                result != null &&
                result.IsSuccess &&
                campaign.Quarter2Phase == Quarter2FlowPhase.NotStarted;
        }

        private void RenderQuarter2(
            EndingRouteCampaignController campaign,
            string selectionResult)
        {
            Quarter2EventDefinition currentEvent =
                campaign.GetCurrentQuarter2Event();
            Quarter2RuntimeState state =
                campaign.Quarter2Controller.RuntimeState;
            quarter2EventText.text = currentEvent == null
                ? $"현재 계열: {campaign.Quarter2CurrentRoute}\n" +
                  $"현재 이벤트: 없음\n" +
                  $"현재 Phase: {campaign.Quarter2Phase}\n" +
                  $"전환 예정 계열: {state.PendingRoute}"
                : $"현재 계열: {currentEvent.Route}\n" +
                  $"현재 이벤트: {currentEvent.Order} / 3\n" +
                  $"이벤트 ID: {currentEvent.EventId}\n" +
                  $"제목: {currentEvent.Title}\n" +
                  $"본문: {currentEvent.Body}";
            quarter2SelectionResultText.text =
                $"선택 결과: {FormatText(selectionResult, "아직 선택하지 않았습니다.")}";

            bool waitingForChoice =
                campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.Quarter2Running &&
                campaign.Quarter2Phase ==
                    Quarter2FlowPhase.WaitingForChoice &&
                currentEvent != null;
            quarter2ProgressButton.interactable = waitingForChoice;
            quarter2RejectButton.interactable = waitingForChoice;

            bool waitingForAdvance =
                campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.Quarter2Running &&
                (campaign.Quarter2Phase ==
                    Quarter2FlowPhase.WaitingForAdvance ||
                 campaign.Quarter2Phase ==
                    Quarter2FlowPhase.RouteSwitchPending);
            quarter2AdvanceButton.interactable = waitingForAdvance;
            SetButtonLabel(
                quarter2AdvanceButton,
                campaign.Quarter2Phase ==
                    Quarter2FlowPhase.RouteSwitchPending
                    ? "반대 계열 시작"
                    : "다음 이벤트");

            quarter2RouteStatusText.text =
                FormatRouteProgress("Signal", state.SignalProgress) +
                "\n\n" +
                FormatRouteProgress("Join", state.JoinProgress) +
                $"\n\n전환 예정 계열: {state.PendingRoute}";
        }

        private void RenderQuarter3(
            EndingRouteCampaignController campaign,
            string operationResult,
            int supportDay,
            int cannedFoodCount,
            int waterCount,
            Quarter3JoinSupportTarget supportTarget,
            Quarter3JoinSupportResource supportResource,
            string recentSupportResult,
            string recentSupportClue)
        {
            Quarter3SourceProgress sourceAProgress =
                campaign.GetQuarter3SourceProgress(
                    Quarter3SandboxDataProvider.SourceA);
            Quarter3SourceProgress sourceBProgress =
                campaign.GetQuarter3SourceProgress(
                    Quarter3SandboxDataProvider.SourceB);
            IReadOnlyList<Quarter3FinalChoiceDefinition> options =
                campaign.GetQuarter3FinalChoiceOptions();

            quarter3StatusText.text =
                $"현재 계열: {campaign.Quarter3CurrentRoute}\n" +
                $"현재 Phase: {campaign.Quarter3Phase}\n" +
                $"획득 단서 ID: " +
                $"{FormatClueIds(campaign.Quarter3AcquiredClueIds)}\n" +
                $"최종 선택 ID: " +
                $"{FormatText(campaign.Quarter3SelectedFinalChoiceId, "없음")}";
            quarter3SourceProgressText.text =
                FormatSourceProgress(sourceAProgress) + "\n" +
                FormatSourceProgress(sourceBProgress);
            quarter3ResultText.text =
                $"처리 결과: " +
                $"{FormatText(operationResult, "3분기 입력을 기다립니다.")}";

            bool running =
                campaign.CampaignState.Phase ==
                EndingRouteCampaignPhase.Quarter3Running;
            bool joinRunning = running &&
                campaign.Quarter3CurrentRoute == BranchRoute.Join;
            joinSupportPanel.SetActive(joinRunning);
            quarter3SourceAButton.gameObject.SetActive(!joinRunning);
            quarter3SourceBButton.gameObject.SetActive(!joinRunning);
            var counts = campaign.GetQuarter3JoinSupportCounts();
            Quarter3JoinSupportDayStatus dayStatus =
                supportDay <= 4
                    ? campaign.GetQuarter3JoinSupportDayStatus(supportDay)
                    : Quarter3JoinSupportDayStatus.Open;
            joinSupportStatusText.text =
                $"현재 지원 일차: {(supportDay <= 4 ? supportDay.ToString() : "종료")}/4\n" +
                $"현재 날짜 상태: {dayStatus}\n" +
                $"테스트 통조림: {cannedFoodCount}   테스트 물: {waterCount}\n" +
                $"선택 대상: {supportTarget}   선택 물자: {supportResource}\n" +
                $"일반 생존자 지원: {counts.SurvivorGroup}   붉은 완장 지원: {counts.RedArmband}\n" +
                $"최근 결과: {FormatText(recentSupportResult, "없음")}\n" +
                $"최근 획득 단서: {FormatText(recentSupportClue, "없음")}\n" +
                $"처리 완료 일차: {FormatProcessedSupportDays(campaign)}   " +
                $"Q3 단서 수: {campaign.Quarter3AcquiredClueIds.Count}";
            attemptSupportButton.interactable = joinRunning && supportDay <= 4 &&
                dayStatus == Quarter3JoinSupportDayStatus.Open;
            nextSupportDayButton.interactable = joinRunning && supportDay <= 4 &&
                dayStatus != Quarter3JoinSupportDayStatus.Open;
            quarter3SourceAButton.interactable =
                running &&
                campaign.GetQuarter3AvailableClues(
                    Quarter3SandboxDataProvider.SourceA).Count > 0;
            quarter3SourceBButton.interactable =
                running &&
                campaign.GetQuarter3AvailableClues(
                    Quarter3SandboxDataProvider.SourceB).Count > 0;
            quarter3OpenFinalChoiceButton.interactable =
                running &&
                campaign.Quarter3Phase ==
                    Quarter3FlowPhase.CollectingClues;

            bool canSelect =
                running &&
                campaign.Quarter3IsFinalChoiceOpen &&
                options.Count == 2;
            quarter3FinalOptionAButton.interactable = canSelect;
            quarter3FinalOptionBButton.interactable = canSelect;
            SetButtonLabel(
                quarter3FinalOptionAButton,
                options.Count > 0
                    ? options[0].FinalChoiceId
                    : "최종 선택 A");
            SetButtonLabel(
                quarter3FinalOptionBButton,
                options.Count > 1
                    ? options[1].FinalChoiceId
                    : "최종 선택 B");
        }

        private void EnsureJoinSupportUi()
        {
            if (joinSupportPanel != null || quarter3Panel == null) return;
            joinSupportPanel = new GameObject("JoinSupportPanel",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            joinSupportPanel.transform.SetParent(quarter3Panel.transform, false);
            VerticalLayoutGroup layout = joinSupportPanel.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandHeight = false;
            joinSupportStatusText = CreateRuntimeText("JoinSupportStatus", joinSupportPanel.transform);
            survivorTargetButton = CreateRuntimeButton("일반 생존자 선택", joinSupportPanel.transform);
            redArmbandTargetButton = CreateRuntimeButton("붉은 완장 선택", joinSupportPanel.transform);
            cannedFoodButton = CreateRuntimeButton("통조림 선택", joinSupportPanel.transform);
            waterButton = CreateRuntimeButton("물 선택", joinSupportPanel.transform);
            cannedFoodAddButton = CreateRuntimeButton("통조림 +1", joinSupportPanel.transform);
            cannedFoodRemoveButton = CreateRuntimeButton("통조림 -1", joinSupportPanel.transform);
            cannedFoodZeroButton = CreateRuntimeButton("통조림 0", joinSupportPanel.transform);
            waterAddButton = CreateRuntimeButton("물 +1", joinSupportPanel.transform);
            waterRemoveButton = CreateRuntimeButton("물 -1", joinSupportPanel.transform);
            waterZeroButton = CreateRuntimeButton("물 0", joinSupportPanel.transform);
            bothZeroButton = CreateRuntimeButton("둘 다 0", joinSupportPanel.transform);
            attemptSupportButton = CreateRuntimeButton("지원 실행", joinSupportPanel.transform);
            nextSupportDayButton = CreateRuntimeButton("다음 지원 일차", joinSupportPanel.transform);
        }

        private Text CreateRuntimeText(string name, Transform parent)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(Text),
                typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            Text text = item.GetComponent<Text>();
            text.font = runtimeKoreanFont;
            text.fontSize = 16;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            item.GetComponent<LayoutElement>().preferredHeight = 145f;
            return text;
        }

        private Button CreateRuntimeButton(string label, Transform parent)
        {
            GameObject item = new(label, typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            item.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 1f);
            item.GetComponent<LayoutElement>().preferredHeight = 28f;
            Text text = CreateRuntimeText("Label", item.transform);
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.GetComponent<LayoutElement>().ignoreLayout = true;
            return item.GetComponent<Button>();
        }

        private static string FormatProcessedSupportDays(
            EndingRouteCampaignController campaign)
        {
            List<string> days = new();
            for (int day = 1; day <= 4; day++)
                if (campaign.GetQuarter3JoinSupportRecord(day) != null)
                    days.Add(day.ToString());
            return days.Count == 0 ? "없음" : string.Join(", ", days);
        }

        private void RenderCampaignResult(
            EndingRouteCampaignController campaign)
        {
            Quarter2RuntimeState state =
                campaign.Quarter2Controller.RuntimeState;
            if (campaign.CampaignState.Phase ==
                EndingRouteCampaignPhase.Quarter2Passed)
            {
                bool sameRoute =
                    state.InitialRoute ==
                    campaign.Quarter2PassedRoute;
                campaignResultText.text =
                    "2분기 통과\n" +
                    $"최초 진입 계열: {state.InitialRoute}\n" +
                    $"통과 계열: {campaign.Quarter2PassedRoute}\n" +
                    $"최초 계열과 동일: {FormatBool(sameRoute)}\n" +
                    "3분기는 아래 버튼으로 명시적으로 시작합니다.";
                startQuarter3Button.interactable =
                    campaign.Quarter3Phase ==
                    Quarter3FlowPhase.NotStarted;
                return;
            }

            if (campaign.CampaignState.Phase ==
                EndingRouteCampaignPhase.AllBranchesFailed)
            {
                startQuarter3Button.interactable = false;
                campaignResultText.text =
                    "AllBranchesFailed\n" +
                    $"구조신호 실패: {FormatBool(campaign.SignalFailed)}\n" +
                    $"합류 실패: {FormatBool(campaign.JoinFailed)}\n" +
                    "마지막 벙커 엔딩은 실행하지 않습니다.";
                return;
            }

            if (campaign.CampaignState.Phase ==
                EndingRouteCampaignPhase.Quarter3Resolved)
            {
                startQuarter3Button.interactable = false;
                campaignResultText.text =
                    "Quarter3Resolved\n" +
                    $"3분기 계열: {campaign.Quarter3CurrentRoute}\n" +
                    $"획득 단서: {campaign.Quarter3AcquiredClueIds.Count}\n" +
                    $"최종 선택 ID: " +
                    $"{campaign.Quarter3SelectedFinalChoiceId}\n" +
                    "실제 엔딩 실행은 이 Sandbox 범위에 포함하지 않습니다.";
                return;
            }

            startQuarter3Button.interactable = false;
            campaignResultText.text = "캠페인 최종 결과 대기 중";
        }

        private void RenderRandomControl(
            EndingRouteCampaignController campaign,
            bool useFixedRandomRoll,
            double fixedRandomRoll,
            string validationText)
        {
            useFixedRandomRollToggle.SetIsOnWithoutNotify(
                useFixedRandomRoll);
            fixedRandomRollInput.SetTextWithoutNotify(
                fixedRandomRoll.ToString("0.######"));
            randomRollStatusText.text =
                $"고정 randomRoll: {FormatBool(useFixedRandomRoll)}  |  " +
                $"값: {fixedRandomRoll:0.######}\n" +
                FormatText(validationText, "0 이상 1 미만 값을 사용합니다.");
        }

        private static string FormatRouteProgress(
            string label,
            Quarter2RouteProgress progress)
        {
            return $"{label}  |  Progress: {progress.ProgressCount}  " +
                   $"Reject: {progress.RejectCount}  " +
                   $"Failed: {FormatBool(progress.Failed)}  " +
                   $"Attempted: {FormatBool(progress.Attempted)}  " +
                   $"Completed: {progress.CompletedEventCount}";
        }

        private static string FormatSourceProgress(
            Quarter3SourceProgress progress)
        {
            return $"{progress.SourceId}  |  전체: {progress.TotalCount}  " +
                   $"획득: {progress.AcquiredCount}  " +
                   $"남음: {progress.RemainingCount}";
        }

        private static string FormatClueIds(
            IReadOnlyList<string> clueIds)
        {
            return clueIds == null || clueIds.Count == 0
                ? "없음"
                : string.Join(", ", clueIds);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button != null
                ? button.GetComponentInChildren<Text>(true)
                : null;
            if (text != null)
            {
                text.text = label;
            }
        }

        private static string GetButtonLabel(Button button)
        {
            Text text = button != null
                ? button.GetComponentInChildren<Text>(true)
                : null;
            return text != null ? text.text : string.Empty;
        }

        private static string FormatText(
            string value,
            string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string FormatBool(bool value)
        {
            return value ? "예" : "아니오";
        }
    }
}
