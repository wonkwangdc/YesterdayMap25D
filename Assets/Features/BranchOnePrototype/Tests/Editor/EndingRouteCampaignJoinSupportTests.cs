using NUnit.Framework;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Flow;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class EndingRouteCampaignJoinSupportTests
    {
        [Test]
        public void CannedFoodSupport_CommitsRecordClueAndConsumptionTogether()
        {
            EndingRouteCampaignController campaign = CreateQuarter3(BranchRoute.Join);
            var result = Support(campaign, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.CannedFood, 1, 0);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.ConsumeResource,
                Is.EqualTo(Quarter3JoinSupportResource.CannedFood));
            Assert.That(result.ConsumeAmount, Is.EqualTo(1));
            Assert.That(campaign.GetQuarter3JoinSupportDayStatus(1),
                Is.EqualTo(Quarter3JoinSupportDayStatus.Supported));
            Assert.That(campaign.GetQuarter3JoinSupportRecord(1), Is.Not.Null);
            Assert.That(campaign.HasQuarter3Clue(result.AcquiredClueId), Is.True);
            Assert.That(campaign.GetQuarter3JoinSupportCounts().SurvivorGroup,
                Is.EqualTo(1));
        }

        [Test]
        public void SupportedDay_CannotBeSupportedAgain()
        {
            EndingRouteCampaignController campaign = CreateQuarter3(BranchRoute.Join);
            Assert.That(Support(campaign, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.Water, 0, 1).IsSuccess, Is.True);
            Assert.That(Support(campaign, 1,
                Quarter3JoinSupportTarget.RedArmband,
                Quarter3JoinSupportResource.Water, 0, 1).Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.SupportDayAlreadyProcessed));
            Assert.That(campaign.Quarter3AcquiredClueIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void Support_IsBlockedOutsideRunningJoinQuarter3()
        {
            EndingRouteCampaignController beforeQ3 =
                EndingRouteCampaignTestFactory.CreateCampaign();
            Assert.That(Support(beforeQ3, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.Water, 0, 1).Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.WrongCampaignPhase));

            EndingRouteCampaignController signal = CreateQuarter3(BranchRoute.Signal);
            Assert.That(Support(signal, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.Water, 0, 1).Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.WrongRoute));

            EndingRouteCampaignController resolved = CreateQuarter3(BranchRoute.Join);
            resolved.OpenQuarter3FinalChoice();
            resolved.SelectQuarter3FinalChoice(Quarter3TestFactory.JoinOptionA);
            Assert.That(Support(resolved, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.Water, 0, 1).Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.Quarter3AlreadyResolved));
        }

        [Test]
        public void Quarter2PassedBeforeQuarter3_SupportIsBlocked()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            ResolveQuarter1AndStartQuarter2(campaign, BranchRoute.Join);
            PassQuarter2(campaign);
            Assert.That(Support(campaign, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.Water, 0, 1).Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.WrongCampaignPhase));
        }

        [Test]
        public void AllBranchesFailed_SupportIsBlocked()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            ResolveQuarter1AndStartQuarter2(campaign, BranchRoute.Join);
            FailCurrentQuarter2Route(campaign);
            Assert.That(campaign.AdvanceQuarter2Step().IsSuccess, Is.True);
            FailCurrentQuarter2Route(campaign);
            Assert.That(campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.AllBranchesFailed));
            Assert.That(Support(campaign, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.Water, 0, 1).Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.WrongCampaignPhase));
        }

        [Test]
        public void FailedSupport_CreatesNeitherRecordNorClue()
        {
            EndingRouteCampaignController campaign = CreateQuarter3(BranchRoute.Join);
            var result = Support(campaign, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.CannedFood, 0, 1);
            Assert.That(result.Outcome,
                Is.EqualTo(Quarter3JoinSupportOutcome.SelectedResourceUnavailable));
            Assert.That(campaign.GetQuarter3JoinSupportRecord(1), Is.Null);
            Assert.That(campaign.Quarter3AcquiredClueIds, Is.Empty);
        }

        [Test]
        public void ResetCampaign_ClearsSupportAndQuarter3Together()
        {
            EndingRouteCampaignController campaign = CreateQuarter3(BranchRoute.Join);
            Assert.That(Support(campaign, 1,
                Quarter3JoinSupportTarget.SurvivorGroup,
                Quarter3JoinSupportResource.CannedFood, 1, 0).IsSuccess, Is.True);
            campaign.ResetCampaign();
            Assert.That(campaign.GetQuarter3JoinSupportRecord(1), Is.Null);
            Assert.That(campaign.GetQuarter3JoinSupportCounts().SurvivorGroup, Is.Zero);
            Assert.That(campaign.Quarter3AcquiredClueIds, Is.Empty);
            Assert.That(campaign.Quarter3Phase, Is.EqualTo(Quarter3FlowPhase.NotStarted));
            Assert.That(campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.NotStarted));
        }

        [Test]
        public void FourMissedDays_StillAllowTwoChoicesAndResolution()
        {
            EndingRouteCampaignController campaign = CreateQuarter3(BranchRoute.Join);
            for (int day = 1; day <= 4; day++)
                Assert.That(Support(campaign, day,
                    Quarter3JoinSupportTarget.SurvivorGroup,
                    Quarter3JoinSupportResource.CannedFood, 0, 0).Outcome,
                    Is.EqualTo(Quarter3JoinSupportOutcome.MissedNoResources));
            Assert.That(campaign.Quarter3AcquiredClueIds, Is.Empty);
            Assert.That(campaign.GetQuarter3JoinSupportCounts(), Is.EqualTo((0, 0)));
            Assert.That(campaign.OpenQuarter3FinalChoice().IsSuccess, Is.True);
            Assert.That(campaign.GetQuarter3FinalChoiceOptions().Count, Is.EqualTo(2));
            Assert.That(campaign.SelectQuarter3FinalChoice(
                Quarter3TestFactory.JoinOptionB).IsSuccess, Is.True);
            Assert.That(campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter3Resolved));
        }

        [Test]
        public void FourSurvivorSupports_KeepOppositeFinalChoiceAvailable()
        {
            EndingRouteCampaignController campaign = CreateQuarter3(BranchRoute.Join);
            for (int day = 1; day <= 4; day++)
                Assert.That(Support(campaign, day,
                    Quarter3JoinSupportTarget.SurvivorGroup,
                    Quarter3JoinSupportResource.Water, 0, 1).IsSuccess, Is.True);
            Assert.That(campaign.Quarter3AcquiredClueIds.Count, Is.EqualTo(4));
            Assert.That(campaign.GetQuarter3JoinSupportCounts(), Is.EqualTo((4, 0)));
            campaign.OpenQuarter3FinalChoice();
            Assert.That(campaign.GetQuarter3FinalChoiceOptions().Count, Is.EqualTo(2));
            Assert.That(campaign.SelectQuarter3FinalChoice(
                Quarter3TestFactory.JoinOptionB).IsSuccess, Is.True);
        }

        private static Quarter3JoinSupportAttemptResult Support(
            EndingRouteCampaignController campaign, int day,
            Quarter3JoinSupportTarget target, Quarter3JoinSupportResource resource,
            int food, int water) =>
            campaign.AttemptQuarter3JoinSupport(day, target, resource,
                new Quarter3JoinSupportInventorySnapshot(food, water));

        private static EndingRouteCampaignController CreateQuarter3(BranchRoute route)
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            ResolveQuarter1AndStartQuarter2(campaign, route);
            PassQuarter2(campaign);
            Assert.That(campaign.AdvanceToQuarter3().IsSuccess, Is.True);
            return campaign;
        }

        private static void ResolveQuarter1AndStartQuarter2(
            EndingRouteCampaignController campaign, BranchRoute route)
        {
            campaign.StartQuarter1();
            int eventNumber = route == BranchRoute.Signal ? 1 : 6;
            for (int day = BranchPrototypeSettings.DefaultStartDay;
                 day <= BranchPrototypeSettings.DefaultLastEventDay; day++)
            {
                Assert.That(campaign.Quarter1Controller.SelectEvent(eventNumber,
                    BranchTestEventFactory.ChoiceId).IsSuccess, Is.True);
                Assert.That(campaign.Quarter1Controller.AdvanceDay(), Is.True);
            }
            BranchDecisionResult decision =
                campaign.Quarter1Controller.ResolveDecision(0.5d);
            Assert.That(decision.DecidedRoute, Is.EqualTo(route));
            Assert.That(campaign.AdvanceToQuarter2().IsSuccess, Is.True);
        }

        private static void PassQuarter2(EndingRouteCampaignController campaign)
        {
            for (int step = 0; step < 2; step++)
            {
                Assert.That(campaign.SelectQuarter2Decision(
                    Quarter2Decision.Progress).IsSuccess, Is.True);
                Assert.That(campaign.AdvanceQuarter2Step().IsSuccess, Is.True);
            }
            Assert.That(campaign.SelectQuarter2Decision(
                Quarter2Decision.Progress).IsSuccess, Is.True);
        }

        private static void FailCurrentQuarter2Route(
            EndingRouteCampaignController campaign)
        {
            Assert.That(campaign.SelectQuarter2Decision(
                Quarter2Decision.Reject).IsSuccess, Is.True);
            Assert.That(campaign.AdvanceQuarter2Step().IsSuccess, Is.True);
            Assert.That(campaign.SelectQuarter2Decision(
                Quarter2Decision.Reject).IsSuccess, Is.True);
        }
    }
}
