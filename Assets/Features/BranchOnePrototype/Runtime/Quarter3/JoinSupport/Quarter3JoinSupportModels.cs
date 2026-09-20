using System;

namespace YesterdayMap.BranchOne.Quarter3.JoinSupport
{
    public enum Quarter3JoinSupportTarget { SurvivorGroup, RedArmband }
    public enum Quarter3JoinSupportResource { CannedFood, Water }
    public enum Quarter3JoinSupportDayStatus { Open, Supported, MissedNoResources }
    public enum Quarter3JoinSupportOutcome
    {
        Supported, MissedNoResources, SelectedResourceUnavailable,
        InvalidSupportDay, SupportDayAlreadyProcessed, DefinitionNotFound,
        InvalidTarget, InvalidResource, WrongCampaignPhase, WrongRoute,
        Quarter3AlreadyResolved, ClueNotAvailable
    }

    [Serializable]
    public sealed class Quarter3JoinSupportDefinition
    {
        public int SupportDay { get; }
        public Quarter3JoinSupportTarget Target { get; }
        public string SourceId { get; }
        public string ClueId { get; }
        public string DisplayName { get; }

        public Quarter3JoinSupportDefinition(int supportDay,
            Quarter3JoinSupportTarget target, string sourceId, string clueId,
            string displayName = "")
        {
            SupportDay = supportDay;
            Target = target;
            SourceId = sourceId ?? string.Empty;
            ClueId = clueId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }
    }

    public readonly struct Quarter3JoinSupportInventorySnapshot
    {
        public int CannedFoodCount { get; }
        public int WaterCount { get; }
        public Quarter3JoinSupportInventorySnapshot(int cannedFoodCount, int waterCount)
        {
            if (cannedFoodCount < 0) throw new ArgumentOutOfRangeException(nameof(cannedFoodCount));
            if (waterCount < 0) throw new ArgumentOutOfRangeException(nameof(waterCount));
            CannedFoodCount = cannedFoodCount;
            WaterCount = waterCount;
        }
        public int GetCount(Quarter3JoinSupportResource resource) =>
            resource == Quarter3JoinSupportResource.CannedFood
                ? CannedFoodCount : WaterCount;
        public bool HasAny => CannedFoodCount > 0 || WaterCount > 0;
    }

    public sealed class Quarter3JoinSupportRecord
    {
        public int SupportDay { get; }
        public Quarter3JoinSupportOutcome Outcome { get; }
        public Quarter3JoinSupportTarget? Target { get; }
        public Quarter3JoinSupportResource? Resource { get; }
        public string ClueId { get; }
        public string SourceId { get; }
        public Quarter3JoinSupportRecord(int day, Quarter3JoinSupportOutcome outcome,
            Quarter3JoinSupportTarget? target = null,
            Quarter3JoinSupportResource? resource = null,
            string clueId = "", string sourceId = "")
        {
            SupportDay = day; Outcome = outcome; Target = target; Resource = resource;
            ClueId = clueId ?? string.Empty; SourceId = sourceId ?? string.Empty;
        }
    }

    public sealed class Quarter3JoinSupportAttemptResult
    {
        public bool IsSuccess => Outcome == Quarter3JoinSupportOutcome.Supported;
        public bool IsProcessed => Outcome == Quarter3JoinSupportOutcome.Supported ||
                                   Outcome == Quarter3JoinSupportOutcome.MissedNoResources;
        public Quarter3JoinSupportOutcome Outcome { get; }
        public int SupportDay { get; }
        public Quarter3JoinSupportTarget Target { get; }
        public Quarter3JoinSupportResource SelectedResource { get; }
        public Quarter3JoinSupportResource? ConsumeResource { get; }
        public int ConsumeAmount { get; }
        public string AcquiredClueId { get; }
        public string AcquiredSourceId { get; }
        public int SurvivorSupportCount { get; }
        public int RedArmbandSupportCount { get; }
        public string FailureReason { get; }
        internal int StateVersion { get; }

        internal Quarter3JoinSupportAttemptResult(Quarter3JoinSupportOutcome outcome,
            int day, Quarter3JoinSupportTarget target,
            Quarter3JoinSupportResource selectedResource,
            Quarter3JoinSupportResource? consumeResource, string clueId,
            string sourceId, int survivorCount, int redCount, string reason,
            int stateVersion = 0)
        {
            Outcome = outcome; SupportDay = day; Target = target;
            SelectedResource = selectedResource; ConsumeResource = consumeResource;
            ConsumeAmount = consumeResource.HasValue ? 1 : 0;
            AcquiredClueId = clueId ?? string.Empty;
            AcquiredSourceId = sourceId ?? string.Empty;
            SurvivorSupportCount = survivorCount;
            RedArmbandSupportCount = redCount;
            FailureReason = reason ?? string.Empty;
            StateVersion = stateVersion;
        }
    }
}
