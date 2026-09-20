using System;

namespace YesterdayMap.BranchOne
{
    [Serializable]
    public sealed class BranchScoreState
    {
        public event Action ScoresChanged;

        public int SignalScore { get; private set; }
        public int JoinScore { get; private set; }
        public long TotalScore => (long)SignalScore + JoinScore;
        public double SignalRatio => TotalScore > 0 ? SignalScore / (double)TotalScore : 0d;
        public double JoinRatio => TotalScore > 0 ? JoinScore / (double)TotalScore : 0d;

        public BranchScoreState()
        {
        }

        public BranchScoreState(int startingSignalScore, int startingJoinScore)
        {
            SignalScore = ClampScore(startingSignalScore);
            JoinScore = ClampScore(startingJoinScore);
        }

        public void AddSignalScore(int amount)
        {
            int next = AddClamped(SignalScore, amount);
            if (next == SignalScore)
            {
                return;
            }

            SignalScore = next;
            ScoresChanged?.Invoke();
        }

        public void AddJoinScore(int amount)
        {
            int next = AddClamped(JoinScore, amount);
            if (next == JoinScore)
            {
                return;
            }

            JoinScore = next;
            ScoresChanged?.Invoke();
        }

        public void Reset(int startingSignalScore = 0, int startingJoinScore = 0)
        {
            int nextSignal = ClampScore(startingSignalScore);
            int nextJoin = ClampScore(startingJoinScore);
            bool changed = SignalScore != nextSignal || JoinScore != nextJoin;

            SignalScore = nextSignal;
            JoinScore = nextJoin;

            if (changed)
            {
                ScoresChanged?.Invoke();
            }
        }

        private static int AddClamped(int current, int amount)
        {
            long result = (long)current + amount;
            if (result <= 0)
            {
                return 0;
            }

            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        private static int ClampScore(int score)
        {
            return Math.Max(0, score);
        }
    }
}
