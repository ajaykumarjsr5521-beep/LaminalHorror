#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.Save;
using NocturneAnnex.Settings;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Drives the real generated MainMenu scene: button flows, confirmation, settings save/revert.</summary>
    public class MainMenuSceneTests
    {
        const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        string _dir;
        string _savePath;
        string _settingsPath;
        MenuBootstrap _boot;
        int _continues, _newGames;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _dir = Path.Combine(Path.GetTempPath(), "na_menu_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            _savePath = Path.Combine(_dir, "save.json");
            _settingsPath = Path.Combine(_dir, "settings.json");
            _continues = _newGames = 0;

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _boot = Object.FindFirstObjectByType<MenuBootstrap>();
            Assert.IsNotNull(_boot, "MainMenu scene must contain a MenuBootstrap");
            _boot.GetComponent<MenuFlow>().SceneLoader = _ => { };   // these tests check events, not scene changes
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Accessibility.Reset();   // the settings screen applies accessibility state globally
        }

        void Start(bool validSave = false, bool corruptSave = false)
        {
            if (validSave) new SaveStore(_savePath).Write(new SaveData { CheckpointId = "lamp_1" });
            if (corruptSave) File.WriteAllText(_savePath, "garbage");
            _boot.SaveStore = new SaveStore(_savePath);
            _boot.SettingsStore = new SettingsStore(_settingsPath, QualitySettings.names.Length);
            _boot.ContinueRequested += () => _continues++;
            _boot.NewGameRequested += () => _newGames++;
            _boot.Initialize();
        }

        [UnityTest]
        public IEnumerator NoSave_ContinueDisabled_NewGameStartsWithoutConfirmation()
        {
            Start();
            yield return null;
            Assert.IsFalse(_boot.MainMenu.ContinueButton.interactable);
            _boot.MainMenu.NewGameButton.onClick.Invoke();
            Assert.AreEqual(1, _newGames);
            Assert.IsFalse(_boot.MainMenu.Confirm.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator ValidSave_ContinueRaisesEvent()
        {
            Start(validSave: true);
            yield return null;
            Assert.IsTrue(_boot.MainMenu.ContinueButton.interactable);
            _boot.MainMenu.ContinueButton.onClick.Invoke();
            Assert.AreEqual(1, _continues);
        }

        [UnityTest]
        public IEnumerator NewGameOverSave_AsksFirst_CancelKeepsSave()
        {
            Start(validSave: true);
            yield return null;
            _boot.MainMenu.NewGameButton.onClick.Invoke();
            Assert.IsTrue(_boot.MainMenu.Confirm.gameObject.activeSelf);
            Assert.AreEqual(0, _newGames);

            _boot.MainMenu.Confirm.No.onClick.Invoke();
            Assert.IsFalse(_boot.MainMenu.Confirm.gameObject.activeSelf);
            Assert.AreEqual(0, _newGames);
            Assert.IsTrue(File.Exists(_savePath), "cancelling must leave the save alone");
        }

        [UnityTest]
        public IEnumerator NewGameOverSave_ConfirmClearsSaveAndStarts()
        {
            Start(validSave: true);
            yield return null;
            _boot.MainMenu.NewGameButton.onClick.Invoke();
            _boot.MainMenu.Confirm.Yes.onClick.Invoke();
            Assert.AreEqual(1, _newGames);
            Assert.IsFalse(File.Exists(_savePath));
        }

        [UnityTest]
        public IEnumerator CorruptSave_ShowsWarning_DisablesContinue_AndIsPreservedOnNewGame()
        {
            Start(corruptSave: true);
            yield return null;
            Assert.IsTrue(_boot.MainMenu.WarningText.gameObject.activeSelf);
            Assert.IsNotEmpty(_boot.MainMenu.WarningText.text);
            Assert.IsFalse(_boot.MainMenu.ContinueButton.interactable);

            _boot.MainMenu.NewGameButton.onClick.Invoke();
            _boot.MainMenu.Confirm.Yes.onClick.Invoke();
            Assert.AreEqual(1, Directory.GetFiles(_dir, "save.json.corrupt-*").Length);
        }

        [UnityTest]
        public IEnumerator Settings_SaveIsEnabledOnlyAfterAChange_AndPersists()
        {
            Start();
            _boot.ShowSettings();
            yield return null;
            var view = _boot.Settings;
            Assert.IsFalse(view.SaveButton.interactable);

            view.LookSensitivity.value = 2f;
            Assert.IsTrue(view.SaveButton.interactable);
            view.SaveButton.onClick.Invoke();

            var stored = new SettingsStore(_settingsPath, QualitySettings.names.Length).Load();
            Assert.AreEqual(SettingsLoadStatus.Ok, stored.Status);
            Assert.AreEqual(2f, stored.Data.LookSensitivity, 1e-3f);
            Assert.IsFalse(view.SaveButton.interactable, "nothing unsaved after saving");
        }

        [UnityTest]
        public IEnumerator Settings_BackDiscardsUnsavedChanges()
        {
            Start();
            _boot.ShowSettings();
            yield return null;
            _boot.Settings.LookSensitivity.value = 2.5f;
            _boot.Settings.BackButton.onClick.Invoke();
            Assert.IsTrue(_boot.MainPanel.activeSelf);
            Assert.IsFalse(_boot.SettingsPanel.activeSelf);

            _boot.ShowSettings();
            yield return null;
            Assert.AreEqual(1f, _boot.Settings.LookSensitivity.value, 1e-3f, "unsaved edit must not come back");
            Assert.IsFalse(File.Exists(_settingsPath), "nothing was saved");
        }

        [UnityTest]
        public IEnumerator Settings_ScrollbarHandleIsProportional_WhenContentOverflows()
        {
            Start();
            _boot.ShowSettings();
            for (int i = 0; i < 4; i++) yield return null;   // let layout and ScrollRect settle
            var scroll = _boot.SettingsPanel.GetComponentInChildren<ScrollRect>();
            Assert.Less(scroll.verticalScrollbar.size, 0.99f, "content taller than the viewport must give a partial-size handle");
            Assert.Greater(scroll.verticalScrollbar.size, 0.1f);
        }

        [UnityTest]
        public IEnumerator Settings_AccessibilityRows_PreviewLive_AndPersist()
        {
            Start();
            _boot.ShowSettings();
            yield return null;
            var view = _boot.Settings;

            view.TextSize.value = Accessibility.LargeText;
            view.ReduceFlicker.isOn = true;
            view.ReduceMotion.isOn = true;
            view.Captions.isOn = false;
            Assert.AreEqual(Accessibility.LargeText, Accessibility.TextSize, "text size previews immediately");
            Assert.IsTrue(Accessibility.ReduceFlicker);
            Assert.IsTrue(Accessibility.ReduceMotion);
            Assert.IsFalse(Accessibility.CaptionsEnabled);

            view.SaveButton.onClick.Invoke();
            var stored = new SettingsStore(_settingsPath, QualitySettings.names.Length).Load().Data;
            Assert.AreEqual(2, stored.TextSize);
            Assert.IsTrue(stored.ReduceFlicker);
            Assert.IsTrue(stored.ReduceMotion);
            Assert.IsFalse(stored.CaptionsEnabled);
        }

        [UnityTest]
        public IEnumerator Settings_BackRevertsAccessibilityPreview()
        {
            Start();
            _boot.ShowSettings();
            yield return null;
            _boot.Settings.TextSize.value = Accessibility.LargeText;
            _boot.Settings.ReduceFlicker.isOn = true;
            _boot.Settings.BackButton.onClick.Invoke();
            Assert.AreEqual(Accessibility.MediumText, Accessibility.TextSize, "unsaved preview must be undone");
            Assert.IsFalse(Accessibility.ReduceFlicker);
        }

        [UnityTest]
        public IEnumerator Credits_BackReturnsToMainPanel()
        {
            Start();
            yield return null;
            _boot.MainMenu.CreditsButton.onClick.Invoke();
            Assert.IsTrue(_boot.CreditsPanel.activeSelf);
            _boot.CreditsBackButton.onClick.Invoke();
            Assert.IsTrue(_boot.MainPanel.activeSelf);
            Assert.IsFalse(_boot.CreditsPanel.activeSelf);
        }
    }
}
#endif
