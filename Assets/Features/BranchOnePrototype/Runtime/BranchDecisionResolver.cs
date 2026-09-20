using System;

namespace YesterdayMap.BranchOne.Decision
{
    public sealed class BranchDecisionResolver
    {
        private const string EmptyScoreFailureReason = "Total score must be greater than zero.";

        public BranchDecisionResult Resolve(int signalScore, int joinScore, double randomRoll)
        {
            return Resolve(signalScore, joinScore, randomRoll, true);
        }

        public BranchDecisionResult Resolve(
            int signalScore,
            int joinScore,
            double randomRoll,
            bool weightedRandomDecision)
        {
            int safeSignalScore = Math.Max(0, signalScore);
            int safeJoinScore = Math.Max(0, joinScore);
            double safeRandomRoll = NormalizeRandomRoll(randomRoll);
            long totalScore = (long)safeSignalScore + safeJoinScore;

            if (totalScore == 0)
            {
                return BranchDecisionResult.Failure(
                    safeSignalScore,
                    safeJoinScore,
                    safeRandomRoll,
                    EmptyScoreFailureReason);
            }

            double signalRatio = safeSignalScore / (double)totalScore;
            double joinRatio = safeJoinScore / (double)totalScore;
            BranchRoute route;

            if (safeJoinScore == 0)
            {
                route = BranchRoute.Signal;
            }
            else if (safeSignalScore == 0)
            {
                route = BranchRoute.Join;
            }
            else if (!weightedRandomDecision && safeSignalScore != safeJoinScore)
            {
                route = safeSignalScore > safeJoinScore
                    ? BranchRoute.Signal
                    : BranchRoute.Join;
            }
            else
            {
                route = safeRandomRoll < signalRatio
                    ? BranchRoute.Signal
                    : BranchRoute.Join;
            }

            return BranchDecisionResult.Success(
                safeSignalScore,
                safeJoinScore,
                signalRatio,
                joinRatio,
                safeRandomRoll,
                route);
        }

        private static double NormalizeRandomRoll(double randomRoll)
        {
            if (double.IsNaN(randomRoll))
            {
                return 0.5d;
            }

            if (randomRoll <= 0d)
            {
                return 0d;
            }

            return randomRoll >= 1d ? 1d : randomRoll;
        }
    }
}
