using NUnit.Framework;
using YesterdayMap.BranchOne.Quarter2;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class Quarter2EventCatalogTests
    {
        [Test]
        public void Catalog_BlocksDuplicateEventIds()
        {
            Quarter2EventCatalog catalog = new();
            Assert.That(
                catalog.TryRegister(
                    Quarter2TestEventFactory.CreateDefinition(
                        "Q2_DUPLICATE",
                        BranchRoute.Signal,
                        1),
                    out _),
                Is.True);

            bool registered = catalog.TryRegister(
                Quarter2TestEventFactory.CreateDefinition(
                    "Q2_DUPLICATE",
                    BranchRoute.Join,
                    1),
                out string failureReason);

            Assert.That(registered, Is.False);
            Assert.That(failureReason, Is.Not.Empty);
            Assert.That(catalog.GetEvents().Count, Is.EqualTo(1));
        }

        [Test]
        public void Catalog_BlocksDuplicateOrdersWithinTheSameRoute()
        {
            Quarter2EventCatalog catalog = new();
            Assert.That(
                catalog.TryRegister(
                    Quarter2TestEventFactory.CreateDefinition(
                        "Q2_SIGNAL_A",
                        BranchRoute.Signal,
                        1),
                    out _),
                Is.True);

            bool registered = catalog.TryRegister(
                Quarter2TestEventFactory.CreateDefinition(
                    "Q2_SIGNAL_B",
                    BranchRoute.Signal,
                    1),
                out string failureReason);

            Assert.That(registered, Is.False);
            Assert.That(failureReason, Is.Not.Empty);
            Assert.That(catalog.GetEvents(BranchRoute.Signal).Count, Is.EqualTo(1));
        }

        [Test]
        public void CatalogValidation_FailsUnlessEachRouteHasThreeEvents()
        {
            Quarter2EventCatalog catalog = Quarter2TestEventFactory.CreateCatalog();
            Assert.That(catalog.TryValidate(out _), Is.True);

            Quarter2EventCatalog incompleteCatalog = new();
            for (int order = 1; order <= 3; order++)
            {
                Assert.That(
                    incompleteCatalog.TryRegister(
                        Quarter2TestEventFactory.CreateDefinition(
                            $"Q2_SIGNAL_{order}",
                            BranchRoute.Signal,
                            order),
                        out _),
                    Is.True);
            }

            for (int order = 1; order <= 2; order++)
            {
                Assert.That(
                    incompleteCatalog.TryRegister(
                        Quarter2TestEventFactory.CreateDefinition(
                            $"Q2_JOIN_{order}",
                            BranchRoute.Join,
                            order),
                        out _),
                    Is.True);
            }

            Assert.That(
                incompleteCatalog.TryValidate(out string failureReason),
                Is.False);
            Assert.That(failureReason, Is.Not.Empty);
        }
    }
}
