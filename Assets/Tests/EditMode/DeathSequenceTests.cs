using NUnit.Framework;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class DeathSequenceTests
    {
        [Test]
        public void FullSequenceIsShortEnoughToRepeat()
        {
            var s = new DeathSequence();
            Assert.LessOrEqual(s.Total, DeathSequence.MaxSeconds);
            Assert.Greater(s.Total, 2f);
        }

        [Test]
        public void ShortVersionIsOneSecondOfBlack()
        {
            var s = new DeathSequence(true);
            Assert.AreEqual(1f, s.Total, 1e-4f);
            Assert.AreEqual(DeathPhase.Black, s.PhaseAt(0.1f));
            Assert.AreEqual(DeathPhase.Done, s.PhaseAt(1.1f));
        }

        [Test]
        public void PhasesRunInOrder()
        {
            var s = new DeathSequence();
            var order = new System.Collections.Generic.List<DeathPhase>();
            for (float t = 0f; t < s.Total + 0.5f; t += 0.05f)
            {
                var p = s.PhaseAt(t);
                if (order.Count == 0 || order[order.Count - 1] != p) order.Add(p);
            }
            CollectionAssert.AreEqual(new[]
            {
                DeathPhase.Reveal, DeathPhase.Approach, DeathPhase.Strike, DeathPhase.Black, DeathPhase.Silence, DeathPhase.Done
            }, order);
        }

        [Test]
        public void ScreenOnlyDarkensAndEndsBlack()
        {
            var s = new DeathSequence();
            float prev = 0f;
            for (float t = 0f; t <= s.Total + 0.2f; t += 0.02f)
            {
                float b = s.BlackAt(t);
                Assert.GreaterOrEqual(b, prev - 1e-4f, "never lightens before the end");
                prev = b;
            }
            Assert.AreEqual(1f, s.BlackAt(s.Total), 1e-4f);
            Assert.AreEqual(1f, new DeathSequence(true).BlackAt(0.9f), 1e-4f);
        }

        [Test]
        public void ScreenStaysClearEnoughToSeeTheEntityDuringTheReveal()
        {
            Assert.Less(new DeathSequence().BlackAt(0.7f), 0.2f);
        }
    }
}
