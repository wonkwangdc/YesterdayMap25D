using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YesterdayMap.BranchOne.Quarter2
{
    public sealed class Quarter2EventCatalog
    {
        public const int RequiredEventCountPerRoute = 3;

        private readonly List<Quarter2EventDefinition> events = new();
        private readonly ReadOnlyCollection<Quarter2EventDefinition> readOnlyEvents;
        private readonly Dictionary<string, Quarter2EventDefinition> eventsById =
            new(StringComparer.Ordinal);
        private readonly Dictionary<BranchRoute, Dictionary<int, Quarter2EventDefinition>>
            eventsByRouteAndOrder = new()
            {
                { BranchRoute.Signal, new Dictionary<int, Quarter2EventDefinition>() },
                { BranchRoute.Join, new Dictionary<int, Quarter2EventDefinition>() }
            };

        public Quarter2EventCatalog()
        {
            readOnlyEvents = events.AsReadOnly();
        }

        public bool TryRegister(
            Quarter2EventDefinition definition,
            out string failureReason)
        {
            if (definition == null)
            {
                failureReason = "Quarter 2 event definition cannot be null.";
                return false;
            }

            if (!definition.TryValidate(out failureReason))
            {
                return false;
            }

            if (eventsById.ContainsKey(definition.EventId))
            {
                failureReason = $"Duplicate Quarter 2 event ID: {definition.EventId}";
                return false;
            }

            Dictionary<int, Quarter2EventDefinition> routeEvents =
                eventsByRouteAndOrder[definition.Route];
            if (routeEvents.ContainsKey(definition.Order))
            {
                failureReason =
                    $"Duplicate Quarter 2 event order {definition.Order} " +
                    $"for route {definition.Route}.";
                return false;
            }

            events.Add(definition);
            events.Sort(CompareEvents);
            eventsById.Add(definition.EventId, definition);
            routeEvents.Add(definition.Order, definition);
            failureReason = string.Empty;
            return true;
        }

        public bool TryGetById(
            string eventId,
            out Quarter2EventDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(eventId) &&
                   eventsById.TryGetValue(eventId, out definition);
        }

        public bool TryGet(
            BranchRoute route,
            int order,
            out Quarter2EventDefinition definition)
        {
            definition = null;
            return eventsByRouteAndOrder.TryGetValue(
                       route,
                       out Dictionary<int, Quarter2EventDefinition> routeEvents) &&
                   routeEvents.TryGetValue(order, out definition);
        }

        public IReadOnlyList<Quarter2EventDefinition> GetEvents()
        {
            return readOnlyEvents;
        }

        public IReadOnlyList<Quarter2EventDefinition> GetEvents(BranchRoute route)
        {
            if (!eventsByRouteAndOrder.TryGetValue(
                    route,
                    out Dictionary<int, Quarter2EventDefinition> routeEvents))
            {
                return Array.Empty<Quarter2EventDefinition>();
            }

            List<Quarter2EventDefinition> routeEventList =
                new(routeEvents.Values);
            routeEventList.Sort((left, right) => left.Order.CompareTo(right.Order));
            return routeEventList.AsReadOnly();
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
            Dictionary<int, Quarter2EventDefinition> routeEvents =
                eventsByRouteAndOrder[route];
            if (routeEvents.Count != RequiredEventCountPerRoute)
            {
                failureReason =
                    $"Route {route} must contain exactly " +
                    $"{RequiredEventCountPerRoute} Quarter 2 events.";
                return false;
            }

            for (int order = 1; order <= RequiredEventCountPerRoute; order++)
            {
                if (!routeEvents.ContainsKey(order))
                {
                    failureReason =
                        $"Route {route} is missing Quarter 2 event order {order}.";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }

        private static int CompareEvents(
            Quarter2EventDefinition left,
            Quarter2EventDefinition right)
        {
            int routeComparison = left.Route.CompareTo(right.Route);
            return routeComparison != 0
                ? routeComparison
                : left.Order.CompareTo(right.Order);
        }
    }
}
