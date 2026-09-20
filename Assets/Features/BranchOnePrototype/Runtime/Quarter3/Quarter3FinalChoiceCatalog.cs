using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YesterdayMap.BranchOne.Quarter3
{
    public sealed class Quarter3FinalChoiceCatalog
    {
        public const int RequiredChoiceCountPerRoute = 2;

        private readonly List<Quarter3FinalChoiceDefinition> choices = new();
        private readonly ReadOnlyCollection<Quarter3FinalChoiceDefinition>
            readOnlyChoices;
        private readonly Dictionary<string, Quarter3FinalChoiceDefinition>
            choicesById = new(StringComparer.Ordinal);

        public Quarter3FinalChoiceCatalog()
        {
            readOnlyChoices = choices.AsReadOnly();
        }

        public bool TryRegister(
            Quarter3FinalChoiceDefinition definition,
            out string failureReason)
        {
            if (definition == null)
            {
                failureReason =
                    "Quarter 3 final choice definition cannot be null.";
                return false;
            }

            if (!definition.TryValidate(out failureReason))
            {
                return false;
            }

            if (choicesById.ContainsKey(definition.FinalChoiceId))
            {
                failureReason =
                    "Duplicate Quarter 3 final choice ID: " +
                    definition.FinalChoiceId;
                return false;
            }

            choices.Add(definition);
            choices.Sort(CompareChoices);
            choicesById.Add(definition.FinalChoiceId, definition);
            failureReason = string.Empty;
            return true;
        }

        public bool TryGetById(
            string finalChoiceId,
            out Quarter3FinalChoiceDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(finalChoiceId) &&
                   choicesById.TryGetValue(finalChoiceId, out definition);
        }

        public IReadOnlyList<Quarter3FinalChoiceDefinition> GetChoices()
        {
            return readOnlyChoices;
        }

        public IReadOnlyList<Quarter3FinalChoiceDefinition> GetChoices(
            BranchRoute route)
        {
            if (!IsPlayableRoute(route))
            {
                return Array.Empty<Quarter3FinalChoiceDefinition>();
            }

            return choices.FindAll(definition => definition.Route == route)
                .AsReadOnly();
        }

        public bool IsChoiceForRoute(
            string finalChoiceId,
            BranchRoute route)
        {
            return TryGetById(
                       finalChoiceId,
                       out Quarter3FinalChoiceDefinition definition) &&
                   definition.Route == route;
        }

        public bool TryValidate(out string failureReason)
        {
            if (!TryValidateRoute(BranchRoute.Signal, out failureReason))
            {
                return false;
            }

            if (!TryValidateRoute(BranchRoute.Join, out failureReason))
            {
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private bool TryValidateRoute(
            BranchRoute route,
            out string failureReason)
        {
            int routeChoiceCount =
                choices.FindAll(definition => definition.Route == route).Count;
            if (routeChoiceCount != RequiredChoiceCountPerRoute)
            {
                failureReason =
                    $"Route {route} must contain exactly " +
                    $"{RequiredChoiceCountPerRoute} Quarter 3 final choices.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private static int CompareChoices(
            Quarter3FinalChoiceDefinition left,
            Quarter3FinalChoiceDefinition right)
        {
            int routeComparison = left.Route.CompareTo(right.Route);
            return routeComparison != 0
                ? routeComparison
                : string.Compare(
                    left.FinalChoiceId,
                    right.FinalChoiceId,
                    StringComparison.Ordinal);
        }

        private static bool IsPlayableRoute(BranchRoute route)
        {
            return route == BranchRoute.Signal || route == BranchRoute.Join;
        }
    }
}
