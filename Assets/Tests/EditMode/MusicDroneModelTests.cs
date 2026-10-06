using NUnit.Framework;
using NocturneAnnex.Audio;

namespace NocturneAnnex.Tests.EditMode
{
    public class MusicDroneModelTests
    {
        [Test]
        public void Target_FollowsTension_BetweenTheFloorAndFullLevel()
        {
            var m = new MusicDroneModel { MinLevel = 0.2f, MaxLevel = 1f };
            Assert.AreEqual(0.2f, m.Target(0f, false), 1e-5f);
            Assert.AreEqual(0.6f, m.Target(0.5f, false), 1e-5f);
            Assert.AreEqual(1f, m.Target(1f, false), 1e-5f);
            Assert.AreEqual(1f, m.Target(5f, false), 1e-5f, "tension is clamped");
            Assert.AreEqual(0.2f, m.Target(-1f, false), 1e-5f);
        }

        [Test]
        public void Blocked_IsSilent()
        {
            var m = new MusicDroneModel();
            Assert.AreEqual(0f, m.Target(1f, true));
        }

        [Test]
        public void Level_RisesSlowlyWithTension_AndFallsFasterWhenItDrops()
        {
            var m = new MusicDroneModel { MinLevel = 0.2f, MaxLevel = 1f, RisePerSecond = 0.4f, FallPerSecond = 1.5f };
            for (int i = 0; i < 20; i++) m.Tick(0.1f, 1f, false);   // 2 s up: 0.8 of rise, capped at 1
            Assert.AreEqual(0.8f, m.Level, 1e-4f);
            for (int i = 0; i < 10; i++) m.Tick(0.1f, 1f, false);
            Assert.AreEqual(1f, m.Level, 1e-4f);
            m.Tick(0.2f, 0f, false);   // fall 0.3 toward 0.2
            Assert.AreEqual(0.7f, m.Level, 1e-4f);
            for (int i = 0; i < 20; i++) m.Tick(0.1f, 0f, false);
            Assert.AreEqual(0.2f, m.Level, 1e-4f);
        }

        [Test]
        public void Pausing_FadesToSilence_AndResumingComesBack()
        {
            var m = new MusicDroneModel();
            for (int i = 0; i < 100; i++) m.Tick(0.1f, 0.5f, false);
            Assert.Greater(m.Level, 0.5f);
            for (int i = 0; i < 100; i++) m.Tick(0.1f, 0.5f, true);
            Assert.AreEqual(0f, m.Level, 1e-5f);
            for (int i = 0; i < 100; i++) m.Tick(0.1f, 0.5f, false);
            Assert.Greater(m.Level, 0.5f);
        }
    }
}
