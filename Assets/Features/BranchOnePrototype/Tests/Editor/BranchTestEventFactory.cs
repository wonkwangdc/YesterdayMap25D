using YesterdayMap.BranchOne.Events;

namespace YesterdayMap.BranchOne.Tests
{
    internal static class BranchTestEventFactory
    {
        internal const string ChoiceId = "choice-main";

        internal static BranchEventCatalog CreateCatalog()
        {
            BranchEventCatalog catalog = new();

            for (int eventNumber = 1; eventNumber <= 10; eventNumber++)
            {
                bool registered = catalog.TryRegister(
                    CreateDefinition(
                        $"event-{eventNumber}",
                        eventNumber,
                        eventNumber <= 5 ? 1 : 0,
                        eventNumber >= 6 ? 1 : 0),
                    out string failureReason);

                if (!registered)
                {
                    throw new System.InvalidOperationException(failureReason);
                }
            }

            return catalog;
        }

        internal static BranchEventDefinition CreateDefinition(
            string eventId,
            int eventNumber,
            int signalScoreDelta = 0,
            int joinScoreDelta = 0,
            int availableFromDay = 2,
            int availableUntilDay = 12,
            bool repeatable = true)
        {
            BranchEventChoice choice = new(
                ChoiceId,
                $"Choose event {eventNumber}",
                $"Event {eventNumber} result",
                signalScoreDelta,
                joinScoreDelta,
                $"Event {eventNumber} diary record");

            return new BranchEventDefinition(
                eventId,
                eventNumber,
                $"Test event {eventNumber}",
                $"Test event {eventNumber} body",
                availableFromDay,
                availableUntilDay,
                repeatable,
                new[] { choice });
        }
    }
}
