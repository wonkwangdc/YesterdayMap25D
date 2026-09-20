using System;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.BranchOne.Quarter2
{
    [Serializable]
    public sealed class Quarter2RuntimeState
    {
        public Quarter2FlowPhase Phase { get; private set; } =
            Quarter2FlowPhase.NotStarted;
        public BranchRoute InitialRoute { get; private set; } = BranchRoute.None;
        public BranchRoute CurrentRoute { get; private set; } = BranchRoute.None;
        public int CurrentEventOrder { get; private set; }
        public Quarter2RouteProgress SignalProgress { get; } =
            new(BranchRoute.Signal);
        public Quarter2RouteProgress JoinProgress { get; } =
            new(BranchRoute.Join);
        public BranchRoute PassedRoute { get; private set; } = BranchRoute.None;
        public BranchRoute PendingRoute { get; private set; } = BranchRoute.None;
        public bool AreAllBranchesFailed { get; private set; }
        public Quarter2Decision? LastDecision { get; private set; }

        public Quarter2RouteProgress GetRouteProgress(BranchRoute route)
        {
            return route switch
            {
                BranchRoute.Signal => SignalProgress,
                BranchRoute.Join => JoinProgress,
                _ => null
            };
        }

        public void Reset()
        {
            Phase = Quarter2FlowPhase.NotStarted;
            InitialRoute = BranchRoute.None;
            CurrentRoute = BranchRoute.None;
            CurrentEventOrder = 0;
            SignalProgress.Reset();
            JoinProgress.Reset();
            PassedRoute = BranchRoute.None;
            PendingRoute = BranchRoute.None;
            AreAllBranchesFailed = false;
            LastDecision = null;
        }

        internal void Start(BranchRoute initialRoute)
        {
            Reset();
            InitialRoute = initialRoute;
            CurrentRoute = initialRoute;
            CurrentEventOrder = 1;
            GetRouteProgress(initialRoute).BeginAttempt();
            Phase = Quarter2FlowPhase.WaitingForChoice;
        }

        internal void RecordDecision(Quarter2Decision decision)
        {
            LastDecision = decision;
        }

        internal void WaitForAdvance()
        {
            Phase = Quarter2FlowPhase.WaitingForAdvance;
        }

        internal void AdvanceCurrentRoute()
        {
            CurrentEventOrder++;
            Phase = Quarter2FlowPhase.WaitingForChoice;
        }

        internal void MarkRouteSwitchPending(BranchRoute pendingRoute)
        {
            PendingRoute = pendingRoute;
            Phase = Quarter2FlowPhase.RouteSwitchPending;
        }

        internal void StartPendingRoute()
        {
            CurrentRoute = PendingRoute;
            PendingRoute = BranchRoute.None;
            CurrentEventOrder = 1;
            GetRouteProgress(CurrentRoute).BeginAttempt();
            Phase = Quarter2FlowPhase.WaitingForChoice;
        }

        internal void MarkPassed()
        {
            PassedRoute = CurrentRoute;
            PendingRoute = BranchRoute.None;
            Phase = Quarter2FlowPhase.Quarter2Passed;
        }

        internal void MarkAllBranchesFailed()
        {
            PendingRoute = BranchRoute.None;
            AreAllBranchesFailed = true;
            Phase = Quarter2FlowPhase.AllBranchesFailed;
        }

        public Quarter2SaveData CaptureState()
        {
            return new Quarter2SaveData
            {
                phase = (int)Phase,
                initialRoute = (int)InitialRoute,
                currentRoute = (int)CurrentRoute,
                currentEventOrder = CurrentEventOrder,
                signal = SignalProgress.CaptureState(),
                join = JoinProgress.CaptureState(),
                passedRoute = (int)PassedRoute,
                pendingRoute = (int)PendingRoute,
                allBranchesFailed = AreAllBranchesFailed,
                lastDecision = LastDecision.HasValue ? (int)LastDecision.Value : -1
            };
        }

        public void Restore(Quarter2SaveData data)
        {
            Reset();
            if (data == null) return;

            Phase = (Quarter2FlowPhase)data.phase;
            InitialRoute = (BranchRoute)data.initialRoute;
            CurrentRoute = (BranchRoute)data.currentRoute;
            CurrentEventOrder = Math.Max(0, data.currentEventOrder);
            SignalProgress.Restore(data.signal);
            JoinProgress.Restore(data.join);
            PassedRoute = (BranchRoute)data.passedRoute;
            PendingRoute = (BranchRoute)data.pendingRoute;
            AreAllBranchesFailed = data.allBranchesFailed;
            LastDecision = data.lastDecision >= 0
                ? (Quarter2Decision?)data.lastDecision
                : null;
        }
    }
}
