using System;
using NUnit.Framework;
using NocturneAnnex.Audio;

namespace NocturneAnnex.Tests.EditMode
{
    public class AudioBusTests
    {
        [Test]
        public void Sliders_DefaultToFull_ClampAndAreIndependent()
        {
            var b = new AudioBus();
            Assert.AreEqual(1f, b.Gain(CueCatalog.Music));
            b.SetSlider(CueCatalog.Music, 1.7f);
            Assert.AreEqual(1f, b.Slider(CueCatalog.Music));
            b.SetSlider(CueCatalog.Music, -2f);
            Assert.AreEqual(0f, b.Slider(CueCatalog.Music));
            b.SetSlider(CueCatalog.Sfx, 0.4f);
            Assert.AreEqual(0.4f, b.Gain(CueCatalog.Sfx), 1e-5f);
            Assert.AreEqual(1f, b.Slider(CueCatalog.Ambience));
        }

        [Test]
        public void UnknownBus_Throws()
        {
            var b = new AudioBus();
            Assert.Throws<ArgumentException>(() => b.Slider("Loud"));
            Assert.Throws<ArgumentException>(() => b.SetSlider("Loud", 1f));
        }

        [Test]
        public void Duck_LowersMusicAndAmbienceButNotSfx_ThenRestores()
        {
            var b = new AudioBus();
            b.Duck(0.5f, 1f);
            for (int i = 0; i < 30; i++) b.Tick(0.05f);   // 1.5 s: duck is held 1 s, so it is releasing now
            b.Duck(0.5f, 1f);
            for (int i = 0; i < 10; i++) b.Tick(0.05f);
            Assert.AreEqual(0.5f, b.Gain(CueCatalog.Music), 1e-4f);
            Assert.AreEqual(0.5f, b.Gain(CueCatalog.Ambience), 1e-4f);
            Assert.AreEqual(1f, b.Gain(CueCatalog.Sfx), 1e-5f, "the cue itself is never ducked");
            for (int i = 0; i < 100; i++) b.Tick(0.05f);   // 5 s later
            Assert.AreEqual(1f, b.Gain(CueCatalog.Music), 1e-4f);
        }

        [Test]
        public void Duck_ScalesWithTheSlider_AndLongerDuckWins()
        {
            var b = new AudioBus();
            b.SetSlider(CueCatalog.Music, 0.8f);
            b.Duck(0.5f, 3f);
            b.Duck(0.5f, 1f);   // shorter, must not cut the 3 s hold
            for (int i = 0; i < 40; i++) b.Tick(0.05f);   // 2 s
            Assert.AreEqual(0.4f, b.Gain(CueCatalog.Music), 1e-4f);
        }
    }
}
