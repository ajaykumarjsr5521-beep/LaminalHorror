using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class PlayerNoiseTests
    {
        [Test]
        public void MovementMapsToTheRightNoiseKind()
        {
            Assert.AreEqual(NoiseKind.Crouch, NoiseTable.KindForMovement(true, false));
            Assert.AreEqual(NoiseKind.Crouch, NoiseTable.KindForMovement(true, true));   // crouch wins
            Assert.AreEqual(NoiseKind.Run, NoiseTable.KindForMovement(false, true));
            Assert.AreEqual(NoiseKind.Walk, NoiseTable.KindForMovement(false, false));
        }

        [Test]
        public void SlammedDoorIsLouderThanGentleDoor()
        {
            Assert.Greater(NoiseTable.Radius(NoiseKind.DoorSlam), NoiseTable.Radius(NoiseKind.DoorGentle) * 2f);
        }

        [Test]
        public void ThrownObjectNoiseIsAtTheLandingPoint()
        {
            var bus = new NoiseBus();
            NoiseEvent got = default;
            bus.Emitted += e => got = e;
            var landing = new Vector3(4f, 0f, 9f);
            bus.Emit(NoiseEvent.Make(landing, NoiseKind.ThrownImpact, 1f));
            Assert.AreEqual(landing, got.Position);
            Assert.AreEqual(NoiseTable.Radius(NoiseKind.ThrownImpact), got.Radius, 1e-4f);
            Assert.AreEqual(1, bus.EmittedCount);
        }

        [Test]
        public void FloorSurfaceScalesTheRadius()
        {
            float walk = NoiseTable.Radius(NoiseKind.Walk);
            var carpet = NoiseEvent.Make(Vector3.zero, NoiseKind.Walk, 0f, false, NoiseTable.SurfaceMultiplier("carpet"));
            var creak = NoiseEvent.Make(Vector3.zero, NoiseKind.Walk, 0f, false, NoiseTable.SurfaceMultiplier("creak"));
            var tile = NoiseEvent.Make(Vector3.zero, NoiseKind.Walk, 0f, false, NoiseTable.SurfaceMultiplier("tile"));
            Assert.Less(carpet.Radius, walk);
            Assert.Greater(creak.Radius, walk);
            Assert.AreEqual(walk, tile.Radius, 1e-4f);
        }

        [Test]
        public void BusToHearingModelEndToEnd()
        {
            var bus = new NoiseBus();
            var hearing = new HearingModel();
            bus.Emitted += hearing.Report;
            bus.Emit(NoiseEvent.Make(Vector3.zero, NoiseKind.Run, 0f));
            Assert.IsTrue(hearing.TryHear(new Vector3(10f, 0, 0), 0f, null, out _, out _));
            hearing.Clear();
            bus.Emit(NoiseEvent.Make(Vector3.zero, NoiseKind.Crouch, 1f));
            Assert.IsFalse(hearing.TryHear(new Vector3(10f, 0, 0), 1f, null, out _, out _));
        }
    }
}
