using NUnit.Framework;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class HapticsPlanTests
    {
        [Test]
        public void EveryEvent_HasAPulse_WhenOn()
        {
            foreach (HapticEvent e in System.Enum.GetValues(typeof(HapticEvent)))
            {
                var p = HapticsPlan.For(e, true, 1f);
                Assert.IsNotNull(p, e.ToString());
                Assert.That(p.Value.Seconds, Is.GreaterThan(0f));
                Assert.That(p.Value.Low, Is.InRange(0f, 1f));
                Assert.That(p.Value.High, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void Disabled_SendsNothing()
        {
            foreach (HapticEvent e in System.Enum.GetValues(typeof(HapticEvent)))
                Assert.IsNull(HapticsPlan.For(e, false, 1f), e.ToString());
        }

        [Test]
        public void TooLowOrBadStrength_SendsNothing()
        {
            Assert.IsNull(HapticsPlan.For(HapticEvent.Hit, true, 0f));
            Assert.IsNull(HapticsPlan.For(HapticEvent.Hit, true, 0.04f));
            Assert.IsNull(HapticsPlan.For(HapticEvent.Hit, true, float.NaN));
            Assert.IsNotNull(HapticsPlan.For(HapticEvent.Hit, true, 0.05f));
        }

        [Test]
        public void Strength_ScalesTheMotors_NotTheDuration_AndNeverExceedsOne()
        {
            var full = HapticsPlan.For(HapticEvent.Hit, true, 1f).Value;
            var half = HapticsPlan.For(HapticEvent.Hit, true, 0.5f).Value;
            Assert.AreEqual(full.Low * 0.5f, half.Low, 0.0001f);
            Assert.AreEqual(full.Seconds, half.Seconds);
            Assert.AreEqual(full.Low, HapticsPlan.For(HapticEvent.Hit, true, 5f).Value.Low, 0.0001f);
        }

        [Test]
        public void HitIsStrongerThanAFootstep()
        {
            Assert.Greater(HapticsPlan.For(HapticEvent.Hit, true, 1f).Value.Low, HapticsPlan.For(HapticEvent.Footstep, true, 1f).Value.Low);
        }

        [Test]
        public void FootstepScale_FadesWithDistance_AndStopsAtRange()
        {
            Assert.AreEqual(1f, HapticsPlan.FootstepScale(0f), 0.0001f);
            Assert.AreEqual(0.5f, HapticsPlan.FootstepScale(HapticsPlan.FootstepRange / 2f), 0.0001f);
            Assert.AreEqual(0f, HapticsPlan.FootstepScale(HapticsPlan.FootstepRange));
            Assert.AreEqual(0f, HapticsPlan.FootstepScale(50f));
        }

        [Test]
        public void Limiter_EnforcesAMinimumGapPerKind()
        {
            var l = new HapticLimiter();
            Assert.IsTrue(l.TryAcquire(HapticEvent.Footstep, 0f));
            Assert.IsFalse(l.TryAcquire(HapticEvent.Footstep, 0.1f));
            Assert.IsTrue(l.TryAcquire(HapticEvent.Hit, 0.1f), "other kinds are independent");
            Assert.IsTrue(l.TryAcquire(HapticEvent.Footstep, 0.25f));
            Assert.IsFalse(l.TryAcquire(HapticEvent.Scare, 0f) && l.TryAcquire(HapticEvent.Scare, 0.9f));
            l.Reset();
            Assert.IsTrue(l.TryAcquire(HapticEvent.Footstep, 0.26f));
        }

        [Test]
        public void HapticSettings_ClampsStrength()
        {
            HapticSettings.Set(true, 3f);
            Assert.AreEqual(1f, HapticSettings.Strength);
            HapticSettings.Set(false, -1f);
            Assert.AreEqual(0f, HapticSettings.Strength);
            Assert.IsFalse(HapticSettings.Enabled);
            HapticSettings.Set(true, 0.7f);   // back to the default for other tests
        }
    }
}
