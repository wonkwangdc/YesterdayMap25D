namespace YesterdayMap.BranchOne.Decision
{
    public sealed class BranchDecisionResult
    {
        public int FinalSignalScore { get; }
        public int FinalJoinScore { get; }
        public double SignalRatio { get; }
        public double JoinRatio { get; }
        public double RandomRoll { get; }
        public BranchRoute DecidedRoute { get; }
        public bool IsSuccess { get; }
        public string FailureReason { get; }

        private BranchDecisionResult(
            int finalSignalScore,
            int finalJoinScore,
            double signalRatio,
            double joinRatio,
            double randomRoll,
            BranchRoute decidedRoute,
            bool isSuccess,
            string failureReason)
        {
            FinalSignalScore = finalSignalScore;
            FinalJoinScore = finalJoinScore;
            SignalRatio = signalRatio;
            JoinRatio = joinRatio;
            RandomRoll = randomRoll;
            DecidedRoute = decidedRoute;
            IsSuccess = isSuccess;
            FailureReason = failureReason ?? string.Empty;
        }

        internal static BranchDecisionResult Success(
            int signalScore,
            int joinScore,
            double signalRatio,
            double joinRatio,
            double randomRoll,
            BranchRoute route)
        {
            return new BranchDecisionResult(
                signalScore,
                joinScore,
                signalRatio,
                joinRatio,
                randomRoll,
                route,
                true,
                string.Empty);
        }

        internal static BranchDecisionResult Failure(
            int signalScore,
            int joinScore,
            double randomRoll,
            string failureReason)
        {
            return new BranchDecisionResult(
                signalScore,
                joinScore,
                0d,
                0d,
                randomRoll,
                BranchRoute.None,
                false,
                failureReason);
        }
    }
}
