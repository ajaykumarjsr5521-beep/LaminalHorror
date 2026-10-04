using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class FlashSafetyTests
    {
        // ---------- FlashBudget ----------

        [Test]
        public void Budget_AllowsThreeThenRefuses_WithinOneSecond()
        {
            var b = new FlashBudget();
            Assert.IsTrue(b.TryFlash(0.0f));
            Assert.IsTrue(b.TryFlash(0.2f));
            Assert.IsTrue(b.TryFlash(0.4f));
            Assert.IsFalse(b.TryFlash(0.6f));
            Assert.IsFalse(b.TryFlash(0.99f));
        }

        [Test]
        public void Budget_RecoversOnceTheOldestFlashLeavesTheWindow()
        {
            var b = new FlashBudget();
            b.TryFlash(0f);
            b.TryFlash(0.2f);
            b.TryFlash(0.4f);
            Assert.IsFalse(b.TryFlash(0.9f));
            Assert.IsTrue(b.TryFlash(1.0f), "the flash at t=0 is now a full second old");
        }

        [Test]
        public void Budget_NeverAllowsMoreThanThreeInAnyOneSecondWindow_UnderHammering()
        {
            var b = new FlashBudget();
            var allowed = new System.Collections.Generic.List<float>();
            for (float t = 0f; t < 20f; t += 0.01f)        // try to flash 100 times a second for 20 s
                if (b.TryFlash(t)) allowed.Add(t);

            for (int i = 0; i < allowed.Count; i++)
            {
                int inWindow = 0;
                for (int j = i; j < allowed.Count && allowed[j] < allowed[i] + 1f; j++) inWindow++;
                Assert.LessOrEqual(inWindow, 3, $"more than 3 flashes starting at t={allowed[i]}");
            }
            Assert.Greater(allowed.Count, 40, "the budget must still allow flashes over time, not block everything");
        }

        [Test]
        public void Budget_TimeGoingBackwards_CannotCreateExtraAllowance()
        {
            var b = new FlashBudget();
            b.TryFlash(10f);
            b.TryFlash(10.1f);
            b.TryFlash(10.2f);
            Assert.IsFalse(b.TryFlash(5f), "an earlier timestamp is treated as 'now', not as a fresh window");
        }

        [Test]
        public void Budget_Clear_ResetsIt()
        {
            var b = new FlashBudget();
            b.TryFlash(0f); b.TryFlash(0.1f); b.TryFlash(0.2f);
            b.Clear();
            Assert.IsTrue(b.TryFlash(0.3f));
        }

        // ---------- SafeFlicker ----------

        static int MidpointCrossingsInWindow(float baseI, float start, float frequency, float depth, bool reduce, float window)
        {
            float mid = baseI * (1f - Mathf.Min(depth, reduce ? SafeFlicker.ReducedMaxDepth : depth) * 0.5f);
            int crossings = 0;
            bool? above = null;
            for (float t = start; t < start + window; t += 0.001f)
            {
                bool now = SafeFlicker.Intensity(baseI, t, frequency, depth, reduce) > mid;
                if (above.HasValue && above.Value != now) crossings++;
                above = now;
            }
            return crossings;
        }

        [Test]
        public void ReduceFlickerOn_NeverChangesStateMoreThanThreeTimesPerSecond()
        {
            for (float start = 0f; start < 10f; start += 0.37f)
                Assert.LessOrEqual(MidpointCrossingsInWindow(1f, start, 12f, 1f, true, 1f), 3, $"window at {start}");
        }

        [Test]
        public void ReduceFlickerOn_LimitsBrightnessSwing()
        {
            float min = float.MaxValue, max = float.MinValue;
            for (float t = 0f; t < 4f; t += 0.001f)
            {
                float v = SafeFlicker.Intensity(1f, t, 12f, 1f, true);
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }
            Assert.LessOrEqual(max - min, SafeFlicker.ReducedMaxDepth + 1e-3f);
        }

        [Test]
        public void ReduceFlickerOff_KeepsTheFastFlicker()
        {
            Assert.Greater(MidpointCrossingsInWindow(1f, 0f, 12f, 1f, false, 1f), 10, "normal flicker is unchanged");
        }

        [Test]
        public void Intensity_ScalesWithBase_AndClampsDepth()
        {
            float v = SafeFlicker.Intensity(2f, 0.0f, 10f, 5f, false);   // depth clamps to 1
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 2f);
        }
    }
}
