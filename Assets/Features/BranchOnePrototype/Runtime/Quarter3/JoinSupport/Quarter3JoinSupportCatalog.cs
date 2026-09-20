using System;
using System.Collections.Generic;

namespace YesterdayMap.BranchOne.Quarter3.JoinSupport
{
    public sealed class Quarter3JoinSupportCatalog
    {
        private readonly List<Quarter3JoinSupportDefinition> definitions = new();
        public IReadOnlyList<Quarter3JoinSupportDefinition> Definitions =>
            definitions.AsReadOnly();

        public bool TryRegister(Quarter3JoinSupportDefinition definition,
            out string failureReason)
        {
            if (definition == null) return Fail("Support definition cannot be null.", out failureReason);
            if (definition.SupportDay < 1 || definition.SupportDay > 4)
                return Fail("Support day must be between 1 and 4.", out failureReason);
            if (!Enum.IsDefined(typeof(Quarter3JoinSupportTarget), definition.Target))
                return Fail("Invalid support target.", out failureReason);
            if (string.IsNullOrWhiteSpace(definition.SourceId) ||
                string.IsNullOrWhiteSpace(definition.ClueId))
                return Fail("Support source and clue IDs are required.", out failureReason);
            if (definitions.Exists(item => item.SupportDay == definition.SupportDay &&
                                           item.Target == definition.Target))
                return Fail("Duplicate support day and target.", out failureReason);
            if (definitions.Exists(item => item.ClueId == definition.ClueId))
                return Fail("Duplicate support clue ID.", out failureReason);
            if (definitions.Exists(item => item.SourceId == definition.SourceId))
                return Fail("Duplicate support source ID.", out failureReason);
            definitions.Add(definition);
            failureReason = string.Empty;
            return true;
        }

        public bool TryValidate(Quarter3ClueCatalog clueCatalog, out string failureReason)
        {
            if (clueCatalog == null) return Fail("Quarter 3 clue catalog is required.", out failureReason);
            if (definitions.Count != 8) return Fail("Join support catalog must contain exactly 8 definitions.", out failureReason);
            for (int day = 1; day <= 4; day++)
            foreach (Quarter3JoinSupportTarget target in Enum.GetValues(typeof(Quarter3JoinSupportTarget)))
            {
                if (!TryGet(day, target, out Quarter3JoinSupportDefinition item))
                    return Fail("Every support day must contain both targets.", out failureReason);
                if (!clueCatalog.TryGetById(item.ClueId, out Quarter3ClueDefinition clue) ||
                    clue.Route != BranchRoute.Join || clue.SourceId != item.SourceId)
                    return Fail($"Support clue is missing or mismatched: {item.ClueId}", out failureReason);
            }
            failureReason = string.Empty;
            return true;
        }

        public bool TryGet(int day, Quarter3JoinSupportTarget target,
            out Quarter3JoinSupportDefinition definition)
        {
            definition = definitions.Find(item => item.SupportDay == day && item.Target == target);
            return definition != null;
        }

        private static bool Fail(string reason, out string failureReason)
        { failureReason = reason; return false; }
    }
}
