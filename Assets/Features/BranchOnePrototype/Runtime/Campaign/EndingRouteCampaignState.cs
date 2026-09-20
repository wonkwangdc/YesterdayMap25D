using System;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.BranchOne.Campaign
{
    [Serializable]
    public sealed class EndingRouteCampaignState
    {
        public EndingRouteCampaignPhase Phase { get; private set; } =
            EndingRouteCampaignPhase.NotStarted;
        public BranchRoute Quarter1Route { get; private set; } = BranchRoute.None;
        public BranchRoute Quarter2CurrentRoute { get; private set; } =
            BranchRoute.None;
        public BranchRoute Quarter2PassedRoute { get; private set; } =
            BranchRoute.None;
        public string Quarter3FinalChoiceId { get; private set; } =
            string.Empty;
        public bool AreAllBranchesFailed { get; private set; }
        public BranchDecisionResult Quarter1DecisionResult { get; private set; }
        public EndingRouteCampaignTransitionResult LastTransitionResult
        {
            get;
            private set;
        }

        public void Reset()
        {
            Phase = EndingRouteCampaignPhase.NotStarted;
            Quarter1Route = BranchRoute.None;
            Quarter2CurrentRoute = BranchRoute.None;
            Quarter2PassedRoute = BranchRoute.None;
            Quarter3FinalChoiceId = string.Empty;
            AreAllBranchesFailed = false;
            Quarter1DecisionResult = null;
            LastTransitionResult = null;
        }

        internal void BeginQuarter1()
        {
            Phase = EndingRouteCampaignPhase.Quarter1Running;
            Quarter1Route = BranchRoute.None;
            Quarter2CurrentRoute = BranchRoute.None;
            Quarter2PassedRoute = BranchRoute.None;
            Quarter3FinalChoiceId = string.Empty;
            AreAllBranchesFailed = false;
            Quarter1DecisionResult = null;
        }

        internal void RecordQuarter1Result(BranchDecisionResult result)
        {
            Quarter1DecisionResult = result;
            Quarter1Route =
                result != null &&
                result.IsSuccess &&
                IsPlayableRoute(result.DecidedRoute)
                    ? result.DecidedRoute
                    : BranchRoute.None;
            Phase = EndingRouteCampaignPhase.Quarter1Resolved;
        }

        internal void BeginQuarter2(BranchRoute route)
        {
            Quarter2CurrentRoute = route;
            Quarter2PassedRoute = BranchRoute.None;
            Quarter3FinalChoiceId = string.Empty;
            AreAllBranchesFailed = false;
            Phase = EndingRouteCampaignPhase.Quarter2Running;
        }

        internal void SynchronizeQuarter2Route(BranchRoute route)
        {
            Quarter2CurrentRoute = route;
        }

        internal void MarkQuarter2Passed(BranchRoute passedRoute)
        {
            Quarter2CurrentRoute = passedRoute;
            Quarter2PassedRoute = passedRoute;
            AreAllBranchesFailed = false;
            Phase = EndingRouteCampaignPhase.Quarter2Passed;
        }

        internal void BeginQuarter3()
        {
            Quarter3FinalChoiceId = string.Empty;
            Phase = EndingRouteCampaignPhase.Quarter3Running;
        }

        internal void MarkQuarter3Resolved(string finalChoiceId)
        {
            Quarter3FinalChoiceId = finalChoiceId ?? string.Empty;
            Phase = EndingRouteCampaignPhase.Quarter3Resolved;
        }

        internal void MarkAllBranchesFailed(BranchRoute currentRoute)
        {
            Quarter2CurrentRoute = currentRoute;
            Quarter2PassedRoute = BranchRoute.None;
            AreAllBranchesFailed = true;
            Phase = EndingRouteCampaignPhase.AllBranchesFailed;
        }

        internal void SetLastTransitionResult(
            EndingRouteCampaignTransitionResult result)
        {
            LastTransitionResult = result;
        }

        public void Restore(CampaignSaveData data)
        {
            Reset();
            if (data == null) return;

            Phase = (EndingRouteCampaignPhase)data.campaignPhase;
            Quarter1Route = (BranchRoute)data.quarter1Route;
            Quarter2CurrentRoute = (BranchRoute)data.quarter2CurrentRoute;
            Quarter2PassedRoute = (BranchRoute)data.quarter2PassedRoute;
            Quarter3FinalChoiceId = data.quarter3FinalChoiceId ?? string.Empty;
            AreAllBranchesFailed = data.allBranchesFailed;

            if (data.quarter1 != null && data.quarter1.decisionCompleted &&
                Quarter1Route != BranchRoute.None)
            {
                long total = (long)data.quarter1.signalScore + data.quarter1.joinScore;
                double signalRatio = total > 0
                    ? data.quarter1.signalScore / (double)total
                    : 0d;
                double joinRatio = total > 0
                    ? data.quarter1.joinScore / (double)total
                    : 0d;
                Quarter1DecisionResult = BranchDecisionResult.Success(
                    data.quarter1.signalScore,
                    data.quarter1.joinScore,
                    signalRatio,
                    joinRatio,
                    0.5d,
                    Quarter1Route);
            }
        }

        private static bool IsPlayableRoute(BranchRoute route)
        {
            return route == BranchRoute.Signal || route == BranchRoute.Join;
        }
    }
}
