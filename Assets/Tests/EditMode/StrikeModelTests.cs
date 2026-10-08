using NUnit.Framework;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class StrikeModelTests
    {
        static bool Run(StrikeModel m, float seconds, float distance)
        {
            bool hit = false;
            for (float t = 0f; t < seconds; t += 0.05f) hit |= m.Step(0.05f, distance);
            return hit;
        }

        [Test]
        public void ContactStartsAWindupAndDoesNotHitAtOnce()
        {
            var m = new StrikeModel();
            Assert.IsTrue(m.CanStart(1.5f));
            m.Start();
            Assert.AreEqual(StrikePhase.Windup, m.Phase);
            Assert.IsFalse(Run(m, 0.4f, 1.0f), "no hit during the windup");
        }

        [Test]
        public void CannotStartFromFarAway() => Assert.IsFalse(new StrikeModel().CanStart(StrikeModel.StrikeDistance + 0.5f));

        [Test]
        public void HitLandsAfterWindupAndStrikeWhenPlayerStaysInReach()
        {
            var m = new StrikeModel();
            m.Start();
            Assert.IsTrue(Run(m, StrikeModel.WindupSeconds + StrikeModel.StrikeSeconds + 0.1f, 1.2f));
            Assert.IsTrue(m.LastWasHit);
            Assert.AreEqual(StrikePhase.Idle, m.Phase);
        }

        [Test]
        public void MovingOutOfReachMakesAMissThenRecovery()
        {
            var m = new StrikeModel();
            m.Start();
            Assert.IsFalse(Run(m, StrikeModel.WindupSeconds + StrikeModel.StrikeSeconds + 0.1f, StrikeModel.HitReach + 0.5f));
            Assert.IsFalse(m.LastWasHit);
            Assert.AreEqual(StrikePhase.Recover, m.Phase);
            Run(m, StrikeModel.RecoverSeconds + 0.1f, 5f);
            Assert.AreEqual(StrikePhase.Idle, m.Phase);
        }

        [Test]
        public void RaiseRisesDuringWindupAndFallsDuringStrike()
        {
            var m = new StrikeModel();
            m.Start();
            Run(m, StrikeModel.WindupSeconds - 0.05f, 1f);
            Assert.Greater(m.Raise, 0.8f);
            Run(m, 0.2f, 1f);
            Assert.Less(m.Raise, 0.5f);
        }

        [Test]
        public void CancelReturnsToIdle()
        {
            var m = new StrikeModel();
            m.Start(); m.Cancel();
            Assert.AreEqual(StrikePhase.Idle, m.Phase);
            Assert.AreEqual(0f, m.Raise);
        }
    }
}
