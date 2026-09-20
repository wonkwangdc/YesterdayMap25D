using System;
using System.Collections.Generic;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Flow;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.BranchOne.Campaign
{
    public sealed class EndingRouteCampaignController
    {
        public event Action StateChanged;

        public BranchOneFlowController Quarter1Controller { get; }
        public Quarter2FlowController Quarter2Controller { get; }
        public Quarter3FlowController Quarter3Controller { get; }
        public Quarter3JoinSupportController Quarter3JoinSupportController { get; }
        public EndingRouteCampaignState CampaignState { get; }
        public Quarter2FlowPhase Quarter2Phase =>
            Quarter2Controller.RuntimeState.Phase;
        public BranchRoute Quarter2CurrentRoute =>
            Quarter2Controller.RuntimeState.CurrentRoute;
        public int Quarter2CurrentEventOrder =>
            Quarter2Controller.RuntimeState.CurrentEventOrder;
        public int SignalProgressCount =>
            Quarter2Controller.RuntimeState.SignalProgress.ProgressCount;
        public int SignalRejectCount =>
            Quarter2Controller.RuntimeState.SignalProgress.RejectCount;
        public bool SignalFailed =>
            Quarter2Controller.RuntimeState.SignalProgress.Failed;
        public int JoinProgressCount =>
            Quarter2Controller.RuntimeState.JoinProgress.ProgressCount;
        public int JoinRejectCount =>
            Quarter2Controller.RuntimeState.JoinProgress.RejectCount;
        public bool JoinFailed =>
            Quarter2Controller.RuntimeState.JoinProgress.Failed;
        public BranchRoute Quarter2PassedRoute =>
            CampaignState.Quarter2PassedRoute;
        public bool AreAllBranchesFailed =>
            CampaignState.AreAllBranchesFailed;
        public Quarter3FlowPhase Quarter3Phase =>
            Quarter3Controller.Phase;
        public BranchRoute Quarter3CurrentRoute =>
            Quarter3Controller.CurrentRoute;
        public bool Quarter3IsActive =>
            Quarter3Controller.IsActive;
        public bool Quarter3IsFinalChoiceOpen =>
            Quarter3Controller.IsFinalChoiceOpen;
        public bool Quarter3IsResolved =>
            Quarter3Controller.IsResolved;
        public IReadOnlyList<string> Quarter3AcquiredClueIds =>
            Quarter3Controller.AcquiredClueIds;
        public string Quarter3SelectedFinalChoiceId =>
            Quarter3Controller.SelectedFinalChoiceId;

        public EndingRouteCampaignController(
            BranchOneFlowController quarter1Controller,
            Quarter2FlowController quarter2Controller,
            Quarter3FlowController quarter3Controller,
            EndingRouteCampaignState campaignState = null,
            Quarter3JoinSupportController quarter3JoinSupportController = null)
        {
            Quarter1Controller = quarter1Controller ??
                throw new ArgumentNullException(nameof(quarter1Controller));
            Quarter2Controller = quarter2Controller ??
                throw new ArgumentNullException(nameof(quarter2Controller));
            Quarter3Controller = quarter3Controller ??
                throw new ArgumentNullException(nameof(quarter3Controller));
            Quarter3JoinSupportController = quarter3JoinSupportController;
            if (Quarter3JoinSupportController != null &&
                !Quarter3JoinSupportController.TryValidate(
                    Quarter3Controller.ClueCatalog,
                    out string joinSupportFailureReason))
            {
                throw new ArgumentException(
                    $"Invalid Quarter 3 Join support catalog: {joinSupportFailureReason}",
                    nameof(quarter3JoinSupportController));
            }
            CampaignState = campaignState ?? new EndingRouteCampaignState();

            Quarter1Controller.DecisionResolved += HandleQuarter1DecisionResolved;
        }

        public EndingRouteCampaignTransitionResult StartQuarter1()
        {
            if (CampaignState.Phase != EndingRouteCampaignPhase.NotStarted)
            {
                return TransitionFailure("The ending route campaign has already started.");
            }

            Quarter1Controller.StartTest();
            CampaignState.BeginQuarter1();
            return TransitionSuccess(
                "Quarter 1 started.",
                EndingRouteCampaignPhase.Quarter1Running);
        }

        public EndingRouteCampaignTransitionResult AdvanceToQuarter2()
        {
            SynchronizeState();

            if (CampaignState.Phase != EndingRouteCampaignPhase.Quarter1Resolved)
            {
                return TransitionFailure(
                    "Quarter 1 must be resolved before Quarter 2 can start.");
            }

            BranchDecisionResult decisionResult =
                CampaignState.Quarter1DecisionResult;
            if (decisionResult == null || !decisionResult.IsSuccess)
            {
                return TransitionFailure(
                    "Quarter 1 did not produce a successful branch decision.");
            }

            if (!IsPlayableRoute(CampaignState.Quarter1Route))
            {
                return TransitionFailure(
                    "Quarter 1 did not produce the Signal or Join route.");
            }

            if (Quarter2Controller.RuntimeState.Phase !=
                Quarter2FlowPhase.NotStarted)
            {
                return TransitionFailure("Quarter 2 has already started.");
            }

            if (!Quarter2Controller.StartQuarter2(CampaignState.Quarter1Route))
            {
                return TransitionFailure(Quarter2Controller.LastFailureReason);
            }

            CampaignState.BeginQuarter2(CampaignState.Quarter1Route);
            return TransitionSuccess(
                "Quarter 2 started.",
                EndingRouteCampaignPhase.Quarter2Running);
        }

        public Quarter2SelectionResult SelectQuarter2Decision(
            Quarter2Decision decision)
        {
            if (CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter2Running)
            {
                const string failureReason =
                    "Quarter 2 decisions require a running Quarter 2 campaign.";
                RecordOperationFailure(failureReason);
                return Quarter2SelectionResult.Failure(
                    failureReason,
                    decision,
                    GetCurrentQuarter2Event(),
                    Quarter2Controller.RuntimeState.GetRouteProgress(
                        Quarter2CurrentRoute),
                    Quarter2Phase);
            }

            Quarter2SelectionResult result =
                Quarter2Controller.SelectDecision(decision);
            if (!result.IsSuccess)
            {
                RecordOperationFailure(result.FailureReason);
                return result;
            }

            SynchronizeState();
            return result;
        }

        public EndingRouteCampaignTransitionResult AdvanceQuarter2Step()
        {
            if (CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter2Running)
            {
                return TransitionFailure(
                    "Quarter 2 can only advance while the campaign is running.");
            }

            if (!Quarter2Controller.AdvanceToNextStep())
            {
                return TransitionFailure(Quarter2Controller.LastFailureReason);
            }

            SynchronizeStateCore();
            return TransitionSuccess(
                "Quarter 2 advanced to the next step.",
                CampaignState.Phase);
        }

        public Quarter2EventDefinition GetCurrentQuarter2Event()
        {
            return CampaignState.Phase ==
                   EndingRouteCampaignPhase.Quarter2Running
                ? Quarter2Controller.GetCurrentEvent()
                : null;
        }

        public EndingRouteCampaignTransitionResult AdvanceToQuarter3()
        {
            SynchronizeState();

            if (CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter2Passed)
            {
                return TransitionFailure(
                    "Quarter 2 must be passed before Quarter 3 can start.");
            }

            BranchRoute passedRoute = CampaignState.Quarter2PassedRoute;
            if (!IsPlayableRoute(passedRoute))
            {
                return TransitionFailure(
                    "Quarter 2 did not produce a passed route.");
            }

            if (Quarter3Controller.Phase !=
                Quarter3FlowPhase.NotStarted)
            {
                return TransitionFailure("Quarter 3 has already started.");
            }

            if (!Quarter3Controller.StartQuarter3(passedRoute))
            {
                return TransitionFailure(
                    Quarter3Controller.LastFailureReason);
            }

            CampaignState.BeginQuarter3();
            return TransitionSuccess(
                "Quarter 3 started.",
                EndingRouteCampaignPhase.Quarter3Running);
        }

        public IReadOnlyList<Quarter3ClueDefinition>
            GetQuarter3AvailableClues(string sourceId)
        {
            return CampaignState.Phase ==
                   EndingRouteCampaignPhase.Quarter3Running
                ? Quarter3Controller.GetAvailableClues(sourceId)
                : Array.Empty<Quarter3ClueDefinition>();
        }

        public Quarter3ClueAcquisitionResult AcquireQuarter3Clue(
            string clueId)
        {
            if (CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter3Running)
            {
                Quarter3ClueAcquisitionStatus status =
                    Quarter3Controller.IsResolved
                        ? Quarter3ClueAcquisitionStatus.Resolved
                        : Quarter3ClueAcquisitionStatus.NotStarted;
                const string failureReason =
                    "Quarter 3 clues require a running Quarter 3 campaign.";
                RecordOperationFailure(failureReason);
                return Quarter3ClueAcquisitionResult.Failure(
                    status,
                    clueId,
                    null,
                    failureReason);
            }

            Quarter3ClueAcquisitionResult result =
                Quarter3Controller.TryAcquireClue(clueId);
            if (!result.IsSuccess)
            {
                RecordOperationFailure(result.FailureReason);
                return result;
            }

            NotifyStateChanged();
            return result;
        }

        public Quarter3JoinSupportDayStatus GetQuarter3JoinSupportDayStatus(
            int day)
        {
            return Quarter3JoinSupportController == null
                ? Quarter3JoinSupportDayStatus.Open
                : Quarter3JoinSupportController.GetSupportDayStatus(day);
        }

        public Quarter3JoinSupportRecord GetQuarter3JoinSupportRecord(int day)
        {
            return Quarter3JoinSupportController?.GetSupportRecord(day);
        }

        public (int SurvivorGroup, int RedArmband)
            GetQuarter3JoinSupportCounts()
        {
            return Quarter3JoinSupportController == null
                ? (0, 0)
                : (Quarter3JoinSupportController.GetSurvivorSupportCount(),
                   Quarter3JoinSupportController.GetRedArmbandSupportCount());
        }

        public Quarter3JoinSupportAttemptResult AttemptQuarter3JoinSupport(
            int day,
            Quarter3JoinSupportTarget target,
            Quarter3JoinSupportResource resource,
            Quarter3JoinSupportInventorySnapshot inventory)
        {
            Quarter3JoinSupportOutcome blockedOutcome;
            string blockedReason;
            if (CampaignState.Phase != EndingRouteCampaignPhase.Quarter3Running)
            {
                blockedOutcome = Quarter3Controller.IsResolved
                    ? Quarter3JoinSupportOutcome.Quarter3AlreadyResolved
                    : Quarter3JoinSupportOutcome.WrongCampaignPhase;
                blockedReason = "Join support requires a running Quarter 3 campaign.";
                return JoinSupportFailure(blockedOutcome, day, target, resource, blockedReason);
            }
            if (Quarter3Controller.CurrentRoute != BranchRoute.Join)
                return JoinSupportFailure(Quarter3JoinSupportOutcome.WrongRoute,
                    day, target, resource, "Join support is only available on the Join route.");
            if (Quarter3JoinSupportController == null)
                return JoinSupportFailure(Quarter3JoinSupportOutcome.DefinitionNotFound,
                    day, target, resource, "Join support is not configured.");

            Quarter3JoinSupportAttemptResult result =
                Quarter3JoinSupportController.PreflightSupport(day, target, resource, inventory);
            if (result.Outcome == Quarter3JoinSupportOutcome.MissedNoResources)
            {
                Quarter3JoinSupportController.CommitMissedNoResources(day);
                NotifyStateChanged();
                return result;
            }
            if (!result.IsSuccess)
                return result;

            Quarter3JoinSupportController.EnsureCanCommit(result);
            Quarter3ClueAcquisitionResult clue =
                Quarter3Controller.TryAcquireClue(result.AcquiredClueId);
            if (!clue.IsSuccess)
                return JoinSupportFailure(Quarter3JoinSupportOutcome.ClueNotAvailable,
                    day, target, resource, clue.FailureReason);

            Quarter3JoinSupportController.CommitSupported(result);
            NotifyStateChanged();
            return new Quarter3JoinSupportAttemptResult(
                Quarter3JoinSupportOutcome.Supported, day, target, resource,
                resource, result.AcquiredClueId, result.AcquiredSourceId,
                Quarter3JoinSupportController.GetSurvivorSupportCount(),
                Quarter3JoinSupportController.GetRedArmbandSupportCount(),
                string.Empty);
        }

        public bool HasQuarter3Clue(string clueId)
        {
            return CanReadQuarter3() &&
                   Quarter3Controller.HasClue(clueId);
        }

        public IReadOnlyList<Quarter3ClueDefinition>
            GetQuarter3AcquiredClues()
        {
            return CanReadQuarter3()
                ? Quarter3Controller.GetAcquiredClues()
                : Array.Empty<Quarter3ClueDefinition>();
        }

        public Quarter3SourceProgress GetQuarter3SourceProgress(
            string sourceId)
        {
            return CanReadQuarter3()
                ? Quarter3Controller.GetSourceProgress(sourceId)
                : new Quarter3SourceProgress(sourceId, 0, 0);
        }

        public IReadOnlyList<Quarter3FinalChoiceDefinition>
            GetQuarter3FinalChoiceOptions()
        {
            return CanReadQuarter3()
                ? Quarter3Controller.GetFinalChoiceOptions()
                : Array.Empty<Quarter3FinalChoiceDefinition>();
        }

        public EndingRouteCampaignTransitionResult
            OpenQuarter3FinalChoice()
        {
            if (CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter3Running)
            {
                return TransitionFailure(
                    "Quarter 3 final choices require a running " +
                    "Quarter 3 campaign.");
            }

            if (!Quarter3Controller.OpenFinalChoice())
            {
                return TransitionFailure(
                    Quarter3Controller.LastFailureReason);
            }

            return TransitionSuccess(
                "Quarter 3 final choices opened.",
                CampaignState.Phase);
        }

        public Quarter3FinalSelectionResult SelectQuarter3FinalChoice(
            string finalChoiceId)
        {
            if (CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter3Running)
            {
                Quarter3FinalSelectionStatus status =
                    Quarter3Controller.IsResolved
                        ? Quarter3FinalSelectionStatus.AlreadyResolved
                        : Quarter3FinalSelectionStatus.NotStarted;
                const string failureReason =
                    "Quarter 3 final selection requires a running " +
                    "Quarter 3 campaign.";
                RecordOperationFailure(failureReason);
                return Quarter3FinalSelectionResult.Failure(
                    status,
                    finalChoiceId,
                    null,
                    failureReason);
            }

            Quarter3FinalSelectionResult result =
                Quarter3Controller.SelectFinalChoice(finalChoiceId);
            if (!result.IsSuccess)
            {
                RecordOperationFailure(result.FailureReason);
                return result;
            }

            CampaignState.MarkQuarter3Resolved(
                result.SelectedFinalChoiceId);
            CampaignState.SetLastTransitionResult(
                EndingRouteCampaignTransitionResult.Success(
                    "Quarter 3 final choice selected.",
                    CampaignState.Phase));
            NotifyStateChanged();
            return result;
        }

        public void SynchronizeState()
        {
            if (SynchronizeStateCore())
            {
                NotifyStateChanged();
            }
        }

        private bool SynchronizeStateCore()
        {
            if (CampaignState.Phase ==
                    EndingRouteCampaignPhase.Quarter1Running &&
                Quarter1Controller.DecisionResult != null)
            {
                CampaignState.RecordQuarter1Result(
                    Quarter1Controller.DecisionResult);
                CampaignState.SetLastTransitionResult(
                    EndingRouteCampaignTransitionResult.Success(
                        "Quarter 1 result synchronized.",
                        CampaignState.Phase));
                return true;
            }

            if (CampaignState.Phase ==
                EndingRouteCampaignPhase.Quarter3Running)
            {
                if (Quarter3Controller.Phase ==
                    Quarter3FlowPhase.Resolved)
                {
                    CampaignState.MarkQuarter3Resolved(
                        Quarter3Controller.SelectedFinalChoiceId);
                    CampaignState.SetLastTransitionResult(
                        EndingRouteCampaignTransitionResult.Success(
                            "Quarter 3 result synchronized.",
                            CampaignState.Phase));
                }

                return true;
            }

            if (CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter2Running)
            {
                return false;
            }

            Quarter2RuntimeState quarter2State =
                Quarter2Controller.RuntimeState;
            CampaignState.SynchronizeQuarter2Route(
                quarter2State.CurrentRoute);

            if (quarter2State.Phase == Quarter2FlowPhase.Quarter2Passed)
            {
                CampaignState.MarkQuarter2Passed(quarter2State.PassedRoute);
                CampaignState.SetLastTransitionResult(
                    EndingRouteCampaignTransitionResult.Success(
                        "Quarter 2 route passed.",
                        CampaignState.Phase));
            }
            else if (
                quarter2State.Phase ==
                Quarter2FlowPhase.AllBranchesFailed)
            {
                CampaignState.MarkAllBranchesFailed(
                    quarter2State.CurrentRoute);
                CampaignState.SetLastTransitionResult(
                    EndingRouteCampaignTransitionResult.Success(
                        "All Quarter 2 branches failed.",
                        CampaignState.Phase));
            }

            return true;
        }

        public void ResetCampaign()
        {
            Quarter1Controller.Reset();
            Quarter2Controller.Reset();
            Quarter3Controller.Reset();
            Quarter3JoinSupportController?.Reset();
            CampaignState.Reset();
            NotifyStateChanged();
        }

        private void HandleQuarter1DecisionResolved(
            BranchDecisionResult result)
        {
            if (CampaignState.Phase !=
                EndingRouteCampaignPhase.Quarter1Running)
            {
                return;
            }

            CampaignState.RecordQuarter1Result(result);
            CampaignState.SetLastTransitionResult(
                EndingRouteCampaignTransitionResult.Success(
                    "Quarter 1 result synchronized.",
                    CampaignState.Phase));
            NotifyStateChanged();
        }

        private EndingRouteCampaignTransitionResult TransitionSuccess(
            string message,
            EndingRouteCampaignPhase phase)
        {
            EndingRouteCampaignTransitionResult result =
                EndingRouteCampaignTransitionResult.Success(message, phase);
            CampaignState.SetLastTransitionResult(result);
            NotifyStateChanged();
            return result;
        }

        private EndingRouteCampaignTransitionResult TransitionFailure(
            string failureReason)
        {
            EndingRouteCampaignTransitionResult result =
                EndingRouteCampaignTransitionResult.Failure(
                    failureReason,
                    CampaignState.Phase);
            CampaignState.SetLastTransitionResult(result);
            NotifyStateChanged();
            return result;
        }

        private void RecordOperationFailure(string failureReason)
        {
            CampaignState.SetLastTransitionResult(
                EndingRouteCampaignTransitionResult.Failure(
                    failureReason,
                    CampaignState.Phase));
            NotifyStateChanged();
        }

        private Quarter3JoinSupportAttemptResult JoinSupportFailure(
            Quarter3JoinSupportOutcome outcome, int day,
            Quarter3JoinSupportTarget target,
            Quarter3JoinSupportResource resource, string reason)
        {
            RecordOperationFailure(reason);
            (int survivor, int red) = GetQuarter3JoinSupportCounts();
            return new Quarter3JoinSupportAttemptResult(outcome, day, target,
                resource, null, string.Empty, string.Empty, survivor, red, reason);
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private bool CanReadQuarter3()
        {
            return CampaignState.Phase ==
                       EndingRouteCampaignPhase.Quarter3Running ||
                   CampaignState.Phase ==
                       EndingRouteCampaignPhase.Quarter3Resolved;
        }

        private static bool IsPlayableRoute(BranchRoute route)
        {
            return route == BranchRoute.Signal || route == BranchRoute.Join;
        }
    }
}
