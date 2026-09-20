using NUnit.Framework;
using YesterdayMap.BranchOne.Quarter2;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class Quarter2FlowControllerTests
    {
        [Test]
        public void StartQuarter2_StartsSignalRoute()
        {
            Quarter2FlowController controller = CreateController();

            Assert.That(controller.StartQuarter2(BranchRoute.Signal), Is.True);
            Assert.That(controller.RuntimeState.InitialRoute, Is.EqualTo(BranchRoute.Signal));
            Assert.That(controller.RuntimeState.CurrentRoute, Is.EqualTo(BranchRoute.Signal));
            Assert.That(controller.RuntimeState.SignalProgress.Attempted, Is.True);
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.WaitingForChoice));
        }

        [Test]
        public void StartQuarter2_StartsJoinRoute()
        {
            Quarter2FlowController controller = CreateController();

            Assert.That(controller.StartQuarter2(BranchRoute.Join), Is.True);
            Assert.That(controller.RuntimeState.InitialRoute, Is.EqualTo(BranchRoute.Join));
            Assert.That(controller.RuntimeState.CurrentRoute, Is.EqualTo(BranchRoute.Join));
            Assert.That(controller.RuntimeState.JoinProgress.Attempted, Is.True);
        }

        [Test]
        public void StartQuarter2_BeginsAtEventOrderOne()
        {
            Quarter2FlowController controller = CreateStartedController();

            Assert.That(controller.RuntimeState.CurrentEventOrder, Is.EqualTo(1));
            Assert.That(controller.GetCurrentEvent().EventId, Is.EqualTo("Q2_SIGNAL_01"));
        }

        [Test]
        public void SelectProgress_IncrementsProgressCount()
        {
            Quarter2FlowController controller = CreateStartedController();

            Quarter2SelectionResult result =
                controller.SelectDecision(Quarter2Decision.Progress);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.RuntimeState.SignalProgress.ProgressCount, Is.EqualTo(1));
            Assert.That(controller.RuntimeState.SignalProgress.CompletedEventCount, Is.EqualTo(1));
        }

        [Test]
        public void SelectReject_IncrementsRejectCount()
        {
            Quarter2FlowController controller = CreateStartedController();

            Quarter2SelectionResult result =
                controller.SelectDecision(Quarter2Decision.Reject);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.RuntimeState.SignalProgress.RejectCount, Is.EqualTo(1));
            Assert.That(controller.RuntimeState.SignalProgress.CompletedEventCount, Is.EqualTo(1));
        }

        [Test]
        public void SelectDecision_EntersWaitingForAdvance()
        {
            Quarter2FlowController controller = CreateStartedController();

            Quarter2SelectionResult result =
                controller.SelectDecision(Quarter2Decision.Progress);

            Assert.That(result.Phase, Is.EqualTo(Quarter2FlowPhase.WaitingForAdvance));
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.WaitingForAdvance));
        }

        [Test]
        public void AdvanceBeforeSelection_IsBlocked()
        {
            Quarter2FlowController controller = CreateStartedController();

            Assert.That(controller.AdvanceToNextStep(), Is.False);
            Assert.That(controller.LastFailureReason, Is.Not.Empty);
            Assert.That(controller.RuntimeState.CurrentEventOrder, Is.EqualTo(1));
        }

        [Test]
        public void SelectingTheSameEventTwice_IsBlocked()
        {
            Quarter2FlowController controller = CreateStartedController();
            Quarter2SelectionResult first =
                controller.SelectDecision(Quarter2Decision.Progress);
            Quarter2SelectionResult second =
                controller.SelectDecision(Quarter2Decision.Reject);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.False);
            Assert.That(controller.RuntimeState.SignalProgress.ProgressCount, Is.EqualTo(1));
            Assert.That(controller.RuntimeState.SignalProgress.RejectCount, Is.Zero);
            Assert.That(controller.RuntimeState.SignalProgress.CompletedEventCount, Is.EqualTo(1));
        }

        [Test]
        public void AdvanceAfterSelection_MovesToNextEvent()
        {
            Quarter2FlowController controller = CreateStartedController();
            controller.SelectDecision(Quarter2Decision.Progress);

            Assert.That(controller.AdvanceToNextStep(), Is.True);
            Assert.That(controller.RuntimeState.CurrentEventOrder, Is.EqualTo(2));
            Assert.That(controller.GetCurrentEvent().EventId, Is.EqualTo("Q2_SIGNAL_02"));
        }

        [Test]
        public void TwoProgressChoices_DoNotPassEarly()
        {
            Quarter2FlowController controller = CreateStartedController();
            SelectAndAdvance(controller, Quarter2Decision.Progress);
            Quarter2SelectionResult second =
                controller.SelectDecision(Quarter2Decision.Progress);

            Assert.That(second.IsSuccess, Is.True);
            Assert.That(controller.RuntimeState.SignalProgress.ProgressCount, Is.EqualTo(2));
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.WaitingForAdvance));
            Assert.That(controller.RuntimeState.PassedRoute, Is.EqualTo(BranchRoute.None));
        }

        [Test]
        public void ThreeProgressChoices_PassCurrentRoute()
        {
            Quarter2FlowController controller = CreateStartedController();
            SelectAndAdvance(controller, Quarter2Decision.Progress);
            SelectAndAdvance(controller, Quarter2Decision.Progress);

            Quarter2SelectionResult result =
                controller.SelectDecision(Quarter2Decision.Progress);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.RuntimeState.SignalProgress.ProgressCount, Is.EqualTo(3));
            Assert.That(controller.RuntimeState.PassedRoute, Is.EqualTo(BranchRoute.Signal));
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.Quarter2Passed));
        }

        [Test]
        public void TwoProgressAndOneReject_PassCurrentRoute()
        {
            Quarter2FlowController controller = CreateStartedController();
            SelectAndAdvance(controller, Quarter2Decision.Progress);
            SelectAndAdvance(controller, Quarter2Decision.Progress);

            controller.SelectDecision(Quarter2Decision.Reject);

            Assert.That(controller.RuntimeState.SignalProgress.ProgressCount, Is.EqualTo(2));
            Assert.That(controller.RuntimeState.SignalProgress.RejectCount, Is.EqualTo(1));
            Assert.That(controller.RuntimeState.PassedRoute, Is.EqualTo(BranchRoute.Signal));
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.Quarter2Passed));
        }

        [Test]
        public void OneReject_AllowsFlowToContinue()
        {
            Quarter2FlowController controller = CreateStartedController();

            controller.SelectDecision(Quarter2Decision.Reject);

            Assert.That(controller.RuntimeState.SignalProgress.Failed, Is.False);
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.WaitingForAdvance));
            Assert.That(controller.AdvanceToNextStep(), Is.True);
            Assert.That(controller.RuntimeState.CurrentEventOrder, Is.EqualTo(2));
        }

        [Test]
        public void TwoRejects_ImmediatelyFailCurrentRoute()
        {
            Quarter2FlowController controller = CreateStartedController();

            FailCurrentRoute(controller);

            Assert.That(controller.RuntimeState.SignalProgress.RejectCount, Is.EqualTo(2));
            Assert.That(controller.RuntimeState.SignalProgress.Failed, Is.True);
        }

        [Test]
        public void TwoRejects_SkipRemainingCurrentRouteEvents()
        {
            Quarter2FlowController controller = CreateStartedController();

            FailCurrentRoute(controller);

            Assert.That(controller.RuntimeState.CurrentEventOrder, Is.EqualTo(2));
            Assert.That(controller.RuntimeState.SignalProgress.CompletedEventCount, Is.EqualTo(2));
            Assert.That(controller.GetCurrentEvent(), Is.Null);
            Assert.That(controller.AdvanceToNextStep(), Is.True);
            Assert.That(controller.RuntimeState.CurrentRoute, Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void CurrentRouteFailure_EntersRouteSwitchPending()
        {
            Quarter2FlowController controller = CreateStartedController();

            FailCurrentRoute(controller);

            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.RouteSwitchPending));
            Assert.That(controller.RuntimeState.PendingRoute, Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void AdvanceFromRouteSwitchPending_StartsOppositeRouteAtEventOne()
        {
            Quarter2FlowController controller = CreateStartedController();
            FailCurrentRoute(controller);

            Assert.That(controller.AdvanceToNextStep(), Is.True);
            Assert.That(controller.RuntimeState.CurrentRoute, Is.EqualTo(BranchRoute.Join));
            Assert.That(controller.RuntimeState.CurrentEventOrder, Is.EqualTo(1));
            Assert.That(controller.GetCurrentEvent().EventId, Is.EqualTo("Q2_JOIN_01"));
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.WaitingForChoice));
        }

        [Test]
        public void OppositeRoute_StartsWithZeroCounters()
        {
            Quarter2FlowController controller = CreateStartedController();
            FailCurrentRoute(controller);

            controller.AdvanceToNextStep();

            Assert.That(controller.RuntimeState.JoinProgress.Attempted, Is.True);
            Assert.That(controller.RuntimeState.JoinProgress.ProgressCount, Is.Zero);
            Assert.That(controller.RuntimeState.JoinProgress.RejectCount, Is.Zero);
            Assert.That(controller.RuntimeState.JoinProgress.CompletedEventCount, Is.Zero);
        }

        [Test]
        public void SwitchingRoutes_PreservesFirstRouteFailureRecord()
        {
            Quarter2FlowController controller = CreateStartedController();
            FailCurrentRoute(controller);

            controller.AdvanceToNextStep();

            Assert.That(controller.RuntimeState.SignalProgress.Attempted, Is.True);
            Assert.That(controller.RuntimeState.SignalProgress.Failed, Is.True);
            Assert.That(controller.RuntimeState.SignalProgress.RejectCount, Is.EqualTo(2));
            Assert.That(controller.RuntimeState.SignalProgress.CompletedEventCount, Is.EqualTo(2));
        }

        [Test]
        public void OppositeRoute_CanPassAfterFirstRouteFails()
        {
            Quarter2FlowController controller = CreateStartedController();
            FailCurrentRoute(controller);
            controller.AdvanceToNextStep();
            SelectAndAdvance(controller, Quarter2Decision.Progress);
            SelectAndAdvance(controller, Quarter2Decision.Progress);

            controller.SelectDecision(Quarter2Decision.Progress);

            Assert.That(controller.RuntimeState.SignalProgress.Failed, Is.True);
            Assert.That(controller.RuntimeState.PassedRoute, Is.EqualTo(BranchRoute.Join));
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.Quarter2Passed));
        }

        [Test]
        public void TwoFailedRoutes_EnterAllBranchesFailed()
        {
            Quarter2FlowController controller = CreateStartedController();
            FailCurrentRoute(controller);
            controller.AdvanceToNextStep();

            FailCurrentRoute(controller);

            Assert.That(controller.RuntimeState.SignalProgress.Failed, Is.True);
            Assert.That(controller.RuntimeState.JoinProgress.Failed, Is.True);
            Assert.That(controller.RuntimeState.AreAllBranchesFailed, Is.True);
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.AllBranchesFailed));
            Assert.That(controller.RuntimeState.PassedRoute, Is.EqualTo(BranchRoute.None));
        }

        [Test]
        public void PassedRoute_BlocksAdditionalSelections()
        {
            Quarter2FlowController controller = CreateStartedController();
            PassCurrentRoute(controller);

            Quarter2SelectionResult result =
                controller.SelectDecision(Quarter2Decision.Reject);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(controller.RuntimeState.SignalProgress.CompletedEventCount, Is.EqualTo(3));
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.Quarter2Passed));
        }

        [Test]
        public void AllBranchesFailed_BlocksAdditionalProgress()
        {
            Quarter2FlowController controller = CreateStartedController();
            FailCurrentRoute(controller);
            controller.AdvanceToNextStep();
            FailCurrentRoute(controller);

            Quarter2SelectionResult selection =
                controller.SelectDecision(Quarter2Decision.Progress);
            bool advanced = controller.AdvanceToNextStep();

            Assert.That(selection.IsSuccess, Is.False);
            Assert.That(advanced, Is.False);
            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.AllBranchesFailed));
        }

        [Test]
        public void Reset_RestoresCompleteInitialState()
        {
            Quarter2FlowController controller = CreateStartedController();
            FailCurrentRoute(controller);
            controller.AdvanceToNextStep();
            controller.SelectDecision(Quarter2Decision.Progress);

            controller.Reset();

            Assert.That(controller.RuntimeState.Phase, Is.EqualTo(Quarter2FlowPhase.NotStarted));
            Assert.That(controller.RuntimeState.InitialRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(controller.RuntimeState.CurrentRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(controller.RuntimeState.CurrentEventOrder, Is.Zero);
            Assert.That(controller.RuntimeState.PassedRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(controller.RuntimeState.PendingRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(controller.RuntimeState.AreAllBranchesFailed, Is.False);
            Assert.That(controller.RuntimeState.LastDecision, Is.Null);
            AssertProgressReset(controller.RuntimeState.SignalProgress);
            AssertProgressReset(controller.RuntimeState.JoinProgress);
            Assert.That(controller.LastFailureReason, Is.Empty);
        }

        private static Quarter2FlowController CreateController()
        {
            return new Quarter2FlowController(Quarter2TestEventFactory.CreateCatalog());
        }

        private static Quarter2FlowController CreateStartedController(
            BranchRoute route = BranchRoute.Signal)
        {
            Quarter2FlowController controller = CreateController();
            Assert.That(controller.StartQuarter2(route), Is.True);
            return controller;
        }

        private static void SelectAndAdvance(
            Quarter2FlowController controller,
            Quarter2Decision decision)
        {
            Assert.That(controller.SelectDecision(decision).IsSuccess, Is.True);
            Assert.That(controller.AdvanceToNextStep(), Is.True);
        }

        private static void FailCurrentRoute(Quarter2FlowController controller)
        {
            SelectAndAdvance(controller, Quarter2Decision.Reject);
            Assert.That(
                controller.SelectDecision(Quarter2Decision.Reject).IsSuccess,
                Is.True);
        }

        private static void PassCurrentRoute(Quarter2FlowController controller)
        {
            SelectAndAdvance(controller, Quarter2Decision.Progress);
            SelectAndAdvance(controller, Quarter2Decision.Progress);
            Assert.That(
                controller.SelectDecision(Quarter2Decision.Progress).IsSuccess,
                Is.True);
        }

        private static void AssertProgressReset(Quarter2RouteProgress progress)
        {
            Assert.That(progress.ProgressCount, Is.Zero);
            Assert.That(progress.RejectCount, Is.Zero);
            Assert.That(progress.Attempted, Is.False);
            Assert.That(progress.Failed, Is.False);
            Assert.That(progress.CompletedEventCount, Is.Zero);
        }
    }
}
