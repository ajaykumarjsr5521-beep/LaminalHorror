#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Flow;
using NocturneAnnex.Save;
using NocturneAnnex.Settings;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Boot to menu to level hand-offs, using the real generated scenes.</summary>
    public class SceneFlowTests
    {
        string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "na_flow_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            LevelLaunch.RequestNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            LevelLaunch.RequestNewGame();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [UnityTest]
        public IEnumerator BootScene_OpensTheMainMenu()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Boot.unity", new LoadSceneParameters(LoadSceneMode.Single));
            // The loader's Start already ran with the real SceneManager; check what it asked for with a fresh instance instead.
            var go = new GameObject("TestBoot");
            go.SetActive(false);
            var loader = go.AddComponent<BootLoader>();
            string requested = null;
            loader.SceneLoader = s => requested = s;
            go.SetActive(true);
            yield return null;
            Assert.AreEqual("MainMenu", requested);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator BootScene_ContainsABootLoader()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Boot.unity", new LoadSceneParameters(LoadSceneMode.Single));
            Assert.IsNotNull(Object.FindFirstObjectByType<BootLoader>(FindObjectsInactive.Include), "Boot must hold a BootLoader or the game starts on a blank screen");
        }

        [UnityTest]
        public IEnumerator MenuContinueAndNewGame_GoToTheLevel_WithTheRightLaunchMode()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = Object.FindFirstObjectByType<MenuBootstrap>();
            var flow = boot.GetComponent<MenuFlow>();
            Assert.IsNotNull(flow, "MainMenu scene must contain a MenuFlow");
            string loaded = null;
            flow.SceneLoader = s => loaded = s;

            string savePath = Path.Combine(_dir, "save.json");
            new SaveStore(savePath).Write(new SaveData { CheckpointId = "hall" });
            boot.SaveStore = new SaveStore(savePath);
            boot.SettingsStore = new SettingsStore(Path.Combine(_dir, "settings.json"), QualitySettings.names.Length);
            boot.Initialize();
            yield return null;

            boot.MainMenu.ContinueButton.onClick.Invoke();
            Assert.AreEqual("Level_B1", loaded);
            Assert.IsTrue(LevelLaunch.ContinueSave);

            loaded = null;
            boot.MainMenu.NewGameButton.onClick.Invoke();   // with a save present this asks first
            boot.MainMenu.Confirm.Yes.onClick.Invoke();
            Assert.AreEqual("Level_B1", loaded);
            Assert.IsFalse(LevelLaunch.ContinueSave, "a new game must not resume the old save");
        }

        [Test]
        public void LevelLaunch_ContinueRequest_IsConsumedOnce()
        {
            LevelLaunch.RequestContinue();
            Assert.IsTrue(LevelLaunch.ConsumeContinue());
            Assert.IsFalse(LevelLaunch.ConsumeContinue(), "a later scene reload must not repeat a stale request");
        }
    }
}
#endif
