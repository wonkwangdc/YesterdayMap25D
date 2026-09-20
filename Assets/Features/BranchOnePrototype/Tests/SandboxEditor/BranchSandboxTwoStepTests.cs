using NUnit.Framework;
using YesterdayMap.BranchOne.Diary;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Flow;

namespace YesterdayMap.BranchOne.Sandbox.Tests
{
    public sealed class BranchSandboxTwoStepTests
    {
        [Test]
        public void Provider_CreatesTenEventsWithActAndDeclineChoices()
        {
            BranchEventCatalog catalog = new BranchSandboxEventProvider().CreateCatalog();

            Assert.That(catalog.GetEvents().Count, Is.EqualTo(10));
            for (int eventNumber = 1; eventNumber <= 10; eventNumber++)
            {
                Assert.That(
                    catalog.TryGetByNumber(
                        eventNumber,
                        out BranchEventDefinition definition),
                    Is.True);
                Assert.That(definition.Choices.Count, Is.EqualTo(2));
                Assert.That(
                    definition.TryGetChoice(
                        BranchSandboxEventProvider.CreateActChoiceId(eventNumber),
                        out BranchEventChoice actChoice),
                    Is.True);
                Assert.That(
                    definition.TryGetChoice(
                        BranchSandboxEventProvider.CreateDeclineChoiceId(eventNumber),
                        out BranchEventChoice declineChoice),
                    Is.True);
                Assert.That(actChoice.Label, Is.EqualTo("행동을 한다"));
                Assert.That(declineChoice.Label, Is.EqualTo("행동하지 않는다"));
                Assert.That(declineChoice.SignalScoreDelta, Is.Zero);
                Assert.That(declineChoice.JoinScoreDelta, Is.Zero);
            }
        }

        [Test]
        public void StartTest_UsesConfiguredRouteBaselineScoresWithoutDiaryEntry()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            flow.DiaryRepository.TryGetRecord(2, out DiaryDayRecord dayTwo);

