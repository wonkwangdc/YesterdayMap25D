using NUnit.Framework;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class Quarter3JoinSupportTests
    {
        [Test]
        public void Catalog_ExactFourDaysAndTwoTargets_IsValid()
        {
            CreateValid(out Quarter3ClueCatalog clues,
                out Quarter3JoinSupportCatalog supports);
            Assert.That(supports.Definitions.Count, Is.EqualTo(8));
            Assert.That(supports.TryValidate(clues, out _), Is.True);
        }

        [Test]
        public void Support_ConsumesOneAndReturnsMatchingClue()
        {
            CreateValid(out _, out Quarter3JoinSupportCatalog catalog);
            Quarter3JoinSupportController controller = new(catalog);
            Quarter3JoinSupportAttemptResult result = controller.AttemptSupport(
                2, Quarter3JoinSupportTarget.RedArmband,
                Quarter3JoinSupportResource.Water,
                new Quarter3JoinSupportInventorySnapshot(0, 1));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.ConsumeAmount, Is.EqualTo(1));
            Assert.That(result.AcquiredClueId, Is.EqualTo("SUPPORT_2_RedArmband"));
            Assert.That(controller.GetRedArmbandSupportCount(), Is.EqualTo(1));
            Assert.That(controller.GetSupportDayStatus(2),
                Is.EqualTo(Quarter3JoinSupportDayStatus.Supported));
        }

        [Test]
        public void NoResources_ConsumesOpportunityWithoutClueOrCount()
        {
            CreateValid(out _, out Quarter3JoinSupportCatalog catalog);
            Quarter3JoinSupportController controller = new(catalog);
            Quarter3JoinSupportAttemptResult result = controller.AttemptSupport(
                1, Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.CannedFood,
                new Quarter3JoinSupportInventorySnapshot(0, 0));
            Assert.That(result.Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.MissedNoResources));
            Assert.That(result.AcquiredClueId, Is.Empty);
            Assert.That(controller.HasProcessedSupportDay(1), Is.True);
            Assert.That(controller.GetSurvivorSupportCount(), Is.Zero);
            Assert.That(controller.AttemptSupport(1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.CannedFood,
                new Quarter3JoinSupportInventorySnapshot(1, 0)).Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.SupportDayAlreadyProcessed));
        }

        [Test]
        public void SelectedResourceUnavailable_DoesNotConsumeOpportunity()
        {
            CreateValid(out _, out Quarter3JoinSupportCatalog catalog);
            Quarter3JoinSupportController controller = new(catalog);
            Quarter3JoinSupportAttemptResult failed = controller.AttemptSupport(
                1, Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.CannedFood,
                new Quarter3JoinSupportInventorySnapshot(0, 1));
            Assert.That(failed.Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.SelectedResourceUnavailable));
            Assert.That(controller.HasProcessedSupportDay(1), Is.False);
            Assert.That(controller.AttemptSupport(1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.Water,
                new Quarter3JoinSupportInventorySnapshot(0, 1)).IsSuccess, Is.True);
        }

        [Test]
        public void Reset_ClearsRecordsAndCounts()
        {
            CreateValid(out _, out Quarter3JoinSupportCatalog catalog);
            Quarter3JoinSupportController controller = new(catalog);
            var result = controller.AttemptSupport(1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.CannedFood,
                new Quarter3JoinSupportInventorySnapshot(1, 0));
            controller.Reset();
            Assert.That(controller.GetSupportDayStatus(1),
                Is.EqualTo(Quarter3JoinSupportDayStatus.Open));
            Assert.That(controller.GetSurvivorSupportCount(), Is.Zero);
        }

        private static void CreateValid(out Quarter3ClueCatalog clues,
            out Quarter3JoinSupportCatalog supports)
        {
            clues = new Quarter3ClueCatalog();
            supports = new Quarter3JoinSupportCatalog();
            for (int day = 1; day <= 4; day++)
            foreach (Quarter3JoinSupportTarget target in
                     System.Enum.GetValues(typeof(Quarter3JoinSupportTarget)))
            {
                string id = $"SUPPORT_{day}_{target}";
                string source = id + "_SOURCE";
                Assert.That(clues.TryRegister(new Quarter3ClueDefinition(
                    id, BranchRoute.Join, source), out _), Is.True);
                Assert.That(supports.TryRegister(
                    new Quarter3JoinSupportDefinition(day, target, source, id),
                    out _), Is.True);
            }
        }
    }
}
