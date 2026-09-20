using System;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.BranchOne.Quarter2
{
    [Serializable]
    public sealed class Quarter2RouteProgress
    {
        public BranchRoute Route { get; }
        public int ProgressCount { get; private set; }
        public int RejectCount { get; private set; }
        public bool Attempted { get; private set; }
        public bool Failed { get; private set; }
        public int CompletedEventCount { get; private set; }

        internal Quarter2RouteProgress(BranchRoute route)
        {
            if (route != BranchRoute.Signal && route != BranchRoute.Join)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(route),
                    "Quarter 2 progress requires the Signal or Join route.");
            }

            Route = route;
        }

        internal void BeginAttempt()
        {
            Attempted = true;
        }

        internal void ApplyDecision(Quarter2Decision decision)
        {
            if (decision == Quarter2Decision.Progress)
            {
                ProgressCount++;
            }
            else if (decision == Quarter2Decision.Reject)
            {
                RejectCount++;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(decision));
            }

            CompletedEventCount++;
        }

        internal void MarkFailed()
        {
            Failed = true;
        }

        internal void Reset()
        {
            ProgressCount = 0;
            RejectCount = 0;
            Attempted = false;
            Failed = false;
            CompletedEventCount = 0;
        }

        public RouteProgressSaveData CaptureState()
        {
            return new RouteProgressSaveData
            {
                progressCount = ProgressCount,
                rejectCount = RejectCount,
                attempted = Attempted,
                failed = Failed,
                completedEventCount = CompletedEventCount
            };
        }

        internal void Restore(RouteProgressSaveData data)
        {
            Reset();
            if (data == null) return;
            ProgressCount = Math.Max(0, data.progressCount);
            RejectCount = Math.Max(0, data.rejectCount);
            Attempted = data.attempted;
            Failed = data.failed;
            CompletedEventCount = Math.Max(0, data.completedEventCount);
        }
    }
}
