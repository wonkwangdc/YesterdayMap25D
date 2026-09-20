namespace YesterdayMap.BranchOne.Quarter3
{
    public sealed class Quarter3ClueAcquisitionResult
    {
        public bool IsSuccess { get; }
        public Quarter3ClueAcquisitionStatus Status { get; }
        public string RequestedClueId { get; }
        public Quarter3ClueDefinition ClueDefinition { get; }
        public string AcquiredClueId =>
            ClueDefinition?.ClueId ?? string.Empty;
        public string FailureReason { get; }

        private Quarter3ClueAcquisitionResult(
            bool isSuccess,
            Quarter3ClueAcquisitionStatus status,
            string requestedClueId,
            Quarter3ClueDefinition clueDefinition,
            string failureReason)
        {
            IsSuccess = isSuccess;
            Status = status;
            RequestedClueId = requestedClueId ?? string.Empty;
            ClueDefinition = clueDefinition;
            FailureReason = failureReason ?? string.Empty;
        }

        internal static Quarter3ClueAcquisitionResult Success(
            string requestedClueId,
            Quarter3ClueDefinition clueDefinition)
        {
            return new Quarter3ClueAcquisitionResult(
                true,
                Quarter3ClueAcquisitionStatus.Success,
                requestedClueId,
                clueDefinition,
                string.Empty);
        }

        internal static Quarter3ClueAcquisitionResult Failure(
            Quarter3ClueAcquisitionStatus status,
            string requestedClueId,
            Quarter3ClueDefinition clueDefinition,
            string failureReason)
        {
            return new Quarter3ClueAcquisitionResult(
                false,
                status,
                requestedClueId,
                clueDefinition,
                failureReason);
        }
    }
}
