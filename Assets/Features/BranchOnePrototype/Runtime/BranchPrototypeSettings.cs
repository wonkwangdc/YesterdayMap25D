using System;

namespace YesterdayMap.BranchOne
{
    [Serializable]
    public sealed class BranchPrototypeSettings
    {
        public const int DefaultStartDay = 2;
        public const int DefaultLastEventDay = 6;
        public const int DefaultDecisionDay = 7;

        public int StartDay { get; }
        public int LastEventDay { get; }
        public int DecisionDay { get; }
        public int StartingSignalScore { get; }
        public int StartingJoinScore { get; }
        public bool RequireEventBeforeAdvance { get; }
        public bool AllowRepeatedEvents { get; }
        public bool WeightedRandomDecision { get; }

        public BranchPrototypeSettings()
            : this(
                DefaultStartDay,
                DefaultLastEventDay,
                DefaultDecisionDay,
                0,
                0,
                true,
                true,
                true)
        {
        }

        public BranchPrototypeSettings(
            int startDay,
            int lastEventDay,
            int decisionDay,
            int startingSignalScore,
            int startingJoinScore,
            bool requireEventBeforeAdvance,
            bool allowRepeatedEvents,
            bool weightedRandomDecision)
        {
            if (startDay < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startDay), "Start day must be at least 1.");
            }

            if (lastEventDay < startDay)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lastEventDay),
                    "Last event day cannot be before the start day.");
            }

            if (decisionDay <= lastEventDay)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decisionDay),
                    "Decision day must be after the last event day.");
            }

            StartDay = startDay;
            LastEventDay = lastEventDay;
            DecisionDay = decisionDay;
            StartingSignalScore = Math.Max(0, startingSignalScore);
            StartingJoinScore = Math.Max(0, startingJoinScore);
            RequireEventBeforeAdvance = requireEventBeforeAdvance;
            AllowRepeatedEvents = allowRepeatedEvents;
            WeightedRandomDecision = weightedRandomDecision;
        }
    }
}
