#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Core;
using NocturneAnnex.Entity;
using NocturneAnnex.Flow;
using NocturneAnnex.Level;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Two lives, checkpoint restore and the death sequence on the real Level_B1.</summary>
    [Timeout(180000)]
    public class DeathFlowTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        string _dir;
        StalkerAgent _stalker;
        DeathController _death;
        LevelBootstrap _level;
        SaveGame _save;
        Transform _player;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            LevelLaunch.RequestNewGame();
            _dir = Path.Combine(Path.GetTempPath(), "na_death_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _level = Object.FindFirstObjectByType<LevelBootstrap>();
            _save = _level.SaveGame;
            _save.FilePathOverride = Path.Combine(_dir, "save.json");
            _stalker = Object.FindFirstObjectByType<StalkerAgent>();
            _death = Object.FindFirstObjectByType<DeathController>();
            _death.RunOverTextSeconds = 0.2f;
            _player = _level.Player;
        }

        [TearDown]
        public void TearDown()
        {
            ModalGate.Reset(); Time.timeScale = 1f;
            try { Directory.Delete(_dir, true); } catch { }
        }

        void Teleport(Vector3 pos)
        {
            var cc = _player.GetComponent<CharacterController>();
            cc.enabled = false; _player.position = pos; cc.enabled = true;
            Physics.SyncTransforms();
        }

        IEnumerator GetCaught(int expectedDeaths, float timeout = 30f)
        {
            Teleport(new Vector3(0f, 0.1f, 4f));
            _stalker.GetComponent<NavMeshAgent>().Warp(new Vector3(0f, 0.1f, 7f));
            float start = Time.time;
            while (Time.time - start < timeout && _death.DeathsHandled < expectedDeaths) yield return null;
            Assert.AreEqual(expectedDeaths, _death.DeathsHandled, "the entity must catch a player standing in the open");
            while (_death.Running) yield return null;
        }

        [UnityTest]
        public IEnumerator FirstCatchSpendsALifeAndRestoresTheCheckpoint()
        {
            Assert.AreEqual(2, _save.Lives.Left);
            _level.Progress.TryReach("hall");
            _save.SaveCheckpoint("hall");
            yield return GetCaught(1);
            Assert.AreEqual(LifeOutcome.Respawn, _death.LastOutcome);
            Assert.AreEqual(1, _save.Lives.Left);
            Assert.AreEqual("hall", _level.Progress.CurrentId, "progress is kept");
            var hall = _level.Checkpoints.First(c => c.Id == "hall");
            Assert.Less(Vector3.Distance(_player.position, hall.Spawn.position), 1f, "back at the last checkpoint");
            Assert.AreEqual(EntityState.Patrol, _stalker.Brain.State);
            Assert.Greater(Vector3.Distance(_stalker.transform.position, _player.position), 8f, "the entity is sent home");
            Assert.AreEqual(0f, _death.Black, 0.01f, "the screen clears");
            Assert.AreEqual("caught", _save.Lives.Log[0].Cause);
        }

        [UnityTest]
        public IEnumerator LivesAreSavedWithTheCheckpoint()
        {
            yield return GetCaught(1);
            var loaded = _save.Store.Load();
            Assert.IsTrue(loaded.Ok);
            Assert.AreEqual(1, loaded.Data.LivesLeft);
            Assert.AreEqual(1, loaded.Data.DeathCount);
            Assert.AreEqual(1, loaded.Data.DeathLog.Length);
        }

        [UnityTest]
        public IEnumerator SecondCatchEndsTheRunAndStartsOver()
        {
            _level.Progress.TryReach("hall");
            yield return GetCaught(1);
            yield return GetCaught(2);
            Assert.AreEqual(LifeOutcome.RunOver, _death.LastOutcome);
            Assert.AreEqual(2, _save.Lives.Left, "a new run has full lives");
            Assert.AreEqual(0, _save.Lives.Deaths);
            Assert.AreEqual(_level.Checkpoints[0].Id, _level.Progress.CurrentId, "starts again from the entrance");
            Assert.IsFalse(_death.Running);
        }

        [UnityTest]
        public IEnumerator DeathSequenceIsShortAndTheScreenGoesBlack()
        {
            Teleport(new Vector3(0f, 0.1f, 4f));
            _stalker.GetComponent<NavMeshAgent>().Warp(new Vector3(0f, 0.1f, 7f));
            float start = Time.time;
            while (Time.time - start < 30f && !_death.Running) yield return null;
            Assert.IsTrue(_death.Running);
            float began = Time.realtimeSinceStartup;
            float peak = 0f;
            while (_death.Running) { peak = Mathf.Max(peak, _death.Black); yield return null; }
            Assert.AreEqual(1f, peak, 0.01f);
            Assert.Less(Time.realtimeSinceStartup - began, 6.5f, "sequence, respawn and fade stay brief");
        }

        [UnityTest]
        public IEnumerator DeathWhileHidingIsRecordedAsFoundHiding()
        {
            var spot = Object.FindObjectsByType<HideSpot>(FindObjectsSortMode.None).First(s => s.name == "Cupboard_Hall");
            spot.Safety = 0f;
            Teleport(spot.ExitPoint.position);
            _stalker.GetComponent<NavMeshAgent>().Warp(new Vector3(-12f, 0.1f, 20f));
            yield return new WaitForSeconds(0.3f);
            spot.Interact(_player.GetComponentInChildren<NocturneAnnex.Interaction.Interactor>());
            Assert.IsTrue(spot.Occupied);
            _stalker.InspectIntervalSeconds = 1f;
            _stalker.InspectRepeatSeconds = 2f;
            _stalker.SetSeed(11);
            _stalker.GetComponent<NavMeshAgent>().Warp(new Vector3(2f, 0.1f, 14f));
            FindObjectEmit(new Vector3(3f, 0f, 14f));
            float start = Time.time;
            while (Time.time - start < 90f && _death.DeathsHandled < 1) yield return null;
            Assert.AreEqual(1, _death.DeathsHandled);
            Assert.AreEqual("found_hiding", _save.Lives.Log[0].Cause);
            Assert.AreEqual("Cupboard_Hall", _save.Lives.Log[0].HidingSpot);
        }

        static void FindObjectEmit(Vector3 at) =>
            Object.FindFirstObjectByType<NoiseHub>().Emit(at, NoiseKind.DoorSlam);
    }
}
#endif
