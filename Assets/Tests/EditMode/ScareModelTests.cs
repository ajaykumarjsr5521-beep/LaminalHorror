using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Entity;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Tests.EditMode
{
    public class ScareModelTests
    {
        [Test]
        public void FalseFootsteps_NeverMatchTheRealStrictAlternation()
        {
            for (int seed = 0; seed < 2000; seed++)
            {
                var steps = FalseFootsteps.Make(seed);
                Assert.GreaterOrEqual(steps.Length, 4);
                Assert.LessOrEqual(steps.Length, 6);
                Assert.IsTrue(FalseFootsteps.HasRepeat(steps), "seed " + seed + " is a clean alternation");
                foreach (var s in steps) Assert.That(s.GapSeconds, Is.InRange(FalseFootsteps.MinGap, FalseFootsteps.MaxGap));
            }
        }

        [Test]
        public void RealRhythm_IsStrictAlternation_SoTheFakeOneDiffers()
        {
            var real = new WoodenLegRhythm();
            var feet = Enumerable.Range(0, 12).Select(_ => real.Next(10f, 0.5f).Foot).ToList();
            for (int i = 1; i < feet.Count; i++) Assert.AreNotEqual(feet[i - 1], feet[i], "the real gait alternates");
        }

        [Test]
        public void FalseFootsteps_CueIdsUseTheSameRecordedVariants()
        {
            Assert.AreEqual("entity.step.wood.2", FalseFootsteps.CueFor(new FalseFootsteps.Step(true, 0.5f), 2));
            Assert.AreEqual("entity.step.normal.3", FalseFootsteps.CueFor(new FalseFootsteps.Step(false, 0.5f), 99));
        }

        [Test]
        public void ToyBoxes_AlwaysLeaveAReachableKey_InAnyOpeningOrder()
        {
            for (int seed = 0; seed < 300; seed++)
            {
                var order = Enumerable.Range(0, ToyBoxModel.BoxCount).OrderBy(i => (i * 7919 + seed * 104729) % 97).ToList();
                var m = new ToyBoxModel(seed);
                var found = new List<ToyBoxContent>();
                foreach (int box in order) found.Add(m.Open(box));
                Assert.AreEqual(1, found.Count(c => c == ToyBoxContent.Key), "seed " + seed);
                Assert.AreEqual(1, found.Count(c => c == ToyBoxContent.SpringToy));
                Assert.AreEqual(1, found.Count(c => c == ToyBoxContent.FootstepBeat));
                Assert.AreEqual(5, found.Count(c => c == ToyBoxContent.Empty));
                Assert.IsTrue(m.KeyFound);
            }
        }

        [Test]
        public void ToyBox_OpensOnce_AndBadIndexesAreEmpty()
        {
            var m = new ToyBoxModel(1);
            var first = Enumerable.Range(0, ToyBoxModel.BoxCount).Select(m.Open).ToList();
            Assert.AreEqual(ToyBoxModel.BoxCount, m.OpenedCount);
            for (int i = 0; i < ToyBoxModel.BoxCount; i++) Assert.AreEqual(ToyBoxContent.Empty, m.Open(i), "already opened");
            Assert.AreEqual(ToyBoxContent.Empty, m.Open(-1));
            Assert.AreEqual(ToyBoxContent.Empty, m.Open(ToyBoxModel.BoxCount));
            Assert.AreEqual(ToyBoxModel.BoxCount, m.OpenedCount);
            Assert.IsTrue(first.Contains(ToyBoxContent.Key));
        }

        [Test]
        public void ToyBox_SameSeedSameLayout_DifferentSeedsVary()
        {
            int KeyBox(int seed) { var m = new ToyBoxModel(seed); for (int i = 0; i < 8; i++) if (m.Open(i) == ToyBoxContent.Key) return i; return -1; }
            Assert.AreEqual(KeyBox(5), KeyBox(5));
            Assert.Greater(Enumerable.Range(0, 40).Select(KeyBox).Distinct().Count(), 3, "the key is not always in the same box");
        }

        [Test]
        public void ViewMath_InViewWithinHalfFov()
        {
            Assert.IsTrue(ViewMath.InView(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 5f), 30f));
            Assert.IsFalse(ViewMath.InView(Vector3.zero, Vector3.forward, new Vector3(5f, 0f, 0f), 30f));
            Assert.IsFalse(ViewMath.InView(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -5f), 30f));
            Assert.IsTrue(ViewMath.InView(Vector3.zero, Vector3.forward, Vector3.zero, 30f));
        }
    }
}
