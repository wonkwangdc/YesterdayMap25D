using System.Collections.Generic;
using YesterdayMap.BranchOne.Persistence;

namespace YesterdayMap.BranchOne.Quarter3.JoinSupport
{
    public sealed class Quarter3JoinSupportController
    {
        private readonly Quarter3JoinSupportCatalog catalog;
        private readonly Dictionary<int, Quarter3JoinSupportRecord> records = new();
        private int survivorCount;
        private int redCount;
        private int stateVersion;

        public Quarter3JoinSupportController(Quarter3JoinSupportCatalog catalog)
        { this.catalog = catalog ?? throw new System.ArgumentNullException(nameof(catalog)); }

        public bool TryValidate(Quarter3ClueCatalog clues, out string reason) =>
            catalog.TryValidate(clues, out reason);
        public Quarter3JoinSupportDefinition GetSupportDefinition(int day,
            Quarter3JoinSupportTarget target) =>
            catalog.TryGet(day, target, out Quarter3JoinSupportDefinition value) ? value : null;
        public Quarter3JoinSupportDayStatus GetSupportDayStatus(int day) =>
            !records.TryGetValue(day, out Quarter3JoinSupportRecord value)
                ? Quarter3JoinSupportDayStatus.Open
                : value.Outcome == Quarter3JoinSupportOutcome.Supported
                    ? Quarter3JoinSupportDayStatus.Supported
                    : Quarter3JoinSupportDayStatus.MissedNoResources;
        public bool HasProcessedSupportDay(int day) => records.ContainsKey(day);
        public Quarter3JoinSupportRecord GetSupportRecord(int day) =>
            records.TryGetValue(day, out Quarter3JoinSupportRecord value) ? value : null;
        public int GetSurvivorSupportCount() => survivorCount;
        public int GetRedArmbandSupportCount() => redCount;

        public Quarter3JoinSupportAttemptResult AttemptSupport(int day,
            Quarter3JoinSupportTarget target, Quarter3JoinSupportResource resource,
            Quarter3JoinSupportInventorySnapshot inventory)
        {
            Quarter3JoinSupportAttemptResult result =
                PreflightSupport(day, target, resource, inventory);
            if (result.Outcome == Quarter3JoinSupportOutcome.MissedNoResources)
                CommitMissedNoResources(day);
            else if (result.IsSuccess)
                CommitSupported(result);
            return result.IsSuccess
                ? Result(Quarter3JoinSupportOutcome.Supported, day, target,
                    resource, resource, GetSupportDefinition(day, target), string.Empty)
                : result;
        }

        public Quarter3JoinSupportAttemptResult PreflightSupport(int day,
            Quarter3JoinSupportTarget target, Quarter3JoinSupportResource resource,
            Quarter3JoinSupportInventorySnapshot inventory)
        {
            if (day < 1 || day > 4) return Result(Quarter3JoinSupportOutcome.InvalidSupportDay, day, target, resource, null, null, "Support day must be between 1 and 4.");
            if (records.ContainsKey(day)) return Result(Quarter3JoinSupportOutcome.SupportDayAlreadyProcessed, day, target, resource, null, null, "Support day has already been processed.");
            if (!System.Enum.IsDefined(typeof(Quarter3JoinSupportTarget), target)) return Result(Quarter3JoinSupportOutcome.InvalidTarget, day, target, resource, null, null, "Invalid support target.");
            if (!System.Enum.IsDefined(typeof(Quarter3JoinSupportResource), resource)) return Result(Quarter3JoinSupportOutcome.InvalidResource, day, target, resource, null, null, "Invalid support resource.");
            if (!inventory.HasAny)
            {
                return Result(Quarter3JoinSupportOutcome.MissedNoResources, day, target, resource, null, null, string.Empty);
            }
            if (inventory.GetCount(resource) == 0) return Result(Quarter3JoinSupportOutcome.SelectedResourceUnavailable, day, target, resource, null, null, "Selected support resource is unavailable.");
            if (!catalog.TryGet(day, target, out Quarter3JoinSupportDefinition definition))
                return Result(Quarter3JoinSupportOutcome.DefinitionNotFound, day, target, resource, null, null, "Support definition was not found.");
            return Result(Quarter3JoinSupportOutcome.Supported, day, target, resource, resource, definition, string.Empty);
        }

