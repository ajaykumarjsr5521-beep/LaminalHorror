using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class WoodenLegRhythmTests
    {
        [Test]
        public void FeetAlternateNormalThenWood()
        {
            var r = new WoodenLegRhythm();
            Foot expected = Foot.Normal;
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(expected, r.Next(10f, 0.3f).Foot);
                expected = expected == Foot.Normal ? Foot.Wood : Foot.Normal;
            }
        }

        [Test]
        public void SameFootNeverRepeatsAVariantEvenWithConstantRandom()
        {
            var r = new WoodenLegRhythm();
            var lastByFoot = new Dictionary<Foot, int>();
            for (int i = 0; i < 100; i++)
            {
                var s = r.Next(10f, 0.6f);
                if (lastByFoot.TryGetValue(s.Foot, out var last)) Assert.AreNotEqual(last, s.Variant);
                lastByFoot[s.Foot] = s.Variant;
                Assert.That(s.Variant, Is.InRange(0, WoodenLegRhythm.Variants - 1));
            }
        }

        [Test]
        public void AllVariantsAreReachable()
        {
            var r = new WoodenLegRhythm();
            var seen = new HashSet<int>();
            for (int i = 0; i < 200; i++)
            {
                var s = r.Next(10f, (i % 8) / 8f);
                if (s.Foot == Foot.Wood) seen.Add(s.Variant);
            }
            Assert.AreEqual(WoodenLegRhythm.Variants, seen.Count);
        }

        [Test]
        public void DistanceBandsChangeGainAndFilter()
        {
            var near = new WoodenLegRhythm().Next(3f, 0f);
            var mid = new WoodenLegRhythm().Next(15f, 0f);
            var far = new WoodenLegRhythm().Next(40f, 0f);
            Assert.Greater(near.Gain, mid.Gain);
            Assert.Greater(mid.Gain, far.Gain);
            Assert.Greater(near.LowPassHz, mid.LowPassHz);
            Assert.Greater(mid.LowPassHz, far.LowPassHz);
        }

        [Test]
        public void CueIdsNameFootAndVariant()
        {
            Assert.AreEqual("entity.step.normal.1", WoodenLegRhythm.CueFor(new EntityStep(Foot.Normal, 1, 1f, 1f)));
            Assert.AreEqual("entity.step.wood.3", WoodenLegRhythm.CueFor(new EntityStep(Foot.Wood, 3, 1f, 1f)));
        }
    }
}
