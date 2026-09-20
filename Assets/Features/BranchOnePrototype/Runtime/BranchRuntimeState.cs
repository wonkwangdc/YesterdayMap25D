using System;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.BranchOne
{
    [Serializable]
    public sealed class BranchRuntimeState
    {
        public int CurrentDay { get; private set; }
        public BranchFlowPhase Phase { get; private set; } = BranchFlowPhase.NotStarted;
        public string TodaySelectedEventId { get; private set; } = string.Empty;
        public bool IsTodayEventCompleted { get; private set; }
        public bool IsDecisionCompleted { get; private set; }
        public BranchRoute DecidedRoute { get; private set; } = BranchRoute.None;

        public void Initialize(BranchPrototypeSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Reset();
            CurrentDay = settings.StartDay;
            Phase = CurrentDay >= settings.DecisionDay
                ? BranchFlowPhase.ResolvingBranch
                : BranchFlowPhase.WaitingForEvent;
        }

        public void Reset()
        {
            CurrentDay = 0;
            Phase = BranchFlowPhase.NotStarted;
            TodaySelectedEventId = string.Empty;
            IsTodayEventCompleted = false;
            IsDecisionCompleted = false;
            DecidedRoute = BranchRoute.None;
        }

        public void BeginEventDay(int day)
        {
            if (day < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(day), "Day must be at least 1.");
            }

            CurrentDay = day;
            Phase = BranchFlowPhase.WaitingForEvent;
            TodaySelectedEventId = string.Empty;
            IsTodayEventCompleted = false;
        }

        public void CompleteEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
            }

            if (Phase != BranchFlowPhase.WaitingForEvent)
            {
                throw new InvalidOperationException("An event can only complete while waiting for an event.");
            }

            TodaySelectedEventId = eventId;
            IsTodayEventCompleted = true;
            Phase = BranchFlowPhase.EventCompleted;
        }

        public void MarkReadyToAdvance()
        {
            if (!IsTodayEventCompleted)
            {
                throw new InvalidOperationException("The current event must be completed first.");
            }

            Phase = BranchFlowPhase.ReadyToAdvance;
        }

        public void BeginBranchResolution(int day)
        {
            if (day < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(day), "Day must be at least 1.");
            }

            CurrentDay = day;
            Phase = BranchFlowPhase.ResolvingBranch;
            TodaySelectedEventId = string.Empty;
            IsTodayEventCompleted = false;
        }

        public void FinishDecision(BranchRoute route)
        {
            if (Phase != BranchFlowPhase.ResolvingBranch)
            {
                throw new InvalidOperationException("Branch resolution has not started.");
            }

            DecidedRoute = route;
            IsDecisionCompleted = true;
            Phase = BranchFlowPhase.Finished;
        }

        public void Restore(Quarter1SaveData data)
        {
            if (data == null) return;
            CurrentDay = Math.Max(0, data.currentDay);
            Phase = (BranchFlowPhase)data.phase;
            TodaySelectedEventId = data.todaySelectedEventId ?? string.Empty;
            IsTodayEventCompleted = data.todayEventCompleted;
            IsDecisionCompleted = data.decisionCompleted;
            DecidedRoute = (BranchRoute)data.decidedRoute;
        }
    }
}
