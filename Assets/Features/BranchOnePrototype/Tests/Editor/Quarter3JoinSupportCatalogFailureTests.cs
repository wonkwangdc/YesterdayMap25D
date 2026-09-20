using System;
using NUnit.Framework;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class Quarter3JoinSupportCatalogFailureTests
    {
        [Test] public void EmptyCatalog_CampaignConstructionFails()
        {
            EndingRouteCampaignController valid =
                EndingRouteCampaignTestFactory.CreateCampaign();
            Assert.Throws<ArgumentException>(() => new EndingRouteCampaignController(
                valid.Quarter1Controller, valid.Quarter2Controller,
                valid.Quarter3Controller,
                quarter3JoinSupportController:
                    new Quarter3JoinSupportController(
                        new Quarter3JoinSupportCatalog())));
        }

        [Test] public void MissingDay_IsRejected() =>
            AssertInvalid(CreateCatalog(skipDay: 4));

        [Test] public void MissingTarget_IsRejected() =>
            AssertInvalid(CreateCatalog(skipDay: 3,
                skipTarget: Quarter3JoinSupportTarget.RedArmband));

        [Test] public void DayZero_IsRejectedOnRegistration()
        {
            Quarter3JoinSupportCatalog catalog = new();
            Assert.That(catalog.TryRegister(new Quarter3JoinSupportDefinition(
                0, Quarter3JoinSupportTarget.SurvivorGroup, "S", "C"),
                out _), Is.False);
        }

        [Test] public void DayFive_IsRejectedOnRegistration()
        {
            Quarter3JoinSupportCatalog catalog = new();
            Assert.That(catalog.TryRegister(new Quarter3JoinSupportDefinition(
                5, Quarter3JoinSupportTarget.SurvivorGroup, "S", "C"),
                out _), Is.False);
        }

        [Test] public void DuplicateDayAndTarget_IsRejectedOnRegistration()
        {
            Quarter3JoinSupportCatalog catalog = new();
            Assert.That(catalog.TryRegister(new Quarter3JoinSupportDefinition(
                1, Quarter3JoinSupportTarget.SurvivorGroup, "S1", "C1"),
                out _), Is.True);
            Assert.That(catalog.TryRegister(new Quarter3JoinSupportDefinition(
                1, Quarter3JoinSupportTarget.SurvivorGroup, "S2", "C2"),
                out _), Is.False);
        }

        [Test] public void DuplicateClueId_IsRejectedOnRegistration()
        {
            Quarter3JoinSupportCatalog catalog = new();
            Assert.That(catalog.TryRegister(new Quarter3JoinSupportDefinition(
                1, Quarter3JoinSupportTarget.SurvivorGroup, "S1", "C"),
                out _), Is.True);
            Assert.That(catalog.TryRegister(new Quarter3JoinSupportDefinition(
                1, Quarter3JoinSupportTarget.RedArmband, "S2", "C"),
                out _), Is.False);
        }

        [Test] public void MissingClue_IsRejected()
        {
            CreateValid(out Quarter3ClueCatalog clues,
                out Quarter3JoinSupportCatalog supports);
            Quarter3ClueCatalog empty = new();
            Assert.That(supports.TryValidate(empty, out _), Is.False);
        }

        [Test] public void SourceMismatch_IsRejected()
        {
            CreateValid(out Quarter3ClueCatalog clues,
                out Quarter3JoinSupportCatalog supports);
            Quarter3ClueCatalog mismatched = new();
            foreach (Quarter3JoinSupportDefinition item in supports.Definitions)
                mismatched.TryRegister(new Quarter3ClueDefinition(item.ClueId,
                    BranchRoute.Join, item.SourceId + "_WRONG"), out _);
            Assert.That(supports.TryValidate(mismatched, out _), Is.False);
        }

        [Test] public void SignalRouteClue_IsRejected()
        {
            CreateValid(out _, out Quarter3JoinSupportCatalog supports);
            Quarter3ClueCatalog signal = new();
            foreach (Quarter3JoinSupportDefinition item in supports.Definitions)
                signal.TryRegister(new Quarter3ClueDefinition(item.ClueId,
                    BranchRoute.Signal, item.SourceId), out _);
            Assert.That(supports.TryValidate(signal, out _), Is.False);
        }

        private static Quarter3JoinSupportCatalog CreateCatalog(
            int skipDay = -1,
            Quarter3JoinSupportTarget? skipTarget = null)
        {
            Quarter3JoinSupportCatalog catalog = new();
            for (int day = 1; day <= 4; day++)
            foreach (Quarter3JoinSupportTarget target in
                     Enum.GetValues(typeof(Quarter3JoinSupportTarget)))
            {
                if (day == skipDay &&
                    (!skipTarget.HasValue || skipTarget.Value == target))
                    continue;
                string id = $"C_{day}_{target}";
                catalog.TryRegister(new Quarter3JoinSupportDefinition(
                    day, target, id + "_S", id), out _);
            }
            return catalog;
        }

        private static void AssertInvalid(Quarter3JoinSupportCatalog catalog)
        {
            Quarter3ClueCatalog clues = new();
            foreach (Quarter3JoinSupportDefinition item in catalog.Definitions)
                clues.TryRegister(new Quarter3ClueDefinition(item.ClueId,
                    BranchRoute.Join, item.SourceId), out _);
            Assert.That(catalog.TryValidate(clues, out _), Is.False);
        }

        private static void CreateValid(out Quarter3ClueCatalog clues,
            out Quarter3JoinSupportCatalog supports)
        {
            supports = CreateCatalog();
            clues = new Quarter3ClueCatalog();
            foreach (Quarter3JoinSupportDefinition item in supports.Definitions)
                clues.TryRegister(new Quarter3ClueDefinition(item.ClueId,
                    BranchRoute.Join, item.SourceId), out _);
        }
    }
}
