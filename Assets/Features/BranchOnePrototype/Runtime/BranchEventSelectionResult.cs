namespace YesterdayMap.BranchOne.Events
{
    public sealed class BranchEventSelectionResult
    {
        public bool IsSuccess { get; }
        public string FailureReason { get; }
        public BranchEventDefinition EventDefinition { get; }
        public BranchEventChoice Choice { get; }
        public int AppliedSignalScoreDelta { get; }
        public int AppliedJoinScoreDelta { get; }

        private BranchEventSelectionResult(
            bool isSuccess,
            string failureReason,
            BranchEventDefinition eventDefinition,
            BranchEventChoice choice,
            int appliedSignalScoreDelta,
            int appliedJoinScoreDelta)
        {
            IsSuccess = isSuccess;
            FailureReason = failureReason ?? string.Empty;
            EventDefinition = eventDefinition;
            Choice = choice;
            AppliedSignalScoreDelta = appliedSignalScoreDelta;
            AppliedJoinScoreDelta = appliedJoinScoreDelta;
        }

        internal static BranchEventSelectionResult Success(
            BranchEventDefinition eventDefinition,
            BranchEventChoice choice,
            int appliedSignalScoreDelta,
            int appliedJoinScoreDelta)
        {
            return new BranchEventSelectionResult(
                true,
                string.Empty,
                eventDefinition,
                choice,
                appliedSignalScoreDelta,
                appliedJoinScoreDelta);
        }

        internal static BranchEventSelectionResult Failure(
            string failureReason,
            BranchEventDefinition eventDefinition = null,
            BranchEventChoice choice = null)
        {
            return new BranchEventSelectionResult(
                false,
                failureReason,
                eventDefinition,
                choice,
                0,
                0);
        }
    }
}
