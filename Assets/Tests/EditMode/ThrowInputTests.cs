using NUnit.Framework;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Tests.EditMode
{
    public class ThrowInputTests
    {
        [Test]
        public void TouchThrowIsOneFrameOnly()
        {
            var t = new TouchInputSource();
            t.PressThrow();
            var a = new PlayerInputState(); t.Poll(ref a);
            var b = new PlayerInputState(); t.Poll(ref b);
            Assert.IsTrue(a.ThrowPressed);
            Assert.IsFalse(b.ThrowPressed);
        }

        [Test]
        public void AggregatorMergesThrowFromAnySource()
        {
            var agg = new InputAggregator();
            var t = new TouchInputSource();
            agg.Add(new TouchInputSource());
            agg.Add(t);
            t.PressThrow();
            Assert.IsTrue(agg.Poll().ThrowPressed);
        }

        [Test]
        public void ModalBlocksThrow() =>
            Assert.IsFalse(InputMath.BlockForModal(new PlayerInputState { ThrowPressed = true }).ThrowPressed);

        [Test]
        public void ResetClearsAPendingThrow()
        {
            var t = new TouchInputSource();
            t.PressThrow(); t.ResetState();
            var s = new PlayerInputState(); t.Poll(ref s);
            Assert.IsFalse(s.ThrowPressed);
        }
    }
}
