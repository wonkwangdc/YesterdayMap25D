using NUnit.Framework;
using YesterdayMap.BranchOne.Quarter3;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class Quarter3CatalogTests
    {
        [Test]
        public void ClueDefinition_ValidValues_PassValidation()
        {
            Quarter3ClueDefinition definition =
                Quarter3TestFactory.CreateClue(
                    "TEST_CLUE",
                    BranchRoute.Signal,
                    Quarter3TestFactory.SourceA);

            Assert.That(definition.TryValidate(out _), Is.True);
            Assert.That(definition.ClueId, Is.EqualTo("TEST_CLUE"));
            Assert.That(definition.Route, Is.EqualTo(BranchRoute.Signal));
            Assert.That(
                definition.SourceId,
                Is.EqualTo(Quarter3TestFactory.SourceA));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ClueDefinition_EmptyClueId_FailsValidation(string clueId)
        {
            Quarter3ClueDefinition definition =
                Quarter3TestFactory.CreateClue(
                    clueId,
                    BranchRoute.Signal,
                    Quarter3TestFactory.SourceA);

            Assert.That(
                definition.TryValidate(out string failureReason),
                Is.False);
            Assert.That(failureReason, Is.Not.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ClueDefinition_EmptySourceId_FailsValidation(
            string sourceId)
        {
            Quarter3ClueDefinition definition =
                Quarter3TestFactory.CreateClue(
                    "TEST_CLUE",
                    BranchRoute.Signal,
                    sourceId);

            Assert.That(
                definition.TryValidate(out string failureReason),
                Is.False);
            Assert.That(failureReason, Is.Not.Empty);
        }

        [Test]
        public void ClueDefinition_InvalidRoute_FailsValidation()
        {
            Quarter3ClueDefinition definition =
                Quarter3TestFactory.CreateClue(
                    "TEST_CLUE",
                    BranchRoute.None,
                    Quarter3TestFactory.SourceA);

            Assert.That(definition.TryValidate(out _), Is.False);
        }

        [Test]
        public void ClueCatalog_NullDefinition_IsRejected()
        {
            Quarter3ClueCatalog catalog = new();

            Assert.That(
                catalog.TryRegister(null, out string failureReason),
                Is.False);
            Assert.That(failureReason, Is.Not.Empty);
        }

        [Test]
        public void ClueCatalog_DuplicateClueId_IsRejected()
        {
            Quarter3ClueCatalog catalog = new();
            Quarter3ClueDefinition first =
                Quarter3TestFactory.CreateClue(
                    "TEST_DUPLICATE",
                    BranchRoute.Signal,
                    Quarter3TestFactory.SourceA);
            Quarter3ClueDefinition duplicate =
                Quarter3TestFactory.CreateClue(
                    "TEST_DUPLICATE",
                    BranchRoute.Join,
                    Quarter3TestFactory.SourceB);

            Assert.That(catalog.TryRegister(first, out _), Is.True);
            Assert.That(
                catalog.TryRegister(
                    duplicate,
                    out string failureReason),
                Is.False);
            Assert.That(failureReason, Is.Not.Empty);
            Assert.That(catalog.GetClues().Count, Is.EqualTo(1));
        }

        [Test]
        public void ClueCatalog_RegisteredClue_CanBeFoundById()
        {
            Quarter3ClueCatalog catalog =
                Quarter3TestFactory.CreateClueCatalog();

            bool found = catalog.TryGetById(
                Quarter3TestFactory.SignalClue01,
                out Quarter3ClueDefinition definition);

            Assert.That(found, Is.True);
            Assert.That(
                definition.SourceId,
                Is.EqualTo(Quarter3TestFactory.SourceA));
        }

        [Test]
        public void ClueCatalog_RouteFilter_ReturnsOnlyMatchingRoute()
        {
            Quarter3ClueCatalog catalog =
                Quarter3TestFactory.CreateClueCatalog();

            var clues = catalog.GetClues(BranchRoute.Join);

            Assert.That(clues.Count, Is.EqualTo(2));
            Assert.That(
                clues,
                Has.All.Property("Route").EqualTo(BranchRoute.Join));
        }

        [Test]
        public void ClueCatalog_RouteAndSourceFilter_ReturnsOnlyMatches()
        {
            Quarter3ClueCatalog catalog =
                Quarter3TestFactory.CreateClueCatalog();

            var clues = catalog.GetClues(
                BranchRoute.Signal,
                Quarter3TestFactory.SourceA);

            Assert.That(clues.Count, Is.EqualTo(2));
            Assert.That(
                clues[0].ClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue01));
            Assert.That(
                clues[1].ClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue02));
        }

        [Test]
        public void ClueCatalog_UnregisteredId_IsNotFound()
        {
            Quarter3ClueCatalog catalog =
                Quarter3TestFactory.CreateClueCatalog();

            Assert.That(
                catalog.TryGetById("TEST_UNKNOWN", out _),
                Is.False);
        }

        [Test]
        public void FinalChoiceDefinition_ValidValues_PassValidation()
        {
            Quarter3FinalChoiceDefinition definition =
                Quarter3TestFactory.CreateFinalChoice(
                    "TEST_OPTION",
                    BranchRoute.Join);

            Assert.That(definition.TryValidate(out _), Is.True);
            Assert.That(
                definition.FinalChoiceId,
                Is.EqualTo("TEST_OPTION"));
            Assert.That(definition.Route, Is.EqualTo(BranchRoute.Join));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void FinalChoiceDefinition_EmptyId_FailsValidation(
            string finalChoiceId)
        {
            Quarter3FinalChoiceDefinition definition =
                Quarter3TestFactory.CreateFinalChoice(
                    finalChoiceId,
                    BranchRoute.Signal);

            Assert.That(
                definition.TryValidate(out string failureReason),
                Is.False);
            Assert.That(failureReason, Is.Not.Empty);
        }

        [Test]
        public void FinalChoiceDefinition_InvalidRoute_FailsValidation()
        {
            Quarter3FinalChoiceDefinition definition =
                Quarter3TestFactory.CreateFinalChoice(
                    "TEST_OPTION",
                    BranchRoute.None);

            Assert.That(definition.TryValidate(out _), Is.False);
        }

        [Test]
        public void FinalChoiceCatalog_NullDefinition_IsRejected()
        {
            Quarter3FinalChoiceCatalog catalog = new();

            Assert.That(
                catalog.TryRegister(null, out string failureReason),
                Is.False);
            Assert.That(failureReason, Is.Not.Empty);
        }

        [Test]
        public void FinalChoiceCatalog_DuplicateId_IsRejected()
        {
            Quarter3FinalChoiceCatalog catalog = new();
            Quarter3FinalChoiceDefinition first =
                Quarter3TestFactory.CreateFinalChoice(
                    "TEST_DUPLICATE",
                    BranchRoute.Signal);
            Quarter3FinalChoiceDefinition duplicate =
                Quarter3TestFactory.CreateFinalChoice(
                    "TEST_DUPLICATE",
                    BranchRoute.Join);

            Assert.That(catalog.TryRegister(first, out _), Is.True);
            Assert.That(catalog.TryRegister(duplicate, out _), Is.False);
            Assert.That(catalog.GetChoices().Count, Is.EqualTo(1));
        }

        [Test]
        public void FinalChoiceCatalog_RouteFilter_ReturnsTwoChoices()
        {
            Quarter3FinalChoiceCatalog catalog =
                Quarter3TestFactory.CreateFinalChoiceCatalog();

            var choices = catalog.GetChoices(BranchRoute.Signal);

            Assert.That(choices.Count, Is.EqualTo(2));
            Assert.That(
                choices,
                Has.All.Property("Route").EqualTo(BranchRoute.Signal));
        }

        [Test]
        public void FinalChoiceCatalog_CompleteCatalog_PassesValidation()
        {
            Quarter3FinalChoiceCatalog catalog =
                Quarter3TestFactory.CreateFinalChoiceCatalog();

            Assert.That(catalog.TryValidate(out _), Is.True);
        }

        [Test]
        public void FinalChoiceCatalog_IncompleteRoute_FailsValidation()
        {
            Quarter3FinalChoiceCatalog catalog = new();
            Assert.That(
                catalog.TryRegister(
                    Quarter3TestFactory.CreateFinalChoice(
                        Quarter3TestFactory.SignalOptionA,
                        BranchRoute.Signal),
                    out _),
                Is.True);

            Assert.That(
                catalog.TryValidate(out string failureReason),
                Is.False);
            Assert.That(failureReason, Is.Not.Empty);
        }

        [Test]
        public void FinalChoiceCatalog_OverfilledRoute_FailsValidation()
        {
            Quarter3FinalChoiceCatalog catalog =
                Quarter3TestFactory.CreateFinalChoiceCatalog();
            Assert.That(
                catalog.TryRegister(
                    Quarter3TestFactory.CreateFinalChoice(
                        "TEST_SIGNAL_OPTION_C",
                        BranchRoute.Signal),
                    out _),
                Is.True);

            Assert.That(catalog.TryValidate(out _), Is.False);
        }
    }
}
