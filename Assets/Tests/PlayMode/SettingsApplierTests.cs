using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Settings;

namespace NocturneAnnex.Tests.PlayMode
{
    public class SettingsApplierTests
    {
        GameObject _go;
        InputRouter _router;
        float _oldVolume;
        float _oldScale;
        int _oldQuality;

        [SetUp]
        public void SetUp()
        {
            _oldVolume = AudioListener.volume;
            _oldScale = ControlsLayout.Scale;
            _oldQuality = QualitySettings.GetQualityLevel();
            _go = new GameObject("Router");
            _router = _go.AddComponent<InputRouter>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
            AudioListener.volume = _oldVolume;
            ControlsLayout.Scale = _oldScale;
            QualitySettings.SetQualityLevel(_oldQuality, true);
            Accessibility.Reset();
        }

        [Test]
        public void Apply_SetsSensitivityInvertScaleAndVolume()
        {
            var s = new SettingsData { LookSensitivity = 2f, InvertY = true, TouchControlsScale = 1.2f, MasterVolume = 0.4f };
            SettingsApplier.Apply(s, _router);
            Assert.AreEqual(2f, _router.LookSensitivity, 1e-4f);
            Assert.IsTrue(_router.InvertY);
            Assert.AreEqual(1.2f, ControlsLayout.Scale, 1e-4f);
            Assert.AreEqual(0.4f, AudioListener.volume, 1e-4f);
        }

        [Test]
        public void Apply_UpdatesAccessibilityState()
        {
            SettingsApplier.Apply(new SettingsData { CaptionsEnabled = false, TextSize = 2, ReduceFlicker = true, ReduceMotion = true }, _router);
            Assert.IsFalse(Accessibility.CaptionsEnabled);
            Assert.AreEqual(Accessibility.LargeText, Accessibility.TextSize);
            Assert.IsTrue(Accessibility.ReduceFlicker);
            Assert.IsTrue(Accessibility.ReduceMotion);
        }

        [Test]
        public void Apply_ClampsOutOfRangeValuesInsteadOfPassingThemOn()
        {
            SettingsApplier.Apply(new SettingsData { LookSensitivity = 99f, TouchControlsScale = 5f }, _router);
            Assert.AreEqual(SettingsData.MaxSensitivity, _router.LookSensitivity);
            Assert.AreEqual(InputMath.MaxControlsScale, ControlsLayout.Scale, 1e-4f);
        }

        [Test]
        public void Apply_WithoutRouter_StillAppliesGlobalSettings()
        {
            SettingsApplier.Apply(new SettingsData { MasterVolume = 0.25f }, null);
            Assert.AreEqual(0.25f, AudioListener.volume, 1e-4f);
        }

        [Test]
        public void Apply_QualityLevel_IsAppliedWhenSet_AndLeftAloneWhenAuto()
        {
            int before = QualitySettings.GetQualityLevel();
            SettingsApplier.Apply(new SettingsData { QualityLevel = -1 }, _router);
            Assert.AreEqual(before, QualitySettings.GetQualityLevel());

            int target = (before + 1) % QualitySettings.names.Length;
            SettingsApplier.Apply(new SettingsData { QualityLevel = target }, _router);
            Assert.AreEqual(target, QualitySettings.GetQualityLevel());
        }
    }
}
