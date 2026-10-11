using NUnit.Framework;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class DangerModelTests
    {
        [TestCase(40f, EntityState.Patrol, 0)]
        [TestCase(20f, EntityState.Patrol, 1)]
        [TestCase(12f, EntityState.Patrol, 2)]
        [TestCase(40f, EntityState.Investigate, 2)]
        [TestCase(40f, EntityState.Search, 2)]
        [TestCase(6f, EntityState.Patrol, 3)]
        [TestCase(40f, EntityState.Listen, 3)]
        [TestCase(2f, EntityState.Patrol, 4)]
        [TestCase(40f, EntityState.Chase, 4)]
        public void Raw_FollowsDistanceAndState(float distance, EntityState state, int expected) =>
            Assert.AreEqual(expected, DangerModel.Raw(distance, state));

        [Test]
        public void Rises_AtOnce()
        {
            var m = new DangerModel();
            Assert.AreEqual(1, m.Step(0.1f, 20f, EntityState.Patrol));
            Assert.AreEqual(4, m.Step(0.1f, 20f, EntityState.Chase));
        }

        [Test]
        public void Falls_OnlyAfterTheDelay_AndABriefDipDoesNotCount()
        {
            var m = new DangerModel { FallDelay = 2f };
            m.Step(0.1f, 2f, EntityState.Chase);
            Assert.AreEqual(4, m.Step(1.9f, 40f, EntityState.Patrol), "still held");
            Assert.AreEqual(0, m.Step(0.2f, 40f, EntityState.Patrol), "delay elapsed");

            m.Step(0.1f, 2f, EntityState.Chase);
            m.Step(1.5f, 40f, EntityState.Patrol);
            m.Step(0.1f, 2f, EntityState.Chase);   // back up resets the timer
            Assert.AreEqual(4, m.Step(1.5f, 40f, EntityState.Patrol));
        }

        [Test]
        public void Reset_ReturnsToSafe()
        {
            var m = new DangerModel();
            m.Step(0.1f, 2f, EntityState.Chase);
            m.Reset();
            Assert.AreEqual(DangerModel.Safe, m.Level);
        }
    }
}
