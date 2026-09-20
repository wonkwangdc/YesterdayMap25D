using System;

namespace YesterdayMap.BranchOne.Quarter3
{
    [Serializable]
    public sealed class Quarter3ClueDefinition
    {
        public string ClueId { get; }
        public BranchRoute Route { get; }
        public string SourceId { get; }
        public string Title { get; }
        public string Content { get; }

        public Quarter3ClueDefinition(
            string clueId,
            BranchRoute route,
            string sourceId)
            : this(clueId, route, sourceId, string.Empty, string.Empty)
        {
        }

        public Quarter3ClueDefinition(
            string clueId,
            BranchRoute route,
            string sourceId,
            string title,
            string content)
        {
            ClueId = clueId ?? string.Empty;
            Route = route;
            SourceId = sourceId ?? string.Empty;
            Title = title ?? string.Empty;
            Content = content ?? string.Empty;
        }

        public bool TryValidate(out string failureReason)
        {
            if (string.IsNullOrWhiteSpace(ClueId))
            {
                failureReason = "Quarter 3 clue ID cannot be empty.";
                return false;
            }

            if (!IsPlayableRoute(Route))
            {
                failureReason =
                    "Quarter 3 clues must use the Signal or Join route.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SourceId))
            {
                failureReason = "Quarter 3 clue source ID cannot be empty.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private static bool IsPlayableRoute(BranchRoute route)
        {
            return route == BranchRoute.Signal || route == BranchRoute.Join;
        }
    }
}
