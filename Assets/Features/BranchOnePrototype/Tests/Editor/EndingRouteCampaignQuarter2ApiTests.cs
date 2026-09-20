using NUnit.Framework;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Quarter2;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class EndingRouteCampaignQuarter2ApiTests
    {
        [Test]
        public void SelectQuarter2Decision_BeforeQuarter2RunningIsBlocked()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();

            Quarter2SelectionResult result =
                campaign.SelectQuarter2Decision(Quarter2Decision.Progress);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.Not.Empty);
            Assert.That(campaign.Quarter2Phase, Is.EqualTo(Quarter2FlowPhase.NotStarted));
        }

        [Test]
        public void AdvanceQuarter2Step_BeforeQuarter2RunningIsBlocked()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceQuarter2Step();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.Not.Empty);
            Assert.That(campaign.Quarter2CurrentEventOrder, Is.Zero);
        }

        [Test]
        public void CampaignApi_SelectsProgress()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();

            Quarter2SelectionResult result =
                campaign.SelectQuarter2Decision(Quarter2Decision.Progress);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Decision, Is.EqualTo(Quarter2Decision.Progress));
            Assert.That(campaign.SignalProgressCount, Is.EqualTo(1));
            Assert.That(campaign.Quarter2Phase, Is.EqualTo(Quarter2FlowPhase.WaitingForAdvance));
        }

        [Test]
        public void CampaignApi_SelectsReject()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();

            Quarter2SelectionResult result =
                campaign.SelectQuarter2Decision(Quarter2Decision.Reject);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Decision, Is.EqualTo(Quarter2Decision.Reject));
            Assert.That(campaign.SignalRejectCount, Is.EqualTo(1));
        }

        [Test]
        public void CampaignReadOnlyCounts_UpdateImmediatelyAfterSelection()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();

            campaign.SelectQuarter2Decision(Quarter2Decision.Progress);

            Assert.That(campaign.SignalProgressCount, Is.EqualTo(1));
            Assert.That(campaign.SignalRejectCount, Is.Zero);
            Assert.That(campaign.JoinProgressCount, Is.Zero);
            Assert.That(campaign.JoinRejectCount, Is.Zero);
        }

        [Test]
        public void Selection_DoesNotRequireSeparateSynchronizeStateCall()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();

            PassCurrentRoute(campaign);

            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Passed));
            Assert.That(campaign.Quarter2PassedRoute, Is.EqualTo(BranchRoute.Signal));
        }

        [Test]
        public void CampaignApi_AdvancesToNextEvent()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();
            campaign.SelectQuarter2Decision(Quarter2Decision.Progress);

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceQuarter2Step();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(campaign.Quarter2CurrentEventOrder, Is.EqualTo(2));
            Assert.That(
                campaign.GetCurrentQuarter2Event().EventId,
                Is.EqualTo("Q2_SIGNAL_02"));
            Assert.That(campaign.Quarter2Phase, Is.EqualTo(Quarter2FlowPhase.WaitingForChoice));
        }

        [Test]
        public void SecondReject_ExposesRouteSwitchPendingImmediately()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();

            FailCurrentRoute(campaign);

            Assert.That(campaign.Quarter2Phase, Is.EqualTo(Quarter2FlowPhase.RouteSwitchPending));
            Assert.That(campaign.SignalRejectCount, Is.EqualTo(2));
            Assert.That(campaign.SignalFailed, Is.True);
            Assert.That(campaign.GetCurrentQuarter2Event(), Is.Null);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Running));
        }

        [Test]
        public void CampaignApi_StartsOppositeRouteAtEventOne()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();
            FailCurrentRoute(campaign);

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceQuarter2Step();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(campaign.Quarter2CurrentRoute, Is.EqualTo(BranchRoute.Join));
            Assert.That(campaign.Quarter2CurrentEventOrder, Is.EqualTo(1));
            Assert.That(
                campaign.GetCurrentQuarter2Event().EventId,
                Is.EqualTo("Q2_JOIN_01"));
        }

        [Test]
        public void OppositeRouteStart_PreservesFirstRouteFailure()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();
            FailCurrentRoute(campaign);

            campaign.AdvanceQuarter2Step();

            Assert.That(campaign.SignalFailed, Is.True);
            Assert.That(campaign.SignalRejectCount, Is.EqualTo(2));
            Assert.That(campaign.JoinProgressCount, Is.Zero);
            Assert.That(campaign.JoinRejectCount, Is.Zero);
            Assert.That(campaign.JoinFailed, Is.False);
        }

        [Test]
        public void CampaignApiOnly_ReachesQuarter2Passed()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();

            PassCurrentRoute(campaign);

            Assert.That(campaign.Quarter2Phase, Is.EqualTo(Quarter2FlowPhase.Quarter2Passed));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Passed));
        }

        [Test]
        public void Quarter2Pass_IsImmediatelyStoredInCampaignState()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Join);

            PassCurrentRoute(campaign);

            Assert.That(campaign.Quarter2PassedRoute, Is.EqualTo(BranchRoute.Join));
            Assert.That(
                campaign.CampaignState.Quarter2PassedRoute,
                Is.EqualTo(BranchRoute.Join));
            Assert.That(campaign.AreAllBranchesFailed, Is.False);
        }

        [Test]
        public void CampaignApiOnly_ReachesAllBranchesFailed()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();

            FailBothRoutes(campaign);

            Assert.That(campaign.Quarter2Phase, Is.EqualTo(Quarter2FlowPhase.AllBranchesFailed));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.AllBranchesFailed));
        }

        [Test]
        public void AllBranchFailure_IsImmediatelyStoredInCampaignState()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();

            FailBothRoutes(campaign);

            Assert.That(campaign.AreAllBranchesFailed, Is.True);
            Assert.That(campaign.CampaignState.AreAllBranchesFailed, Is.True);
            Assert.That(campaign.SignalFailed, Is.True);
            Assert.That(campaign.JoinFailed, Is.True);
            Assert.That(campaign.Quarter2PassedRoute, Is.EqualTo(BranchRoute.None));
        }

        [Test]
        public void Quarter2Passed_BlocksAdditionalWrapperOperations()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();
            PassCurrentRoute(campaign);

            Quarter2SelectionResult selection =
                campaign.SelectQuarter2Decision(Quarter2Decision.Reject);
            EndingRouteCampaignTransitionResult advance =
                campaign.AdvanceQuarter2Step();

            Assert.That(selection.IsSuccess, Is.False);
            Assert.That(advance.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Passed));
        }

        [Test]
        public void AllBranchesFailed_BlocksAdditionalWrapperOperations()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();
            FailBothRoutes(campaign);

            Quarter2SelectionResult selection =
                campaign.SelectQuarter2Decision(Quarter2Decision.Progress);
            EndingRouteCampaignTransitionResult advance =
                campaign.AdvanceQuarter2Step();

            Assert.That(selection.IsSuccess, Is.False);
            Assert.That(advance.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.AllBranchesFailed));
        }

        [Test]
        public void ResetCampaign_ResetsWrapperState()
        {
            EndingRouteCampaignController campaign = CreateQuarter2Campaign();
            campaign.SelectQuarter2Decision(Quarter2Decision.Progress);

            campaign.ResetCampaign();

            Assert.That(campaign.Quarter2Phase, Is.EqualTo(Quarter2FlowPhase.NotStarted));
            Assert.That(campaign.Quarter2CurrentRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.Quarter2CurrentEventOrder, Is.Zero);
            Assert.That(campaign.GetCurrentQuarter2Event(), Is.Null);
            Assert.That(campaign.SignalProgressCount, Is.Zero);
            Assert.That(campaign.SignalRejectCount, Is.Zero);
            Assert.That(campaign.SignalFailed, Is.False);
            Assert.That(campaign.JoinProgressCount, Is.Zero);
            Assert.That(campaign.JoinRejectCount, Is.Zero);
            Assert.That(campaign.JoinFailed, Is.False);
            Assert.That(campaign.Quarter2PassedRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.AreAllBranchesFailed, Is.False);
        }

        private static EndingRouteCampaignController CreateQuarter2Campaign(
            BranchRoute route = BranchRoute.Signal)
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            Assert.That(campaign.StartQuarter1().IsSuccess, Is.True);

            int eventNumber = route == BranchRoute.Signal ? 1 : 6;
            for (
                int day = BranchPrototypeSettings.DefaultStartDay;
                day <= BranchPrototypeSettings.DefaultLastEventDay;
                day++)
            {
                Assert.That(
                    campaign.Quarter1Controller.SelectEvent(
                        eventNumber,
                        BranchTestEventFactory.ChoiceId).IsSuccess,
                    Is.True);
                Assert.That(campaign.Quarter1Controller.AdvanceDay(), Is.True);
            }

            BranchDecisionResult decisionResult =
                campaign.Quarter1Controller.ResolveDecision(0.5d);
            Assert.That(decisionResult.IsSuccess, Is.True);
            Assert.That(decisionResult.DecidedRoute, Is.EqualTo(route));
            Assert.That(campaign.AdvanceToQuarter2().IsSuccess, Is.True);
            return campaign;
        }

        private static void PassCurrentRoute(
            EndingRouteCampaignController campaign)
        {
            SelectAndAdvance(campaign, Quarter2Decision.Progress);
            SelectAndAdvance(campaign, Quarter2Decision.Progress);
            Assert.That(
                campaign.SelectQuarter2Decision(
                    Quarter2Decision.Progress).IsSuccess,
                Is.True);
        }

        private static void FailCurrentRoute(
            EndingRouteCampaignController campaign)
        {
            SelectAndAdvance(campaign, Quarter2Decision.Reject);
            Assert.That(
                campaign.SelectQuarter2Decision(
                    Quarter2Decision.Reject).IsSuccess,
                Is.True);
        }

        private static void FailBothRoutes(
            EndingRouteCampaignController campaign)
        {
            FailCurrentRoute(campaign);
            Assert.That(campaign.AdvanceQuarter2Step().IsSuccess, Is.True);
            FailCurrentRoute(campaign);
        }

        private static void SelectAndAdvance(
            EndingRouteCampaignController campaign,
            Quarter2Decision decision)
        {
            Assert.That(
                campaign.SelectQuarter2Decision(decision).IsSuccess,
                Is.True);
            Assert.That(campaign.AdvanceQuarter2Step().IsSuccess, Is.True);
        }
    }
}
