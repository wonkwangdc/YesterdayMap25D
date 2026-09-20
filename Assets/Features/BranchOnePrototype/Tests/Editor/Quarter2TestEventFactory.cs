using System;
using YesterdayMap.BranchOne.Quarter2;

namespace YesterdayMap.BranchOne.Tests
{
    internal static class Quarter2TestEventFactory
    {
        public static Quarter2EventCatalog CreateCatalog()
        {
            Quarter2EventCatalog catalog = new();

            Register(catalog, "Q2_SIGNAL_01", BranchRoute.Signal, 1);
            Register(catalog, "Q2_SIGNAL_02", BranchRoute.Signal, 2);
            Register(catalog, "Q2_SIGNAL_03", BranchRoute.Signal, 3);
            Register(catalog, "Q2_JOIN_01", BranchRoute.Join, 1);
            Register(catalog, "Q2_JOIN_02", BranchRoute.Join, 2);
            Register(catalog, "Q2_JOIN_03", BranchRoute.Join, 3);

            return catalog;
        }

        public static Quarter2EventDefinition CreateDefinition(
            string eventId,
            BranchRoute route,
            int order)
        {
            return new Quarter2EventDefinition(
                eventId,
                route,
                order,
                string.Empty,
                string.Empty);
        }

        private static void Register(
            Quarter2EventCatalog catalog,
            string eventId,
            BranchRoute route,
            int order)
        {
            if (!catalog.TryRegister(
                    CreateDefinition(eventId, route, order),
                    out string failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }
        }
    }
}
