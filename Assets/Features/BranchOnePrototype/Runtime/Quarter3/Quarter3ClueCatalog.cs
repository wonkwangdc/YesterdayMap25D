using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YesterdayMap.BranchOne.Quarter3
{
    public sealed class Quarter3ClueCatalog
    {
        private readonly List<Quarter3ClueDefinition> clues = new();
        private readonly ReadOnlyCollection<Quarter3ClueDefinition> readOnlyClues;
        private readonly Dictionary<string, Quarter3ClueDefinition> cluesById =
            new(StringComparer.Ordinal);

        public Quarter3ClueCatalog()
        {
            readOnlyClues = clues.AsReadOnly();
        }

        public bool TryRegister(
            Quarter3ClueDefinition definition,
            out string failureReason)
        {
            if (definition == null)
            {
                failureReason = "Quarter 3 clue definition cannot be null.";
                return false;
            }

            if (!definition.TryValidate(out failureReason))
            {
                return false;
            }

            if (cluesById.ContainsKey(definition.ClueId))
            {
                failureReason =
                    $"Duplicate Quarter 3 clue ID: {definition.ClueId}";
                return false;
            }

            clues.Add(definition);
            clues.Sort(CompareClues);
            cluesById.Add(definition.ClueId, definition);
            failureReason = string.Empty;
            return true;
        }

        public bool TryGetById(
            string clueId,
            out Quarter3ClueDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(clueId) &&
                   cluesById.TryGetValue(clueId, out definition);
        }

        public IReadOnlyList<Quarter3ClueDefinition> GetClues()
        {
            return readOnlyClues;
        }

        public IReadOnlyList<Quarter3ClueDefinition> GetClues(
            BranchRoute route)
        {
            if (!IsPlayableRoute(route))
            {
                return Array.Empty<Quarter3ClueDefinition>();
            }

            return clues.FindAll(definition => definition.Route == route)
                .AsReadOnly();
        }

        public IReadOnlyList<Quarter3ClueDefinition> GetClues(
            BranchRoute route,
            string sourceId)
        {
            if (!IsPlayableRoute(route) ||
                string.IsNullOrWhiteSpace(sourceId))
            {
                return Array.Empty<Quarter3ClueDefinition>();
            }

            return clues.FindAll(definition =>
                    definition.Route == route &&
                    string.Equals(
                        definition.SourceId,
                        sourceId,
                        StringComparison.Ordinal))
                .AsReadOnly();
        }

        private static int CompareClues(
            Quarter3ClueDefinition left,
            Quarter3ClueDefinition right)
        {
            int routeComparison = left.Route.CompareTo(right.Route);
            return routeComparison != 0
                ? routeComparison
                : string.Compare(
                    left.ClueId,
                    right.ClueId,
                    StringComparison.Ordinal);
        }

        private static bool IsPlayableRoute(BranchRoute route)
        {
            return route == BranchRoute.Signal || route == BranchRoute.Join;
        }
    }
}
