using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YesterdayMap.BranchOne.Events
{
    public sealed class BranchEventCatalog
    {
        private readonly List<BranchEventDefinition> events = new();
        private readonly ReadOnlyCollection<BranchEventDefinition> readOnlyEvents;
        private readonly Dictionary<string, BranchEventDefinition> eventsById =
            new(StringComparer.Ordinal);
        private readonly Dictionary<int, BranchEventDefinition> eventsByNumber = new();

        public BranchEventCatalog()
        {
            readOnlyEvents = events.AsReadOnly();
        }

        public bool TryRegister(BranchEventDefinition definition, out string failureReason)
        {
            if (definition == null)
            {
                failureReason = "Event definition cannot be null.";
                return false;
            }

            if (!definition.TryValidate(out failureReason))
            {
                return false;
            }

            if (eventsById.ContainsKey(definition.EventId))
            {
                failureReason = $"Duplicate event ID: {definition.EventId}";
                return false;
            }

            if (eventsByNumber.ContainsKey(definition.EventNumber))
            {
                failureReason = $"Duplicate event number: {definition.EventNumber}";
                return false;
            }

            events.Add(definition);
            events.Sort((left, right) => left.EventNumber.CompareTo(right.EventNumber));
            eventsById.Add(definition.EventId, definition);
            eventsByNumber.Add(definition.EventNumber, definition);
            failureReason = string.Empty;
            return true;
        }

        public bool TryGetById(string eventId, out BranchEventDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(eventId) &&
                   eventsById.TryGetValue(eventId, out definition);
        }

        public bool TryGetByNumber(int eventNumber, out BranchEventDefinition definition)
        {
            return eventsByNumber.TryGetValue(eventNumber, out definition);
        }

        public IReadOnlyList<BranchEventDefinition> GetAvailableEvents(int day)
        {
            if (day < 1)
            {
                return Array.Empty<BranchEventDefinition>();
            }

            return events.FindAll(definition => definition.IsAvailableOnDay(day)).AsReadOnly();
        }

        public IReadOnlyList<BranchEventDefinition> GetEvents()
        {
            return readOnlyEvents;
        }
    }
}
