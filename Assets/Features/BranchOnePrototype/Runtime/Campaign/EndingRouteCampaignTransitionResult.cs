namespace YesterdayMap.BranchOne.Campaign
{
    public sealed class EndingRouteCampaignTransitionResult
    {
        public bool IsSuccess { get; }
        public string FailureReason { get; }
        public string Message { get; }
        public EndingRouteCampaignPhase Phase { get; }

        private EndingRouteCampaignTransitionResult(
            bool isSuccess,
            string failureReason,
            string message,
            EndingRouteCampaignPhase phase)
        {
            IsSuccess = isSuccess;
            FailureReason = failureReason ?? string.Empty;
            Message = message ?? string.Empty;
            Phase = phase;
        }

        internal static EndingRouteCampaignTransitionResult Success(
            string message,
            EndingRouteCampaignPhase phase)
        {
            return new EndingRouteCampaignTransitionResult(
                true,
                string.Empty,
                message,
                phase);
        }

        internal static EndingRouteCampaignTransitionResult Failure(
            string failureReason,
            EndingRouteCampaignPhase phase)
        {
            return new EndingRouteCampaignTransitionResult(
                false,
                failureReason,
                string.Empty,
                phase);
        }
    }
}
