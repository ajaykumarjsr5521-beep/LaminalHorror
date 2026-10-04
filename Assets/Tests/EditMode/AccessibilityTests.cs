using NUnit.Framework;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class AccessibilityTests
    {
        [SetUp]
        public void SetUp() => Accessibility.Reset();

        [TearDown]
        public void TearDown() => Accessibility.Reset();

        [Test]
        public void Defaults_CaptionsOn_MediumText_NoReductions()
        {
            Assert.IsTrue(Accessibility.CaptionsEnabled);
            Assert.AreEqual(Accessibility.MediumText, Accessibility.TextSize);
            Assert.AreEqual(1f, Accessibility.TextScale);
            Assert.IsFalse(Accessibility.ReduceFlicker);
            Assert.IsFalse(Accessibility.ReduceMotion);
            Assert.AreEqual(1f, Accessibility.MotionScale);
        }

        [TestCase(Accessibility.SmallText, 0.85f)]
        [TestCase(Accessibility.MediumText, 1f)]
        [TestCase(Accessibility.LargeText, 1.25f)]
        public void TextScale_MatchesSize(int size, float scale)
        {
            Accessibility.Set(true, size, false, false);
            Assert.AreEqual(scale, Accessibility.TextScale, 1e-4f);
        }

        [TestCase(-3, Accessibility.SmallText)]
        [TestCase(7, Accessibility.LargeText)]
        public void OutOfRangeTextSize_IsClamped(int input, int expected)
        {
            Accessibility.Set(true, input, false, false);
            Assert.AreEqual(expected, Accessibility.TextSize);
        }

        [Test]
        public void ReduceMotion_ZeroesMotionScale()
        {
            Accessibility.Set(true, Accessibility.MediumText, false, true);
            Assert.AreEqual(0f, Accessibility.MotionScale);
        }

        [Test]
        public void Changed_FiresOnlyWhenSomethingActuallyChanges()
        {
            int fired = 0;
            void H() => fired++;
            Accessibility.Changed += H;
            try
            {
                Accessibility.Set(true, Accessibility.MediumText, false, false);   // same as defaults
                Assert.AreEqual(0, fired);
                Accessibility.Set(true, Accessibility.LargeText, false, false);
                Assert.AreEqual(1, fired);
                Accessibility.Set(true, Accessibility.LargeText, false, false);
                Assert.AreEqual(1, fired);
                Accessibility.Set(true, Accessibility.LargeText, true, false);
                Assert.AreEqual(2, fired);
            }
            finally { Accessibility.Changed -= H; }
        }
    }
}
