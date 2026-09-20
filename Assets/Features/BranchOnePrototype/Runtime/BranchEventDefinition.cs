using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YesterdayMap.BranchOne.Events
{
    [Serializable]
    public sealed class BranchEventDefinition
    {
        private readonly List<BranchEventChoice> choices;
        private readonly ReadOnlyCollection<BranchEventChoice> readOnlyChoices;

        public string EventId { get; }
        public int EventNumber { get; }
        public string Title { get; }
        public string Body { get; }
        public int AvailableFromDay { get; }
        public int AvailableUntilDay { get; }
        public bool Repeatable { get; }
        public IReadOnlyList<BranchEventChoice> Choices => readOnlyChoices;

        public BranchEventDefinition(
            string eventId,
            int eventNumber,
            string title,
            string body,
            int availableFromDay,
            int availableUntilDay,
            bool repeatable,
            IEnumerable<BranchEventChoice> choices)
        {
            EventId = eventId ?? string.Empty;
            EventNumber = eventNumber;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            AvailableFromDay = availableFromDay;
            AvailableUntilDay = availableUntilDay;
            Repeatable = repeatable;
            this.choices = choices == null
                ? new List<BranchEventChoice>()
                : new List<BranchEventChoice>(choices);
            readOnlyChoices = this.choices.AsReadOnly();
        }

        public bool IsAvailableOnDay(int day)
        {
            return day >= AvailableFromDay && day <= AvailableUntilDay;
        }

        public bool TryGetChoice(string choiceId, out BranchEventChoice choice)
        {
            choice = null;
            if (string.IsNullOrWhiteSpace(choiceId))
            {
                return false;
            }

            choice = choices.Find(candidate =>
                candidate != null &&
                string.Equals(candidate.ChoiceId, choiceId, StringComparison.Ordinal));
            return choice != null;
        }

        public bool TryValidate(out string failureReason)
        {
            if (string.IsNullOrWhiteSpace(EventId))
            {
                failureReason = "Event ID cannot be empty.";
                return false;
            }

            if (EventNumber <= 0)
            {
                failureReason = "Event number must be greater than zero.";
                return false;
            }

            if (AvailableFromDay < 1)
            {
                failureReason = "Available from day must be at least 1.";
                return false;
            }

            if (AvailableUntilDay < AvailableFromDay)
            {
                failureReason = "Available until day cannot be before available from day.";
                return false;
            }

            if (choices.Count == 0)
            {
                failureReason = "An event must contain at least one choice.";
                return false;
            }

            HashSet<string> choiceIds = new(StringComparer.Ordinal);
            foreach (BranchEventChoice choice in choices)
            {
                if (choice == null)
                {
                    failureReason = "Event choices cannot contain null.";
                    return false;
                }

                if (!choice.TryValidate(out failureReason))
                {
                    return false;
                }

                if (!choiceIds.Add(choice.ChoiceId))
                {
                    failureReason = $"Duplicate choice ID: {choice.ChoiceId}";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
