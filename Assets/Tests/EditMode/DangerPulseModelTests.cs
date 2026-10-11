using NUnit.Framework;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class DangerPulseModelTests
    {
        [Test]
        public void ReduceMotion_RemovesShake_AndPulse()
        {
            for (int l = 0; l <= 4; l++) Assert.AreEqual(0f, DangerPulseModel.ShakeAmplitude(l, true));
            Assert.AreEqual(DangerPulseModel.VignetteAlpha(4, 0.1f, true, false), DangerPulseModel.VignetteAlpha(4, 0.6f, true, false), "no pulse");
        }

        [Test]
        public void ReduceFlicker_KeepsASteadyTint()
        {
            Assert.AreEqual(DangerPulseModel.VignetteAlpha(3, 0.1f, false, true), DangerPulseModel.VignetteAlpha(3, 0.35f, false, true));
        }

        [Test]
        public void Normal_PulsesAndGrowsWithLevel()
        {
            Assert.AreNotEqual(DangerPulseModel.VignetteAlpha(4, 0.25f, false, false), DangerPulseModel.VignetteAlpha(4, 0.75f, false, false));
            Assert.Greater(DangerPulseModel.ShakeAmplitude(4, false), DangerPulseModel.ShakeAmplitude(3, false));
            Assert.AreEqual(0f, DangerPulseModel.ShakeAmplitude(0, false));
            Assert.Greater(DangerPulseModel.VignetteAlpha(4, 0f, false, true), DangerPulseModel.VignetteAlpha(1, 0f, false, true));
        }
    }
}
