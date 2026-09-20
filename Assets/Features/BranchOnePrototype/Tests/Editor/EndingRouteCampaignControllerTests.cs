using NUnit.Framework;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Quarter2;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class EndingRouteCampaignControllerTests
    {
        [Test]
        public void Campaign_InitiallyIsNotStarted()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();

            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.NotStarted));
            Assert.That(campaign.CampaignState.Quarter1Route, Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.CampaignState.Quarter2PassedRoute, Is.EqualTo(BranchRoute.None));
        }

        [Test]
        public void StartQuarter1_EntersQuarter1Running()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();

            EndingRouteCampaignTransitionResult result =
                campaign.StartQuarter1();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter1Running));
            Assert.That(
                campaign.Quarter1Controller.RuntimeState.Phase,
                Is.EqualTo(BranchFlowPhase.WaitingForEvent));
        }

        [Test]
        public void AdvanceToQuarter2_BeforeQuarter1DecisionIsBlocked()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            campaign.StartQuarter1();

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceToQuarter2();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.Not.Empty);
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.Phase,
                Is.EqualTo(Quarter2FlowPhase.NotStarted));
        }

        [Test]
        public void SignalDecision_IsStoredInCampaignState()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();

            BranchDecisionResult result =
                ResolveQuarter1(campaign, BranchRoute.Signal);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(campaign.CampaignState.Quarter1DecisionResult, Is.SameAs(result));
            Assert.That(campaign.CampaignState.Quarter1Route, Is.EqualTo(BranchRoute.Signal));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter1Resolved));
        }

        [Test]
        public void JoinDecision_IsStoredInCampaignState()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();

            BranchDecisionResult result =
                ResolveQuarter1(campaign, BranchRoute.Join);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(campaign.CampaignState.Quarter1DecisionResult, Is.SameAs(result));
            Assert.That(campaign.CampaignState.Quarter1Route, Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void Quarter1Decision_DoesNotAutomaticallyStartQuarter2()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();

            ResolveQuarter1(campaign, BranchRoute.Signal);

            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter1Resolved));
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.Phase,
                Is.EqualTo(Quarter2FlowPhase.NotStarted));
        }

        [Test]
        public void AdvanceToQuarter2_StartsSignalRouteFromSignalDecision()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);

            Assert.That(
                campaign.Quarter2Controller.RuntimeState.InitialRoute,
                Is.EqualTo(BranchRoute.Signal));
            Assert.That(campaign.CampaignState.Quarter2CurrentRoute, Is.EqualTo(BranchRoute.Signal));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Running));
        }

        [Test]
        public void AdvanceToQuarter2_StartsJoinRouteFromJoinDecision()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Join);

            Assert.That(
                campaign.Quarter2Controller.RuntimeState.InitialRoute,
                Is.EqualTo(BranchRoute.Join));
            Assert.That(campaign.CampaignState.Quarter2CurrentRoute, Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void Quarter2_StartsAtEventOrderOne()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);

            Assert.That(
                campaign.Quarter2Controller.RuntimeState.CurrentEventOrder,
                Is.EqualTo(1));
            Assert.That(
                campaign.Quarter2Controller.GetCurrentEvent().EventId,
                Is.EqualTo("Q2_SIGNAL_01"));
        }

        [Test]
        public void AdvanceToQuarter2_CannotStartQuarter2Twice()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);

            EndingRouteCampaignTransitionResult secondStart =
                campaign.AdvanceToQuarter2();

            Assert.That(secondStart.IsSuccess, Is.False);
            Assert.That(secondStart.FailureReason, Is.Not.Empty);
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.CurrentEventOrder,
                Is.EqualTo(1));
        }

        [Test]
        public void NoneQuarter1Route_BlocksQuarter2Start()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            campaign.StartQuarter1();

            Assert.That(campaign.CampaignState.Quarter1Route, Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.AdvanceToQuarter2().IsSuccess, Is.False);
            Assert.That(campaign.CampaignState.Quarter2CurrentRoute, Is.EqualTo(BranchRoute.None));
        }

        [Test]
        public void FailedQuarter1Decision_BlocksQuarter2Start()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign(true);

            BranchDecisionResult decisionResult = ResolveFailedQuarter1(campaign);
            EndingRouteCampaignTransitionResult transition =
                campaign.AdvanceToQuarter2();

            Assert.That(decisionResult.IsSuccess, Is.False);
            Assert.That(decisionResult.DecidedRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.CampaignState.Quarter1DecisionResult, Is.SameAs(decisionResult));
            Assert.That(transition.IsSuccess, Is.False);
            Assert.That(transition.FailureReason, Is.Not.Empty);
        }

        [Test]
        public void Quarter1Scores_AreNotTransferredToQuarter2Counters()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);

            Assert.That(campaign.Quarter1Controller.ScoreState.SignalScore, Is.GreaterThan(1));
            Assert.That(campaign.Quarter1Controller.ScoreState.JoinScore, Is.EqualTo(1));
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.SignalProgress.ProgressCount,
                Is.Zero);
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.SignalProgress.RejectCount,
                Is.Zero);
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.JoinProgress.ProgressCount,
                Is.Zero);
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.JoinProgress.RejectCount,
                Is.Zero);
        }

        [Test]
        public void SynchronizeState_KeepsCampaignRunningDuringQuarter2()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);

            campaign.Quarter2Controller.SelectDecision(Quarter2Decision.Progress);
            campaign.SynchronizeState();

            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Running));
            Assert.That(campaign.CampaignState.Quarter2PassedRoute, Is.EqualTo(BranchRoute.None));
        }

        [Test]
        public void SynchronizeState_StoresSignalQuarter2Pass()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);

            PassCurrentQuarter2Route(campaign);
            campaign.SynchronizeState();

            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Passed));
            Assert.That(
                campaign.CampaignState.Quarter2PassedRoute,
                Is.EqualTo(BranchRoute.Signal));
        }

        [Test]
        public void SynchronizeState_StoresJoinQuarter2Pass()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Join);

            PassCurrentQuarter2Route(campaign);
            campaign.SynchronizeState();

            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Passed));
            Assert.That(
                campaign.CampaignState.Quarter2PassedRoute,
                Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void FirstRouteFailure_PreservesOppositeRouteSwitchFlow()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);

            FailCurrentQuarter2Route(campaign);
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.Phase,
                Is.EqualTo(Quarter2FlowPhase.RouteSwitchPending));
            Assert.That(campaign.Quarter2Controller.AdvanceToNextStep(), Is.True);
            campaign.SynchronizeState();

            Assert.That(
                campaign.Quarter2Controller.RuntimeState.SignalProgress.Failed,
                Is.True);
            Assert.That(campaign.CampaignState.Quarter2CurrentRoute, Is.EqualTo(BranchRoute.Join));
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.CurrentEventOrder,
                Is.EqualTo(1));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Running));
        }

        [Test]
        public void OppositeRoutePass_IsSynchronized()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);
            FailCurrentQuarter2Route(campaign);
            campaign.Quarter2Controller.AdvanceToNextStep();

            PassCurrentQuarter2Route(campaign);
            campaign.SynchronizeState();

            Assert.That(
                campaign.Quarter2Controller.RuntimeState.SignalProgress.Failed,
                Is.True);
            Assert.That(
                campaign.CampaignState.Quarter2PassedRoute,
                Is.EqualTo(BranchRoute.Join));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Passed));
        }

        [Test]
        public void BothRouteFailures_AreSynchronized()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);
            FailCurrentQuarter2Route(campaign);
            campaign.Quarter2Controller.AdvanceToNextStep();

            FailCurrentQuarter2Route(campaign);
            campaign.SynchronizeState();

            Assert.That(campaign.CampaignState.AreAllBranchesFailed, Is.True);
            Assert.That(campaign.CampaignState.Quarter2PassedRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.AllBranchesFailed));
        }

        [Test]
        public void Quarter2Passed_BlocksAdditionalQuarter2Transition()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);
            PassCurrentQuarter2Route(campaign);
            campaign.SynchronizeState();

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceToQuarter2();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Passed));
        }

        [Test]
        public void AllBranchesFailed_BlocksAdditionalQuarter2Transition()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);
            FailCurrentQuarter2Route(campaign);
            campaign.Quarter2Controller.AdvanceToNextStep();
            FailCurrentQuarter2Route(campaign);
            campaign.SynchronizeState();

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceToQuarter2();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.AllBranchesFailed));
        }

        [Test]
        public void ResetCampaign_ResetsBothFlowsAndCampaignState()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(BranchRoute.Signal);
            campaign.Quarter2Controller.SelectDecision(Quarter2Decision.Reject);

            campaign.ResetCampaign();

            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.NotStarted));
            Assert.That(campaign.CampaignState.Quarter1Route, Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.CampaignState.Quarter2CurrentRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.CampaignState.Quarter2PassedRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.CampaignState.AreAllBranchesFailed, Is.False);
            Assert.That(campaign.CampaignState.Quarter1DecisionResult, Is.Null);
            Assert.That(campaign.CampaignState.LastTransitionResult, Is.Null);
            Assert.That(
                campaign.Quarter1Controller.RuntimeState.Phase,
                Is.EqualTo(BranchFlowPhase.NotStarted));
            Assert.That(campaign.Quarter1Controller.ScoreState.SignalScore, Is.EqualTo(1));
            Assert.That(campaign.Quarter1Controller.ScoreState.JoinScore, Is.EqualTo(1));
            Assert.That(campaign.Quarter1Controller.DiaryRepository.GetRecords(), Is.Empty);
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.Phase,
                Is.EqualTo(Quarter2FlowPhase.NotStarted));
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.SignalProgress.CompletedEventCount,
                Is.Zero);
            Assert.That(
                campaign.Quarter2Controller.RuntimeState.JoinProgress.CompletedEventCount,
                Is.Zero);
        }

        private static EndingRouteCampaignController CreateQuarter2Campaign(
            BranchRoute route)
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            ResolveQuarter1(campaign, route);
            Assert.That(campaign.AdvanceToQuarter2().IsSuccess, Is.True);
            return campaign;
        }

        private static BranchDecisionResult ResolveQuarter1(
            EndingRouteCampaignController campaign,
            BranchRoute route)
        {
            Assert.That(campaign.StartQuarter1().IsSuccess, Is.True);
            int eventNumber = route == BranchRoute.Signal ? 1 : 6;

            CompleteQuarter1EventDays(campaign, eventNumber);

            BranchDecisionResult result =
                campaign.Quarter1Controller.ResolveDecision(0.5d);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.DecidedRoute, Is.EqualTo(route));
            return result;
        }

        private static BranchDecisionResult ResolveFailedQuarter1(
            EndingRouteCampaignController campaign)
        {
            Assert.That(campaign.StartQuarter1().IsSuccess, Is.True);
            CompleteQuarter1EventDays(campaign, 1);

            BranchDecisionResult result =
                campaign.Quarter1Controller.ResolveDecision(0.5d);
            Assert.That(result.IsSuccess, Is.False);
            return result;
        }

        private static void CompleteQuarter1EventDays(
            EndingRouteCampaignController campaign,
            int eventNumber)
        {
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
                Assert.That(
                    campaign.Quarter1Controller.AdvanceDay(),
                    Is.True);
            }
        }

        private static void PassCurrentQuarter2Route(
            EndingRouteCampaignController campaign)
        {
            SelectAndAdvanceQuarter2(campaign, Quarter2Decision.Progress);
            SelectAndAdvanceQuarter2(campaign, Quarter2Decision.Progress);
            Assert.That(
                campaign.Quarter2Controller.SelectDecision(
                    Quarter2Decision.Progress).IsSuccess,
                Is.True);
        }

        private static void FailCurrentQuarter2Route(
            EndingRouteCampaignController campaign)
        {
            SelectAndAdvanceQuarter2(campaign, Quarter2Decision.Reject);
            Assert.That(
                campaign.Quarter2Controller.SelectDecision(
                    Quarter2Decision.Reject).IsSuccess,
                Is.True);
        }

        private static void SelectAndAdvanceQuarter2(
            EndingRouteCampaignController campaign,
            Quarter2Decision decision)
        {
            Assert.That(
                campaign.Quarter2Controller.SelectDecision(decision).IsSuccess,
                Is.True);
            Assert.That(
                campaign.Quarter2Controller.AdvanceToNextStep(),
                Is.True);
        }
    }
}
