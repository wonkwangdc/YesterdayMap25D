using NUnit.Framework;
using YesterdayMap.BranchOne.Quarter3;

namespace YesterdayMap.BranchOne.Sandbox.Tests
{
    public sealed class Quarter3SandboxDataProviderTests
    {
        [Test]
        public void CreateClueCatalog_ReturnsExpectedTestDefinitions()
        {
            Quarter3ClueCatalog catalog =
                new Quarter3SandboxDataProvider().CreateClueCatalog();

            Assert.That(catalog.GetClues(BranchRoute.Signal).Count, Is.EqualTo(3));
            Assert.That(catalog.GetClues(BranchRoute.Join).Count, Is.EqualTo(2));
            Assert.That(
                catalog.GetClues(
                    BranchRoute.Signal,
                    Quarter3SandboxDataProvider.SourceA).Count,
                Is.EqualTo(2));
        }

        [Test]
        public void CreateFinalChoiceCatalog_ReturnsTwoChoicesPerRoute()
        {
            Quarter3FinalChoiceCatalog catalog =
                new Quarter3SandboxDataProvider()
                    .CreateFinalChoiceCatalog();

            Assert.That(catalog.TryValidate(out _), Is.True);
            Assert.That(
                catalog.GetChoices(BranchRoute.Signal).Count,
                Is.EqualTo(2));
            Assert.That(
                catalog.GetChoices(BranchRoute.Join).Count,
                Is.EqualTo(2));
        }

        [Test]
        public void ProviderCatalogs_ContainOnlyTestIds()
        {
            Quarter3SandboxDataProvider provider = new();
            Quarter3ClueCatalog clues = provider.CreateClueCatalog();
            Quarter3FinalChoiceCatalog choices =
                provider.CreateFinalChoiceCatalog();

            Assert.That(
                clues.GetClues(),
                Has.All.Property("ClueId").StartsWith("TEST_"));
            Assert.That(
                choices.GetChoices(),
                Has.All.Property("FinalChoiceId").StartsWith("TEST_"));
        }
    }
}
