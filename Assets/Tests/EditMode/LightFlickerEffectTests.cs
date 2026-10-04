using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.Core;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Tests.EditMode
{
    public class LightFlickerEffectTests
    {
        const float Frame = 1f / 60f;

        static List<float> Run(LightFlickerEffect fx, bool reduce, float seconds, out float min)
        {
            var dimStarts = new List<float>();
            min = 1f;
            bool prevDim = false;
            for (float t = 0f; t < seconds; t += Frame)
            {
                float m = fx.Multiplier(t, reduce);
                if (m < min) min = m;
                bool dim = m < 0.999f;
                if (dim && !prevDim) dimStarts.Add(t);
                prevDim = dim;
            }
            return dimStarts;
        }

        static int MaxInAnySecond(List<float> times)
        {
            int max = 0;
            for (int i = 0; i < times.Count; i++)
            {
                int n = 0;
                for (int j = i; j < times.Count && times[j] < times[i] + 1f; j++) n++;
                if (n > max) max = n;
            }
            return max;
        }

        [Test]
        public void ReduceOff_EvenAtEightHertz_NeverMoreThanThreeDimsPerSecond()
        {
            var fx = new LightFlickerEffect(new FlashBudget(), frequencyHz: 8f, depth: 0.9f);
            var dims = Run(fx, reduce: false, seconds: 10f, out float min);
            Assert.Greater(dims.Count, 5, "the light should still visibly flicker");
            Assert.LessOrEqual(MaxInAnySecond(dims), 3);
            Assert.AreEqual(0.1f, min, 0.01f, "unreduced flicker keeps its authored depth");
        }

        [Test]
        public void ReduceOn_IsSlowAndShallow()
        {
            var fx = new LightFlickerEffect(new FlashBudget(), frequencyHz: 8f, depth: 0.9f);
            Run(fx, reduce: true, seconds: 10f, out float min);
            Assert.GreaterOrEqual(min, 0.75f - 0.001f, "at most 25 percent deep");

            // count crossings of the waveform midpoint per second: the F-08 limit is 3
            var crossings = new List<float>();
            float mid = (1f + min) / 2f;
            bool above = true;
            for (float t = 0f; t < 10f; t += Frame)
            {
                bool nowAbove = fx.Multiplier(t, true) > mid;
                if (nowAbove != above) crossings.Add(t);
                above = nowAbove;
            }
            Assert.LessOrEqual(MaxInAnySecond(crossings), 3);
        }

        [Test]
        public void SlowAuthoredFlicker_IsNotThrottled()
        {
            var fx = new LightFlickerEffect(new FlashBudget(), frequencyHz: 1f, depth: 0.5f);
            var dims = Run(fx, false, 10f, out _);
            Assert.AreEqual(10, dims.Count, 0, "1 Hz is under the budget, so every dip happens");
        }

        [Test]
        public void MultiplierStaysBetweenZeroAndOne()
        {
            var fx = new LightFlickerEffect(null, 5f, 2f);   // depth above 1 is clamped by SafeFlicker
            for (float t = 0f; t < 5f; t += Frame)
            {
                float m = fx.Multiplier(t, false);
                Assert.That(m, Is.InRange(0f, 1f));
            }
        }
    }
}
