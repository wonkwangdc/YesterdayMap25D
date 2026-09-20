using System;
using System.Collections.Generic;
using UnityEngine;
using YesterdayMap.BranchOne;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Diary;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Flow;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;
using YesterdayMap.BranchOne.Persistence;
using YesterdayMap.Character;
using YesterdayMap.Core;
using YesterdayMap.Exploration;
using YesterdayMap.Resources;
using YesterdayMap.UI;

namespace YesterdayMap.Events
{
    // Connects the existing three-quarter campaign to the Shelter door and UI.
    public sealed class ShelterEventDialogueController : MonoBehaviour
    {
        private const string Quarter2ProgressChoice = "Q2_PROGRESS";
        private const string Quarter2RejectChoice = "Q2_REJECT";
        private const string Quarter2IntroNextChoice = "Q2_INTRO_NEXT";
        private const string Quarter3SignalIntroNextChoice =
            "Q3_SIGNAL_INTRO_NEXT";
        private const string Quarter3JoinIntroNextChoice =
            "Q3_JOIN_INTRO_NEXT";
        private const string JoinSurvivorTargetChoice = "Q3_JOIN_TARGET_SURVIVORS";
        private const string JoinRedTargetChoice = "Q3_JOIN_TARGET_RED";
        private const string JoinViewSurvivorClaimChoice = "Q3_JOIN_VIEW_SURVIVORS";
        private const string JoinViewRedClaimChoice = "Q3_JOIN_VIEW_RED";
        private const string Quarter3RouteGuideChoice = "Q3_ROUTE_GUIDE_ACKNOWLEDGE";
        private const string MorningStatusDialogueChoice = "MORNING_STATUS_ACKNOWLEDGE";
        private const string Quarter2SignalMorningDialogueChoice =
            "Q2_SIGNAL_MORNING_NEXT";
        private const string Quarter2JoinReturnDialogueChoice =
            "Q2_JOIN_RETURN_ACKNOWLEDGE";
        private const string LastSurvivalEndingDialogueChoice =
            "LAST_SURVIVAL_ENDING_ACKNOWLEDGE";
        private const string Quarter3FinalSleepBlockChoice =
            "Q3_FINAL_SLEEP_BLOCK_ACKNOWLEDGE";
        private const string LastBunkerIntroChoice = "LAST_BUNKER_INTRO_ACKNOWLEDGE";
        private const string DayOneTutorialMonologueChoice =
            "DAY_ONE_TUTORIAL_MONOLOGUE_ACKNOWLEDGE";
        private const string Quarter3SignalExplorationNotice =
            "[시스템 안내] 탐사에서 구조신호와 관련된 판단 기록을 찾을 수 있습니다.";
        private const string Quarter3JoinSupportNotice =
            "[시스템 안내] 메인일기에서 스토리를 확인하면 두 집단의 지원 요청을 선택할 수 있습니다.";
        private const string JoinFoodChoice = "Q3_JOIN_RESOURCE_FOOD";
        private const string JoinWaterChoice = "Q3_JOIN_RESOURCE_WATER";
        private const string Quarter2ArmedGroupEvent = "Q2_JOIN_EVT_03";
        private const int ResourceUnitsPerSupportItem = 2;
        private const int JoinResourceRequiredDay = 1;
        private static readonly Quarter3JoinSupportInventorySnapshot FreeJoinSupportInventory = new(1, 1);
        private const string Quarter3NoticeTitle = "최종 판단 준비";
        private const string Quarter3NoticeMessage =
            "3분기는 앞으로 <b>5일 동안 진행</b>됩니다.\n\n" +
            "<b>1~4일 차 탐사</b>에서 일반 보상과 함께 판단 기록을 획득할 수 있으며, " +
            "<b>5일 차에는 어느 구조신호를 따를지 최종 선택</b>해야 합니다.\n\n" +
            "판단 기록은 총 6개지만 탐사 기회는 최대 4회이므로 모든 기록을 확인할 수는 없습니다.\n\n" +
            "탐사 지도에서 장소별 기록 수와 획득 현황을 확인하고, 필요한 자원과 정보를 고려해 탐사 장소를 선택하십시오.\n\n" +
            "획득한 기록은 일기장에 자동으로 등록됩니다.";
        private const string Quarter3JoinNoticeTitle = "최종 합류 준비";
        private const string Quarter3JoinNoticeMessage =
            "3분기는 앞으로 <b>5일 동안 진행</b>됩니다.\n\n" +
            "<b>1~4일 차</b>에는 일반 생존자 무리와 붉은 완장 경비대 중 한쪽을 지원하며 판단 기록을 획득할 수 있고, " +
            "<b>5일 차에는 어느 집단과 함께할지 최종 선택</b>해야 합니다.\n\n" +
            "지원한 집단에 따라 서로 다른 사건과 내부 상황을 확인하게 되며, 해당 집단의 생존 방식과 탈출 계획에 관한 판단 기록을 획득합니다.\n\n" +
            "전체 판단 기록은 8개이며, 이번 진행에서 획득할 수 있는 기록은 최대 4개입니다. 한 번의 진행에서 모든 기록을 확인할 수는 없습니다.\n\n" +
            "획득하지 못한 판단 기록이 있더라도 5일 차 최종 선택은 제한되지 않습니다.\n\n" +
            "획득한 기록과 지원 내역은 일기장에 자동으로 등록되며, 최종 선택 전에 다시 확인할 수 있습니다.";

        [SerializeField] private DayCycleManager dayCycle;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private ShelterEventDialogueUI dialogueUI;
        [SerializeField] private Texture2D protagonistPortrait;
        [SerializeField] private Texture2D radioPortrait;
        [SerializeField] private Texture2D raiderGroupPortrait;
        [SerializeField] private Texture2D survivorGroupPortrait;
        [SerializeField] private Texture2D armedGroupPortrait;
        [SerializeField] private Texture2D militaryRescuePortrait;
        [SerializeField] private bool weightedRandomDecision = true;
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private ExplorationManager explorationManager;
        [SerializeField] private SleepConfirmationUI quarter3NoticeUI;
        [SerializeField] private SignalEndingSequence signalEndingSequence;
        [SerializeField] private JoinEndingSequence joinEndingSequence;

        private readonly HashSet<int> completedDays = new();
        private readonly HashSet<int> viewedDays = new();
        private readonly HashSet<int> joinStoryViewedDays = new();
        private readonly List<string> campaignRecords = new();
        private readonly List<string> pendingMorningDialogueLines = new();
        private EndingRouteCampaignController campaign;
        private Quarter3ClueCatalog quarter3ClueCatalog;
        private BranchPrototypeSettings settings;
        private BranchEventDefinition pendingQuarter1Event;
        private Quarter2EventDefinition pendingQuarter2IntroEvent;
        private IReadOnlyList<ShelterIntroDialogueLine> pendingQuarter2IntroLines =
            Array.Empty<ShelterIntroDialogueLine>();
        private int pendingQuarter2IntroIndex;
        private IReadOnlyList<ShelterIntroDialogueLine> pendingQuarter3SignalIntroLines =
            Array.Empty<ShelterIntroDialogueLine>();
        private int pendingQuarter3SignalIntroIndex;
        private int pendingQuarter3SignalIntroDay = -1;
        private IReadOnlyList<ShelterIntroDialogueLine> pendingQuarter3JoinIntroLines =
            Array.Empty<ShelterIntroDialogueLine>();
        private int pendingQuarter3JoinIntroIndex;
        private int pendingQuarter3JoinIntroDay = -1;
        private Quarter3JoinSupportTarget pendingJoinTarget;
        private int pendingJoinSupportDay = -1;
        private PendingChoiceKind pendingChoiceKind;
        private IReadOnlyList<BranchEventDefinition> lastBunkerEvents;
        private int lastBunkerEventOffset;
        private int lastBunkerStartDay = -1;
        private int lastBunkerStoryDay;
        private readonly HashSet<int> lastBunkerDiaryUnlockedDays = new();
        private readonly HashSet<int> lastBunkerFixedEventCompletedDays = new();
        private bool lastBunkerStoryAdvancePending;
        private bool lastBunkerIntroPresented;
        private bool lastBunkerIntroAcknowledged;
        private int quarter2LastChoiceDay = -1;
        private int quarter3StartDay = -1;
        private bool quarter3StartNoticePresented;
        private bool quarter3StartNoticeAcknowledged;
        private bool quarter3RouteGuidePresented;
        private bool quarter3RouteGuideAcknowledged;
        private bool quarter3FinalStoryAcknowledged;
        private bool quarter3FinalMorningDialogueShown;
        private int quarter2SignalDialogueAcknowledgedMask;
        private int pendingQuarter2SignalDialogueOrder = -1;
        private int pendingQuarter2SignalDialogueIndex = -1;
        private int quarter2JoinReturnDialogueAcknowledgedMask;
        private int pendingQuarter2JoinReturnDialogueOrder = -1;
        private int pendingQuarter2JoinReturnDialogueIndex = -1;
        private ExplorationManager subscribedExplorationManager;
        private Action lastSurvivalEndingDialogueCompleted;
        private Action dayOneTutorialMonologueCompleted;
        private string lastQuarter1ScoreChange = "아직 점수 변동 없음";
        private string lastQuarter2DecisionResult = "아직 2분기 선택 없음";

