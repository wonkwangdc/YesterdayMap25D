namespace YesterdayMap.BranchOne.Sandbox
{
    public static class BranchSandboxSettings
    {
        public const int StartingScorePerRoute = 1;

        public static BranchPrototypeSettings Create(
            bool weightedRandomDecision = true)
        {
            return new BranchPrototypeSettings(
                BranchPrototypeSettings.DefaultStartDay,
                BranchPrototypeSettings.DefaultLastEventDay,
                BranchPrototypeSettings.DefaultDecisionDay,
                StartingScorePerRoute,
                StartingScorePerRoute,
                true,
                true,
                weightedRandomDecision);
        }
    }
}
