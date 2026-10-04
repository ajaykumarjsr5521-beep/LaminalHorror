using System;
using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Tests.EditMode
{
    public class TensionDirectorTests
    {
        const float Step = 0.5f;

        static List<float> Simulate(TensionDirector d, float seconds, bool unsafeZone = true)
        {
            var times = new List<float>();
            for (float t = 0f; t < seconds; t += Step)
            {
                d.Tick(Step, unsafeZone, blocked: false);
                if (d.TryTakeEvent()) times.Add(t);
            }
            return times;
        }

        [Test]
        public void ThirtyMinutesUnsafe_EventsAreNeverCloserThanTheMinimumGap()
        {
            var d = new TensionDirector(new TensionSettings());
            var times = Simulate(d, 1800f);
            Assert.GreaterOrEqual(times.Count, 10, "pacing should still produce events");
            for (int i = 1; i < times.Count; i++)
                Assert.GreaterOrEqual(times[i] - times[i - 1], 45f - 0.001f, $"events {i - 1} and {i}");
        }

        [Test]
        public void SafeZone_DrainsTensionToZero_AndNeverFires()
        {
            var d = new TensionDirector(new TensionSettings());
            for (int i = 0; i < 40; i++) d.Tick(Step, true, false);
            Assert.Greater(d.Tension, 0.3f);
            var times = Simulate(d, 600f, unsafeZone: false);
            Assert.AreEqual(0f, d.Tension);
            Assert.IsEmpty(times);
        }

        [Test]
        public void Blocked_ChangesNothing_HoweverLong()
        {
            var d = new TensionDirector(new TensionSettings());
            for (int i = 0; i < 20; i++) d.Tick(Step, true, false);
            float before = d.Tension;
            for (int i = 0; i < 100000; i++) d.Tick(Step, true, blocked: true);
            Assert.AreEqual(before, d.Tension);
        }

        [Test]
        public void Blocked_TimeDoesNotCountTowardsTheGap()
        {
            var d = new TensionDirector(new TensionSettings { MinGapSeconds = 45f });
            while (!d.CanFire) d.Tick(Step, true, false);
            Assert.IsTrue(d.TryTakeEvent());
            for (int i = 0; i < 1000; i++) d.Tick(Step, true, blocked: true);   // a long pause
            d.Tick(Step, true, false);
            Assert.IsFalse(d.CanFire, "time spent paused must not count towards the gap");
        }

        [Test]
        public void FirstEvent_IsNotDelayedByAGap()
        {
            var d = new TensionDirector(new TensionSettings());
            float elapsed = 0f;
            while (!d.CanFire) { d.Tick(Step, true, false); elapsed += Step; }
            Assert.Less(elapsed, 45f, "tension alone decides the first event");
        }

        [Test]
        public void TakingAnEvent_LowersTension_NeverBelowZero()
        {
            var d = new TensionDirector(new TensionSettings { ReliefAfterEvent = 1f });
            while (!d.CanFire) d.Tick(Step, true, false);
            Assert.IsTrue(d.TryTakeEvent());
            Assert.AreEqual(0f, d.Tension);
        }

        [Test]
        public void TryTakeEvent_RefusesWhenTooSoonOrTooCalm()
        {
            var d = new TensionDirector(new TensionSettings());
            Assert.IsFalse(d.TryTakeEvent(), "no tension yet");
            while (!d.CanFire) d.Tick(Step, true, false);
            Assert.IsTrue(d.TryTakeEvent());
            Assert.IsFalse(d.TryTakeEvent(), "just fired");
        }

        [Test]
        public void StoryMode_HalvesRise_AndDoublesGap()
        {
            var normal = new TensionDirector(new TensionSettings());
            var story = new TensionDirector(new TensionSettings(), storyMode: true);
            normal.Tick(10f, true, false);
            story.Tick(10f, true, false);
            Assert.AreEqual(normal.Tension / 2f, story.Tension, 0.0001f);
            Assert.AreEqual(normal.EffectiveGapSeconds * 2f, story.EffectiveGapSeconds, 0.0001f);

            var times = Simulate(new TensionDirector(new TensionSettings(), storyMode: true), 1800f);
            Assert.GreaterOrEqual(times.Count, 5);
            for (int i = 1; i < times.Count; i++) Assert.GreaterOrEqual(times[i] - times[i - 1], 90f - 0.001f);
        }

        [Test]
        public void Tension_StaysWithinZeroAndOne()
        {
            var d = new TensionDirector(new TensionSettings());
            for (int i = 0; i < 5000; i++) d.Tick(Step, true, false);
            Assert.LessOrEqual(d.Tension, 1f);
            for (int i = 0; i < 5000; i++) d.Tick(Step, false, false);
            Assert.GreaterOrEqual(d.Tension, 0f);
        }

        [Test]
        public void Reset_ClearsTensionAndTheGap()
        {
            var d = new TensionDirector(new TensionSettings());
            while (!d.CanFire) d.Tick(Step, true, false);
            d.TryTakeEvent();
            d.Reset();
            Assert.AreEqual(0f, d.Tension);
            while (!d.CanFire) d.Tick(Step, true, false);
            Assert.IsTrue(d.TryTakeEvent(), "after a reset the first event is not held by the old gap");
        }

        [Test]
        public void InvalidSettings_Throw()
        {
            Assert.Throws<ArgumentException>(() => new TensionDirector(new TensionSettings { MinGapSeconds = 0f }));
            Assert.Throws<ArgumentException>(() => new TensionDirector(new TensionSettings { RisePerSecondUnsafe = -1f }));
            Assert.Throws<ArgumentException>(() => new TensionDirector(new TensionSettings { DecayPerSecondSafe = 0f }));
            Assert.Throws<ArgumentException>(() => new TensionDirector(new TensionSettings { FireThreshold = 1.5f }));
            Assert.Throws<ArgumentException>(() => new TensionDirector(new TensionSettings { ReliefAfterEvent = -0.1f }));
            Assert.Throws<ArgumentNullException>(() => new TensionDirector(null));
        }
    }
}
