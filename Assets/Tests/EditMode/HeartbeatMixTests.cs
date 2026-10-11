using NUnit.Framework;
using NocturneAnnex.Audio;

namespace NocturneAnnex.Tests.EditMode
{
    public class HeartbeatMixTests
    {
        [Test]
        public void Table_RisesWithDanger_AndAmbienceFalls()
        {
            for (int l = 1; l <= 4; l++)
            {
                Assert.Greater(HeartbeatMix.TargetHeartbeat(l), HeartbeatMix.TargetHeartbeat(l - 1));
                Assert.Greater(HeartbeatMix.TargetBpm(l), HeartbeatMix.TargetBpm(l - 1));
                Assert.Less(HeartbeatMix.TargetAmbience(l, false), HeartbeatMix.TargetAmbience(l - 1, false));
            }
            Assert.AreEqual(0f, HeartbeatMix.TargetHeartbeat(0), "silent when safe");
        }

        [Test]
        public void OutOfRangeLevel_IsClamped()
        {
            Assert.AreEqual(HeartbeatMix.TargetHeartbeat(4), HeartbeatMix.TargetHeartbeat(9));
            Assert.AreEqual(HeartbeatMix.TargetHeartbeat(0), HeartbeatMix.TargetHeartbeat(-3));
        }

        [Test]
        public void Hiding_LowersAmbienceToTheTableValue_AndIsNeverLouderThanNormal()
        {
            Assert.AreEqual(HeartbeatMix.HideAmbience, HeartbeatMix.TargetAmbience(0, true), 0.0001f);
            for (int l = 0; l <= 4; l++)
                Assert.LessOrEqual(HeartbeatMix.TargetAmbience(l, true), HeartbeatMix.TargetAmbience(l, false) + 0.0001f);
        }

        [Test]
        public void WidestChange_SettlesWithinOneSecond()
        {
            var m = new HeartbeatMix();
            m.Snap(0, false);
            for (int i = 0; i < 10; i++) m.Step(0.1f, 4, false);
            Assert.AreEqual(HeartbeatMix.TargetHeartbeat(4), m.Heartbeat, 0.001f);
            Assert.AreEqual(HeartbeatMix.TargetBpm(4), m.Bpm, 0.01f);
            Assert.AreEqual(HeartbeatMix.TargetAmbience(4, false), m.Ambience, 0.001f);
        }

        [Test]
        public void Smoothing_DoesNotJump()
        {
            var m = new HeartbeatMix();
            m.Snap(0, false);
            m.Step(0.1f, 4, false);
            Assert.Less(m.Heartbeat, 0.2f);
            Assert.Greater(m.Heartbeat, 0f);
        }

        [Test]
        public void Snap_JumpsToTargets()
        {
            var m = new HeartbeatMix();
            m.Snap(3, true);
            Assert.AreEqual(HeartbeatMix.TargetHeartbeat(3), m.Heartbeat);
            Assert.AreEqual(HeartbeatMix.TargetAmbience(3, true), m.Ambience);
        }
    }
}
