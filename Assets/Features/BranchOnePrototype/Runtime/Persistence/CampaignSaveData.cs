using System;

namespace YesterdayMap.BranchOne.Persistence
{
    [Serializable]
    public sealed class CampaignSaveData
    {
        public int campaignPhase;
        public int quarter1Route;
        public int quarter2CurrentRoute;
        public int quarter2PassedRoute;
        public string quarter3FinalChoiceId = string.Empty;
        public bool allBranchesFailed;
        public Quarter1SaveData quarter1 = new();
        public Quarter2SaveData quarter2 = new();
        public Quarter3SaveData quarter3 = new();
        public JoinSupportSaveData joinSupport = new();
        public int[] completedDays = Array.Empty<int>();
        public int[] viewedDays = Array.Empty<int>();
        public int[] joinStoryViewedDays = Array.Empty<int>();
        public string[] campaignRecords = Array.Empty<string>();
        public int lastBunkerEventOffset;
        public int lastBunkerStartDay = -1;
        public bool usesLastBunkerStoryProgress;
        public int lastBunkerStoryDay;
        public int[] lastBunkerDiaryUnlockedDays = Array.Empty<int>();
        public int[] lastBunkerFixedEventCompletedDays = Array.Empty<int>();
        public bool lastBunkerIntroAcknowledged;
        public int quarter2LastChoiceDay = -1;
        public int quarter3StartDay = -1;
        public bool quarter3StartNoticeAcknowledged;
        public bool quarter3RouteGuideAcknowledged;
        public bool quarter3FinalStoryAcknowledged;
        public bool quarter3FinalMorningDialogueShown;
        public bool quarter2SignalMorningDialogueAcknowledged;
        public int quarter2SignalDialogueAcknowledgedMask;
        public bool quarter2JoinReturnDialogueAcknowledged;
        public int quarter2JoinReturnDialogueAcknowledgedMask;
        public string lastQuarter1ScoreChange = string.Empty;
        public string lastQuarter2DecisionResult = string.Empty;
    }

    [Serializable]
    public sealed class Quarter1SaveData
    {
        public int currentDay;
        public int phase;
        public string todaySelectedEventId = string.Empty;
        public bool todayEventCompleted;
        public bool decisionCompleted;
        public int decidedRoute;
        public int signalScore;
        public int joinScore;
        public string[] selectedEventIds = Array.Empty<string>();
        public DiaryDaySaveData[] diaryDays = Array.Empty<DiaryDaySaveData>();
    }

    [Serializable]
    public sealed class DiaryDaySaveData
    {
        public int day;
        public string mainDiary = string.Empty;
        public string[] eventRecords = Array.Empty<string>();
        public string[] explorationRecords = Array.Empty<string>();
    }

    [Serializable]
    public sealed class Quarter2SaveData
    {
        public int phase;
        public int initialRoute;
        public int currentRoute;
        public int currentEventOrder;
        public RouteProgressSaveData signal = new();
        public RouteProgressSaveData join = new();
        public int passedRoute;
        public int pendingRoute;
        public bool allBranchesFailed;
        public int lastDecision = -1;
    }

    [Serializable]
    public sealed class RouteProgressSaveData
    {
        public int progressCount;
        public int rejectCount;
        public bool attempted;
        public bool failed;
        public int completedEventCount;
    }

    [Serializable]
    public sealed class Quarter3SaveData
    {
        public int phase;
        public int currentRoute;
        public string[] acquiredClueIds = Array.Empty<string>();
        public string selectedFinalChoiceId = string.Empty;
    }

    [Serializable]
    public sealed class JoinSupportSaveData
    {
        public JoinSupportRecordSaveData[] records =
            Array.Empty<JoinSupportRecordSaveData>();
    }

    [Serializable]
    public sealed class JoinSupportRecordSaveData
    {
        public int day;
        public int outcome;
        public bool hasTarget;
        public int target;
        public bool hasResource;
        public int resource;
        public string clueId = string.Empty;
        public string sourceId = string.Empty;
    }
}
