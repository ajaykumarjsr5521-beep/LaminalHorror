#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Core;
using NocturneAnnex.Save;
using NocturneAnnex.Settings;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>F-14k: the Settings screen has a haptics toggle and strength that preview live, persist on Save and revert on Back.</summary>
    public class HapticsSettingsScreenTests
    {
        const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        string _dir;
        string _settingsPath;
        MenuBootstrap _boot;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _dir = Path.Combine(Path.GetTempPath(), "na_hap_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            _settingsPath = Path.Combine(_dir, "settings.json");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _boot = Object.FindFirstObjectByType<MenuBootstrap>();
            _boot.GetComponent<MenuFlow>().SceneLoader = _ => { };
            _boot.SaveStore = new SaveStore(Path.Combine(_dir, "save.json"));
            _boot.SettingsStore = new SettingsStore(_settingsPath, QualitySettings.names.Length);
            _boot.Initialize();
            _boot.ShowSettings();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Accessibility.Reset();
            HapticSettings.Set(true, 0.7f);
        }

        [Test]
        public void TheScreenShowsTheControls_WithTheDefaults()
        {
            Assert.IsNotNull(_boot.Settings.Haptics);
            Assert.IsNotNull(_boot.Settings.HapticsStrength);
            Assert.IsTrue(_boot.Settings.Haptics.isOn);
            Assert.AreEqual(0.7f, _boot.Settings.HapticsStrength.value, 0.0001f);
        }

        [Test]
        public void Changes_PreviewLive_AndPersistOnSave()
        {
            _boot.Settings.Haptics.isOn = false;
            _boot.Settings.HapticsStrength.value = 0.3f;
            Assert.IsFalse(HapticSettings.Enabled, "previews at once");
            Assert.AreEqual(0.3f, HapticSettings.Strength, 0.0001f);

            _boot.Settings.SaveButton.onClick.Invoke();
            var stored = new SettingsStore(_settingsPath, QualitySettings.names.Length).Load().Data;
            Assert.IsFalse(stored.HapticsEnabled);
            Assert.AreEqual(0.3f, stored.HapticsStrength, 0.0001f);
        }

        [Test]
        public void Back_RevertsAnUnsavedPreview()
        {
            _boot.Settings.Haptics.isOn = false;
            _boot.Settings.BackButton.onClick.Invoke();
            Assert.IsTrue(HapticSettings.Enabled);
        }
    }
}
#endif
