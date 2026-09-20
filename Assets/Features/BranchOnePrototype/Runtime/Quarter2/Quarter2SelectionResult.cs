namespace YesterdayMap.BranchOne.Quarter2
{
    public sealed class Quarter2SelectionResult
    {
        public bool IsSuccess { get; }
        public string FailureReason { get; }
        public Quarter2Decision Decision { get; }
        public Quarter2EventDefinition EventDefinition { get; }
        public int ProgressCount { get; }
        public int RejectCount { get; }
        public Quarter2FlowPhase Phase { get; }

        private Quarter2SelectionResult(
            bool isSuccess,
            string failureReason,
            Quarter2Decision decision,
            Quarter2EventDefinition eventDefinition,
            int progressCount,
            int rejectCount,
            Quarter2FlowPhase phase)
        {
            IsSuccess = isSuccess;
            FailureReason = failureReason ?? string.Empty;
            Decision = decision;
            EventDefinition = eventDefinition;
            ProgressCount = progressCount;
            RejectCount = rejectCount;
            Phase = phase;
        }

        internal static Quarter2SelectionResult Success(
            Quarter2Decision decision,
            Quarter2EventDefinition eventDefinition,
            Quarter2RouteProgress progress,
            Quarter2FlowPhase phase)
        {
            return new Quarter2SelectionResult(
                true,
                string.Empty,
                decision,
                eventDefinition,
                progress.ProgressCount,
                progress.RejectCount,
                phase);
        }

        internal static Quarter2SelectionResult Failure(
            string failureReason,
            Quarter2Decision decision,
            Quarter2EventDefinition eventDefinition,
            Quarter2RouteProgress progress,
            Quarter2FlowPhase phase)
        {
            return new Quarter2SelectionResult(
                false,
                failureReason,
                decision,
                eventDefinition,
                progress?.ProgressCount ?? 0,
                progress?.RejectCount ?? 0,
                phase);
        }
    }
}
