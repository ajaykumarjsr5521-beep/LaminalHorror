using NUnit.Framework;
using NocturneAnnex.Settings;

namespace NocturneAnnex.Tests.EditMode
{
    public class SettingsDataTests
    {
        [Test]
        public void Defaults_AreInRange_AndUnchangedByClamp()
        {
            var s = SettingsData.CreateDefault();
            s.Clamp(3);
            Assert.AreEqual(1f, s.LookSensitivity);
            Assert.AreEqual(1f, s.MasterVolume);
            Assert.AreEqual(1f, s.TouchControlsScale);
            Assert.AreEqual(-1, s.QualityLevel);
            Assert.IsTrue(s.CaptionsEnabled);
            Assert.IsFalse(s.StoryMode);
        }

        [Test]
        public void Clamp_PullsOutOfRangeValuesIntoRange()
        {
            var s = new SettingsData
            {
                LookSensitivity = 50f, MasterVolume = -1f, MusicVolume = 7f, SfxVolume = 2f, TouchControlsScale = 0.1f
            };
            s.Clamp(3);
            Assert.AreEqual(SettingsData.MaxSensitivity, s.LookSensitivity);
            Assert.AreEqual(0f, s.MasterVolume);
            Assert.AreEqual(1f, s.MusicVolume);
            Assert.AreEqual(1f, s.SfxVolume);
            Assert.AreEqual(0.8f, s.TouchControlsScale, 1e-4f);
        }

        [Test]
        public void Clamp_NaNAndInfinity_FallBackToDefaults()
        {
            var s = new SettingsData { LookSensitivity = float.NaN, MasterVolume = float.PositiveInfinity };
            s.Clamp(3);
            Assert.AreEqual(1f, s.LookSensitivity);
            Assert.AreEqual(1f, s.MasterVolume);
        }

        [TestCase(-5, 3, -1)]
        [TestCase(3, 3, -1)]
        [TestCase(2, 3, 2)]
        [TestCase(-1, 3, -1)]
        [TestCase(0, 0, -1)]
        public void Clamp_QualityLevel_MustExist(int level, int count, int expected)
        {
            var s = new SettingsData { QualityLevel = level };
            s.Clamp(count);
            Assert.AreEqual(expected, s.QualityLevel);
        }
    }
}
