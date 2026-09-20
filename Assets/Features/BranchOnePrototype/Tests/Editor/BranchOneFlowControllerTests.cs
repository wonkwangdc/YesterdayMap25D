using NUnit.Framework;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Diary;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Flow;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class BranchOneFlowControllerTests
    {
        [Test]
        public void StartTest_BeginsOnDayTwo()
        {
            BranchOneFlowController flow = CreateFlow();

            flow.StartTest();

            Assert.That(flow.RuntimeState.CurrentDay, Is.EqualTo(2));
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.WaitingForEvent));
            Assert.That(flow.RuntimeState.IsTodayEventCompleted, Is.False);
        }

        [Test]
        public void StartTest_CreatesDayTwoDiaryRecord()
        {
            BranchOneFlowController flow = CreateFlow();

            flow.StartTest();

            Assert.That(flow.DiaryRepository.TryGetRecord(2, out DiaryDayRecord record), Is.True);
            Assert.That(record.Day, Is.EqualTo(2));
            Assert.That(flow.DiaryRepository.GetRecords().Count, Is.EqualTo(1));
        }

        [Test]
        public void SelectEvent_AppliesSignalScore()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            BranchEventSelectionResult result =
                flow.SelectEvent(1, BranchTestEventFactory.ChoiceId);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AppliedSignalScoreDelta, Is.EqualTo(1));
            Assert.That(result.AppliedJoinScoreDelta, Is.Zero);
            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(1));
            Assert.That(flow.ScoreState.JoinScore, Is.Zero);
        }

        [Test]
        public void SelectEvent_AppliesJoinScore()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            BranchEventSelectionResult result =
                flow.SelectEvent("event-6", BranchTestEventFactory.ChoiceId);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AppliedSignalScoreDelta, Is.Zero);
            Assert.That(result.AppliedJoinScoreDelta, Is.EqualTo(1));
            Assert.That(flow.ScoreState.SignalScore, Is.Zero);
            Assert.That(flow.ScoreState.JoinScore, Is.EqualTo(1));
        }

        [Test]
        public void SelectEvent_AddsChoiceResultToCurrentDayDiary()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            BranchEventSelectionResult result =
                flow.SelectEvent(4, BranchTestEventFactory.ChoiceId);
            flow.DiaryRepository.TryGetRecord(2, out DiaryDayRecord dayTwo);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(dayTwo.EventRecords, Is.EquivalentTo(new[] { "Event 4 diary record" }));
        }

        [Test]
        public void SelectEvent_BlocksSecondSelectionOnSameDay()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            BranchEventSelectionResult first =
                flow.SelectEvent(1, BranchTestEventFactory.ChoiceId);
            BranchEventSelectionResult second =
                flow.SelectEvent(6, BranchTestEventFactory.ChoiceId);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.False);
            Assert.That(second.FailureReason, Is.Not.Empty);
            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(1));
            Assert.That(flow.ScoreState.JoinScore, Is.Zero);
        }

        [Test]
        public void AdvanceDay_BlocksBeforeEventSelection()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            bool advanced = flow.AdvanceDay();

            Assert.That(advanced, Is.False);
            Assert.That(flow.RuntimeState.CurrentDay, Is.EqualTo(2));
            Assert.That(flow.LastFailureReason, Is.Not.Empty);
        }

        [Test]
        public void AdvanceDay_MovesAfterEventSelection()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            flow.SelectEvent(1, BranchTestEventFactory.ChoiceId);

            bool advanced = flow.AdvanceDay();

            Assert.That(advanced, Is.True);
            Assert.That(flow.RuntimeState.CurrentDay, Is.EqualTo(3));
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.WaitingForEvent));
            Assert.That(flow.RuntimeState.IsTodayEventCompleted, Is.False);
        }

        [Test]
        public void AdvanceDay_PreservesPreviousDayDiaryRecord()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            flow.SelectEvent(2, BranchTestEventFactory.ChoiceId);

            flow.AdvanceDay();

            Assert.That(flow.DiaryRepository.TryGetRecord(2, out DiaryDayRecord dayTwo), Is.True);
            Assert.That(flow.DiaryRepository.TryGetRecord(3, out DiaryDayRecord dayThree), Is.True);
            Assert.That(dayTwo.EventRecords, Is.EquivalentTo(new[] { "Event 2 diary record" }));
            Assert.That(dayThree.EventRecords, Is.Empty);
            Assert.That(flow.DiaryRepository.GetRecords().Count, Is.EqualTo(2));
        }

        [Test]
        public void AdvanceDay_MovesFromLastEventDayToDecisionDay()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            AdvanceThroughEventDays(flow);

            Assert.That(
                flow.RuntimeState.CurrentDay,
                Is.EqualTo(BranchPrototypeSettings.DefaultDecisionDay));
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.ResolvingBranch));
            Assert.That(flow.RuntimeState.IsDecisionCompleted, Is.False);
        }

        [Test]
        public void SelectEvent_BlocksOnDecisionDay()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            AdvanceThroughEventDays(flow);

            BranchEventSelectionResult result =
                flow.SelectEvent(1, BranchTestEventFactory.ChoiceId);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.Not.Empty);
            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(5));
        }

        [Test]
        public void ResolveDecision_ResolvesRouteOnDecisionDay()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            AdvanceThroughEventDays(flow);

            BranchDecisionResult result = flow.ResolveDecision(0.95d);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.FinalSignalScore, Is.EqualTo(5));
            Assert.That(result.FinalJoinScore, Is.Zero);
            Assert.That(result.DecidedRoute, Is.EqualTo(BranchRoute.Signal));
            Assert.That(flow.DecisionResult, Is.SameAs(result));
            Assert.That(flow.RuntimeState.IsDecisionCompleted, Is.True);
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.Finished));
        }

        [Test]
        public void ResolveDecision_BlocksDuplicateResolution()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            AdvanceThroughEventDays(flow);
            BranchDecisionResult first = flow.ResolveDecision(0.25d);

            BranchDecisionResult second = flow.ResolveDecision(0.75d);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.False);
            Assert.That(second.FailureReason, Is.Not.Empty);
            Assert.That(flow.DecisionResult, Is.SameAs(first));
            Assert.That(flow.RuntimeState.DecidedRoute, Is.EqualTo(first.DecidedRoute));
        }

        [Test]
        public void ResolveDecision_UsesWeightedRatioWhenEnabled()
        {
            BranchPrototypeSettings settings = CreateShortSettings(weightedRandomDecision: true);
            BranchOneFlowController flow = new(BranchTestEventFactory.CreateCatalog(), settings);
            flow.StartTest();
            SelectAndAdvance(flow, 1);
            SelectAndAdvance(flow, 2);
            SelectAndAdvance(flow, 6);

            BranchDecisionResult result = flow.ResolveDecision(0.9d);

            Assert.That(result.SignalRatio, Is.EqualTo(2d / 3d).Within(0.0001d));
            Assert.That(result.DecidedRoute, Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void ResolveDecision_SelectsHigherScoreWhenWeightedRandomIsDisabled()
        {
            BranchPrototypeSettings settings = CreateShortSettings(weightedRandomDecision: false);
            BranchOneFlowController flow = new(BranchTestEventFactory.CreateCatalog(), settings);
            flow.StartTest();
            SelectAndAdvance(flow, 1);
            SelectAndAdvance(flow, 2);
            SelectAndAdvance(flow, 6);

            BranchDecisionResult result = flow.ResolveDecision(0.9d);

            Assert.That(result.FinalSignalScore, Is.EqualTo(2));
            Assert.That(result.FinalJoinScore, Is.EqualTo(1));
            Assert.That(result.DecidedRoute, Is.EqualTo(BranchRoute.Signal));
        }

        [Test]
        public void ResolveDecision_UsesFiftyFiftyForTieWhenWeightedRandomIsDisabled()
        {
            BranchPrototypeSettings settings = new(
                2,
                3,
                4,
                0,
                0,
                true,
                true,
                false);
            BranchOneFlowController signalFlow =
                new(BranchTestEventFactory.CreateCatalog(), settings);
            signalFlow.StartTest();
            SelectAndAdvance(signalFlow, 1);
            SelectAndAdvance(signalFlow, 6);

            BranchOneFlowController joinFlow =
                new(BranchTestEventFactory.CreateCatalog(), settings);
            joinFlow.StartTest();
            SelectAndAdvance(joinFlow, 1);
            SelectAndAdvance(joinFlow, 6);

            Assert.That(
                signalFlow.ResolveDecision(0.49d).DecidedRoute,
                Is.EqualTo(BranchRoute.Signal));
            Assert.That(
                joinFlow.ResolveDecision(0.5d).DecidedRoute,
                Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void Reset_ClearsAllFlowRuntimeData()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            flow.SelectEvent(6, BranchTestEventFactory.ChoiceId);

            flow.Reset();

            Assert.That(flow.RuntimeState.CurrentDay, Is.Zero);
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.NotStarted));
            Assert.That(flow.RuntimeState.TodaySelectedEventId, Is.Empty);
            Assert.That(flow.RuntimeState.IsTodayEventCompleted, Is.False);
            Assert.That(flow.RuntimeState.IsDecisionCompleted, Is.False);
            Assert.That(flow.RuntimeState.DecidedRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(flow.ScoreState.SignalScore, Is.Zero);
            Assert.That(flow.ScoreState.JoinScore, Is.Zero);
            Assert.That(flow.DiaryRepository.GetRecords(), Is.Empty);
            Assert.That(flow.DecisionResult, Is.Null);
            Assert.That(flow.LastFailureReason, Is.Empty);
        }

        private static BranchOneFlowController CreateFlow()
        {
            return new BranchOneFlowController(BranchTestEventFactory.CreateCatalog());
        }

        private static BranchOneFlowController CreateStartedFlow()
        {
            BranchOneFlowController flow = CreateFlow();
            flow.StartTest();
            return flow;
        }

        private static void AdvanceThroughEventDays(BranchOneFlowController flow)
        {
            while (flow.RuntimeState.CurrentDay <= BranchPrototypeSettings.DefaultLastEventDay)
            {
                BranchEventSelectionResult selection =
                    flow.SelectEvent(1, BranchTestEventFactory.ChoiceId);
                Assert.That(selection.IsSuccess, Is.True);
                Assert.That(flow.AdvanceDay(), Is.True);
            }
        }

        private static BranchPrototypeSettings CreateShortSettings(bool weightedRandomDecision)
        {
            return new BranchPrototypeSettings(
                2,
                4,
                5,
                0,
                0,
                true,
                true,
                weightedRandomDecision);
        }

        private static void SelectAndAdvance(BranchOneFlowController flow, int eventNumber)
        {
            Assert.That(
                flow.SelectEvent(eventNumber, BranchTestEventFactory.ChoiceId).IsSuccess,
                Is.True);
            Assert.That(flow.AdvanceDay(), Is.True);
        }
    }
}
