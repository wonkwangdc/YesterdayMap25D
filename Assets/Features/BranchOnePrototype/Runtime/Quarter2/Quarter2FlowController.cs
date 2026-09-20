using System;

namespace YesterdayMap.BranchOne.Quarter2
{
    public sealed class Quarter2FlowController
    {
        private const int RejectLimit = 2;

        public Quarter2EventCatalog EventCatalog { get; }
        public Quarter2RuntimeState RuntimeState { get; }
        public string LastFailureReason { get; private set; } = string.Empty;

        public Quarter2FlowController(
            Quarter2EventCatalog eventCatalog,
            Quarter2RuntimeState runtimeState = null)
        {
            EventCatalog = eventCatalog ??
                throw new ArgumentNullException(nameof(eventCatalog));
            RuntimeState = runtimeState ?? new Quarter2RuntimeState();
        }

        public bool StartQuarter2(BranchRoute initialRoute)
        {
            if (RuntimeState.Phase != Quarter2FlowPhase.NotStarted)
            {
                return FlowFailure("Quarter 2 has already started.");
            }

            if (!IsPlayableRoute(initialRoute))
            {
                return FlowFailure("Quarter 2 requires the Signal or Join route.");
            }

            if (!EventCatalog.TryValidate(out string failureReason))
            {
                return FlowFailure(failureReason);
            }

            RuntimeState.Start(initialRoute);
            LastFailureReason = string.Empty;
            return true;
        }

        public Quarter2EventDefinition GetCurrentEvent()
        {
            if (RuntimeState.Phase != Quarter2FlowPhase.WaitingForChoice)
            {
                return null;
            }

            return EventCatalog.TryGet(
                RuntimeState.CurrentRoute,
                RuntimeState.CurrentEventOrder,
                out Quarter2EventDefinition definition)
                ? definition
                : null;
        }

        public Quarter2SelectionResult SelectDecision(Quarter2Decision decision)
        {
            Quarter2EventDefinition currentEvent = GetCurrentEvent();
            Quarter2RouteProgress currentProgress =
                RuntimeState.GetRouteProgress(RuntimeState.CurrentRoute);

            if (RuntimeState.Phase != Quarter2FlowPhase.WaitingForChoice)
            {
                return SelectionFailure(
                    "A Quarter 2 decision can only be selected while waiting for a choice.",
                    decision,
                    currentEvent,
                    currentProgress);
            }

            if (!Enum.IsDefined(typeof(Quarter2Decision), decision))
            {
                return SelectionFailure(
                    "The Quarter 2 decision is invalid.",
                    decision,
                    currentEvent,
                    currentProgress);
            }

            if (currentEvent == null || currentProgress == null)
            {
                return SelectionFailure(
                    "The current Quarter 2 event could not be found.",
                    decision,
                    currentEvent,
                    currentProgress);
            }

            currentProgress.ApplyDecision(decision);
            RuntimeState.RecordDecision(decision);

            if (currentProgress.RejectCount >= RejectLimit)
            {
                currentProgress.MarkFailed();
                BranchRoute oppositeRoute = GetOppositeRoute(RuntimeState.CurrentRoute);
                Quarter2RouteProgress oppositeProgress =
                    RuntimeState.GetRouteProgress(oppositeRoute);

                if (oppositeProgress.Failed)
                {
                    RuntimeState.MarkAllBranchesFailed();
                }
                else
                {
                    RuntimeState.MarkRouteSwitchPending(oppositeRoute);
                }
            }
            else if (
                RuntimeState.CurrentEventOrder ==
                Quarter2EventCatalog.RequiredEventCountPerRoute)
            {
                RuntimeState.MarkPassed();
            }
            else
            {
                RuntimeState.WaitForAdvance();
            }

            LastFailureReason = string.Empty;
            return Quarter2SelectionResult.Success(
                decision,
                currentEvent,
                currentProgress,
                RuntimeState.Phase);
        }

        public bool AdvanceToNextStep()
        {
            if (RuntimeState.Phase == Quarter2FlowPhase.WaitingForAdvance)
            {
                RuntimeState.AdvanceCurrentRoute();
                LastFailureReason = string.Empty;
                return true;
            }

            if (RuntimeState.Phase == Quarter2FlowPhase.RouteSwitchPending)
            {
                RuntimeState.StartPendingRoute();
                LastFailureReason = string.Empty;
                return true;
            }

            return FlowFailure(
                "The Quarter 2 flow is not waiting to advance to the next step.");
        }

        public void Reset()
        {
            RuntimeState.Reset();
            LastFailureReason = string.Empty;
        }

        private Quarter2SelectionResult SelectionFailure(
            string failureReason,
            Quarter2Decision decision,
            Quarter2EventDefinition eventDefinition,
            Quarter2RouteProgress progress)
        {
            LastFailureReason = failureReason;
            return Quarter2SelectionResult.Failure(
                failureReason,
                decision,
                eventDefinition,
                progress,
                RuntimeState.Phase);
        }

        private bool FlowFailure(string failureReason)
        {
            LastFailureReason = failureReason;
            return false;
        }

        private static bool IsPlayableRoute(BranchRoute route)
        {
            return route == BranchRoute.Signal || route == BranchRoute.Join;
        }

        private static BranchRoute GetOppositeRoute(BranchRoute route)
        {
            return route == BranchRoute.Signal
                ? BranchRoute.Join
                : BranchRoute.Signal;
        }
    }
}
