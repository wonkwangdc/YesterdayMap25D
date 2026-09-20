using System;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Flow;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.BranchOne.Tests
{
    internal static class EndingRouteCampaignTestFactory
    {
        public static EndingRouteCampaignController CreateCampaign(
            bool useZeroScoreQuarter1Events = false)
        {
            BranchEventCatalog quarter1Catalog = useZeroScoreQuarter1Events
                ? CreateZeroScoreQuarter1Catalog()
                : BranchTestEventFactory.CreateCatalog();
            int startingScore = useZeroScoreQuarter1Events ? 0 : 1;
            BranchPrototypeSettings settings = new(
                BranchPrototypeSettings.DefaultStartDay,
                BranchPrototypeSettings.DefaultLastEventDay,
                BranchPrototypeSettings.DefaultDecisionDay,
                startingScore,
                startingScore,
                true,
                true,
                false);
            BranchOneFlowController quarter1Controller = new(
                quarter1Catalog,
                settings);
            Quarter2FlowController quarter2Controller = new(
                Quarter2TestEventFactory.CreateCatalog());
            Quarter3ClueCatalog quarter3Clues =
                Quarter3TestFactory.CreateClueCatalog();
            Quarter3JoinSupportCatalog supportCatalog = new();
            for (int day = 1; day <= 4; day++)
            foreach (Quarter3JoinSupportTarget target in
                     Enum.GetValues(typeof(Quarter3JoinSupportTarget)))
            {
                string id = $"TEST_SUPPORT_{day}_{target}";
                quarter3Clues.TryRegister(
                    new Quarter3ClueDefinition(id, BranchRoute.Join, id + "_SOURCE"),
                    out _);
                supportCatalog.TryRegister(new Quarter3JoinSupportDefinition(
                    day, target, id + "_SOURCE", id), out _);
            }
            Quarter3FlowController quarter3Controller = new(
                quarter3Clues,
                Quarter3TestFactory.CreateFinalChoiceCatalog());

            return new EndingRouteCampaignController(
                quarter1Controller,
                quarter2Controller,
                quarter3Controller,
                quarter3JoinSupportController:
                    new Quarter3JoinSupportController(supportCatalog));
        }

        private static BranchEventCatalog CreateZeroScoreQuarter1Catalog()
        {
            BranchEventCatalog catalog = new();

            for (int eventNumber = 1; eventNumber <= 10; eventNumber++)
            {
                if (!catalog.TryRegister(
                        BranchTestEventFactory.CreateDefinition(
                            $"zero-event-{eventNumber}",
                            eventNumber),
                        out string failureReason))
                {
                    throw new InvalidOperationException(failureReason);
                }
            }

            return catalog;
        }
    }
}
