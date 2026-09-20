namespace YesterdayMap.BranchOne.Quarter3
{
    public sealed class Quarter3SourceProgress
    {
        public string SourceId { get; }
        public int TotalCount { get; }
        public int AcquiredCount { get; }
        public int RemainingCount { get; }

        public Quarter3SourceProgress(
            string sourceId,
            int totalCount,
            int acquiredCount)
        {
            SourceId = sourceId ?? string.Empty;
            TotalCount = totalCount;
            AcquiredCount = acquiredCount;
            RemainingCount = totalCount - acquiredCount;
        }
    }
}
