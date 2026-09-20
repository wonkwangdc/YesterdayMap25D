using System;

namespace YesterdayMap.BranchOne.Quarter2
{
    [Serializable]
    public sealed class Quarter2EventDefinition
    {
        public string EventId { get; }
        public BranchRoute Route { get; }
        public int Order { get; }
        public string Title { get; }
        public string Body { get; }
        public string Location { get; }
        public string ProgressChoiceText { get; }
        public string ProgressResultText { get; }
        public string RejectChoiceText { get; }
        public string RejectResultText { get; }
        public float ProgressHealthDelta { get; }
        public float ProgressHungerDelta { get; }
        public float ProgressThirstDelta { get; }
        public float ProgressMoraleDelta { get; }
        public bool RequiresExploration { get; }

        public Quarter2EventDefinition(
            string eventId,
            BranchRoute route,
            int order,
            string title,
            string body,
            string location = "",
            string progressChoiceText = "",
            string progressResultText = "",
            string rejectChoiceText = "",
            string rejectResultText = "",
            float progressHealthDelta = 0f,
            float progressHungerDelta = 0f,
            float progressThirstDelta = 0f,
            float progressMoraleDelta = 0f,
            bool requiresExploration = false)
        {
            EventId = eventId ?? string.Empty;
            Route = route;
            Order = order;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Location = location ?? string.Empty;
            ProgressChoiceText = progressChoiceText ?? string.Empty;
            ProgressResultText = progressResultText ?? string.Empty;
            RejectChoiceText = rejectChoiceText ?? string.Empty;
            RejectResultText = rejectResultText ?? string.Empty;
            ProgressHealthDelta = progressHealthDelta;
            ProgressHungerDelta = progressHungerDelta;
            ProgressThirstDelta = progressThirstDelta;
            ProgressMoraleDelta = progressMoraleDelta;
            RequiresExploration = requiresExploration;
        }

        public bool TryValidate(out string failureReason)
        {
            if (string.IsNullOrWhiteSpace(EventId))
            {
                failureReason = "Quarter 2 event ID cannot be empty.";
                return false;
            }

            if (!IsPlayableRoute(Route))
            {
                failureReason = "Quarter 2 events must use the Signal or Join route.";
                return false;
            }

            if (Order <= 0)
            {
                failureReason = "Quarter 2 event order must be greater than zero.";
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
