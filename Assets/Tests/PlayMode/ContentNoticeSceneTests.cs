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
    /// <summary>First-launch content notice, driven through the real MainMenu scene.</summary>
    public class ContentNoticeSceneTests
    {
        const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        string _dir;
        string _settingsPath;
        MenuBootstrap _boot;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _dir = Path.Combine(Path.GetTempPath(), "na_notice_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            _settingsPath = Path.Combine(_dir, "settings.json");

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _boot = Object.FindFirstObjectByType<MenuBootstrap>();
            Assert.IsNotNull(_boot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Accessibility.Reset();
        }

        void Start()
        {
            _boot.SaveStore = new SaveStore(Path.Combine(_dir, "save.json"));
            _boot.SettingsStore = new SettingsStore(_settingsPath, QualitySettings.names.Length);
            _boot.Initialize();
        }

        SettingsStore Store => new SettingsStore(_settingsPath, QualitySettings.names.Length);

        [UnityTest]
        public IEnumerator FirstLaunch_ShowsTheNoticeBeforeTheMenu()
        {
            Start();
            yield return null;
            Assert.IsTrue(_boot.NoticePanel.activeSelf);
            Assert.IsFalse(_boot.MainPanel.activeSelf);
        }

        [UnityTest]
        public IEnumerator AcknowledgingIt_ShowsTheMenu_AndRemembersAcrossLaunches()
        {
            Start();
            _boot.NoticeOkButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(_boot.MainPanel.activeSelf);
            Assert.IsFalse(_boot.NoticePanel.activeSelf);
            Assert.IsTrue(Store.Load().Data.ContentNoticeAccepted);

            _boot.Initialize();   // next launch
            yield return null;
            Assert.IsTrue(_boot.MainPanel.activeSelf, "an accepted notice is not shown again");
            Assert.IsFalse(_boot.NoticePanel.activeSelf);
        }

        [UnityTest]
        public IEnumerator Acknowledging_KeepsOtherSavedSettings()
        {
            Store.Write(new SettingsData { LookSensitivity = 2f, TextSize = 2 });
            Start();
            _boot.NoticeOkButton.onClick.Invoke();
            yield return null;
            var data = Store.Load().Data;
            Assert.IsTrue(data.ContentNoticeAccepted);
            Assert.AreEqual(2f, data.LookSensitivity, 1e-3f);
            Assert.AreEqual(2, data.TextSize);
        }

        [UnityTest]
        public IEnumerator CanBeReadAgainFromCredits_WithoutChangingAnything()
        {
            Store.Write(new SettingsData { ContentNoticeAccepted = true });
            Start();
            yield return null;
            Assert.IsTrue(_boot.MainPanel.activeSelf);

            _boot.MainMenu.CreditsButton.onClick.Invoke();
            _boot.CreditsNoticeButton.onClick.Invoke();
            Assert.IsTrue(_boot.NoticePanel.activeSelf);
            _boot.NoticeOkButton.onClick.Invoke();
            Assert.IsTrue(_boot.CreditsPanel.activeSelf, "re-reading returns to the credits screen");
            Assert.IsFalse(_boot.NoticePanel.activeSelf);
        }

        [UnityTest]
        public IEnumerator CorruptSettings_ShowNoticeAndKeepTheBadFileAfterAccepting()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_settingsPath, "garbage");
            Start();
            yield return null;
            Assert.IsTrue(_boot.NoticePanel.activeSelf);

            _boot.NoticeOkButton.onClick.Invoke();
            Assert.AreEqual(1, Directory.GetFiles(_dir, "settings.json.corrupt-*").Length, "the unreadable file must not be lost");
            Assert.IsTrue(Store.Load().Data.ContentNoticeAccepted);
        }

        [UnityTest]
        public IEnumerator WhenSavingFails_TheMenuStillOpens_AndTheProblemIsShown()
        {
            Start();
            Directory.CreateDirectory(AtomicFile.TempPathFor(_settingsPath));   // forces the write to fail
            _boot.NoticeOkButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(_boot.MainPanel.activeSelf, "a save problem must not trap the player on the notice");
            Assert.IsTrue(_boot.MainMenu.WarningText.gameObject.activeSelf);
            Assert.IsNotEmpty(_boot.MainMenu.WarningText.text);
        }
    }
}
#endif
