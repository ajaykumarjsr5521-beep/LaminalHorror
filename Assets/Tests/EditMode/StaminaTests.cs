using NUnit.Framework;
using NocturneAnnex.Player;

namespace NocturneAnnex.Tests.EditMode
{
    public class StaminaTests
    {
        [Test]
        public void Sprinting_DrainsStamina()
        {
            var s = new StaminaModel();
            s.Tick(1f, true);
            Assert.AreEqual(75f, s.Current, 1e-3f);
        }

        [Test]
        public void RunningDry_ExhaustsAndBlocksSprint()
        {
            var s = new StaminaModel();
            for (int i = 0; i < 50; i++) s.Tick(0.1f, true);
            Assert.IsTrue(s.Exhausted);
            Assert.IsFalse(s.CanSprint);
        }

        [Test]
        public void Regen_WaitsForDelay()
        {
            var s = new StaminaModel();
            s.Tick(1f, true);
            s.Tick(0.5f, false);
            Assert.AreEqual(75f, s.Current, 1e-3f);
            s.Tick(1f, false);
            Assert.Greater(s.Current, 75f);
        }

        [Test]
        public void Exhausted_ResumesOnlyAfterThreshold()
        {
            var s = new StaminaModel();
            while (!s.Exhausted) s.Tick(0.1f, true);
            s.Tick(0.5f, false);   // inside regen delay: no recovery
            Assert.IsTrue(s.Exhausted);
            s.Tick(0.6f, false);   // delay passed, ~12 stamina: still below the 25 threshold
            Assert.IsTrue(s.Exhausted);
            s.Tick(1.0f, false);   // ~32 stamina
            Assert.IsFalse(s.Exhausted);
            Assert.IsTrue(s.CanSprint);
        }

        [Test]
        public void Regen_NeverExceedsMax()
        {
            var s = new StaminaModel();
            s.Tick(10f, false);
            Assert.AreEqual(s.Max, s.Current, 1e-3f);
        }
    }
}
