using NUnit.Framework;
using NocturneAnnex.Settings;

namespace NocturneAnnex.Tests.EditMode
{
    public class HapticsSettingsTests
    {
        [Test]
        public void Defaults_AreOn_At70Percent()
        {
            var s = SettingsData.CreateDefault();
            Assert.IsTrue(s.HapticsEnabled);
            Assert.AreEqual(0.7f, s.HapticsStrength, 0.0001f);
        }

        [Test]
        public void CopyFromAndValueEquals_IncludeHaptics()
        {
            var a = new SettingsData { HapticsEnabled = false, HapticsStrength = 0.2f };
            var b = SettingsData.CreateDefault();
            Assert.IsFalse(a.ValueEquals(b));
            b.CopyFrom(a);
            Assert.IsFalse(b.HapticsEnabled);
            Assert.AreEqual(0.2f, b.HapticsStrength, 0.0001f);
            Assert.IsTrue(a.ValueEquals(b));
            b.HapticsStrength = 0.3f;
            Assert.IsFalse(a.ValueEquals(b));
        }

        [Test]
        public void Clamp_KeepsStrengthInRange_AndFixesNaN()
        {
            var s = new SettingsData { HapticsStrength = 4f };
            s.Clamp(3);
            Assert.AreEqual(1f, s.HapticsStrength);
            s.HapticsStrength = -2f;
            s.Clamp(3);
            Assert.AreEqual(0f, s.HapticsStrength);
            s.HapticsStrength = float.NaN;
            s.Clamp(3);
            Assert.AreEqual(0.7f, s.HapticsStrength, 0.0001f);
        }

        [Test]
        public void OlderFileWithoutHaptics_LoadsTheDefaults()
        {
            var s = UnityEngine.JsonUtility.FromJson<SettingsData>("{\"Version\":1,\"LookSensitivity\":1.5}");
            Assert.IsTrue(s.HapticsEnabled);
            Assert.AreEqual(0.7f, s.HapticsStrength, 0.0001f);
            Assert.AreEqual(1.5f, s.LookSensitivity, 0.0001f);
        }

        [Test]
        public void Apply_PushesHapticsIntoTheLiveSetting()
        {
            SettingsApplier.Apply(new SettingsData { HapticsEnabled = false, HapticsStrength = 0.4f }, null);
            Assert.IsFalse(NocturneAnnex.Core.HapticSettings.Enabled);
            Assert.AreEqual(0.4f, NocturneAnnex.Core.HapticSettings.Strength, 0.0001f);
            SettingsApplier.Apply(SettingsData.CreateDefault(), null);
            Assert.IsTrue(NocturneAnnex.Core.HapticSettings.Enabled);
        }
    }
}
