using NUnit.Framework;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Diary;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class BranchOneCoreTests
    {
        [Test]
        public void AddSignalScore_IncreasesSignalScoreAndNotifies()
        {
            BranchScoreState scores = new();
            int notifications = 0;
            scores.ScoresChanged += () => notifications++;

            scores.AddSignalScore(3);

            Assert.That(scores.SignalScore, Is.EqualTo(3));
            Assert.That(scores.JoinScore, Is.Zero);
            Assert.That(scores.TotalScore, Is.EqualTo(3));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void AddJoinScore_IncreasesJoinScoreAndNotifies()
        {
            BranchScoreState scores = new();
            int notifications = 0;
            scores.ScoresChanged += () => notifications++;

            scores.AddJoinScore(4);

            Assert.That(scores.SignalScore, Is.Zero);
            Assert.That(scores.JoinScore, Is.EqualTo(4));
            Assert.That(scores.TotalScore, Is.EqualTo(4));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void Reset_ClearsScoresAndPreventsNegativeValues()
        {
            BranchScoreState scores = new(5, 7);

            scores.AddSignalScore(-100);
            scores.AddJoinScore(-100);
            scores.Reset();

            Assert.That(scores.SignalScore, Is.Zero);
            Assert.That(scores.JoinScore, Is.Zero);
            Assert.That(scores.TotalScore, Is.Zero);
            Assert.That(scores.SignalRatio, Is.Zero);
            Assert.That(scores.JoinRatio, Is.Zero);
        }

        [Test]
        public void DiaryRepository_PreservesRecordsForEachDayWithoutOverwrite()
        {
            BranchDiaryRepository repository = new();
            DiaryDayRecord dayTwo = repository.GetOrCreateRecord(2, "2일차 메인 기록");
            repository.AddEventRecord(2, "이벤트 1 선택");
            repository.AddEventRecord(3, "이벤트 6 선택");

            DiaryDayRecord sameDayTwo = repository.GetOrCreateRecord(2, "덮어쓰면 안 되는 기록");
            bool foundDayThree = repository.TryGetRecord(3, out DiaryDayRecord dayThree);

            Assert.That(sameDayTwo, Is.SameAs(dayTwo));
            Assert.That(sameDayTwo.MainDiary, Is.EqualTo("2일차 메인 기록"));
            Assert.That(sameDayTwo.EventRecords, Is.EquivalentTo(new[] { "이벤트 1 선택" }));
            Assert.That(foundDayThree, Is.True);
            Assert.That(dayThree.EventRecords, Is.EquivalentTo(new[] { "이벤트 6 선택" }));
            Assert.That(repository.GetRecords().Count, Is.EqualTo(2));
        }

        [Test]
        public void DecisionResolver_FailsWhenTotalScoreIsZero()
        {
            BranchDecisionResolver resolver = new();

            BranchDecisionResult result = resolver.Resolve(0, 0, 0.25d);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.DecidedRoute, Is.EqualTo(BranchRoute.None));
            Assert.That(result.FailureReason, Is.Not.Empty);
        }

        [Test]
        public void DecisionResolver_SelectsSignalAtOneHundredPercent()
        {
            BranchDecisionResolver resolver = new();

            BranchDecisionResult result = resolver.Resolve(10, 0, 1d);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.SignalRatio, Is.EqualTo(1d));
            Assert.That(result.JoinRatio, Is.Zero);
            Assert.That(result.DecidedRoute, Is.EqualTo(BranchRoute.Signal));
        }

        [Test]
        public void DecisionResolver_SelectsJoinAtOneHundredPercent()
        {
            BranchDecisionResolver resolver = new();

            BranchDecisionResult result = resolver.Resolve(0, 10, 0d);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.SignalRatio, Is.Zero);
            Assert.That(result.JoinRatio, Is.EqualTo(1d));
            Assert.That(result.DecidedRoute, Is.EqualTo(BranchRoute.Join));
        }

        [TestCase(0.74d, BranchRoute.Signal)]
        [TestCase(0.75d, BranchRoute.Join)]
        [TestCase(-2d, BranchRoute.Signal)]
        [TestCase(2d, BranchRoute.Join)]
        public void DecisionResolver_UsesFixedRollAgainstScoreRatio(double randomRoll, BranchRoute expectedRoute)
        {
            BranchDecisionResolver resolver = new();

            BranchDecisionResult result = resolver.Resolve(3, 1, randomRoll);

            Assert.That(result.SignalRatio, Is.EqualTo(0.75d));
            Assert.That(result.JoinRatio, Is.EqualTo(0.25d));
            Assert.That(result.DecidedRoute, Is.EqualTo(expectedRoute));
            Assert.That(result.RandomRoll, Is.InRange(0d, 1d));
        }

        [TestCase(0.49d, BranchRoute.Signal)]
        [TestCase(0.5d, BranchRoute.Join)]
        public void DecisionResolver_UsesFiftyFiftyThresholdForTiedScores(
            double randomRoll,
            BranchRoute expectedRoute)
        {
            BranchDecisionResolver resolver = new();

            BranchDecisionResult result = resolver.Resolve(5, 5, randomRoll);

            Assert.That(result.SignalRatio, Is.EqualTo(0.5d));
            Assert.That(result.JoinRatio, Is.EqualTo(0.5d));
            Assert.That(result.DecidedRoute, Is.EqualTo(expectedRoute));
        }
    }
}
