using System;
using System.Linq;
using NUnit.Framework;
using NocturneAnnex.Liminal;

namespace NocturneAnnex.Tests.EditMode
{
    public class EntityPhaseMachineTests
    {
        static EntityPhaseMachine Make(out LegendProgress legend, int minLegend = 2)
        {
            legend = new LegendProgress();
            return new EntityPhaseMachine(legend, minLegend);
        }

        [Test]
        public void StartsAtLegend_AndAdvancesOnePhasePerThreshold()
        {
            var m = Make(out _);
            Assert.AreEqual(EntityPhase.Legend, m.Phase);
            CollectionAssert.AreEqual(new[] { EntityPhase.Rumor }, m.AddEvidence(1));
            Assert.AreEqual(EntityPhase.Rumor, m.Phase);
            Assert.IsEmpty(m.AddEvidence(1));   // 2 < 3
            CollectionAssert.AreEqual(new[] { EntityPhase.Clue }, m.AddEvidence(1));
        }

        [Test]
        public void BigEvidenceJump_NeverSkipsAPhase()
        {
            var m = Make(out var legend);
            legend.Found("a"); legend.Found("b");
            var entered = m.AddEvidence(100);
            CollectionAssert.AreEqual(new[] { EntityPhase.Rumor, EntityPhase.Clue, EntityPhase.Phenomenon, EntityPhase.Sighting,
                EntityPhase.Evidence, EntityPhase.Presence, EntityPhase.Hunt }, entered);
        }

        [Test]
        public void Hunt_NeedsEnoughLegendEntries_AndStartsWhenTheyAreFound()
        {
            var m = Make(out var legend, minLegend: 2);
            m.AddEvidence(100);
            Assert.AreEqual(EntityPhase.Presence, m.Phase, "plenty of evidence but the player has not learned the legend");
            legend.Found("rumor1");
            Assert.IsEmpty(m.Advance());
            legend.Found("rumor2");
            CollectionAssert.AreEqual(new[] { EntityPhase.Hunt }, m.Advance());
        }

        [Test]
        public void SurvivalAndAftermath_OnlyByExplicitCalls_InOrder()
        {
            var m = Make(out var legend, 0);
            Assert.Throws<InvalidOperationException>(() => m.EndHunt());
            m.AddEvidence(100);
            Assert.AreEqual(EntityPhase.Hunt, m.Phase);
            Assert.Throws<InvalidOperationException>(() => m.Settle());
            m.EndHunt();
            Assert.AreEqual(EntityPhase.Survival, m.Phase);
            m.Settle();
            Assert.AreEqual(EntityPhase.Aftermath, m.Phase);
            Assert.IsEmpty(m.AddEvidence(100), "nothing advances past Aftermath");
        }

        [Test]
        public void Snapshot_RestoresExactly_AndBadValuesAreClamped()
        {
            var m = Make(out _);
            m.AddEvidence(6);
            var (phase, evidence) = m.Snapshot();
            var other = Make(out _);
            other.Restore(phase, evidence);
            Assert.AreEqual(m.Phase, other.Phase);
            Assert.AreEqual(m.Evidence, other.Evidence);
            other.Restore(99, -5);
            Assert.AreEqual(EntityPhase.Aftermath, other.Phase);
            Assert.AreEqual(0, other.Evidence);
        }

        [Test]
        public void PhaseEntered_FiresForEachNewPhase()
        {
            var m = Make(out _);
            var seen = new System.Collections.Generic.List<EntityPhase>();
            m.PhaseEntered += seen.Add;
            m.AddEvidence(5);
            CollectionAssert.AreEqual(new[] { EntityPhase.Rumor, EntityPhase.Clue, EntityPhase.Phenomenon }, seen);
        }

        [Test]
        public void BadThresholds_Throw()
        {
            var l = new LegendProgress();
            Assert.Throws<ArgumentNullException>(() => new EntityPhaseMachine(null));
            Assert.Throws<ArgumentException>(() => new EntityPhaseMachine(l, 2, new[] { 1, 2, 3 }));
            Assert.Throws<ArgumentException>(() => new EntityPhaseMachine(l, 2, new[] { 1, 2, 3, 3, 5, 6, 7 }));
        }

        [Test]
        public void LegendProgress_CountsEachEntryOnce_AndRestores()
        {
            var l = new LegendProgress();
            Assert.IsTrue(l.Found("a"));
            Assert.IsFalse(l.Found("a"));
            Assert.IsFalse(l.Found(""));
            Assert.AreEqual(1, l.Count);
            l.Found("b");
            var snap = l.Snapshot();
            var l2 = new LegendProgress();
            l2.Restore(snap);
            Assert.AreEqual(2, l2.Count);
            Assert.IsTrue(l2.Has("b"));
            CollectionAssert.AreEqual(new[] { "a", "b" }, snap.OrderBy(x => x).ToArray());
        }
    }
}
