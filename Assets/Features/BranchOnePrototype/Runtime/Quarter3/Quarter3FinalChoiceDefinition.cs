using System;

namespace YesterdayMap.BranchOne.Quarter3
{
    [Serializable]
    public sealed class Quarter3FinalChoiceDefinition
    {
        public string FinalChoiceId { get; }
        public BranchRoute Route { get; }

        public Quarter3FinalChoiceDefinition(
            string finalChoiceId,
            BranchRoute route)
        {
            FinalChoiceId = finalChoiceId ?? string.Empty;
            Route = route;
        }

        public bool TryValidate(out string failureReason)
        {
            if (string.IsNullOrWhiteSpace(FinalChoiceId))
            {
                failureReason =
                    "Quarter 3 final choice ID cannot be empty.";
                return false;
            }

            if (!IsPlayableRoute(Route))
            {
                failureReason =
                    "Quarter 3 final choices must use the Signal or Join route.";
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
