using NUnit.Framework;
using YesterdayMap.BranchOne.Events;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class BranchEventCatalogTests
    {
        [Test]
        public void Catalog_LooksUpEventsByIdAndNumber()
        {
            BranchEventCatalog catalog = BranchTestEventFactory.CreateCatalog();

            bool foundById = catalog.TryGetById("event-3", out BranchEventDefinition byId);
            bool foundByNumber = catalog.TryGetByNumber(3, out BranchEventDefinition byNumber);

            Assert.That(foundById, Is.True);
            Assert.That(foundByNumber, Is.True);
            Assert.That(byId, Is.SameAs(byNumber));
            Assert.That(byId.EventNumber, Is.EqualTo(3));
        }

        [Test]
        public void Catalog_BlocksDuplicateIdsAndNumbers()
        {
            BranchEventCatalog catalog = new();
            Assert.That(
                catalog.TryRegister(
                    BranchTestEventFactory.CreateDefinition("event-1", 1),
                    out _),
                Is.True);

            bool duplicateIdRegistered = catalog.TryRegister(
                BranchTestEventFactory.CreateDefinition("event-1", 2),
                out string duplicateIdReason);
            bool duplicateNumberRegistered = catalog.TryRegister(
                BranchTestEventFactory.CreateDefinition("event-2", 1),
                out string duplicateNumberReason);

            Assert.That(duplicateIdRegistered, Is.False);
            Assert.That(duplicateIdReason, Is.Not.Empty);
            Assert.That(duplicateNumberRegistered, Is.False);
            Assert.That(duplicateNumberReason, Is.Not.Empty);
            Assert.That(catalog.GetEvents().Count, Is.EqualTo(1));
        }

        [Test]
        public void Catalog_FiltersEventsByAvailableDayRange()
        {
            BranchEventCatalog catalog = BranchTestEventFactory.CreateCatalog();

            Assert.That(catalog.GetAvailableEvents(1), Is.Empty);
            Assert.That(catalog.GetAvailableEvents(2).Count, Is.EqualTo(10));
            Assert.That(catalog.GetAvailableEvents(12).Count, Is.EqualTo(10));
            Assert.That(catalog.GetAvailableEvents(13), Is.Empty);
        }
    }
}
