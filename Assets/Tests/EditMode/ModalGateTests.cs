using NUnit.Framework;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class ModalGateTests
    {
        [SetUp]
        public void SetUp() => ModalGate.Reset();

        [TearDown]
        public void TearDown() => ModalGate.Reset();

        [Test]
        public void StartsClosed() => Assert.IsFalse(ModalGate.IsOpen);

        [Test]
        public void Open_ThenDispose_ClosesAgain()
        {
            var h = ModalGate.Open();
            Assert.IsTrue(ModalGate.IsOpen);
            h.Dispose();
            Assert.IsFalse(ModalGate.IsOpen);
        }

        [Test]
        public void DisposingTheSameHandleTwice_ReleasesOnlyOnce()
        {
            var a = ModalGate.Open();
            var b = ModalGate.Open();
            a.Dispose();
            a.Dispose();
            Assert.IsTrue(ModalGate.IsOpen, "the second modal must still hold the gate");
            b.Dispose();
            Assert.IsFalse(ModalGate.IsOpen);
        }

        [Test]
        public void Nested_StaysOpenUntilAllAreReleased()
        {
            var a = ModalGate.Open();
            var b = ModalGate.Open();
            b.Dispose();
            Assert.IsTrue(ModalGate.IsOpen);
            a.Dispose();
            Assert.IsFalse(ModalGate.IsOpen);
        }

        [Test]
        public void StaleHandleFromBeforeReset_CannotReleaseANewerModal()
        {
            var stale = ModalGate.Open();
            ModalGate.Reset();
            var fresh = ModalGate.Open();
            stale.Dispose();
            Assert.IsTrue(ModalGate.IsOpen, "disposing a pre-reset handle must not close the new modal");
            fresh.Dispose();
            Assert.IsFalse(ModalGate.IsOpen);
        }

        [Test]
        public void Reset_ClearsEverything_AndStaleHandlesCannotGoNegative()
        {
            var h = ModalGate.Open();
            ModalGate.Reset();
            Assert.IsFalse(ModalGate.IsOpen);
            h.Dispose();
            var next = ModalGate.Open();
            Assert.IsTrue(ModalGate.IsOpen);
            next.Dispose();
            Assert.IsFalse(ModalGate.IsOpen);
        }
    }
}
