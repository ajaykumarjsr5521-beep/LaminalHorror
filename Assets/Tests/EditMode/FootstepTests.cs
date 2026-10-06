using NUnit.Framework;
using NocturneAnnex.Audio;

namespace NocturneAnnex.Tests.EditMode
{
    public class FootstepTests
    {
        [Test]
        public void Timer_StepsOncePerStride_AndCarriesTheRemainder()
        {
            var t = new FootstepTimer();
            Assert.IsFalse(t.Advance(1.0f, true, 1.7f));
            Assert.IsTrue(t.Advance(1.0f, true, 1.7f));    // 2.0 travelled, 0.3 left over
            Assert.IsFalse(t.Advance(1.0f, true, 1.7f));   // 1.3
            Assert.IsTrue(t.Advance(0.5f, true, 1.7f));    // 1.8
        }

        [Test]
        public void Timer_IsSilentInTheAir_AndForgetsDistance()
        {
            var t = new FootstepTimer();
            t.Advance(1.5f, true, 1.7f);
            Assert.IsFalse(t.Advance(5f, false, 1.7f));
            Assert.IsFalse(t.Advance(0.5f, true, 1.7f), "distance from before the jump must not count");
        }

        [Test]
        public void Timer_HugeDistance_GivesOneStepNotMany()
        {
            var t = new FootstepTimer();
            Assert.IsTrue(t.Advance(50f, true, 1.7f));
            Assert.IsFalse(t.Advance(0.1f, true, 1.7f));
        }

        [Test]
        public void Timer_BadStride_NeverSteps()
        {
            var t = new FootstepTimer();
            Assert.IsFalse(t.Advance(10f, true, 0f));
        }

        [Test]
        public void Surfaces_MapToTheirCue_AndUnknownFallsBackToConcrete()
        {
            Assert.AreEqual("footstep.tile", FloorSurface.CueFor("tile"));
            Assert.AreEqual("footstep.carpet", FloorSurface.CueFor("carpet"));
            Assert.AreEqual("footstep.concrete", FloorSurface.CueFor("concrete"));
            Assert.AreEqual("footstep.concrete", FloorSurface.CueFor("lava"));
            Assert.AreEqual("footstep.concrete", FloorSurface.CueFor(null));
        }

        [Test]
        public void EverySurfaceHasACueInTheDefaultCatalog()
        {
            var cat = CueCatalog.CreateDefault();
            foreach (var s in CueCatalog.Surfaces) Assert.IsNotNull(cat.Find(FloorSurface.CueFor(s)), s);
        }
    }
}
