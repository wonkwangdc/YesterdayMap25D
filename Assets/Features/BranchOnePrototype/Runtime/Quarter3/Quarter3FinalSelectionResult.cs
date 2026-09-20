namespace YesterdayMap.BranchOne.Quarter3
{
    public sealed class Quarter3FinalSelectionResult
    {
        public bool IsSuccess { get; }
        public Quarter3FinalSelectionStatus Status { get; }
        public string RequestedFinalChoiceId { get; }
        public Quarter3FinalChoiceDefinition FinalChoiceDefinition { get; }
        public string SelectedFinalChoiceId =>
            FinalChoiceDefinition?.FinalChoiceId ?? string.Empty;
        public string FailureReason { get; }

        private Quarter3FinalSelectionResult(
            bool isSuccess,
            Quarter3FinalSelectionStatus status,
            string requestedFinalChoiceId,
            Quarter3FinalChoiceDefinition finalChoiceDefinition,
            string failureReason)
        {
            IsSuccess = isSuccess;
            Status = status;
            RequestedFinalChoiceId = requestedFinalChoiceId ?? string.Empty;
            FinalChoiceDefinition = finalChoiceDefinition;
            FailureReason = failureReason ?? string.Empty;
        }

        internal static Quarter3FinalSelectionResult Success(
            string requestedFinalChoiceId,
            Quarter3FinalChoiceDefinition finalChoiceDefinition)
        {
            return new Quarter3FinalSelectionResult(
                true,
                Quarter3FinalSelectionStatus.Success,
                requestedFinalChoiceId,
                finalChoiceDefinition,
                string.Empty);
        }

        internal static Quarter3FinalSelectionResult Failure(
            Quarter3FinalSelectionStatus status,
            string requestedFinalChoiceId,
            Quarter3FinalChoiceDefinition finalChoiceDefinition,
            string failureReason)
        {
            return new Quarter3FinalSelectionResult(
                false,
                status,
                requestedFinalChoiceId,
                finalChoiceDefinition,
                failureReason);
        }
    }
}
