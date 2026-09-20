namespace YesterdayMap.BranchOne
{
    public enum BranchFlowPhase
    {
        NotStarted,
        WaitingForEvent,
        EventCompleted,
        ReadyToAdvance,
        ResolvingBranch,
        Finished
    }
}