        public int Quarter1SignalScore
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.Quarter1Controller.ScoreState.SignalScore;
            }
        }

        public int Quarter1JoinScore
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.Quarter1Controller.ScoreState.JoinScore;
            }
        }

        public string LastQuarter1ScoreChange => lastQuarter1ScoreChange;

        public BranchRoute Quarter2CurrentRoute
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.Quarter2CurrentRoute;
            }
        }

        public int Quarter2CurrentEventOrder
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.Quarter2CurrentEventOrder;
            }
        }

        public bool IsQuarter2SignalDeskEventActive
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.CampaignState.Phase ==
                           EndingRouteCampaignPhase.Quarter2Running &&
                       campaign.Quarter2CurrentRoute == BranchRoute.Signal &&
                       campaign.Quarter2CurrentEventOrder is 1 or 3;
            }
        }

        public int Quarter2SignalProgressCount
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.SignalProgressCount;
            }
        }

        public int Quarter2SignalRejectCount
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.SignalRejectCount;
            }
        }

        public int Quarter2JoinProgressCount
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.JoinProgressCount;
            }
        }

        public int Quarter2JoinRejectCount
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.JoinRejectCount;
            }
        }

        public string LastQuarter2DecisionResult => lastQuarter2DecisionResult;

        public bool CanSkipQuarter1ForTesting
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.CampaignState.Phase ==
                       EndingRouteCampaignPhase.Quarter1Running;
            }
        }

        public bool CanOpenEvent
        {
            get
            {
                ResolveReferences();
                CreateCampaignIfNeeded();
                if (dialogueUI == null)
                {
                    return false;
                }

                int day = CurrentDay;
                switch (campaign.CampaignState.Phase)
                {
                    case EndingRouteCampaignPhase.Quarter1Running:
                        return day >= settings.StartDay &&
                               day <= settings.LastEventDay &&
                               !completedDays.Contains(day);
                    case EndingRouteCampaignPhase.Quarter2Running:
                        return CanOpenCurrentQuarter2Event() &&
                               !completedDays.Contains(day);
                    case EndingRouteCampaignPhase.Quarter3Running:
                        int localDay = Quarter3LocalDay;
                        return localDay >= 1 &&
                               (localDay >= 5 || !completedDays.Contains(day));
                    case EndingRouteCampaignPhase.AllBranchesFailed:
                        return CanOpenLastBunkerEvent();
                    default:
                        return false;
                }
            }
        }

        public bool ShouldShowEventMarker =>
            CanOpenEvent && !viewedDays.Contains(CurrentDay);

        public EndingRouteCampaignPhase CampaignPhase =>
            campaign?.CampaignState.Phase ?? EndingRouteCampaignPhase.NotStarted;

        public bool IsQuarter3SignalStoryActive =>
            CampaignPhase == EndingRouteCampaignPhase.Quarter3Running &&
            campaign != null &&
            campaign.Quarter3CurrentRoute == BranchRoute.Signal;

        public bool IsQuarter3SignalStoryPendingBeforeExploration
        {
            get
            {
                ResolveReferences();
                CreateCampaignIfNeeded();
                return campaign.CampaignState.Phase ==
                           EndingRouteCampaignPhase.Quarter3Running &&
                       campaign.Quarter3CurrentRoute == BranchRoute.Signal &&
                       Quarter3LocalDay is >= 1 and <= 4 &&
                       !completedDays.Contains(CurrentDay);
            }
        }

        public bool IsQuarter3JoinStoryPending
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.CampaignState.Phase ==
                           EndingRouteCampaignPhase.Quarter3Running &&
                       campaign.Quarter3CurrentRoute == BranchRoute.Join &&
                       Quarter3LocalDay >= 1 &&
                       (Quarter3LocalDay >= 5
                           ? !IsQuarter3FinalChoiceReady
                           : !joinStoryViewedDays.Contains(CurrentDay));
            }
        }

        public bool IsQuarter3StoryPendingBeforeSleep
        {
            get
            {
                ResolveReferences();
                CreateCampaignIfNeeded();
                if (campaign.CampaignState.Phase !=
                        EndingRouteCampaignPhase.Quarter3Running ||
                    Quarter3LocalDay < 1 ||
                    Quarter3LocalDay > 4)
                {
                    return false;
                }

                return campaign.Quarter3CurrentRoute switch
                {
                    BranchRoute.Signal => !completedDays.Contains(CurrentDay),
                    BranchRoute.Join => !joinStoryViewedDays.Contains(CurrentDay),
                    _ => false
                };
            }
        }

        public bool IsQuarter3ClueActionPendingBeforeSleep
        {
            get
            {
                ResolveReferences();
                CreateCampaignIfNeeded();
                if (campaign.CampaignState.Phase !=
                        EndingRouteCampaignPhase.Quarter3Running ||
                    Quarter3LocalDay < 1 ||
                    Quarter3LocalDay > 4 ||
                    IsQuarter3StoryPendingBeforeSleep)
                {
                    return false;
                }

                return campaign.Quarter3CurrentRoute switch
                {
                    BranchRoute.Signal =>
                        explorationManager == null ||
                        !explorationManager.HasExploredToday,
                    BranchRoute.Join =>
                        campaign.GetQuarter3JoinSupportDayStatus(
                            Quarter3LocalDay) ==
                        Quarter3JoinSupportDayStatus.Open,
                    _ => false
                };
            }
        }

        public bool TryGetLastBunkerSleepDelayReason(out string reason)
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            reason = string.Empty;

            if (campaign.CampaignState.Phase !=
                EndingRouteCampaignPhase.AllBranchesFailed)
            {
                return false;
            }

            EnsureLastBunkerStartDay();
            InitializeLastBunkerStoryProgressIfNeeded();
            int localDay = LastBunkerLocalDay;
            if (localDay is < 1 or > 9)
            {
                return false;
            }

            RecordCurrentLastBunkerDiaryUnlock();
            bool hasDiary =
                lastBunkerDiaryUnlockedDays.Contains(localDay);
            bool hasFixedEvent =
                IsLastBunkerFixedEventCompleted(localDay);
            if (hasDiary && hasFixedEvent)
            {
                return false;
            }

            if (!hasDiary && !hasFixedEvent)
            {
                reason =
                    "오늘의 일기를 생성하고 문 앞의 고정 이벤트를 완료해야 다음 이야기가 진행됩니다.";
            }
            else if (!hasDiary)
            {
                reason =
                    "탐사를 통해 오늘의 일기를 생성해야 다음 이야기가 진행됩니다.";
            }
            else
            {
                reason =
                    "문 앞의 고정 이벤트를 완료해야 다음 이야기가 진행됩니다.";
            }

            return true;
        }

        public void PrepareLastBunkerSleep()
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            lastBunkerStoryAdvancePending = false;
            if (campaign.CampaignState.Phase !=
                EndingRouteCampaignPhase.AllBranchesFailed)
            {
                return;
            }

            EnsureLastBunkerStartDay();
            InitializeLastBunkerStoryProgressIfNeeded();
            int localDay = LastBunkerLocalDay;
            if (localDay is < 1 or > 9)
            {
                return;
            }

            RecordCurrentLastBunkerDiaryUnlock();
            lastBunkerStoryAdvancePending =
                lastBunkerDiaryUnlockedDays.Contains(localDay) &&
                IsLastBunkerFixedEventCompleted(localDay);
        }

        public bool IsReadyForLastBunkerEndingSleep
        {
            get
            {
                ResolveReferences();
                CreateCampaignIfNeeded();
                EnsureLastBunkerStartDay();
                InitializeLastBunkerStoryProgressIfNeeded();
                return campaign.CampaignState.Phase ==
                           EndingRouteCampaignPhase.AllBranchesFailed &&
                       LastBunkerLocalDay == 9 &&
                       lastBunkerDiaryUnlockedDays.Contains(9) &&
                       IsLastBunkerFixedEventCompleted(9);
            }
        }

        public bool IsQuarter3FinalChoiceDay
        {
            get
            {
                CreateCampaignIfNeeded();
                return campaign.CampaignState.Phase ==
                           EndingRouteCampaignPhase.Quarter3Running &&
                       Quarter3LocalDay >= 5;
            }
        }

        public bool IsQuarter3FinalChoiceReady
        {
            get
            {
                CreateCampaignIfNeeded();
                return IsQuarter3FinalChoiceDay &&
                       (quarter3FinalStoryAcknowledged ||
                        campaign.Quarter3IsFinalChoiceOpen);
            }
        }

        public bool TryShowQuarter3FinalSleepBlockDialogue()
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            if (!IsQuarter3FinalChoiceDay)
            {
                return false;
            }

            const string body = "더 이상 지체할 시간이 없어.";
            if (dialogueUI == null)
            {
                ShowMessage(body);
                return true;
            }

            pendingChoiceKind = PendingChoiceKind.Quarter3FinalSleepBlock;
            dialogueUI.ShowChoice(
                CurrentDay,
                GameSession.CurrentPlayerName,
                "혼잣말",
                body,
                Quarter3FinalSleepBlockChoice,
                "확인",
                string.Empty,
                string.Empty,
                protagonistPortrait,
                null);
            return true;
        }

        private int CurrentDay => dayCycle != null ? dayCycle.CurrentDay : 1;
        private int LastBunkerLocalDay
        {
            get
            {
                if (lastBunkerStartDay < 0 ||
                    CurrentDay < lastBunkerStartDay)
                {
                    return 0;
                }

                return lastBunkerStoryDay > 0
                    ? lastBunkerStoryDay
                    : Mathf.Max(1, CurrentDay - lastBunkerStartDay + 1);
            }
        }
        private int Quarter3LocalDay
        {
            get
            {
                if (quarter3StartDay < 0)
                {
                    return 0;
                }

                HashSet<int> completedStoryDays =
                    campaign != null &&
                    campaign.Quarter3CurrentRoute == BranchRoute.Join
                        ? joinStoryViewedDays
                        : completedDays;
                int completedStoryCount = 0;
                foreach (int completedDay in completedStoryDays)
                {
                    if (completedDay >= quarter3StartDay &&
                        completedDay < CurrentDay)
                    {
                        completedStoryCount++;
                    }
                }

                return Mathf.Clamp(1 + completedStoryCount, 1, 5);
            }
        }

        private void Awake()
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            BindDialogue();
            BindExplorationReturn();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindDialogue();
            BindExplorationReturn();
            if (dayCycle != null)
            {
                dayCycle.DayChanged -= HandleDayChanged;
                dayCycle.DayChanged += HandleDayChanged;
            }
        }

        private void Start()
        {
            CreateCampaignIfNeeded();
            SyncCampaignToCurrentDay();
            BindExplorationReturn();
            TryShowQuarter2SignalMorningDialogue();
        }

        private void OnDisable()
        {
            if (dayCycle != null)
            {
                dayCycle.DayChanged -= HandleDayChanged;
            }

            if (subscribedExplorationManager != null)
            {
                subscribedExplorationManager.ReturnedToShelter -=
                    HandleExplorationReturnedToShelter;
                subscribedExplorationManager = null;
            }
        }

        private void OnDestroy()
        {
            if (dialogueUI != null)
            {
                dialogueUI.ChoiceRequested -= HandleChoiceRequested;
                dialogueUI.Closed -= HandleDayOneTutorialMonologueClosed;
            }
        }

        public bool ShowProtagonistMonologue(
            string body,
            Action onCompleted = null,
            string continueLabel = "확인")
        {
            ResolveReferences();
            if (dialogueUI == null || dialogueUI.IsOpen)
            {
                return false;
            }

            dayOneTutorialMonologueCompleted = onCompleted;
            pendingChoiceKind = PendingChoiceKind.DayOneTutorialMonologue;
            dialogueUI.Closed -= HandleDayOneTutorialMonologueClosed;
            dialogueUI.Closed += HandleDayOneTutorialMonologueClosed;
            dialogueUI.ShowChoice(
                CurrentDay,
                GameSession.CurrentPlayerName,
                GameSession.CurrentPlayerName,
                body,
                DayOneTutorialMonologueChoice,
                continueLabel,
                string.Empty,
                string.Empty,
                protagonistPortrait,
                null);
            return true;
        }

        public void Configure(
            DayCycleManager cycle,
            UIManager ui,
            ShelterEventDialogueUI dialogue,
            Texture2D protagonist,
            Texture2D radio,
            Texture2D raiderGroup,
            Texture2D survivorGroup,
            Texture2D armedGroup,
            Texture2D militaryRescue,
            CharacterStats stats)
        {
            dayCycle = cycle;
            uiManager = ui;
            dialogueUI = dialogue;
            protagonistPortrait = protagonist;
            radioPortrait = radio;
            raiderGroupPortrait = raiderGroup;
            survivorGroupPortrait = survivorGroup;
            armedGroupPortrait = armedGroup;
            militaryRescuePortrait = militaryRescue;
            characterStats = stats;
            BindDialogue();
        }

        public bool ShowLastSurvivalEndingDialogue(
            string body,
            Action onCompleted)
        {
            ResolveReferences();
            if (dialogueUI == null)
            {
                return false;
            }

            lastSurvivalEndingDialogueCompleted = onCompleted;
            pendingChoiceKind = PendingChoiceKind.LastSurvivalEndingDialogue;
            dialogueUI.ShowChoice(
                CurrentDay,
                GameSession.CurrentPlayerName,
                GameSession.CurrentPlayerName,
                body,
                LastSurvivalEndingDialogueChoice,
                "다음",
                string.Empty,
                string.Empty,
                protagonistPortrait,
                null);
            return true;
        }

        public void TryOpenTodaysEvent()
        {
            ResolveReferences();
            if (pendingChoiceKind == PendingChoiceKind.Quarter3RouteGuide)
            {
                AcknowledgeQuarter3RouteGuide(Quarter3RouteGuideChoice);
            }

            CreateCampaignIfNeeded();
            SyncCampaignToCurrentDay();

            if (CurrentDay < settings.StartDay)
            {
                ShowMessage($"{settings.StartDay}일차부터 문 앞 이벤트가 발생합니다.");
                return;
            }

            if (completedDays.Contains(CurrentDay))
            {
                ShowMessage(IsQuarter3SignalStoryActive
                    ? "오늘의 스토리는 이미 확인했습니다."
                    : "오늘 문 앞 이벤트는 이미 확인했습니다.");
                return;
            }

            switch (campaign.CampaignState.Phase)
            {
                case EndingRouteCampaignPhase.Quarter1Running:
                    OpenQuarter1Event();
                    break;
                case EndingRouteCampaignPhase.Quarter2Running:
                    OpenQuarter2Event();
                    break;
                case EndingRouteCampaignPhase.Quarter2Passed:
                    ShowMessage("다음 날부터 마지막 분기의 단서를 확인할 수 있습니다.");
                    break;
                case EndingRouteCampaignPhase.AllBranchesFailed:
                    OpenLastBunkerEvent();
                    break;
                case EndingRouteCampaignPhase.Quarter3Running:
                    OpenQuarter3Event();
                    break;
                case EndingRouteCampaignPhase.Quarter3Resolved:
                    ShowMessage("최종 선택은 이미 기록되었습니다.");
                    break;
                default:
                    ShowMessage("오늘은 문 앞에서 진행할 이벤트가 없습니다.");
                    break;
            }
        }

        public IReadOnlyList<string> GetEventDiaryRecords()
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            List<string> records = new();
            int currentDay = CurrentDay;
            if (campaign.Quarter1Controller.DiaryRepository.TryGetRecord(
                    currentDay,
                    out DiaryDayRecord record))
            {
                foreach (string entry in record.EventRecords)
                {
                    records.Add($"{currentDay}일차 - {entry}");
                }
            }

            string currentDayPrefix = $"{currentDay}일차 -";
            foreach (string entry in campaignRecords)
            {
                if (entry.StartsWith(currentDayPrefix, StringComparison.Ordinal) &&
                    !IsCurrentStoryDiaryRecord(entry))
                {
                    records.Add(entry);
                }
            }

            return records.AsReadOnly();
        }

        public IReadOnlyList<string> GetTodayDiaryRecords()
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            List<string> records = new();
            int currentDay = CurrentDay;

            if (TryGetCurrentCommonStoryTitle(out string commonStoryTitle))
            {
                string storyPrefix =
                    $"{currentDay}일차 - {commonStoryTitle}";
                foreach (string entry in campaignRecords)
                {
                    if (entry.StartsWith(
                            storyPrefix,
                            StringComparison.Ordinal))
                    {
                        records.Add(entry);
                        break;
                    }
                }
            }

            if (campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.AllBranchesFailed &&
                IsLastBunkerDiaryUnlocked(LastBunkerLocalDay) &&
                ShelterCampaignCatalog.TryGetLastBunkerIsolationEvent(
                    LastBunkerLocalDay,
                    out BranchEventDefinition isolationEvent,
                    out string todayDiary))
            {
                if (!string.IsNullOrWhiteSpace(todayDiary))
                {
                    records.Add(
                        $"{currentDay}일차 - {isolationEvent.Title}\n\n" +
                        todayDiary);
                }
            }

            if (campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.AllBranchesFailed &&
                IsLastBunkerDiaryUnlocked(LastBunkerLocalDay) &&
                ShelterCampaignCatalog.TryGetLastBunkerDiary(
                    LastBunkerLocalDay,
                    out _,
                    out string lastBunkerTitle,
                    out string lastBunkerBody,
                    out _))
            {
                records.Add(
                    $"{currentDay}일차 - {lastBunkerTitle}\n\n" +
                    lastBunkerBody);
            }

            return records.AsReadOnly();
        }

        public bool TryGetLastBunkerExplorationResultAppend(
            out string resultAppend)
        {
            CreateCampaignIfNeeded();
            resultAppend = string.Empty;
            if (campaign.CampaignState.Phase !=
                EndingRouteCampaignPhase.AllBranchesFailed)
            {
                return false;
            }

            if (lastBunkerDiaryUnlockedDays.Contains(LastBunkerLocalDay))
            {
                return false;
            }

            if (ShelterCampaignCatalog.TryGetLastBunkerDiary(
                    LastBunkerLocalDay,
                    out _,
                    out _,
                    out _,
                    out resultAppend) &&
                !string.IsNullOrWhiteSpace(resultAppend))
            {
                return true;
            }

            return false;
        }

        public IReadOnlyList<Quarter3ClueDefinition> GetQuarter3AcquiredClues()
        {
            CreateCampaignIfNeeded();
            return campaign.GetQuarter3AcquiredClues();
        }

        public bool TryGetQuarter3SignalExplorationProgress(
            string locationName,
            out Quarter3SourceProgress progress)
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            progress = new Quarter3SourceProgress(string.Empty, 0, 0);

            if (campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter3Running ||
                campaign.Quarter3CurrentRoute != BranchRoute.Signal ||
                Quarter3LocalDay is < 1 or > 4 ||
                !ShelterCampaignCatalog.TryGetSignalExplorationSource(
                    locationName,
                    out string sourceId))
            {
                return false;
            }

            progress = campaign.Quarter3Controller.GetSourceProgress(sourceId);
            return progress.TotalCount > 0;
        }

        public bool TryAcquireQuarter3SignalExplorationClue(
            string locationName,
            out string acquiredTitle)
        {
            acquiredTitle = string.Empty;
            if (!TryGetQuarter3SignalExplorationProgress(
                    locationName,
                    out Quarter3SourceProgress progress) ||
                progress.AcquiredCount >= progress.TotalCount ||
                !ShelterCampaignCatalog.TryGetSignalExplorationSource(
                    locationName,
                    out string sourceId))
            {
                return false;
            }

            IReadOnlyList<Quarter3ClueDefinition> available =
                campaign.Quarter3Controller.GetAvailableClues(sourceId);
            if (available.Count == 0)
            {
                return false;
            }

            Quarter3ClueDefinition definition =
                available[UnityEngine.Random.Range(0, available.Count)];
            Quarter3ClueAcquisitionResult result =
                campaign.AcquireQuarter3Clue(definition.ClueId);
            if (!result.IsSuccess)
            {
                Debug.LogWarning(result.FailureReason);
                return false;
            }

            acquiredTitle = definition.Title;
            return true;
        }

        private bool IsCurrentStoryDiaryRecord(string entry)
        {
            if (TryGetCurrentCommonStoryTitle(out string title) &&
                entry.StartsWith(
                    $"{CurrentDay}일차 - {title}",
                    StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        private bool TryGetCurrentCommonStoryTitle(out string title)
        {
            title = string.Empty;
            if (campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter3Running ||
                Quarter3LocalDay is < 1 or > 5)
            {
                return false;
            }

            return campaign.Quarter3CurrentRoute switch
            {
                BranchRoute.Signal =>
                    ShelterCampaignCatalog.TryGetQuarter3SignalStory(
                        Quarter3LocalDay,
                        out title,
                        out _),
                BranchRoute.Join =>
                    ShelterCampaignCatalog.TryGetQuarter3JoinCommonStory(
                        Quarter3LocalDay,
                        out title,
                        out _),
                _ => false
            };
        }

        public CampaignSaveData CaptureState()
        {
            CreateCampaignIfNeeded();
            return new CampaignSaveData
            {
                campaignPhase = (int)campaign.CampaignState.Phase,
                quarter1Route = (int)campaign.CampaignState.Quarter1Route,
                quarter2CurrentRoute = (int)campaign.CampaignState.Quarter2CurrentRoute,
                quarter2PassedRoute = (int)campaign.CampaignState.Quarter2PassedRoute,
                quarter3FinalChoiceId = campaign.CampaignState.Quarter3FinalChoiceId,
                allBranchesFailed = campaign.CampaignState.AreAllBranchesFailed,
                quarter1 = campaign.Quarter1Controller.CaptureState(),
                quarter2 = campaign.Quarter2Controller.RuntimeState.CaptureState(),
                quarter3 = campaign.Quarter3Controller.RuntimeState.CaptureState(),
                joinSupport = campaign.Quarter3JoinSupportController?.CaptureState(),
                completedDays = new List<int>(completedDays).ToArray(),
                viewedDays = new List<int>(viewedDays).ToArray(),
                joinStoryViewedDays = new List<int>(joinStoryViewedDays).ToArray(),
                campaignRecords = campaignRecords.ToArray(),
                lastBunkerEventOffset = lastBunkerEventOffset,
                lastBunkerStartDay = lastBunkerStartDay,
                usesLastBunkerStoryProgress = true,
                lastBunkerStoryDay = lastBunkerStoryDay,
                lastBunkerDiaryUnlockedDays =
                    new List<int>(lastBunkerDiaryUnlockedDays).ToArray(),
                lastBunkerFixedEventCompletedDays =
                    new List<int>(lastBunkerFixedEventCompletedDays).ToArray(),
                lastBunkerIntroAcknowledged =
                    lastBunkerIntroAcknowledged,
                quarter2LastChoiceDay = quarter2LastChoiceDay,
                quarter3StartDay = quarter3StartDay,
                quarter3StartNoticeAcknowledged =
                    quarter3StartNoticeAcknowledged,
                quarter3RouteGuideAcknowledged =
                    quarter3RouteGuideAcknowledged,
                quarter3FinalStoryAcknowledged =
                    quarter3FinalStoryAcknowledged,
                quarter3FinalMorningDialogueShown =
                    quarter3FinalMorningDialogueShown,
                quarter2SignalMorningDialogueAcknowledged =
                    IsQuarter2SignalDialogueAcknowledged(1),
                quarter2SignalDialogueAcknowledgedMask =
                    quarter2SignalDialogueAcknowledgedMask,
                quarter2JoinReturnDialogueAcknowledged =
                    IsQuarter2JoinReturnDialogueAcknowledged(1),
                quarter2JoinReturnDialogueAcknowledgedMask =
                    quarter2JoinReturnDialogueAcknowledgedMask,
                lastQuarter1ScoreChange = lastQuarter1ScoreChange,
                lastQuarter2DecisionResult = lastQuarter2DecisionResult
            };
        }

        public void RestoreState(CampaignSaveData data)
        {
            if (data == null) return;

            ResolveReferences();
            CreateCampaignIfNeeded();
            campaign.ResetCampaign();
            campaign.Quarter1Controller.RestoreState(data.quarter1);
            campaign.Quarter2Controller.RuntimeState.Restore(data.quarter2);
            campaign.Quarter3Controller.RuntimeState.Restore(data.quarter3);
            campaign.Quarter3JoinSupportController?.RestoreState(data.joinSupport);
            campaign.CampaignState.Restore(data);

            lastBunkerEventOffset = Mathf.Max(0, data.lastBunkerEventOffset);
            if (data.quarter1?.selectedEventIds is { Length: > 0 })
            {
                ConfigureLastBunkerEvents(
                    data.quarter1.selectedEventIds,
                    false);
            }

            completedDays.Clear();
            if (data.completedDays != null)
            {
                foreach (int day in data.completedDays)
                    completedDays.Add(day);
            }

            viewedDays.Clear();
            if (data.viewedDays != null)
            {
                foreach (int day in data.viewedDays)
                    viewedDays.Add(day);
            }

            joinStoryViewedDays.Clear();
            if (data.joinStoryViewedDays != null)
            {
                foreach (int day in data.joinStoryViewedDays)
                    joinStoryViewedDays.Add(day);
            }

            campaignRecords.Clear();
            if (data.campaignRecords != null)
            {
                foreach (string record in data.campaignRecords)
                {
                    campaignRecords.Add(
                        NormalizeQuarter3DiaryTitle(record));
                }
            }

            lastBunkerStartDay = data.lastBunkerStartDay;
            lastBunkerStoryDay = data.usesLastBunkerStoryProgress
                ? Mathf.Max(0, data.lastBunkerStoryDay)
                : 0;
            lastBunkerDiaryUnlockedDays.Clear();
            if (data.lastBunkerDiaryUnlockedDays != null)
            {
                foreach (int localDay in data.lastBunkerDiaryUnlockedDays)
                {
                    if (localDay > 0)
                    {
                        lastBunkerDiaryUnlockedDays.Add(localDay);
                    }
                }
            }

            lastBunkerFixedEventCompletedDays.Clear();
            if (data.lastBunkerFixedEventCompletedDays != null)
            {
                foreach (int localDay in
                         data.lastBunkerFixedEventCompletedDays)
                {
                    if (localDay > 0)
                    {
                        lastBunkerFixedEventCompletedDays.Add(localDay);
                    }
                }
            }

            lastBunkerStoryAdvancePending = false;
            lastBunkerIntroAcknowledged =
                data.lastBunkerIntroAcknowledged;
            lastBunkerIntroPresented = false;
            quarter2LastChoiceDay = data.quarter2LastChoiceDay;
            quarter3StartDay = data.quarter3StartDay;
            quarter3StartNoticeAcknowledged =
                data.quarter3StartNoticeAcknowledged;
            quarter3StartNoticePresented =
                quarter3StartNoticeAcknowledged;
            quarter3RouteGuideAcknowledged =
                data.quarter3RouteGuideAcknowledged;
            quarter3RouteGuidePresented = false;
            quarter3FinalStoryAcknowledged =
                data.quarter3FinalStoryAcknowledged ||
                campaign.Quarter3IsFinalChoiceOpen ||
                HasLegacyQuarter3FinalStoryAcknowledgement();
            quarter3FinalMorningDialogueShown =
                data.quarter3FinalMorningDialogueShown;
            quarter2SignalDialogueAcknowledgedMask =
                data.quarter2SignalDialogueAcknowledgedMask;
            if (data.quarter2SignalMorningDialogueAcknowledged)
            {
                quarter2SignalDialogueAcknowledgedMask |= 1;
            }
            pendingQuarter2SignalDialogueOrder = -1;
            pendingQuarter2SignalDialogueIndex = -1;
            quarter2JoinReturnDialogueAcknowledgedMask =
                data.quarter2JoinReturnDialogueAcknowledgedMask;
            if (data.quarter2JoinReturnDialogueAcknowledged)
            {
                quarter2JoinReturnDialogueAcknowledgedMask |= 1;
            }
            pendingQuarter2JoinReturnDialogueOrder = -1;
            pendingQuarter2JoinReturnDialogueIndex = -1;
            lastQuarter1ScoreChange = data.lastQuarter1ScoreChange ?? string.Empty;
            lastQuarter2DecisionResult = data.lastQuarter2DecisionResult ?? string.Empty;
            pendingChoiceKind = PendingChoiceKind.None;
            pendingQuarter1Event = null;
            ClearQuarter3SignalIntro();
            ClearQuarter3JoinIntro();
            pendingMorningDialogueLines.Clear();
            dialogueUI?.Close();
        }

        private static string NormalizeQuarter3DiaryTitle(string record)
        {
            if (string.IsNullOrEmpty(record))
            {
                return record;
            }

            for (int localDay = 1; localDay <= 5; localDay++)
            {
                string prefix = $"3분기 {localDay}일 차 — ";
                int prefixIndex = record.IndexOf(
                    prefix,
                    StringComparison.Ordinal);
                if (prefixIndex >= 0)
                {
                    return record.Remove(prefixIndex, prefix.Length);
                }
            }

            return record;
        }

        public bool SkipQuarter1ForTesting()
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            if (campaign.CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter1Running)
            {
                ShowMessage("[테스트] 현재는 1분기를 스킵할 수 없습니다.");
                return false;
            }

            dialogueUI?.Close();
            BranchOneFlowController flow = campaign.Quarter1Controller;
            while (flow.RuntimeState.CurrentDay <= settings.LastEventDay)
            {
                if (flow.RuntimeState.IsTodayEventCompleted)
                {
                    if (!flow.AdvanceDay())
                    {
                        ShowMessage(
                            $"[테스트] 1분기 날짜 진행 실패: {flow.LastFailureReason}");
                        return false;
                    }

                    continue;
                }

                IReadOnlyList<BranchEventDefinition> available =
                    flow.GetAvailableEventsForCurrentDay();
                if (available.Count == 0)
                {
                    ShowMessage("[테스트] 자동 진행할 1분기 이벤트가 없습니다.");
                    return false;
                }

                BranchEventDefinition definition =
                    available[UnityEngine.Random.Range(0, available.Count)];
                int choiceIndex = UnityEngine.Random.Range(0, definition.Choices.Count);
                if (flow.RuntimeState.CurrentDay == settings.LastEventDay &&
                    flow.ScoreState.SignalScore + flow.ScoreState.JoinScore == 0)
                {
                    choiceIndex = 0;
                }

                BranchEventChoice choice = definition.Choices[choiceIndex];
                if (!ShelterQuarter1ChoiceEffects.CanApply(
                        definition,
                        choice,
                        resources,
                        out _))
                {
                    choice = definition.Choices[choiceIndex == 0 ? 1 : 0];
                }

                BranchEventSelectionResult selection =
                    flow.SelectEvent(definition.EventId, choice.ChoiceId);
                if (!selection.IsSuccess)
                {
                    ShowMessage($"[테스트] 1분기 자동 선택 실패: {selection.FailureReason}");
                    return false;
                }

                Quarter1ChoiceEffectResult effect =
                    ShelterQuarter1ChoiceEffects.Apply(
                        definition,
                        selection.Choice,
                        characterStats,
                        resources);
                string diaryText =
                    $"{definition.Title}\n\n{effect.BuildDiaryText()}";
                flow.DiaryRepository.ReplaceLastEventRecord(
                    flow.RuntimeState.CurrentDay,
                    diaryText);
                UpdateLastQuarter1ScoreChange(definition, selection);

                if (!flow.AdvanceDay())
                {
                    ShowMessage($"[테스트] 1분기 날짜 진행 실패: {flow.LastFailureReason}");
                    return false;
                }
            }

            BranchDecisionResult decision =
                flow.ResolveDecision(UnityEngine.Random.value);
            if (!decision.IsSuccess)
            {
                ShowMessage($"[테스트] 1분기 판정 실패: {decision.FailureReason}");
                return false;
            }

            campaign.SynchronizeState();
            EndingRouteCampaignTransitionResult transition =
                campaign.AdvanceToQuarter2();
            if (!transition.IsSuccess)
            {
                ShowMessage($"[테스트] 2분기 시작 실패: {transition.FailureReason}");
                return false;
            }

            completedDays.Clear();
            viewedDays.Clear();
            joinStoryViewedDays.Clear();
            pendingChoiceKind = PendingChoiceKind.None;
            pendingQuarter1Event = null;
            quarter2LastChoiceDay = -1;
            dayCycle?.SetDayForTesting(settings.DecisionDay);
            ShowMessage("[테스트] 1분기를 자동 진행하고 2분기를 시작했습니다.");
            return true;
        }

        public bool SkipToQuarter3ForTesting(BranchRoute route)
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            if (route != BranchRoute.Signal && route != BranchRoute.Join)
            {
                ShowMessage("[테스트] 3분기로 이동할 계열이 올바르지 않습니다.");
                return false;
            }

            int startDay = dayCycle != null
                ? settings.DecisionDay + 3
                : CurrentDay;
            RouteProgressSaveData passedProgress = new()
            {
                progressCount = 3,
                attempted = true,
                completedEventCount = 3
            };
            CampaignSaveData testState = new()
            {
                campaignPhase =
                    (int)EndingRouteCampaignPhase.Quarter3Running,
                quarter1Route = (int)route,
                quarter2CurrentRoute = (int)route,
                quarter2PassedRoute = (int)route,
                quarter1 = new Quarter1SaveData
                {
                    currentDay = settings.DecisionDay,
                    phase = (int)BranchFlowPhase.Finished,
                    decisionCompleted = true,
                    decidedRoute = (int)route,
                    signalScore = route == BranchRoute.Signal ? 1 : 0,
                    joinScore = route == BranchRoute.Join ? 1 : 0
                },
                quarter2 = new Quarter2SaveData
                {
                    phase = (int)Quarter2FlowPhase.Quarter2Passed,
                    initialRoute = (int)route,
                    currentRoute = (int)route,
                    currentEventOrder = 3,
                    signal = route == BranchRoute.Signal
                        ? passedProgress
                        : new RouteProgressSaveData(),
                    join = route == BranchRoute.Join
                        ? passedProgress
                        : new RouteProgressSaveData(),
                    passedRoute = (int)route
                },
                quarter3 = new Quarter3SaveData
                {
                    phase = (int)Quarter3FlowPhase.CollectingClues,
                    currentRoute = (int)route
                },
                quarter3StartDay = startDay,
                lastQuarter1ScoreChange =
                    "[테스트] 3분기 직행을 위해 판정을 완료했습니다.",
                lastQuarter2DecisionResult =
                    "[테스트] 3분기 직행을 위해 계열을 통과했습니다."
            };

            RestoreState(testState);
            dayCycle?.SetDayForTesting(startDay);
            TryShowQuarter3StartNotice();

            string routeName = route == BranchRoute.Signal
                ? "구조신호"
                : "합류";
            bool succeeded =
                campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.Quarter3Running &&
                campaign.Quarter3CurrentRoute == route &&
                Quarter3LocalDay == 1;
            ShowMessage(succeeded
                ? $"[테스트] 3분기 {routeName} 계열 1일 차로 이동했습니다."
                : $"[테스트] 3분기 {routeName} 계열 이동에 실패했습니다.");
            return succeeded;
        }

        public bool SkipToQuarter3EndingEveForTesting(BranchRoute route)
        {
            ResolveReferences();
            CreateCampaignIfNeeded();
            if (route != BranchRoute.Signal && route != BranchRoute.Join)
            {
                ShowMessage("[테스트] 엔딩 계열이 올바르지 않습니다.");
                return false;
            }

            int startDay = dayCycle != null
                ? settings.DecisionDay + 3
                : CurrentDay;
            int endingEveDay = startDay + 3;
            RouteProgressSaveData passedProgress = new()
            {
                progressCount = 3,
                attempted = true,
                completedEventCount = 3
            };
            JoinSupportRecordSaveData[] supportRecords =
                route == BranchRoute.Join
                    ? new JoinSupportRecordSaveData[4]
                    : Array.Empty<JoinSupportRecordSaveData>();
            string[] acquiredClueIds = new string[4];
            int[] completedStoryDays = new int[4];
            for (int localDay = 1; localDay <= 4; localDay++)
            {
                Quarter3JoinSupportTarget joinTarget = localDay % 2 == 1
                    ? Quarter3JoinSupportTarget.SurvivorGroup
                    : Quarter3JoinSupportTarget.RedArmband;
                string clueId = route == BranchRoute.Signal
                    ? ShelterCampaignCatalog.SignalClueId(localDay)
                    : ShelterCampaignCatalog.JoinClueId(
                        localDay,
                        joinTarget);
                acquiredClueIds[localDay - 1] = clueId;
                completedStoryDays[localDay - 1] =
                    startDay + localDay - 1;
                if (route == BranchRoute.Join)
                {
                    supportRecords[localDay - 1] =
                        new JoinSupportRecordSaveData
                    {
                        day = localDay,
                        outcome =
                            (int)Quarter3JoinSupportOutcome.Supported,
                        hasTarget = true,
                        target = (int)joinTarget,
                        hasResource = true,
                        resource =
                            (int)Quarter3JoinSupportResource.CannedFood,
                        clueId = clueId,
                        sourceId = ShelterCampaignCatalog.JoinSourceId(
                            localDay,
                            joinTarget)
                    };
                }
            }

            CampaignSaveData testState = new()
            {
                campaignPhase =
                    (int)EndingRouteCampaignPhase.Quarter3Running,
                quarter1Route = (int)route,
                quarter2CurrentRoute = (int)route,
                quarter2PassedRoute = (int)route,
                quarter1 = new Quarter1SaveData
                {
                    currentDay = settings.DecisionDay,
                    phase = (int)BranchFlowPhase.Finished,
                    decisionCompleted = true,
                    decidedRoute = (int)route,
                    signalScore = route == BranchRoute.Signal ? 1 : 0,
                    joinScore = route == BranchRoute.Join ? 1 : 0
                },
                quarter2 = new Quarter2SaveData
                {
                    phase = (int)Quarter2FlowPhase.Quarter2Passed,
                    initialRoute = (int)route,
                    currentRoute = (int)route,
                    currentEventOrder = 3,
                    signal = route == BranchRoute.Signal
                        ? passedProgress
                        : new RouteProgressSaveData(),
                    join = route == BranchRoute.Join
                        ? passedProgress
                        : new RouteProgressSaveData(),
                    passedRoute = (int)route
                },
                quarter3 = new Quarter3SaveData
                {
                    phase = (int)Quarter3FlowPhase.CollectingClues,
                    currentRoute = (int)route,
                    acquiredClueIds = acquiredClueIds
                },
                joinSupport = new JoinSupportSaveData
                {
                    records = supportRecords
                },
                completedDays = route == BranchRoute.Signal
                    ? completedStoryDays
                    : Array.Empty<int>(),
                joinStoryViewedDays = route == BranchRoute.Join
                    ? completedStoryDays
                    : Array.Empty<int>(),
                quarter3StartDay = startDay,
                quarter3StartNoticeAcknowledged = true,
                quarter3RouteGuideAcknowledged = true,
                lastQuarter1ScoreChange =
                    $"[테스트] {route} 계열 진입을 위해 판정을 완료했습니다.",
                lastQuarter2DecisionResult =
                    $"[테스트] {route} 계열을 통과했습니다."
            };

            RestoreState(testState);
            dayCycle?.SetDayForTesting(endingEveDay);
            if (route == BranchRoute.Signal && explorationManager != null)
            {
                explorationManager.RestoreState(new ExplorationSaveData
                {
                    lastExplorationDay = endingEveDay,
                    history = new[]
                    {
                        "[임시 테스트] 신호계열 엔딩 전날 탐사를 완료했습니다."
                    }
                });
            }
            pendingChoiceKind = PendingChoiceKind.None;
            pendingJoinSupportDay = -1;
            ClearQuarter3SignalIntro();
            ClearQuarter3JoinIntro();
            dialogueUI?.Close();

            bool succeeded =
                campaign.CampaignState.Phase ==
                    EndingRouteCampaignPhase.Quarter3Running &&
                campaign.Quarter3CurrentRoute == route &&
                Quarter3LocalDay == 4 &&
                !IsQuarter3StoryPendingBeforeSleep &&
                !IsQuarter3ClueActionPendingBeforeSleep;
            string routeName = route == BranchRoute.Signal
                ? "신호계열"
                : "합류계열";
            ShowMessage(succeeded
                ? $"[임시 테스트] {routeName} 4일 차 밤입니다. 침대에서 잠들면 두 엔딩 중 하나를 고르는 최종 선택일이 시작됩니다."
                : $"[테스트] {routeName} 엔딩 전날 밤 이동에 실패했습니다.");
            return succeeded;
        }

        public bool SkipToLastBunkerForTesting()
        {
            ResolveReferences();
            CreateCampaignIfNeeded();

            // 2분기는 1분기 판정일에 시작하며, 두 계열에서 각각 두 번씩
            // 거부한 다음 날이 마지막 벙커 잠복 구간의 첫날이다.
            int startDay = dayCycle != null
                ? settings.DecisionDay + 4
                : CurrentDay;
            BranchRoute initialRoute = BranchRoute.Signal;
            RouteProgressSaveData failedSignal = new()
            {
                rejectCount = 2,
                attempted = true,
                failed = true,
                completedEventCount = 2
            };
            RouteProgressSaveData failedJoin = new()
            {
                rejectCount = 2,
                attempted = true,
                failed = true,
                completedEventCount = 2
            };
            List<string> scheduledEventIds = new();
            foreach (BranchEventDefinition definition in
                     campaign.Quarter1Controller.EventCatalog.GetEvents())
            {
                scheduledEventIds.Add(definition.EventId);
            }

            CampaignSaveData testState = new()
            {
                campaignPhase =
                    (int)EndingRouteCampaignPhase.AllBranchesFailed,
                quarter1Route = (int)initialRoute,
                quarter2CurrentRoute = (int)BranchRoute.Join,
                quarter2PassedRoute = (int)BranchRoute.None,
                allBranchesFailed = true,
                quarter1 = new Quarter1SaveData
                {
                    currentDay = settings.DecisionDay,
                    phase = (int)BranchFlowPhase.Finished,
                    decisionCompleted = true,
                    decidedRoute = (int)initialRoute,
                    signalScore = 1,
                    joinScore = 0,
                    selectedEventIds = scheduledEventIds.ToArray()
                },
                quarter2 = new Quarter2SaveData
                {
                    phase = (int)Quarter2FlowPhase.AllBranchesFailed,
                    initialRoute = (int)initialRoute,
                    currentRoute = (int)BranchRoute.Join,
                    currentEventOrder = 2,
                    signal = failedSignal,
                    join = failedJoin,
                    allBranchesFailed = true,
                    lastDecision = (int)Quarter2Decision.Reject
                },
                lastBunkerStartDay = startDay,
                usesLastBunkerStoryProgress = true,
                lastBunkerStoryDay = 1,
                lastBunkerEventOffset = lastBunkerEventOffset,
                quarter2LastChoiceDay = -1,
                lastQuarter1ScoreChange =
                    "[테스트] 마지막 벙커 진입을 위해 판정을 완료했습니다.",
                lastQuarter2DecisionResult =
                    "[테스트] 두 계열 진입에 모두 실패했습니다."
            };

            RestoreState(testState);
            completedDays.Remove(startDay);
            viewedDays.Remove(startDay);
            pendingChoiceKind = PendingChoiceKind.None;
            pendingQuarter1Event = null;
            dialogueUI?.Close();
            dayCycle?.SetDayForTesting(startDay);
            SyncCampaignToCurrentDay();

            bool succeeded =
                campaign.CampaignState.Phase ==
                EndingRouteCampaignPhase.AllBranchesFailed &&
                lastBunkerStartDay == startDay;
            ShowMessage(succeeded
                ? $"[테스트] {startDay}일차에서 마지막 벙커 잠복 구간에 진입했습니다."
                : "[테스트] 마지막 벙커 진입에 실패했습니다.");
            return succeeded;
        }

        public bool SkipToLastBunkerEndingEveForTesting()
        {
            if (!SkipToLastBunkerForTesting())
                return false;

            lastBunkerStoryDay = 9;
            int endingEveDay = lastBunkerStartDay + 8;
            dayCycle?.SetDayForTesting(endingEveDay);
            lastBunkerDiaryUnlockedDays.Add(9);
            lastBunkerFixedEventCompletedDays.Add(9);
            completedDays.Add(endingEveDay);
            lastBunkerStoryAdvancePending = false;
            dialogueUI?.Close();
            ShowMessage(
                "[임시 테스트] 마지막 벙커 9일차 밤입니다. 침대에서 잠들면 엔딩 사진 연출이 시작됩니다.");
            return IsReadyForLastBunkerEndingSleep;
        }

        private Texture2D ResolveQuarter2PartnerPortrait(
            Quarter2EventDefinition definition)
        {
            if (definition == null || definition.Route != BranchRoute.Join)
            {
                return null;
            }

            DialoguePartner partner = definition.EventId == Quarter2ArmedGroupEvent
                ? DialoguePartner.ArmedGroup
                : DialoguePartner.SurvivorGroup;
            return ResolvePartnerPortrait(partner);
        }

        private Texture2D ResolveJoinTargetPortrait(
            Quarter3JoinSupportTarget target)
        {
            return ResolvePartnerPortrait(
                target == Quarter3JoinSupportTarget.RedArmband
                    ? DialoguePartner.ArmedGroup
                    : DialoguePartner.SurvivorGroup);
        }

        private Texture2D ResolvePartnerPortrait(DialoguePartner partner)
        {
            return partner switch
            {
                DialoguePartner.RaiderGroup => raiderGroupPortrait,
                DialoguePartner.SurvivorGroup => survivorGroupPortrait,
                DialoguePartner.ArmedGroup => armedGroupPortrait,
                DialoguePartner.MilitaryRescue => militaryRescuePortrait,
                _ => null
            };
        }

        private void OpenQuarter1Event()
        {
            int day = CurrentDay;
            if (day > settings.LastEventDay)
            {
                ShowMessage("1분기 선택은 종료되었습니다.");
                return;
            }

            IReadOnlyList<BranchEventDefinition> available =
                campaign.Quarter1Controller.GetAvailableEventsForCurrentDay();
            if (available.Count == 0)
            {
                ShowMessage("오늘 선택할 수 있는 이벤트가 없습니다.");
                return;
            }

            pendingQuarter1Event = available[(day - settings.StartDay) % available.Count];
            pendingChoiceKind = PendingChoiceKind.Quarter1;
            viewedDays.Add(day);
            dialogueUI.ShowEvent(
                pendingQuarter1Event,
                day,
                protagonistPortrait,
                null);
        }

        private void OpenQuarter2Event()
        {
            Quarter2EventDefinition definition = campaign.GetCurrentQuarter2Event();
            if (definition == null)
            {
                ShowMessage("다음 사건은 잠을 자고 다음 날 확인할 수 있습니다.");
                return;
            }

            if (definition.RequiresExploration &&
                (explorationManager == null || !explorationManager.HasExploredToday))
            {
                ShowMessage("오늘 탐사를 마치고 벙커로 돌아온 뒤 확인할 수 있는 사건입니다.");
                return;
            }

            if (ShelterCampaignCatalog.TryGetQuarter2IntroDialogue(
                    definition.EventId,
                    out IReadOnlyList<ShelterIntroDialogueLine> introLines) &&
                introLines.Count > 0)
            {
                pendingQuarter2IntroEvent = definition;
                pendingQuarter2IntroLines = introLines;
                pendingQuarter2IntroIndex =
                    GetQuarter2IntroStartIndex(definition.EventId);
                viewedDays.Add(CurrentDay);
                ShowCurrentQuarter2IntroLine();
                return;
            }

            ShowQuarter2Choice(definition);
        }

        private int GetQuarter2IntroStartIndex(string eventId)
        {
            if (eventId == "Q2_JOIN_EVT_02" &&
                IsQuarter2JoinReturnDialogueAcknowledged(2))
            {
                return 2;
            }

            if (eventId == "Q2_JOIN_EVT_03" &&
                IsQuarter2JoinReturnDialogueAcknowledged(3))
            {
                return 1;
            }

            return 0;
        }

        private void ShowQuarter2Choice(Quarter2EventDefinition definition)
        {
            pendingChoiceKind = PendingChoiceKind.Quarter2;
            viewedDays.Add(CurrentDay);
            dialogueUI.ShowChoice(
                CurrentDay,
                definition.Location,
                definition.Title,
                definition.Body,
                Quarter2ProgressChoice,
                definition.ProgressChoiceText,
                Quarter2RejectChoice,
                definition.RejectChoiceText,
                protagonistPortrait,
                ResolveQuarter2PartnerPortrait(definition));
        }

        private void ShowCurrentQuarter2IntroLine()
        {
            if (pendingQuarter2IntroEvent == null ||
                pendingQuarter2IntroIndex < 0 ||
                pendingQuarter2IntroIndex >= pendingQuarter2IntroLines.Count)
            {
                return;
            }

            ShelterIntroDialogueLine line =
                pendingQuarter2IntroLines[pendingQuarter2IntroIndex];
            pendingChoiceKind = PendingChoiceKind.Quarter2Intro;
            bool isProtagonist = IsProtagonistSpeaker(line.Speaker);
            bool isRadio = line.Speaker == "라디오" || line.Speaker == "수신기";
            Texture2D partnerPortrait =
                line.Speaker == "문밖의 사람"
                    ? ResolveQuarter2PartnerPortrait(pendingQuarter2IntroEvent)
                    : null;
            dialogueUI.ShowDialogueLine(
                CurrentDay,
                DisplaySpeakerName(line.Speaker),
                line.Body,
                Quarter2IntroNextChoice,
                isProtagonist ? protagonistPortrait : null,
                isRadio ? null : partnerPortrait,
                isRadio ? 0.62f : 1f);
        }

        private bool CanOpenCurrentQuarter2Event()
        {
            if (campaign.Quarter2Phase != Quarter2FlowPhase.WaitingForChoice)
            {
                return false;
            }

            Quarter2EventDefinition definition = campaign.GetCurrentQuarter2Event();
            return definition != null &&
                   (!definition.RequiresExploration ||
                    (explorationManager != null && explorationManager.HasExploredToday));
        }

        private void OpenLastBunkerEvent()
        {
            EnsureLastBunkerStartDay();
            InitializeLastBunkerStoryProgressIfNeeded();
            int localDay = LastBunkerLocalDay;
            if (localDay < 1)
            {
                ShowMessage("마지막 벙커 이야기는 다음 날부터 시작됩니다.");
                return;
            }

            if (ShelterCampaignCatalog.TryGetLastBunkerIsolationEvent(
                    localDay,
                    out BranchEventDefinition isolationEvent,
                    out _))
            {
                if (lastBunkerFixedEventCompletedDays.Contains(localDay))
                {
                    ShowMessage("오늘의 고정 이벤트는 이미 확인했습니다.");
                    return;
                }

                pendingQuarter1Event = isolationEvent;
                pendingChoiceKind = PendingChoiceKind.LastBunker;
                viewedDays.Add(CurrentDay);
                dialogueUI.ShowEvent(
                    pendingQuarter1Event,
                    CurrentDay,
                    protagonistPortrait,
                    null);
                return;
            }

            if (lastBunkerEvents == null || lastBunkerEvents.Count == 0)
            {
                ShowMessage("오늘 발생할 벙커 내부 이벤트가 없습니다.");
                return;
            }

            int calendarDay =
                Mathf.Max(1, CurrentDay - lastBunkerStartDay + 1);
            int index =
                (calendarDay - 1 + lastBunkerEventOffset) %
                lastBunkerEvents.Count;
            pendingQuarter1Event = lastBunkerEvents[index];
            pendingChoiceKind = PendingChoiceKind.LastBunker;
            viewedDays.Add(CurrentDay);
            dialogueUI.ShowEvent(
                pendingQuarter1Event,
                CurrentDay,
                protagonistPortrait,
                null);
        }

        private void OpenQuarter3Event()
        {
            int localDay = Quarter3LocalDay;
            if (localDay < 1)
            {
                ShowMessage("마지막 분기는 아직 시작되지 않았습니다.");
                return;
            }

            if (campaign.Quarter3CurrentRoute == BranchRoute.Signal)
            {
                if (localDay >= 5 && IsQuarter3FinalChoiceReady)
                {
                    OpenFinalChoice();
                    return;
                }

                OpenSignalStory(localDay >= 5 ? 5 : localDay);
                return;
            }

            if (IsQuarter3JoinStoryPending)
            {
                PresentJoinStoryNotice(localDay >= 5 ? 5 : localDay);
                return;
            }

            if (localDay >= 5)
            {
                OpenFinalChoice();
            }
            else
            {
                OpenJoinSupport(localDay);
            }
        }

        private void PresentJoinStoryNotice(int localDay)
        {
            if (ShelterCampaignCatalog.TryGetQuarter3JoinIntroDialogue(
                    localDay,
                    out IReadOnlyList<ShelterIntroDialogueLine> introLines) &&
                introLines.Count > 0)
            {
                pendingQuarter3JoinIntroDay = localDay;
                pendingQuarter3JoinIntroLines = introLines;
                pendingQuarter3JoinIntroIndex = 0;
                ShowCurrentQuarter3JoinIntroLine();
                return;
            }

            CompleteJoinStoryNotice(localDay);
        }

        private void CompleteJoinStoryNotice(int localDay)
        {
            RecordJoinCommonStoryIfNeeded(localDay);
            dialogueUI?.Close();
            ShowMessage(localDay < 5
                ? Quarter3JoinSupportNotice
                : "메인일기에서 스토리를 확인하세요");
        }

        private void ShowCurrentQuarter3JoinIntroLine()
        {
            if (pendingQuarter3JoinIntroDay < 1 ||
                pendingQuarter3JoinIntroIndex < 0 ||
                pendingQuarter3JoinIntroIndex >=
                    pendingQuarter3JoinIntroLines.Count)
            {
                return;
            }

            ShelterIntroDialogueLine line =
                pendingQuarter3JoinIntroLines[pendingQuarter3JoinIntroIndex];
            bool isProtagonist = IsProtagonistSpeaker(line.Speaker);
            Texture2D partnerPortrait = line.Speaker switch
            {
                "붉은 완장 경비대" => armedGroupPortrait,
                "상황" => null,
                _ => survivorGroupPortrait
            };
            pendingChoiceKind = PendingChoiceKind.Quarter3JoinIntro;
            dialogueUI.ShowDialogueLine(
                CurrentDay,
                DisplaySpeakerName(line.Speaker),
                line.Body,
                Quarter3JoinIntroNextChoice,
                isProtagonist ? protagonistPortrait : null,
                isProtagonist ? null : partnerPortrait);
        }

        private void AdvanceQuarter3JoinIntro(string choiceId)
        {
            if (choiceId != Quarter3JoinIntroNextChoice ||
                pendingQuarter3JoinIntroDay < 1)
            {
                return;
            }

            pendingQuarter3JoinIntroIndex++;
            if (pendingQuarter3JoinIntroIndex <
                pendingQuarter3JoinIntroLines.Count)
            {
                ShowCurrentQuarter3JoinIntroLine();
                return;
            }

            int localDay = pendingQuarter3JoinIntroDay;
            ClearQuarter3JoinIntro();
            CompleteJoinStoryNotice(localDay);
        }

        private void ClearQuarter3JoinIntro()
        {
            pendingQuarter3JoinIntroLines =
                Array.Empty<ShelterIntroDialogueLine>();
            pendingQuarter3JoinIntroIndex = 0;
            pendingQuarter3JoinIntroDay = -1;
        }

        public void MarkTodaysJoinStoryViewed()
        {
            CreateCampaignIfNeeded();
            if (campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter3Running ||
                campaign.Quarter3CurrentRoute != BranchRoute.Join ||
                !ShelterCampaignCatalog.TryGetQuarter3JoinCommonStory(
                    Quarter3LocalDay,
                    out string title,
                    out _))
            {
                return;
            }

            string recordPrefix = $"{CurrentDay}일차 - {title}";
            foreach (string record in campaignRecords)
            {
                if (!record.StartsWith(
                        recordPrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                joinStoryViewedDays.Add(CurrentDay);
                if (Quarter3LocalDay >= 5)
                {
                    quarter3FinalStoryAcknowledged = true;
                }

                return;
            }
        }

        private void RecordJoinCommonStoryIfNeeded(int localDay)
        {
            if (!ShelterCampaignCatalog.TryGetQuarter3JoinCommonStory(
                    localDay,
                    out string title,
                    out string body))
            {
                return;
            }

            string recordPrefix = $"{CurrentDay}일차 - {title}";
            foreach (string record in campaignRecords)
            {
                if (record.StartsWith(recordPrefix, StringComparison.Ordinal))
                {
                    return;
                }
            }

            campaignRecords.Add($"{recordPrefix}\n\n{body}");
        }

        private void OpenSignalStory(int localDay)
        {
            if (!ShelterCampaignCatalog.TryGetQuarter3SignalStory(
                    localDay,
                    out string title,
                    out string body))
            {
                ShowMessage("오늘 확인할 구조신호 이야기가 없습니다.");
                return;
            }

            string recordPrefix = $"{CurrentDay}일차 - {title}";
            bool alreadyRecorded = false;
            foreach (string record in campaignRecords)
            {
                if (record.StartsWith(
                        recordPrefix,
                        StringComparison.Ordinal))
                {
                    alreadyRecorded = true;
                    break;
                }
            }

            if (alreadyRecorded)
            {
                if (localDay >= 5)
                {
                    quarter3FinalStoryAcknowledged = true;
                    OpenFinalChoice();
                }
                else
                {
                    ShowMessage("오늘의 스토리는 이미 메인 일기에 기록했습니다.");
                }

                return;
            }

            if (ShelterCampaignCatalog.TryGetQuarter3SignalIntroDialogue(
                    localDay,
                    out IReadOnlyList<ShelterIntroDialogueLine> introLines) &&
                introLines.Count > 0)
            {
                pendingQuarter3SignalIntroDay = localDay;
                pendingQuarter3SignalIntroLines = introLines;
                pendingQuarter3SignalIntroIndex = 0;
                ShowCurrentQuarter3SignalIntroLine();
                return;
            }

            CompleteSignalStory(localDay, title, body);
        }

        private void CompleteSignalStory(
            int localDay,
            string title,
            string body)
        {
            string recordPrefix = $"{CurrentDay}일차 - {title}";
            campaignRecords.Add($"{recordPrefix}\n\n{body}");
            pendingChoiceKind = PendingChoiceKind.None;
            if (localDay < 5)
            {
                viewedDays.Add(CurrentDay);
                CompleteCurrentDay();
            }
            else
            {
                quarter3FinalStoryAcknowledged = true;
            }

            dialogueUI?.Close();
            ShowMessage(localDay < 5
                ? Quarter3SignalExplorationNotice
                : "메인일기에서 스토리를 확인하세요");
        }

        private void ShowCurrentQuarter3SignalIntroLine()
        {
            if (pendingQuarter3SignalIntroDay < 1 ||
                pendingQuarter3SignalIntroIndex < 0 ||
                pendingQuarter3SignalIntroIndex >=
                    pendingQuarter3SignalIntroLines.Count)
            {
                return;
            }

            ShelterIntroDialogueLine line =
                pendingQuarter3SignalIntroLines[pendingQuarter3SignalIntroIndex];
            bool isProtagonist = IsProtagonistSpeaker(line.Speaker);
            pendingChoiceKind = PendingChoiceKind.Quarter3SignalIntro;
            dialogueUI.ShowDialogueLine(
                CurrentDay,
                DisplaySpeakerName(line.Speaker),
                line.Body,
                Quarter3SignalIntroNextChoice,
                isProtagonist ? protagonistPortrait : null,
                null,
                isProtagonist ? 1f : 0.62f);
        }

        private void AdvanceQuarter3SignalIntro(string choiceId)
        {
            if (choiceId != Quarter3SignalIntroNextChoice ||
                pendingQuarter3SignalIntroDay < 1)
            {
                return;
            }

            pendingQuarter3SignalIntroIndex++;
            if (pendingQuarter3SignalIntroIndex <
                pendingQuarter3SignalIntroLines.Count)
            {
                ShowCurrentQuarter3SignalIntroLine();
                return;
            }

            int localDay = pendingQuarter3SignalIntroDay;
            ClearQuarter3SignalIntro();
            if (!ShelterCampaignCatalog.TryGetQuarter3SignalStory(
                    localDay,
                    out string title,
                    out string body))
            {
                ShowMessage("오늘 확인할 구조신호 이야기가 없습니다.");
                return;
            }

            CompleteSignalStory(localDay, title, body);
        }

        private void ClearQuarter3SignalIntro()
        {
            pendingQuarter3SignalIntroLines =
                Array.Empty<ShelterIntroDialogueLine>();
            pendingQuarter3SignalIntroIndex = 0;
            pendingQuarter3SignalIntroDay = -1;
        }

        private void OpenJoinSupport(int localDay)
        {
            if (localDay == JoinResourceRequiredDay)
            {
                int foodItems = GetSupportItemCount(ResourceType.Food);
                int waterItems = GetSupportItemCount(ResourceType.Water);
                if (foodItems == 0 && waterItems == 0)
                {
                    ResolveJoinSupportWithoutResources(localDay);
                    return;
                }
            }

            pendingChoiceKind = PendingChoiceKind.JoinTarget;
            pendingJoinSupportDay = localDay;
            viewedDays.Add(CurrentDay);
            ShowJoinTargetClaim(localDay, Quarter3JoinSupportTarget.SurvivorGroup);
        }

        private void ShowJoinTargetClaim(
            int localDay,
            Quarter3JoinSupportTarget displayedTarget)
        {
            string title = $"합류 이벤트 {localDay}/4";
            string survivorLabel = "생존자 공동체를 돕는다";
            string redLabel = "붉은 완장을 돕는다";
            string survivorClaim = "일반 생존자들이 도움을 요청했다.";
            string redClaim = "붉은 완장 경비대가 도움을 요청했다.";
            if (ShelterCampaignCatalog.TryGetQuarter3JoinEventStory(
                    localDay,
                    out string eventTitle,
                    out string eventSurvivorClaim,
                    out string eventRedClaim))
            {
                title = eventTitle;
                survivorClaim = eventSurvivorClaim;
                redClaim = eventRedClaim;
            }

            if (campaign.Quarter3JoinSupportController != null)
            {
                Quarter3JoinSupportDefinition survivorDefinition =
                    campaign.Quarter3JoinSupportController.GetSupportDefinition(
                        localDay, Quarter3JoinSupportTarget.SurvivorGroup);
                Quarter3JoinSupportDefinition redDefinition =
                    campaign.Quarter3JoinSupportController.GetSupportDefinition(
                        localDay, Quarter3JoinSupportTarget.RedArmband);
                if (survivorDefinition != null &&
                    !string.IsNullOrWhiteSpace(survivorDefinition.DisplayName))
                {
                    survivorLabel = survivorDefinition.DisplayName;
                }

                if (redDefinition != null &&
                    !string.IsNullOrWhiteSpace(redDefinition.DisplayName))
                {
                    redLabel = redDefinition.DisplayName;
                }
            }

            bool showingSurvivors =
                displayedTarget == Quarter3JoinSupportTarget.SurvivorGroup;
            string body = showingSurvivors
                ? $"[일반 생존자]\n{survivorClaim}"
                : $"[붉은 완장]\n{redClaim}";
            string firstChoiceId = showingSurvivors
                ? JoinSurvivorTargetChoice
                : JoinViewSurvivorClaimChoice;
            string firstChoiceLabel = showingSurvivors
                ? survivorLabel
                : "일반 생존자 주장 보기";
            string secondChoiceId = showingSurvivors
                ? JoinViewRedClaimChoice
                : JoinRedTargetChoice;
            string secondChoiceLabel = showingSurvivors
                ? "붉은 완장 주장 보기"
                : redLabel;

            dialogueUI.ShowChoice(
                CurrentDay,
                "문 앞의 요청",
                title,
                body,
                firstChoiceId,
                firstChoiceLabel,
                secondChoiceId,
                secondChoiceLabel,
                protagonistPortrait,
                ResolveJoinTargetPortrait(displayedTarget));
        }

        private void OpenJoinResourceChoice()
        {
            pendingChoiceKind = PendingChoiceKind.JoinResource;
            dialogueUI.ShowChoice(
                CurrentDay,
                "지원 물자 선택",
                "무엇을 건넬 것인가",
                $"통조림 {GetSupportItemCount(ResourceType.Food)}개 / 물 {GetSupportItemCount(ResourceType.Water)}개",
                JoinFoodChoice,
                "통조림 1개를 건넨다",
                JoinWaterChoice,
                "물 1개를 건넨다",
                protagonistPortrait,
                ResolveJoinTargetPortrait(pendingJoinTarget));
        }

        private void OpenFinalChoice()
        {
            if (!campaign.Quarter3IsFinalChoiceOpen)
            {
                EndingRouteCampaignTransitionResult openResult = campaign.OpenQuarter3FinalChoice();
                if (!openResult.IsSuccess)
                {
                    ShowMessage(openResult.FailureReason);
                    return;
                }
            }

            pendingChoiceKind = PendingChoiceKind.FinalChoice;
            viewedDays.Add(CurrentDay);
            if (campaign.Quarter3CurrentRoute == BranchRoute.Signal)
            {
                dialogueUI.ShowChoice(
                    CurrentDay,
                    "마지막 방송",
                    "어느 주파수를 믿을 것인가",
                    BuildSignalFinalChoiceBody(),
                    ShelterCampaignCatalog.SignalMilitaryChoice,
                    "군 주파수를 따른다",
                    ShelterCampaignCatalog.SignalSecondFrequencyChoice,
                    "두 번째 주파수를 따른다",
                    protagonistPortrait,
                    militaryRescuePortrait);
            }
            else
            {
                ShowJoinFinalChoice(
                    Quarter3JoinSupportTarget.SurvivorGroup);
            }
        }

        private void ShowJoinFinalChoice(
            Quarter3JoinSupportTarget displayedTarget)
        {
            bool showingSurvivors =
                displayedTarget == Quarter3JoinSupportTarget.SurvivorGroup;
            string firstChoiceId = showingSurvivors
                ? ShelterCampaignCatalog.JoinSurvivorsChoice
                : JoinViewSurvivorClaimChoice;
            string firstChoiceLabel = showingSurvivors
                ? "일반 생존자들과 함께 지하 통로로 떠난다"
                : "일반 생존자 의견 보기";
            string secondChoiceId = showingSurvivors
                ? JoinViewRedClaimChoice
                : ShelterCampaignCatalog.JoinRedArmbandChoice;
            string secondChoiceLabel = showingSurvivors
                ? "붉은 완장 의견 보기"
                : "붉은 완장을 차고 경비대와 함께 떠난다";

            dialogueUI.ShowChoice(
                CurrentDay,
                "마지막 합류 요청",
                "누구와 함께 이곳을 떠날 것인가",
                BuildJoinFinalChoiceBody(displayedTarget),
                firstChoiceId,
                firstChoiceLabel,
                secondChoiceId,
                secondChoiceLabel,
                protagonistPortrait,
                ResolveJoinTargetPortrait(displayedTarget));
        }

        private string BuildSignalFinalChoiceBody()
        {
            ShelterCampaignCatalog.TryGetQuarter3SignalStory(
                5,
                out _,
                out string lastBroadcast);

            List<string> acquiredTitles = new();
            foreach (string clueId in campaign.Quarter3AcquiredClueIds)
            {
                if (quarter3ClueCatalog != null &&
                    quarter3ClueCatalog.TryGetById(
                        clueId,
                        out Quarter3ClueDefinition clue) &&
                    !string.IsNullOrWhiteSpace(clue.Title))
                {
                    acquiredTitles.Add($"• {clue.Title}");
                }
            }

            string recordSummary = acquiredTitles.Count > 0
                ? string.Join("\n", acquiredTitles)
                : "• 획득한 판단 기록 없음";
            return
                $"{lastBroadcast}\n\n" +
                $"지금까지 획득한 판단 기록 ({acquiredTitles.Count}개)\n" +
                recordSummary;
        }

        private string BuildJoinFinalChoiceBody(
            Quarter3JoinSupportTarget displayedTarget)
        {
            ShelterCampaignCatalog.TryGetQuarter3JoinEventStory(
                5,
                out _,
                out string survivorScene,
                out string redScene);

            List<string> acquiredTitles = new();
            foreach (string clueId in campaign.Quarter3AcquiredClueIds)
            {
                if (quarter3ClueCatalog != null &&
                    quarter3ClueCatalog.TryGetById(
                        clueId,
                        out Quarter3ClueDefinition clue) &&
                    !string.IsNullOrWhiteSpace(clue.Title))
                {
                    acquiredTitles.Add($"• {clue.Title}");
                }
            }

            string recordSummary = acquiredTitles.Count > 0
                ? string.Join("\n", acquiredTitles)
                : "• 획득한 판단 기록 없음";
            string displayedClaim =
                displayedTarget == Quarter3JoinSupportTarget.SurvivorGroup
                    ? $"[일반 생존자]\n{survivorScene}"
                    : $"[붉은 완장]\n{redScene}";
            return
                $"{displayedClaim}\n\n" +
                $"지금까지 획득한 판단 기록 ({acquiredTitles.Count}개)\n" +
                recordSummary;
        }

        private void HandleChoiceRequested(string choiceId)
        {
            if (string.IsNullOrWhiteSpace(choiceId))
            {
                return;
            }

            switch (pendingChoiceKind)
            {
                case PendingChoiceKind.Quarter1:
                    SelectQuarter1(choiceId);
                    break;
                case PendingChoiceKind.LastBunker:
                    SelectLastBunker(choiceId);
                    break;
                case PendingChoiceKind.Quarter2:
                    SelectQuarter2(choiceId);
                    break;
                case PendingChoiceKind.Quarter2Intro:
                    AdvanceQuarter2Intro(choiceId);
                    break;
                case PendingChoiceKind.Quarter3SignalIntro:
                    AdvanceQuarter3SignalIntro(choiceId);
                    break;
                case PendingChoiceKind.Quarter3JoinIntro:
                    AdvanceQuarter3JoinIntro(choiceId);
                    break;
                case PendingChoiceKind.Quarter3RouteGuide:
                    AcknowledgeQuarter3RouteGuide(choiceId);
                    break;
                case PendingChoiceKind.MorningStatusDialogue:
                    AcknowledgeMorningStatusDialogue(choiceId);
                    break;
                case PendingChoiceKind.Quarter2SignalMorningDialogue:
                    AdvanceQuarter2SignalMorningDialogue(choiceId);
                    break;
                case PendingChoiceKind.Quarter2JoinReturnDialogue:
                    AcknowledgeQuarter2JoinReturnDialogue(choiceId);
                    break;
                case PendingChoiceKind.LastSurvivalEndingDialogue:
                    AcknowledgeLastSurvivalEndingDialogue(choiceId);
                    break;
                case PendingChoiceKind.Quarter3FinalSleepBlock:
                    AcknowledgeQuarter3FinalSleepBlock(choiceId);
                    break;
                case PendingChoiceKind.LastBunkerIntro:
                    AcknowledgeLastBunkerIntro(choiceId);
                    break;
                case PendingChoiceKind.DayOneTutorialMonologue:
                    AcknowledgeDayOneTutorialMonologue(choiceId);
                    break;
                case PendingChoiceKind.JoinTarget:
                    SelectJoinTarget(choiceId);
                    break;
                case PendingChoiceKind.JoinResource:
                    SelectJoinResource(choiceId);
                    break;
                case PendingChoiceKind.FinalChoice:
                    SelectFinalChoice(choiceId);
                    break;
            }
        }

        private void AcknowledgeDayOneTutorialMonologue(string choiceId)
        {
            if (choiceId != DayOneTutorialMonologueChoice) return;
            dialogueUI?.Close();
        }

        private void HandleDayOneTutorialMonologueClosed()
        {
            if (pendingChoiceKind != PendingChoiceKind.DayOneTutorialMonologue)
            {
                return;
            }

            pendingChoiceKind = PendingChoiceKind.None;
            if (dialogueUI != null)
            {
                dialogueUI.Closed -= HandleDayOneTutorialMonologueClosed;
            }

            Action completed = dayOneTutorialMonologueCompleted;
            dayOneTutorialMonologueCompleted = null;
            completed?.Invoke();
        }

        private void SelectQuarter1(string choiceId)
        {
            if (pendingQuarter1Event == null)
            {
                return;
            }

            if (!pendingQuarter1Event.TryGetChoice(choiceId, out BranchEventChoice choice))
            {
                ShowMessage("선택지를 찾을 수 없습니다.");
                return;
            }

            if (!ShelterQuarter1ChoiceEffects.CanApply(
                    pendingQuarter1Event,
                    choice,
                    resources,
                    out string effectFailureReason))
            {
                ShowMessage(effectFailureReason);
                return;
            }

            BranchEventSelectionResult result = campaign.Quarter1Controller.SelectEvent(
                pendingQuarter1Event.EventId,
                choiceId);
            if (!result.IsSuccess)
            {
                ShowMessage(result.FailureReason);
                return;
            }

            Quarter1ChoiceEffectResult effectResult = ShelterQuarter1ChoiceEffects.Apply(
                pendingQuarter1Event,
                result.Choice,
                characterStats,
                resources);
            string diaryText =
                $"{pendingQuarter1Event.Title}\n\n" +
                effectResult.BuildDiaryText();
            campaign.Quarter1Controller.DiaryRepository.ReplaceLastEventRecord(
                CurrentDay,
                diaryText);
            UpdateLastQuarter1ScoreChange(pendingQuarter1Event, result);
            CompleteCurrentDay();
            dialogueUI.ShowDiaryResultPrompt();
            pendingQuarter1Event = null;
        }

        private void UpdateLastQuarter1ScoreChange(
            BranchEventDefinition definition,
            BranchEventSelectionResult result)
        {
            if (result.AppliedSignalScoreDelta > 0)
            {
                lastQuarter1ScoreChange =
                    $"최근: {definition.Title}\n구조신호 +{result.AppliedSignalScoreDelta}";
            }
            else if (result.AppliedJoinScoreDelta > 0)
            {
                lastQuarter1ScoreChange =
                    $"최근: {definition.Title}\n합류 +{result.AppliedJoinScoreDelta}";
            }
            else
            {
                lastQuarter1ScoreChange =
                    $"최근: {definition.Title}\n계열 점수 변화 없음";
            }
        }

        private void SelectLastBunker(string choiceId)
        {
            if (pendingQuarter1Event == null ||
                !pendingQuarter1Event.TryGetChoice(choiceId, out BranchEventChoice choice))
            {
                ShowMessage("선택지를 찾을 수 없습니다.");
                return;
            }

            if (!ShelterQuarter1ChoiceEffects.CanApply(
                    pendingQuarter1Event,
                    choice,
                    resources,
                    out string failureReason))
            {
                ShowMessage(failureReason);
                return;
            }

            Quarter1ChoiceEffectResult effect = ShelterQuarter1ChoiceEffects.Apply(
                pendingQuarter1Event,
                choice,
                characterStats,
                resources);
            int localDay = LastBunkerLocalDay;
            if (ShelterCampaignCatalog.TryGetLastBunkerIsolationEvent(
                    localDay,
                    out BranchEventDefinition fixedEvent,
                    out _) &&
                fixedEvent.EventId == pendingQuarter1Event.EventId)
            {
                lastBunkerFixedEventCompletedDays.Add(localDay);
            }

            campaignRecords.Add(
                $"{CurrentDay}일차 - {pendingQuarter1Event.Title}\n\n{effect.BuildDiaryText()}");
            CompleteCurrentDay();
            dialogueUI.ShowDiaryResultPrompt();
            pendingQuarter1Event = null;
        }

        private void SelectQuarter2(string choiceId)
        {
            Quarter2Decision decision = choiceId == Quarter2ProgressChoice
                ? Quarter2Decision.Progress
                : Quarter2Decision.Reject;
            Quarter2SelectionResult result = campaign.SelectQuarter2Decision(decision);
            if (!result.IsSuccess)
            {
                ShowMessage(result.FailureReason);
                return;
            }

            quarter2LastChoiceDay = CurrentDay;
            if (campaign.CampaignState.Phase ==
                EndingRouteCampaignPhase.AllBranchesFailed)
            {
                lastBunkerStartDay = CurrentDay + 1;
                lastBunkerStoryDay = 1;
                lastBunkerDiaryUnlockedDays.Clear();
                lastBunkerFixedEventCompletedDays.Clear();
                lastBunkerStoryAdvancePending = false;
                lastBunkerIntroPresented = false;
                lastBunkerIntroAcknowledged = false;
            }
            ApplyQuarter2ProgressEffects(result.EventDefinition, decision);
            CompleteCurrentDay();
            string resultText = decision == Quarter2Decision.Progress
                ? result.EventDefinition.ProgressResultText
                : result.EventDefinition.RejectResultText;
            lastQuarter2DecisionResult =
                $"최근: {result.EventDefinition.Title}\n" +
                (decision == Quarter2Decision.Progress
                    ? "진행 선택"
                    : "거부 선택");
            campaignRecords.Add(
                $"{CurrentDay}일차 - {result.EventDefinition.Title}\n\n{resultText}");
            dialogueUI.ShowDiaryResultPrompt();
        }

        private void AdvanceQuarter2Intro(string choiceId)
        {
            if (choiceId != Quarter2IntroNextChoice ||
                pendingQuarter2IntroEvent == null)
            {
                return;
            }

            pendingQuarter2IntroIndex++;
            if (pendingQuarter2IntroIndex < pendingQuarter2IntroLines.Count)
            {
                ShowCurrentQuarter2IntroLine();
                return;
            }

            Quarter2EventDefinition definition = pendingQuarter2IntroEvent;
            ClearQuarter2Intro();
            ShowQuarter2Choice(definition);
        }

        private void ClearQuarter2Intro()
        {
            pendingQuarter2IntroEvent = null;
            pendingQuarter2IntroLines = Array.Empty<ShelterIntroDialogueLine>();
            pendingQuarter2IntroIndex = 0;
        }

        private void ApplyQuarter2ProgressEffects(
            Quarter2EventDefinition definition,
            Quarter2Decision decision)
        {
            if (definition == null ||
                decision != Quarter2Decision.Progress ||
                characterStats == null)
            {
                return;
            }

            if (!Mathf.Approximately(definition.ProgressHealthDelta, 0f))
            {
                characterStats.ModifyHealth(
                    definition.ProgressHealthDelta,
                    "2분기 일반 이벤트");
            }

            if (!Mathf.Approximately(definition.ProgressHungerDelta, 0f))
            {
                characterStats.ModifyHunger(definition.ProgressHungerDelta);
            }

            if (!Mathf.Approximately(definition.ProgressThirstDelta, 0f))
            {
                characterStats.ModifyThirst(definition.ProgressThirstDelta);
            }

            if (!Mathf.Approximately(definition.ProgressMoraleDelta, 0f))
            {
                characterStats.ModifyMorale(definition.ProgressMoraleDelta);
            }
        }

        private void SelectJoinTarget(string choiceId)
        {
            if (choiceId == JoinViewSurvivorClaimChoice)
            {
                ShowJoinTargetClaim(
                    pendingJoinSupportDay,
                    Quarter3JoinSupportTarget.SurvivorGroup);
                return;
            }

            if (choiceId == JoinViewRedClaimChoice)
            {
                ShowJoinTargetClaim(
                    pendingJoinSupportDay,
                    Quarter3JoinSupportTarget.RedArmband);
                return;
            }

            if (choiceId == JoinSurvivorTargetChoice)
            {
                pendingJoinTarget = Quarter3JoinSupportTarget.SurvivorGroup;
            }
            else if (choiceId == JoinRedTargetChoice)
            {
                pendingJoinTarget = Quarter3JoinSupportTarget.RedArmband;
            }
            else
            {
                ShowMessage("지원 대상을 찾을 수 없습니다.");
                return;
            }

            if (pendingJoinSupportDay == JoinResourceRequiredDay)
            {
                OpenJoinResourceChoice();
            }
            else
            {
                ResolveJoinSupportWithoutSpending();
            }
        }

        private void SelectJoinResource(string choiceId)
        {
            Quarter3JoinSupportResource supportResource = choiceId == JoinFoodChoice
                ? Quarter3JoinSupportResource.CannedFood
                : Quarter3JoinSupportResource.Water;
            ResourceType resourceType = supportResource == Quarter3JoinSupportResource.CannedFood
                ? ResourceType.Food
                : ResourceType.Water;

            Quarter3JoinSupportInventorySnapshot inventory = new(
                GetSupportItemCount(ResourceType.Food),
                GetSupportItemCount(ResourceType.Water));
            Quarter3JoinSupportAttemptResult result = campaign.AttemptQuarter3JoinSupport(
                pendingJoinSupportDay,
                pendingJoinTarget,
                supportResource,
                inventory);

            if (result.Outcome == Quarter3JoinSupportOutcome.SelectedResourceUnavailable)
            {
                ShowMessage("선택한 물자가 부족합니다. 다른 물자를 선택하세요.");
                return;
            }

            if (!result.IsProcessed)
            {
                ShowMessage(result.FailureReason);
                return;
            }

            if (result.IsSuccess)
            {
                resources.TrySpend(resourceType, ResourceUnitsPerSupportItem);
            }

            CompleteCurrentDay();
            if (result.IsSuccess)
            {
                dialogueUI.ShowDiaryResultPrompt();
            }
            else
            {
                dialogueUI.ShowResultText(
                    "지원 기록",
                    "보낼 물자가 없어 오늘의 지원 요청을 놓쳤다.");
            }
        }

        private void ResolveJoinSupportWithoutSpending()
        {
            Quarter3JoinSupportAttemptResult result = campaign.AttemptQuarter3JoinSupport(
                pendingJoinSupportDay,
                pendingJoinTarget,
                Quarter3JoinSupportResource.CannedFood,
                FreeJoinSupportInventory);

            if (!result.IsProcessed)
            {
                ShowMessage(result.FailureReason);
                return;
            }

            CompleteCurrentDay();
            dialogueUI.ShowDiaryResultPrompt();
        }

        private void SelectFinalChoice(string choiceId)
        {
            if (campaign.Quarter3CurrentRoute == BranchRoute.Join)
            {
                if (choiceId == JoinViewSurvivorClaimChoice)
                {
                    ShowJoinFinalChoice(
                        Quarter3JoinSupportTarget.SurvivorGroup);
                    return;
                }

                if (choiceId == JoinViewRedClaimChoice)
                {
                    ShowJoinFinalChoice(
                        Quarter3JoinSupportTarget.RedArmband);
                    return;
                }
            }

            Quarter3FinalSelectionResult result = campaign.SelectQuarter3FinalChoice(choiceId);
            if (!result.IsSuccess)
            {
                ShowMessage(result.FailureReason);
                return;
            }

            completedDays.Add(CurrentDay);
            dialogueUI.Close();
            EndingId endingId = choiceId switch
            {
                ShelterCampaignCatalog.SignalMilitaryChoice => EndingId.LastFrequency,
                ShelterCampaignCatalog.SignalSecondFrequencyChoice => EndingId.DoorOpenedNight,
                ShelterCampaignCatalog.JoinSurvivorsChoice => EndingId.WithStrangers,
                ShelterCampaignCatalog.JoinRedArmbandChoice => EndingId.RedArmband,
                _ => EndingId.None
            };
            string choiceRecord;
            if (choiceId == ShelterCampaignCatalog.SignalMilitaryChoice)
            {
                choiceRecord = "군 주파수를 따라 외곽 구조 지점으로 이동했다.";
            }
            else if (choiceId == ShelterCampaignCatalog.SignalSecondFrequencyChoice)
            {
                choiceRecord = "두 번째 주파수를 따라 가까운 집결 장소로 이동했다.";
            }
            else if (ShelterCampaignCatalog.TryGetQuarter3JoinFinalChoiceRecord(
                         choiceId,
                         out string joinRecord))
            {
                choiceRecord = joinRecord;
            }
            else
            {
                choiceRecord = $"최종 선택: {choiceId}";
            }
            campaignRecords.Add($"{CurrentDay}일차 - {choiceRecord}");
            ResolveReferences();
            if ((endingId == EndingId.LastFrequency ||
                 endingId == EndingId.DoorOpenedNight) &&
                signalEndingSequence != null &&
                signalEndingSequence.Play(endingId))
            {
                return;
            }

            if ((endingId == EndingId.WithStrangers ||
                 endingId == EndingId.RedArmband) &&
                joinEndingSequence != null &&
                joinEndingSequence.Play(endingId))
            {
                return;
            }

            gameManager?.CompleteEnding(endingId);
        }

        private void ResolveJoinSupportWithoutResources(int localDay)
        {
            Quarter3JoinSupportAttemptResult result = campaign.AttemptQuarter3JoinSupport(
                localDay,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.CannedFood,
                new Quarter3JoinSupportInventorySnapshot(0, 0));
            if (!result.IsProcessed)
            {
                ShowMessage(result.FailureReason);
                return;
            }

            pendingChoiceKind = PendingChoiceKind.None;
            viewedDays.Add(CurrentDay);
            CompleteCurrentDay();
            string message = "보낼 통조림이나 물이 없어 오늘의 지원 요청을 놓쳤다.";
            dialogueUI.ShowChoice(
                CurrentDay,
                "지원 기록",
                "빈 손",
                message,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                protagonistPortrait,
                null);
            dialogueUI.ShowResultText("지원 기록", message);
        }

        private void HandleDayChanged()
        {
            pendingMorningDialogueLines.Clear();
            ClearQuarter2Intro();
            ClearQuarter3SignalIntro();
            ClearQuarter3JoinIntro();
            if (!IsQuarter3FinalChoiceDay &&
                characterStats != null &&
                characterStats.MorningDialogueDay == CurrentDay)
            {
                pendingMorningDialogueLines.AddRange(
                    characterStats.MorningDialogueLines);
            }

            pendingChoiceKind = PendingChoiceKind.None;
            pendingQuarter1Event = null;
            dialogueUI?.Close();
            AdvanceLastBunkerStoryAfterSleep();
            SyncCampaignToCurrentDay();
            QueueQuarter3FinalMorningDialogue();
            if (TryShowQuarter2SignalMorningDialogue())
            {
                return;
            }

            if (pendingMorningDialogueLines.Count > 0)
            {
                ShowMorningStatusDialogue();
            }
        }

        private void SyncCampaignToCurrentDay()
        {
            CreateCampaignIfNeeded();
            int day = CurrentDay;

            if (campaign.CampaignState.Phase == EndingRouteCampaignPhase.Quarter1Running)
            {
                int targetDay = Mathf.Min(day, settings.DecisionDay);
                while (campaign.Quarter1Controller.RuntimeState.CurrentDay < targetDay)
                {
                    if (!campaign.Quarter1Controller.AdvanceDay())
                    {
                        Debug.LogWarning(campaign.Quarter1Controller.LastFailureReason);
                        break;
                    }
                }

                if (day >= settings.DecisionDay &&
                    campaign.Quarter1Controller.RuntimeState.CurrentDay == settings.DecisionDay)
                {
                    campaign.Quarter1Controller.ResolveDecision(UnityEngine.Random.value);
                    campaign.SynchronizeState();
                }
            }

            if (campaign.CampaignState.Phase == EndingRouteCampaignPhase.Quarter1Resolved)
            {
                EndingRouteCampaignTransitionResult result = campaign.AdvanceToQuarter2();
                if (result.IsSuccess)
                {
                    ShowMessage(campaign.Quarter2CurrentRoute == BranchRoute.Signal
                        ? "선택이 구조신호 노선으로 이어졌습니다."
                        : "선택이 합류 노선으로 이어졌습니다. 문 앞에서 다음 사건을 확인하세요.");
                }
            }

            if (campaign.CampaignState.Phase == EndingRouteCampaignPhase.Quarter2Running &&
                quarter2LastChoiceDay >= 0 &&
                day > quarter2LastChoiceDay &&
                (campaign.Quarter2Phase == Quarter2FlowPhase.WaitingForAdvance ||
                 campaign.Quarter2Phase == Quarter2FlowPhase.RouteSwitchPending))
            {
                campaign.AdvanceQuarter2Step();
                quarter2LastChoiceDay = -1;
            }

            campaign.SynchronizeState();
            if (campaign.CampaignState.Phase == EndingRouteCampaignPhase.Quarter2Passed &&
                (quarter2LastChoiceDay < 0 || day > quarter2LastChoiceDay))
            {
                EndingRouteCampaignTransitionResult result = campaign.AdvanceToQuarter3();
                if (result.IsSuccess)
                {
                    quarter3StartDay = day;
                    quarter2LastChoiceDay = -1;
                    quarter3FinalStoryAcknowledged = false;
                    quarter3FinalMorningDialogueShown = false;
                    ShowMessage("마지막 분기가 시작되었습니다. 시스템 안내를 확인하세요.");
                }
            }

            if (campaign.CampaignState.Phase == EndingRouteCampaignPhase.Quarter3Running &&
                campaign.Quarter3CurrentRoute == BranchRoute.Join)
            {
                MarkSkippedJoinSupportDays();
            }

            EnsureLastBunkerStartDay();
            TryShowLastBunkerIntro();
            TryShowQuarter3StartNotice();
        }

        private void EnsureLastBunkerStartDay()
        {
            if (campaign == null ||
                campaign.CampaignState.Phase !=
                EndingRouteCampaignPhase.AllBranchesFailed ||
                lastBunkerStartDay >= 0)
            {
                return;
            }

            lastBunkerStartDay = completedDays.Contains(CurrentDay)
                ? CurrentDay + 1
                : CurrentDay;
            lastBunkerStoryDay = 1;
            lastBunkerDiaryUnlockedDays.Clear();
            lastBunkerFixedEventCompletedDays.Clear();
            lastBunkerStoryAdvancePending = false;
        }

        private void InitializeLastBunkerStoryProgressIfNeeded()
        {
            if (campaign == null ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.AllBranchesFailed ||
                lastBunkerStartDay < 0 ||
                CurrentDay < lastBunkerStartDay ||
                lastBunkerStoryDay > 0)
            {
                return;
            }

            lastBunkerStoryDay =
                Mathf.Max(1, CurrentDay - lastBunkerStartDay + 1);
            int localDay = lastBunkerStoryDay;
            if (completedDays.Contains(CurrentDay) &&
                ShelterCampaignCatalog.TryGetLastBunkerIsolationEvent(
                    localDay,
                    out _,
                    out _))
            {
                lastBunkerFixedEventCompletedDays.Add(localDay);
            }
        }

        private void RecordCurrentLastBunkerDiaryUnlock()
        {
            int localDay = LastBunkerLocalDay;
            if (localDay is < 1 or > 9 ||
                explorationManager == null ||
                !explorationManager.HasExploredToday)
            {
                return;
            }

            if (ShelterCampaignCatalog.TryGetLastBunkerIsolationEvent(
                    localDay,
                    out _,
                    out string todayDiary))
            {
                if (!string.IsNullOrWhiteSpace(todayDiary))
                {
                    lastBunkerDiaryUnlockedDays.Add(localDay);
                }

                return;
            }

            if (ShelterCampaignCatalog.TryGetLastBunkerDiary(
                    localDay,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                lastBunkerDiaryUnlockedDays.Add(localDay);
            }
        }

        private bool IsLastBunkerFixedEventCompleted(int localDay)
        {
            return !ShelterCampaignCatalog.TryGetLastBunkerIsolationEvent(
                       localDay,
                       out _,
                       out _) ||
                   lastBunkerFixedEventCompletedDays.Contains(localDay);
        }

        private bool CanOpenLastBunkerEvent()
        {
            EnsureLastBunkerStartDay();
            InitializeLastBunkerStoryProgressIfNeeded();
            int localDay = LastBunkerLocalDay;
            if (localDay < 1)
            {
                return false;
            }

            if (ShelterCampaignCatalog.TryGetLastBunkerIsolationEvent(
                    localDay,
                    out _,
                    out _))
            {
                return !lastBunkerFixedEventCompletedDays.Contains(localDay);
            }

            return !completedDays.Contains(CurrentDay);
        }

        private bool IsLastBunkerDiaryUnlocked(int localDay)
        {
            return lastBunkerDiaryUnlockedDays.Contains(localDay) ||
                   (explorationManager != null &&
                    explorationManager.HasExploredToday);
        }

        private void AdvanceLastBunkerStoryAfterSleep()
        {
            if (!lastBunkerStoryAdvancePending)
            {
                return;
            }

            lastBunkerStoryAdvancePending = false;
            if (campaign == null ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.AllBranchesFailed ||
                lastBunkerStoryDay is < 1 or > 9)
            {
                return;
            }

            lastBunkerStoryDay++;
        }

        private void TryShowLastBunkerIntro()
        {
            if (pendingMorningDialogueLines.Count > 0 ||
                campaign == null ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.AllBranchesFailed ||
                LastBunkerLocalDay != 1 ||
                lastBunkerIntroPresented ||
                lastBunkerIntroAcknowledged)
            {
                return;
            }

            const string line =
                "……이상한 꿈을 꾼 것 같은데, 잘 기억이 안 나네.";
            ResolveReferences();
            if (dialogueUI == null)
            {
                lastBunkerIntroAcknowledged = true;
                ShowMessage(line);
                return;
            }

            lastBunkerIntroPresented = true;
            pendingChoiceKind = PendingChoiceKind.LastBunkerIntro;
            dialogueUI.ShowChoice(
                CurrentDay,
                GameSession.CurrentPlayerName,
                "희미한 기억",
                line,
                LastBunkerIntroChoice,
                "확인",
                string.Empty,
                string.Empty,
                protagonistPortrait,
                null);
        }

        private void AcknowledgeLastBunkerIntro(string choiceId)
        {
            if (choiceId != LastBunkerIntroChoice)
            {
                return;
            }

            lastBunkerIntroAcknowledged = true;
            lastBunkerIntroPresented = false;
            pendingChoiceKind = PendingChoiceKind.None;
            dialogueUI?.Close();
        }

        private void TryShowQuarter3StartNotice()
        {
            if (campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter3Running ||
                (campaign.Quarter3CurrentRoute != BranchRoute.Signal &&
                 campaign.Quarter3CurrentRoute != BranchRoute.Join) ||
                Quarter3LocalDay < 1)
            {
                return;
            }

            quarter3StartNoticeAcknowledged = true;
            quarter3StartNoticePresented = false;
            ShowQuarter3RouteGuide();
        }

        private void AcknowledgeQuarter3StartNotice()
        {
            quarter3StartNoticeAcknowledged = true;
            ShowQuarter3RouteGuide();
        }

        private void ShowQuarter3RouteGuide()
        {
            if (pendingMorningDialogueLines.Count > 0 ||
                quarter3RouteGuidePresented ||
                quarter3RouteGuideAcknowledged ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter3Running)
            {
                return;
            }

            ResolveReferences();
            bool isJoinRoute =
                campaign.Quarter3CurrentRoute == BranchRoute.Join;
            string title = isJoinRoute
                ? "문밖의 목소리"
                : "라디오 소리";
            string body = isJoinRoute
                ? "……문밖에서 사람들 목소리가 들리네. 무슨 일인지 확인해보자."
                : "라디오가 울리는 것 같다. 라디오를 직접 확인해 봐야겠어.";

            if (dialogueUI == null)
            {
                quarter3RouteGuideAcknowledged = true;
                ShowMessage(body);
                return;
            }

            quarter3RouteGuidePresented = true;
            pendingChoiceKind = PendingChoiceKind.Quarter3RouteGuide;
            dialogueUI.ShowChoice(
                CurrentDay,
                GameSession.CurrentPlayerName,
                title,
                body,
                Quarter3RouteGuideChoice,
                "확인",
                string.Empty,
                string.Empty,
                protagonistPortrait,
                null);
        }

        private void AcknowledgeQuarter3RouteGuide(string choiceId)
        {
            if (choiceId != Quarter3RouteGuideChoice)
            {
                return;
            }

            quarter3RouteGuideAcknowledged = true;
            quarter3RouteGuidePresented = false;
            pendingChoiceKind = PendingChoiceKind.None;
            dialogueUI?.Close();
        }

        private void ShowMorningStatusDialogue()
        {
            if (pendingMorningDialogueLines.Count == 0)
            {
                return;
            }

            ResolveReferences();
            if (dialogueUI == null)
            {
                string fallbackBody = string.Join(
                    "\n\n",
                    pendingMorningDialogueLines);
                pendingMorningDialogueLines.Clear();
                ShowMessage(fallbackBody);
                ResumeAutomaticDialogue();
                return;
            }

            string body = pendingMorningDialogueLines[0];
            pendingChoiceKind = PendingChoiceKind.MorningStatusDialogue;
            dialogueUI.ShowChoice(
                CurrentDay,
                GameSession.CurrentPlayerName,
                "아침의 혼잣말",
                body,
                MorningStatusDialogueChoice,
                pendingMorningDialogueLines.Count > 1
                    ? "다음"
                    : "확인",
                string.Empty,
                string.Empty,
                protagonistPortrait,
                null);
        }

        private bool TryShowQuarter2SignalMorningDialogue()
        {
            if (campaign == null ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter2Running ||
                campaign.Quarter2CurrentRoute != BranchRoute.Signal)
            {
                return false;
            }

            int eventOrder = campaign.Quarter2CurrentEventOrder;
            if (eventOrder != 1 && eventOrder != 3)
            {
                return false;
            }

            return TryShowQuarter2SignalDialogue(eventOrder);
        }

        private void AdvanceQuarter2SignalMorningDialogue(string choiceId)
        {
            if (choiceId != Quarter2SignalMorningDialogueChoice)
            {
                return;
            }

            IReadOnlyList<ShelterIntroDialogueLine> lines =
                GetQuarter2SignalDialogueLines(
                    pendingQuarter2SignalDialogueOrder);
            pendingQuarter2SignalDialogueIndex++;
            if (pendingQuarter2SignalDialogueIndex < lines.Count)
            {
                TryShowQuarter2SignalDialogue(
                    pendingQuarter2SignalDialogueOrder);
                return;
            }

            MarkQuarter2SignalDialogueAcknowledged(
                pendingQuarter2SignalDialogueOrder);
            pendingQuarter2SignalDialogueOrder = -1;
            pendingQuarter2SignalDialogueIndex = -1;
            pendingChoiceKind = PendingChoiceKind.None;
            dialogueUI?.Close();
            if (pendingMorningDialogueLines.Count > 0)
            {
                ShowMorningStatusDialogue();
                return;
            }

            ResumeAutomaticDialogue();
        }

        private void HandleExplorationReturnedToShelter()
        {
            if (TryShowQuarter2SignalReturnDialogue())
            {
                return;
            }

            TryShowQuarter2JoinReturnDialogue();
        }

        private bool TryShowQuarter2SignalReturnDialogue()
        {
            if (campaign == null ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter2Running ||
                campaign.Quarter2CurrentRoute != BranchRoute.Signal ||
                campaign.Quarter2CurrentEventOrder != 2 ||
                explorationManager == null ||
                !explorationManager.HasExploredToday)
            {
                return false;
            }

            return TryShowQuarter2SignalDialogue(2);
        }

        private bool TryShowQuarter2SignalDialogue(int eventOrder)
        {
            if (IsQuarter2SignalDialogueAcknowledged(eventOrder))
            {
                return false;
            }

            IReadOnlyList<ShelterIntroDialogueLine> lines =
                GetQuarter2SignalDialogueLines(eventOrder);
            if (lines.Count == 0)
            {
                return false;
            }

            ResolveReferences();
            if (dialogueUI == null)
            {
                MarkQuarter2SignalDialogueAcknowledged(eventOrder);
                List<string> bodies = new();
                foreach (ShelterIntroDialogueLine line in lines)
                {
                    bodies.Add(line.Body);
                }

                ShowMessage(string.Join("\n\n", bodies));
                return true;
            }

            if (pendingQuarter2SignalDialogueOrder != eventOrder)
            {
                pendingQuarter2SignalDialogueOrder = eventOrder;
                pendingQuarter2SignalDialogueIndex = 0;
            }

            int index = Mathf.Clamp(
                pendingQuarter2SignalDialogueIndex,
                0,
                lines.Count - 1);
            ShelterIntroDialogueLine currentLine = lines[index];
            bool isRadio = currentLine.Speaker == "라디오";
            bool isProtagonist = IsProtagonistSpeaker(currentLine.Speaker);
            pendingChoiceKind =
                PendingChoiceKind.Quarter2SignalMorningDialogue;
            dialogueUI.ShowDialogueLine(
                CurrentDay,
                DisplaySpeakerName(currentLine.Speaker),
                currentLine.Body,
                Quarter2SignalMorningDialogueChoice,
                isProtagonist ? protagonistPortrait : null,
                null,
                isRadio ? 0.62f : 1f);
            return true;
        }

        private static IReadOnlyList<ShelterIntroDialogueLine>
            GetQuarter2SignalDialogueLines(int eventOrder)
        {
            return eventOrder switch
            {
                1 => new[]
                {
                    new ShelterIntroDialogueLine("라디오", "치..지직..ㅊ..치칙"),
                    new ShelterIntroDialogueLine(
                        "한도윤",
                        "뭐지? 라디오를 확인해 봐야겠어.")
                },
                2 => new[]
                {
                    new ShelterIntroDialogueLine(
                        "한도윤",
                        "……탐사 도중 신호가 강해졌던 곳이 있었어. 다시 한번 가봐야겠어.")
                },
                3 => new[]
                {
                    new ShelterIntroDialogueLine("라디오", "치지직..ㅊ..직.."),
                    new ShelterIntroDialogueLine(
                        "한도윤",
                        "어? 소리가 더 선명하게 들리는 것 같은데? 확인해 봐야겠어.")
                },
                _ => Array.Empty<ShelterIntroDialogueLine>()
            };
        }

        private bool IsQuarter2SignalDialogueAcknowledged(int eventOrder)
        {
            int bit = 1 << Mathf.Max(0, eventOrder - 1);
            return (quarter2SignalDialogueAcknowledgedMask & bit) != 0;
        }

        private void MarkQuarter2SignalDialogueAcknowledged(int eventOrder)
        {
            int bit = 1 << Mathf.Max(0, eventOrder - 1);
            quarter2SignalDialogueAcknowledgedMask |= bit;
        }

        private bool TryShowQuarter2JoinReturnDialogue()
        {
            if (campaign == null ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter2Running ||
                campaign.Quarter2CurrentRoute != BranchRoute.Join ||
                explorationManager == null ||
                !explorationManager.HasExploredToday)
            {
                return false;
            }

            int eventOrder = campaign.Quarter2CurrentEventOrder;
            if (eventOrder is < 1 or > 3 ||
                IsQuarter2JoinReturnDialogueAcknowledged(eventOrder))
            {
                return false;
            }

            IReadOnlyList<ShelterIntroDialogueLine> lines =
                GetQuarter2JoinReturnDialogueLines(eventOrder);
            if (lines.Count == 0)
            {
                return false;
            }

            ResolveReferences();
            if (dialogueUI == null)
            {
                MarkQuarter2JoinReturnDialogueAcknowledged(eventOrder);
                List<string> bodies = new();
                foreach (ShelterIntroDialogueLine line in lines)
                {
                    bodies.Add(line.Body);
                }

                ShowMessage(string.Join("\n\n", bodies));
                return true;
            }

            if (pendingQuarter2JoinReturnDialogueOrder != eventOrder)
            {
                pendingQuarter2JoinReturnDialogueOrder = eventOrder;
                pendingQuarter2JoinReturnDialogueIndex = 0;
            }

            int index = Mathf.Clamp(
                pendingQuarter2JoinReturnDialogueIndex,
                0,
                lines.Count - 1);
            ShelterIntroDialogueLine currentLine = lines[index];
            bool isProtagonist = IsProtagonistSpeaker(currentLine.Speaker);
            pendingChoiceKind = PendingChoiceKind.Quarter2JoinReturnDialogue;
            dialogueUI.ShowDialogueLine(
                CurrentDay,
                DisplaySpeakerName(currentLine.Speaker),
                currentLine.Body,
                Quarter2JoinReturnDialogueChoice,
                isProtagonist ? protagonistPortrait : null,
                isProtagonist ? null : survivorGroupPortrait);
            return true;
        }

        private static IReadOnlyList<ShelterIntroDialogueLine>
            GetQuarter2JoinReturnDialogueLines(int eventOrder)
        {
            return eventOrder switch
            {
                1 => new[]
                {
                    new ShelterIntroDialogueLine(
                        "한도윤",
                        "들어오면서 문 앞에 뭔가 있었던 것 같은데…… 확인해 보자.")
                },
                2 => new[]
                {
                    new ShelterIntroDialogueLine("문밖의 사람", "똑똑똑.."),
                    new ShelterIntroDialogueLine(
                        "한도윤",
                        "누가 문을 두드린 것 같은데? 확인해 보자.")
                },
                3 => new[]
                {
                    new ShelterIntroDialogueLine(
                        "한도윤",
                        "문밖에 사람이 있던 것 같은데…… 말을 걸어봐야겠어.")
                },
                _ => Array.Empty<ShelterIntroDialogueLine>()
            };
        }

        private bool IsQuarter2JoinReturnDialogueAcknowledged(int eventOrder)
        {
            int bit = 1 << Mathf.Max(0, eventOrder - 1);
            return (quarter2JoinReturnDialogueAcknowledgedMask & bit) != 0;
        }

        private void MarkQuarter2JoinReturnDialogueAcknowledged(int eventOrder)
        {
            int bit = 1 << Mathf.Max(0, eventOrder - 1);
            quarter2JoinReturnDialogueAcknowledgedMask |= bit;
        }

        private void AcknowledgeQuarter2JoinReturnDialogue(string choiceId)
        {
            if (choiceId != Quarter2JoinReturnDialogueChoice)
            {
                return;
            }

            IReadOnlyList<ShelterIntroDialogueLine> lines =
                GetQuarter2JoinReturnDialogueLines(
                    pendingQuarter2JoinReturnDialogueOrder);
            pendingQuarter2JoinReturnDialogueIndex++;
            if (pendingQuarter2JoinReturnDialogueIndex < lines.Count)
            {
                TryShowQuarter2JoinReturnDialogue();
                return;
            }

            MarkQuarter2JoinReturnDialogueAcknowledged(
                pendingQuarter2JoinReturnDialogueOrder);
            pendingQuarter2JoinReturnDialogueOrder = -1;
            pendingQuarter2JoinReturnDialogueIndex = -1;
            pendingChoiceKind = PendingChoiceKind.None;
            dialogueUI?.Close();
        }

        private void AcknowledgeMorningStatusDialogue(string choiceId)
        {
            if (choiceId != MorningStatusDialogueChoice)
            {
                return;
            }

            pendingMorningDialogueLines.RemoveAt(0);
            if (pendingMorningDialogueLines.Count > 0)
            {
                ShowMorningStatusDialogue();
                return;
            }

            pendingChoiceKind = PendingChoiceKind.None;
            dialogueUI?.Close();
            ResumeAutomaticDialogue();
        }

        private void AcknowledgeLastSurvivalEndingDialogue(string choiceId)
        {
            if (choiceId != LastSurvivalEndingDialogueChoice)
            {
                return;
            }

            pendingChoiceKind = PendingChoiceKind.None;
            dialogueUI?.Close();
            Action completed = lastSurvivalEndingDialogueCompleted;
            lastSurvivalEndingDialogueCompleted = null;
            completed?.Invoke();
        }

        private void AcknowledgeQuarter3FinalSleepBlock(string choiceId)
        {
            if (choiceId != Quarter3FinalSleepBlockChoice)
            {
                return;
            }

            pendingChoiceKind = PendingChoiceKind.None;
            dialogueUI?.Close();
        }

        private void ResumeAutomaticDialogue()
        {
            TryShowLastBunkerIntro();
            TryShowQuarter3StartNotice();
        }

        private void QueueQuarter3FinalMorningDialogue()
        {
            if (quarter3FinalMorningDialogueShown ||
                campaign == null ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter3Running ||
                Quarter3LocalDay < 5)
            {
                return;
            }

            switch (campaign.Quarter3CurrentRoute)
            {
                case BranchRoute.Signal:
                    pendingMorningDialogueLines.Add(
                        "오늘 두 주파수에서 마지막 집결 안내를 보내겠다고 했지.");
                    pendingMorningDialogueLines.Add(
                        "마지막 방송까지 듣고… 어느 쪽을 믿을지 결정해야 한다.");
                    break;
                case BranchRoute.Join:
                    pendingMorningDialogueLines.Add(
                        "오늘 아침, 두 무리 모두 거점을 떠난다고 했지.");
                    pendingMorningDialogueLines.Add(
                        "두 무리의 마지막 입장을 확인하고… 누구와 함께 갈지 결정해야 한다.");
                    break;
                default:
                    return;
            }

            quarter3FinalMorningDialogueShown = true;
        }

        private void MarkSkippedJoinSupportDays()
        {
            int currentLocalDay = Quarter3LocalDay;
            for (int supportDay = 1; supportDay < currentLocalDay && supportDay <= 4; supportDay++)
            {
                if (campaign.GetQuarter3JoinSupportDayStatus(supportDay) !=
                    Quarter3JoinSupportDayStatus.Open)
                {
                    continue;
                }

                campaign.AttemptQuarter3JoinSupport(
                    supportDay,
                    Quarter3JoinSupportTarget.SurvivorGroup,
                    Quarter3JoinSupportResource.CannedFood,
                    new Quarter3JoinSupportInventorySnapshot(0, 0));
            }
        }

        private bool HasLegacyQuarter3FinalStoryAcknowledgement()
        {
            if (campaign == null ||
                campaign.CampaignState.Phase !=
                    EndingRouteCampaignPhase.Quarter3Running)
            {
                return false;
            }

            if (campaign.Quarter3CurrentRoute == BranchRoute.Signal)
            {
                if (!ShelterCampaignCatalog.TryGetQuarter3SignalStory(
                        5,
                        out string title,
                        out _))
                {
                    return false;
                }

                string titleMarker = $" - {title}\n";
                foreach (string record in campaignRecords)
                {
                    if (record.IndexOf(
                            titleMarker,
                            StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (campaign.Quarter3CurrentRoute != BranchRoute.Join ||
                !ShelterCampaignCatalog.TryGetQuarter3JoinCommonStory(
                    5,
                    out string joinTitle,
                    out _))
            {
                return false;
            }

            foreach (int viewedDay in joinStoryViewedDays)
            {
                string recordPrefix = $"{viewedDay}일차 - {joinTitle}";
                foreach (string record in campaignRecords)
                {
                    if (record.StartsWith(
                            recordPrefix,
                            StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CompleteCurrentDay()
        {
            completedDays.Add(CurrentDay);
            pendingChoiceKind = PendingChoiceKind.None;
        }

        private int GetSupportItemCount(ResourceType type)
        {
            return resources == null
                ? 0
                : resources.GetAmount(type) / ResourceUnitsPerSupportItem;
        }

        private void CreateCampaignIfNeeded()
        {
            if (campaign != null)
            {
                return;
            }

            settings = new BranchPrototypeSettings(
                BranchPrototypeSettings.DefaultStartDay,
                BranchPrototypeSettings.DefaultLastEventDay,
                BranchPrototypeSettings.DefaultDecisionDay,
                1,
                1,
                false,
                true,
                weightedRandomDecision);
            BranchOneFlowController quarter1 = new(
                ShelterCampaignCatalog.CreateQuarter1Catalog(),
                settings,
                new BranchRuntimeState(),
                new BranchScoreState(),
                new BranchDiaryRepository(),
                new BranchDecisionResolver());
            List<string> scheduledQuarter1EventIds = new();
            foreach (BranchEventDefinition definition in
                     quarter1.EventCatalog.GetEvents())
            {
                scheduledQuarter1EventIds.Add(definition.EventId);
            }

            ConfigureLastBunkerEvents(scheduledQuarter1EventIds, true);
            Quarter2FlowController quarter2 = new(
                ShelterCampaignCatalog.CreateQuarter2Catalog());
            quarter3ClueCatalog =
                ShelterCampaignCatalog.CreateQuarter3ClueCatalog();
            Quarter3FlowController quarter3 = new(
                quarter3ClueCatalog,
                ShelterCampaignCatalog.CreateFinalChoiceCatalog());
            Quarter3JoinSupportController support = new(
                ShelterCampaignCatalog.CreateJoinSupportCatalog());

            campaign = new EndingRouteCampaignController(
                quarter1,
                quarter2,
                quarter3,
                quarter3JoinSupportController: support);
            EndingRouteCampaignTransitionResult result = campaign.StartQuarter1();
            if (!result.IsSuccess)
            {
                Debug.LogError(result.FailureReason);
            }
        }

        private void ConfigureLastBunkerEvents(
            IEnumerable<string> excludedEventIds,
            bool randomizeOffset)
        {
            HashSet<string> excludedIds = new(
                excludedEventIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            List<BranchEventDefinition> unseenEvents = new();
            foreach (BranchEventDefinition definition in
                     ShelterQuarter1EventCatalog.GetLastBunkerDefinitions())
            {
                if (!excludedIds.Contains(definition.EventId))
                {
                    unseenEvents.Add(definition);
                }
            }

            lastBunkerEvents = unseenEvents.Count > 0
                ? unseenEvents.AsReadOnly()
                : ShelterQuarter1EventCatalog.GetLastBunkerDefinitions();
            lastBunkerEventOffset = lastBunkerEvents.Count == 0
                ? 0
                : randomizeOffset
                    ? UnityEngine.Random.Range(0, lastBunkerEvents.Count)
                    : lastBunkerEventOffset % lastBunkerEvents.Count;
        }

        private void ResolveReferences()
        {
            if (dayCycle == null) dayCycle = FindFirstObjectByType<DayCycleManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (dialogueUI == null)
            {
                dialogueUI = FindFirstObjectByType<ShelterEventDialogueUI>(FindObjectsInactive.Include);
            }

            if (resources == null) resources = FindFirstObjectByType<ResourceManager>();
            if (explorationManager == null) explorationManager = FindFirstObjectByType<ExplorationManager>();
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (characterStats == null) characterStats = FindFirstObjectByType<CharacterStats>();
            if (signalEndingSequence == null)
            {
                signalEndingSequence =
                    FindFirstObjectByType<SignalEndingSequence>(
                        FindObjectsInactive.Include);
            }
            if (joinEndingSequence == null)
            {
                joinEndingSequence =
                    FindFirstObjectByType<JoinEndingSequence>(
                        FindObjectsInactive.Include);
            }
            if (quarter3NoticeUI == null)
            {
                quarter3NoticeUI =
                    FindFirstObjectByType<SleepConfirmationUI>(
                        FindObjectsInactive.Include);
            }
        }

        private void BindDialogue()
        {
            if (dialogueUI == null)
            {
                return;
            }

            dialogueUI.ChoiceRequested -= HandleChoiceRequested;
            dialogueUI.ChoiceRequested += HandleChoiceRequested;
        }

        private void BindExplorationReturn()
        {
            if (subscribedExplorationManager == explorationManager)
            {
                return;
            }

            if (subscribedExplorationManager != null)
            {
                subscribedExplorationManager.ReturnedToShelter -=
                    HandleExplorationReturnedToShelter;
            }

            subscribedExplorationManager = explorationManager;
            if (subscribedExplorationManager != null)
            {
                subscribedExplorationManager.ReturnedToShelter +=
                    HandleExplorationReturnedToShelter;
            }
        }

        private void ShowMessage(string message)
        {
            if (uiManager != null)
            {
                uiManager.ShowMessage(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        private static bool IsProtagonistSpeaker(string speaker) =>
            speaker == "한도윤";

        private static string DisplaySpeakerName(string speaker) =>
            IsProtagonistSpeaker(speaker)
                ? GameSession.CurrentPlayerName
                : speaker;

        private ResourceManager resources;
        private GameManager gameManager;

        private enum PendingChoiceKind
        {
            None,
            Quarter1,
            LastBunker,
            Quarter2,
            Quarter2Intro,
            Quarter3SignalIntro,
            Quarter3JoinIntro,
            Quarter3RouteGuide,
            MorningStatusDialogue,
            Quarter2SignalMorningDialogue,
            Quarter2JoinReturnDialogue,
            LastSurvivalEndingDialogue,
            Quarter3FinalSleepBlock,
            LastBunkerIntro,
            DayOneTutorialMonologue,
            JoinTarget,
            JoinResource,
            FinalChoice
        }

        private enum DialoguePartner
        {
            None,
            RaiderGroup,
            SurvivorGroup,
            ArmedGroup,
            MilitaryRescue
        }
    }
}
