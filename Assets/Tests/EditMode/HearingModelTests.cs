using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class HearingModelTests
    {
        static NoiseEvent N(NoiseKind k, float x, float t = 0f, bool story = false) =>
            NoiseEvent.Make(new Vector3(x, 0, 0), k, t, story);

        [Test]
        public void RunCarriesFartherThanWalkAndCrouchIsQuietest()
        {
            Assert.Greater(NoiseTable.Radius(NoiseKind.Run), NoiseTable.Radius(NoiseKind.Walk));
            Assert.Greater(NoiseTable.Radius(NoiseKind.Walk), NoiseTable.Radius(NoiseKind.Crouch));
            foreach (NoiseKind k in System.Enum.GetValues(typeof(NoiseKind)))
                Assert.LessOrEqual(NoiseTable.Radius(NoiseKind.Crouch), NoiseTable.Radius(k));
        }

        [Test]
        public void SoundOutsideRadiusIsNeverHeard()
        {
            var h = new HearingModel();
            h.Report(N(NoiseKind.Walk, 0f));
            Assert.IsFalse(h.TryHear(new Vector3(6.5f, 0, 0), 0f, null, out _, out _));
            Assert.IsTrue(h.TryHear(new Vector3(5f, 0, 0), 0f, null, out _, out _));
        }

        [Test]
        public void EachWallHalvesTheRadius()
        {
            var h = new HearingModel();
            h.Report(N(NoiseKind.Run, 0f)); // 14 m
            Assert.IsTrue(h.TryHear(new Vector3(8f, 0, 0), 0f, (a, b) => 0, out _, out _));
            Assert.IsFalse(h.TryHear(new Vector3(8f, 0, 0), 0f, (a, b) => 1, out _, out _));   // 7 m
            Assert.IsFalse(h.TryHear(new Vector3(4f, 0, 0), 0f, (a, b) => 2, out _, out _));   // 3.5 m
            Assert.IsTrue(h.TryHear(new Vector3(3f, 0, 0), 0f, (a, b) => 2, out _, out _));
        }

        [Test]
        public void OldNoisesFadeAndExpire()
        {
            var h = new HearingModel();
            h.Report(N(NoiseKind.Run, 0f, t: 0f));
            h.TryHear(Vector3.zero, 0f, null, out _, out var fresh);
            h.TryHear(Vector3.zero, 5f, null, out _, out var older);
            Assert.Less(older, fresh);
            Assert.IsFalse(h.TryHear(Vector3.zero, HearingModel.MaxAgeSeconds + 0.1f, null, out _, out _));
            Assert.AreEqual(0, h.Count);
        }

        [Test]
        public void StrongestRecentNoiseWins()
        {
            var h = new HearingModel();
            h.Report(N(NoiseKind.Walk, 2f, t: 1f));
            h.Report(N(NoiseKind.DoorSlam, 2f, t: 1f));
            Assert.IsTrue(h.TryHear(Vector3.zero, 1f, null, out var best, out _));
            Assert.AreEqual(NoiseKind.DoorSlam, best.Kind);
        }

        [Test]
        public void HandledNoiseIsNotHeardAgain()
        {
            var h = new HearingModel();
            h.Report(N(NoiseKind.Run, 1f, t: 2f));
            Assert.IsTrue(h.TryHear(Vector3.zero, 2f, null, out var e, out _));
            h.MarkHandled(e.Time);
            Assert.IsFalse(h.TryHear(Vector3.zero, 2.5f, null, out _, out _));
            h.Report(N(NoiseKind.Walk, 1f, t: 3f));
            Assert.IsTrue(h.TryHear(Vector3.zero, 3f, null, out _, out _));
        }

        [Test]
        public void StoryModeHalvesRadii()
        {
            foreach (NoiseKind k in System.Enum.GetValues(typeof(NoiseKind)))
                Assert.AreEqual(NoiseTable.Radius(k) * 0.5f, NoiseTable.Radius(k, true), 1e-4f);
            var h = new HearingModel();
            h.Report(N(NoiseKind.Run, 0f, story: true)); // 7 m
            Assert.IsFalse(h.TryHear(new Vector3(8f, 0, 0), 0f, null, out _, out _));
        }
    }
}
