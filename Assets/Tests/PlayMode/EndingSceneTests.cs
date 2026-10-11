#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Core;
using NocturneAnnex.Entity;
using NocturneAnnex.Flow;
using NocturneAnnex.Interaction;
using NocturneAnnex.Level;
using NocturneAnnex.Player;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>F-14l on Level_R10: Escaped shows the card with the run's numbers, the eyes fade in, the last door is loud, and Menu leaves nothing blocked.</summary>
    [Timeout(120000)]
    public class EndingSceneTests
    {
        string _dir, _menu;
        SaveGame _save;
        EndingController _ending;
        Transform _player;

        IEnumerator Load()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            RoomFlow.ResetForTests();
            LevelLaunch.RequestNewGame();
            _dir = Path.Combine(Path.GetTempPath(), "na_end_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RoomFlow.ScenePath(RoomCatalog.Count), new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _save = Object.FindFirstObjectByType<SaveGame>();
            _save.FilePathOverride = Path.Combine(_dir, "save.json");
            _ending = Object.FindFirstObjectByType<EndingController>();
            _player = Object.FindFirstObjectByType<PlayerMotor>().transform;
            _ending.SceneLoader = n => _menu = n;
            _ending.SilenceSeconds = 0.05f; _ending.FadeSeconds = 0.05f; _ending.EyesDelaySeconds = 0.1f; _ending.EyesFadeSeconds = 0.2f;
        }

        [TearDown]
        public void TearDown()
        {
            RoomFlow.ResetForTests();
            ModalGate.Reset();
            Time.timeScale = 1f;
            if (_dir != null && Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [UnityTest]
        public IEnumerator RoomTen_HasTheEndingParts()
        {
            yield return Load();
            Assert.IsNotNull(_ending);
            Assert.IsNotNull(Object.FindFirstObjectByType<RunStatsRecorder>());
            var chase = Object.FindFirstObjectByType<FinalChase>();
            Assert.IsNotNull(chase, "room 10 has the final chase");
            Assert.IsNotNull(chase.ExitDoor);
            Assert.IsNotNull(chase.Hub);
        }

        [UnityTest]
        public IEnumerator Escaped_ShowsTheCardWithTheRunNumbers_ThenEyes_ThenMenuIsClean()
        {
            yield return Load();
            _save.Rooms.Enter(10);
            _save.Stats.AddSeconds(754f);
            _save.Stats.AddSecret("misfile");
            _save.Stats.AddSecret("toybox:3");
            _save.Lives.LoseLife();

            RoomFlow.Go(_save, RoomCatalog.Count + 1, false);   // leaving the last room
            Assert.IsTrue(_ending.Showing);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!_ending.CardVisible && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(_ending.CardVisible, "the card appears");
            Assert.IsTrue(ModalGate.IsOpen, "the card holds the gate while shown");

            var lines = _ending.StatLines;
            Assert.AreEqual(EndingText.Lines(new EndingStats(_save.Stats.Seconds, 1, 1, RunLives.MaxLives, 10, 10, 2)).Length, lines.Length);
            StringAssert.Contains("Time: 12:", lines[0]);
            Assert.AreEqual("Deaths: 1", lines[1]);
            Assert.AreEqual("Lives left: 1 of 2", lines[2]);
            Assert.AreEqual("Rooms: 10 of 10", lines[3]);
            Assert.AreEqual("Secrets found: 2", lines[4]);

            deadline = Time.realtimeSinceStartup + 10f;
            while (_ending.EyesAlpha < 0.85f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.GreaterOrEqual(_ending.EyesAlpha, 0.85f, "the closing eyes fade in");

            _ending.MenuButton.onClick.Invoke();
            Assert.AreEqual("MainMenu", _menu);
            Assert.IsFalse(ModalGate.IsOpen, "no input stays blocked");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, _save.Stats.SecretCount, "a finished run is cleared");
            Assert.AreEqual(RunLives.MaxLives, _save.Lives.Left);
        }

        [UnityTest]
        public IEnumerator OpeningTheExitDoor_IsLoud()
        {
            yield return Load();
            var chase = Object.FindFirstObjectByType<FinalChase>();
            int heard = 0;
            var hub = chase.Hub;
            hub.Bus.Emitted += e => heard++;
            chase.ExitDoor.Unlock();
            chase.ExitDoor.Interact(_player.GetComponentInChildren<Interactor>());
            yield return null;
            Assert.IsTrue(chase.Triggered);
            Assert.GreaterOrEqual(heard, 1);
        }

        [UnityTest]
        public IEnumerator Recorder_CountsTimeAndSecrets()
        {
            yield return Load();
            float before = _save.Stats.Seconds;
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(_save.Stats.Seconds, before);
            var sw = Object.FindObjectsByType<UnlockSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (sw != null)
            {
                sw.Interact(_player.GetComponentInChildren<Interactor>());
                if (sw.Used) Assert.AreEqual(1, _save.Stats.SecretCount);
            }
        }
    }
}
#endif