        public void CommitMissedNoResources(int day)
        {
            if (day < 1 || day > 4)
                throw new System.ArgumentOutOfRangeException(nameof(day));
            if (records.ContainsKey(day))
                throw new System.InvalidOperationException("Support day has already been processed.");
            records.Add(day, new Quarter3JoinSupportRecord(
                day, Quarter3JoinSupportOutcome.MissedNoResources));
            stateVersion++;
        }

        public void CommitSupported(Quarter3JoinSupportAttemptResult preflight)
        {
            EnsureCanCommit(preflight);
            records.Add(preflight.SupportDay, new Quarter3JoinSupportRecord(preflight.SupportDay,
                Quarter3JoinSupportOutcome.Supported, preflight.Target,
                preflight.SelectedResource, preflight.AcquiredClueId, preflight.AcquiredSourceId));
            if (preflight.Target == Quarter3JoinSupportTarget.SurvivorGroup) survivorCount++;
            else redCount++;
            stateVersion++;
        }

        public void EnsureCanCommit(Quarter3JoinSupportAttemptResult preflight)
        {
            if (preflight == null)
                throw new System.ArgumentNullException(nameof(preflight));
            if (!preflight.IsSuccess)
                throw new System.InvalidOperationException("Only a successful support preflight can be committed.");
            if (preflight.StateVersion != stateVersion)
                throw new System.InvalidOperationException("Join support state changed after preflight.");
            if (records.ContainsKey(preflight.SupportDay))
                throw new System.InvalidOperationException("Support day has already been processed.");
            Quarter3JoinSupportDefinition definition =
                GetSupportDefinition(preflight.SupportDay, preflight.Target);
            if (definition == null ||
                definition.ClueId != preflight.AcquiredClueId ||
                definition.SourceId != preflight.AcquiredSourceId)
                throw new System.InvalidOperationException("Support preflight no longer matches its definition.");
        }

        public void Reset() { records.Clear(); survivorCount = 0; redCount = 0; stateVersion++; }

        public JoinSupportSaveData CaptureState()
        {
            List<JoinSupportRecordSaveData> saved = new();
            foreach (Quarter3JoinSupportRecord record in records.Values)
            {
                saved.Add(new JoinSupportRecordSaveData
                {
                    day = record.SupportDay,
                    outcome = (int)record.Outcome,
                    hasTarget = record.Target.HasValue,
                    target = record.Target.HasValue ? (int)record.Target.Value : 0,
                    hasResource = record.Resource.HasValue,
                    resource = record.Resource.HasValue ? (int)record.Resource.Value : 0,
                    clueId = record.ClueId,
                    sourceId = record.SourceId
                });
            }
            saved.Sort((left, right) => left.day.CompareTo(right.day));
            return new JoinSupportSaveData { records = saved.ToArray() };
        }

        public void RestoreState(JoinSupportSaveData data)
        {
            Reset();
            if (data?.records == null) return;

            foreach (JoinSupportRecordSaveData saved in data.records)
            {
                if (saved == null || saved.day < 1 || saved.day > 4) continue;
                Quarter3JoinSupportTarget? target = saved.hasTarget
                    ? (Quarter3JoinSupportTarget?)saved.target
                    : null;
                Quarter3JoinSupportResource? resource = saved.hasResource
                    ? (Quarter3JoinSupportResource?)saved.resource
                    : null;
                Quarter3JoinSupportRecord record = new(
                    saved.day,
                    (Quarter3JoinSupportOutcome)saved.outcome,
                    target,
                    resource,
                    saved.clueId,
                    saved.sourceId);
                records[saved.day] = record;
                if (record.Outcome == Quarter3JoinSupportOutcome.Supported &&
                    record.Target.HasValue)
                {
                    if (record.Target.Value == Quarter3JoinSupportTarget.SurvivorGroup)
                        survivorCount++;
                    else
                        redCount++;
                }
            }
            stateVersion++;
        }

        private Quarter3JoinSupportAttemptResult Result(Quarter3JoinSupportOutcome outcome,
            int day, Quarter3JoinSupportTarget target, Quarter3JoinSupportResource resource,
            Quarter3JoinSupportResource? consume, Quarter3JoinSupportDefinition definition,
            string reason) => new(outcome, day, target, resource, consume,
                definition?.ClueId, definition?.SourceId, survivorCount, redCount,
                reason, stateVersion);
    }
}
