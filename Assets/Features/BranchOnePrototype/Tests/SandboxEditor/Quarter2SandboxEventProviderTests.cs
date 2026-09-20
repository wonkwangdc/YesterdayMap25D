using NUnit.Framework;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Sandbox;

namespace YesterdayMap.BranchOne.Sandbox.Tests
{
    public sealed class Quarter2SandboxEventProviderTests
    {
        [Test]
        public void Provider_CreatesValidSixEventCatalog()
        {
            Quarter2EventCatalog catalog =
                new Quarter2SandboxEventProvider().CreateCatalog();

            Assert.That(catalog.TryValidate(out _), Is.True);
            Assert.That(catalog.GetEvents().Count, Is.EqualTo(6));
            Assert.That(catalog.GetEvents(BranchRoute.Signal).Count, Is.EqualTo(3));
            Assert.That(catalog.GetEvents(BranchRoute.Join).Count, Is.EqualTo(3));
        }

        [Test]
        public void Provider_CreatesSignalEventsInFixedOrder()
        {
            Quarter2EventCatalog catalog =
                new Quarter2SandboxEventProvider().CreateCatalog();

            Assert.That(
                catalog.TryGet(
                    BranchRoute.Signal,
                    1,
                    out Quarter2EventDefinition first),
                Is.True);
            Assert.That(first.EventId, Is.EqualTo("Q2_SIGNAL_01"));
            Assert.That(first.Title, Is.EqualTo("구조신호 2분기 이벤트 1"));
            Assert.That(first.Body, Is.Not.Empty);
        }

        [Test]
        public void Provider_CreatesJoinEventsInFixedOrder()
        {
            Quarter2EventCatalog catalog =
                new Quarter2SandboxEventProvider().CreateCatalog();

            Assert.That(
                catalog.TryGet(
                    BranchRoute.Join,
                    3,
                    out Quarter2EventDefinition third),
                Is.True);
            Assert.That(third.EventId, Is.EqualTo("Q2_JOIN_03"));
            Assert.That(third.Title, Is.EqualTo("합류 2분기 이벤트 3"));
            Assert.That(third.Body, Is.Not.Empty);
        }
    }
}