            Assert.That(flow.RuntimeState.CurrentDay, Is.EqualTo(2));
            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(1));
            Assert.That(flow.ScoreState.JoinScore, Is.EqualTo(1));
            Assert.That(dayTwo.EventRecords, Is.Empty);
        }

        [Test]
        public void Reset_RestoresConfiguredRouteBaselineScores()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            flow.SelectEvent(
                1,
                BranchSandboxEventProvider.CreateActChoiceId(1));

            flow.Reset();

            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(1));
            Assert.That(flow.ScoreState.JoinScore, Is.EqualTo(1));
            Assert.That(flow.DiaryRepository.GetRecords(), Is.Empty);
            Assert.That(flow.DecisionResult, Is.Null);
        }

        [Test]
        public void PreviewingDefinition_DoesNotApplyScoreDiaryOrCompletion()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            bool found = flow.EventCatalog.TryGetByNumber(
                1,
                out BranchEventDefinition definition);

            Assert.That(found, Is.True);
            Assert.That(definition.Title, Is.Not.Empty);
            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(1));
            Assert.That(flow.ScoreState.JoinScore, Is.EqualTo(1));
            Assert.That(flow.RuntimeState.IsTodayEventCompleted, Is.False);
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.WaitingForEvent));
            Assert.That(
                flow.DiaryRepository.TryGetRecord(2, out DiaryDayRecord dayTwo),
                Is.True);
            Assert.That(dayTwo.EventRecords, Is.Empty);
        }

        [TestCase(1, 2, 1)]
        [TestCase(6, 1, 2)]
        public void ActChoice_AppliesExpectedRouteScore(
            int eventNumber,
            int expectedSignal,
            int expectedJoin)
        {
            BranchOneFlowController flow = CreateStartedFlow();

            BranchEventSelectionResult result = flow.SelectEvent(
                eventNumber,
                BranchSandboxEventProvider.CreateActChoiceId(eventNumber));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(expectedSignal));
            Assert.That(flow.ScoreState.JoinScore, Is.EqualTo(expectedJoin));
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.ReadyToAdvance));
        }

        [Test]
        public void DeclineChoice_CompletesEventWithoutScoreAndWritesDiary()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            BranchEventSelectionResult result = flow.SelectEvent(
                3,
                BranchSandboxEventProvider.CreateDeclineChoiceId(3));
            flow.DiaryRepository.TryGetRecord(2, out DiaryDayRecord dayTwo);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(1));
            Assert.That(flow.ScoreState.JoinScore, Is.EqualTo(1));
            Assert.That(flow.RuntimeState.IsTodayEventCompleted, Is.True);
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.ReadyToAdvance));
            Assert.That(
                dayTwo.EventRecords,
                Is.EquivalentTo(new[] { "이벤트 3에 해당하는 행동을 하지 않았다." }));
        }

        [Test]
        public void CompletedChoice_BlocksSecondChoiceOnSameDay()
        {
            BranchOneFlowController flow = CreateStartedFlow();
            BranchEventSelectionResult first = flow.SelectEvent(
                1,
                BranchSandboxEventProvider.CreateDeclineChoiceId(1));

            BranchEventSelectionResult second = flow.SelectEvent(
                1,
                BranchSandboxEventProvider.CreateActChoiceId(1));

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.False);
            Assert.That(flow.ScoreState.SignalScore, Is.EqualTo(1));
            Assert.That(flow.ScoreState.JoinScore, Is.EqualTo(1));
            Assert.That(flow.DiaryRepository.GetRecords()[0].EventRecords.Count, Is.EqualTo(1));
        }

        [Test]
        public void TwoChoiceCatalog_PreservesFullQuarterOneFlow()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            while (flow.RuntimeState.CurrentDay <= BranchPrototypeSettings.DefaultLastEventDay)
            {
                int eventNumber = flow.RuntimeState.CurrentDay % 2 == 0 ? 1 : 6;
                BranchEventSelectionResult result = flow.SelectEvent(
                    eventNumber,
                    BranchSandboxEventProvider.CreateActChoiceId(eventNumber));
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(flow.AdvanceDay(), Is.True);
            }

            var decision = flow.ResolveDecision(0.4d);

            Assert.That(
                flow.RuntimeState.CurrentDay,
                Is.EqualTo(BranchPrototypeSettings.DefaultDecisionDay));
            Assert.That(decision.IsSuccess, Is.True);
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.Finished));
            Assert.That(flow.DiaryRepository.GetRecords().Count, Is.EqualTo(5));
        }

        [Test]
        public void DecliningEveryDay_ResolvesWithBaselineFiftyFiftyScores()
        {
            BranchOneFlowController flow = CreateStartedFlow();

            while (flow.RuntimeState.CurrentDay <= BranchPrototypeSettings.DefaultLastEventDay)
            {
                BranchEventSelectionResult result = flow.SelectEvent(
                    1,
                    BranchSandboxEventProvider.CreateDeclineChoiceId(1));
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(flow.AdvanceDay(), Is.True);
            }

            var decision = flow.ResolveDecision(0.49d);

            Assert.That(decision.IsSuccess, Is.True);
            Assert.That(decision.FinalSignalScore, Is.EqualTo(1));
            Assert.That(decision.FinalJoinScore, Is.EqualTo(1));
            Assert.That(decision.SignalRatio, Is.EqualTo(0.5d));
            Assert.That(decision.JoinRatio, Is.EqualTo(0.5d));
            Assert.That(flow.RuntimeState.Phase, Is.EqualTo(BranchFlowPhase.Finished));
        }

        private static BranchOneFlowController CreateStartedFlow()
        {
            BranchEventCatalog catalog = new BranchSandboxEventProvider().CreateCatalog();
            BranchOneFlowController flow = new(
                catalog,
                BranchSandboxSettings.Create());
            flow.StartTest();
            return flow;
        }
    }
}
