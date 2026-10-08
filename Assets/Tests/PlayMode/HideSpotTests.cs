#if UNITY_EDITOR
using System.Collections;
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
using NocturneAnnex.Interaction;
using NocturneAnnex.Level;
using NocturneAnnex.Player;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Hiding on the real Level_B1: entering, leaving, noise, being unseen, and the entity opening spots.</summary>
    [Timeout(150000)]
    public class HideSpotTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        StalkerAgent _stalker;
        NoiseHub _hub;
        Transform _player;
        PlayerMotor _motor;
        Interactor _interactor;
        HideSpot _hall, _stacks;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _stalker = Object.FindFirstObjectByType<StalkerAgent>();
            _hub = Object.FindFirstObjectByType<NoiseHub>();
            _player = Object.FindFirstObjectByType<LevelBootstrap>().Player;
            _motor = _player.GetComponent<PlayerMotor>();
            _interactor = _player.GetComponentInChildren<Interactor>();
            var spots = Object.FindObjectsByType<HideSpot>(FindObjectsSortMode.None);
            _hall = spots.First(s => s.name == "Cupboard_Hall");
            _stacks = spots.First(s => s.name == "Cupboard_Stacks");
        }

        [TearDown]
        public void TearDown() { ModalGate.Reset(); Time.timeScale = 1f; }

        void Teleport(Transform t, Vector3 pos)
        {
            var cc = t.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            t.position = pos;
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }

        void WarpStalker(Vector3 pos) => _stalker.GetComponent<NavMeshAgent>().Warp(pos);

        [UnityTest]
        public IEnumerator ThreeSpotsExistWithDifferentSafety()
        {
            var spots = Object.FindObjectsByType<HideSpot>(FindObjectsSortMode.None);
            Assert.AreEqual(3, spots.Length);
            Assert.Greater(spots.Max(s => s.Safety), spots.Min(s => s.Safety));
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnteringMovesThePlayerInsideAndBlocksWalkingThenLeaves()
        {
            Teleport(_player, _hall.ExitPoint.position);
            WarpStalker(new Vector3(-12f, 0.1f, 20f));
            yield return new WaitForSeconds(0.3f);
            int noises = _hub.Bus.EmittedCount;
            _hall.Interact(_interactor);
            Assert.IsTrue(_hall.Occupied);
            Assert.IsFalse(_motor.enabled, "a hidden player cannot walk");
            Assert.Less(Vector3.Distance(_player.position, _hall.HidePoint.position), 0.2f);
            Assert.Greater(_hub.Bus.EmittedCount, noises, "getting in makes noise");
            yield return null;
            yield return null;
            _hall.Leave();
            Assert.IsFalse(_hall.Occupied);
            Assert.IsTrue(_motor.enabled);
            Assert.Less(Vector3.Distance(_player.position, _hall.ExitPoint.position), 0.2f);
        }

        [UnityTest]
        public IEnumerator StandingEntryIsNotSilent()
        {
            Teleport(_player, _hall.ExitPoint.position);
            WarpStalker(new Vector3(-12f, 0.1f, 20f));
            yield return new WaitForSeconds(0.3f);
            _hall.Interact(_interactor);
            float standing = _hall.EntryNoise;
            _hall.Leave();
            yield return null;
            yield return null;
            Assert.Greater(standing, 0.1f);
        }

        [UnityTest]
        public IEnumerator CannotHideWhileTheEntityIsLookingAtYou()
        {
            Teleport(_player, _hall.ExitPoint.position);
            WarpStalker(_player.position + new Vector3(-4f, 0f, 0f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(_stalker.SeesPlayerNow, "test setup: the entity must see the player");
            _hall.Interact(_interactor);
            Assert.IsFalse(_hall.Occupied);
        }

        [UnityTest]
        public IEnumerator HiddenPlayerIsNotSeen()
        {
            Teleport(_player, _hall.ExitPoint.position);
            WarpStalker(new Vector3(-12f, 0.1f, 20f));
            yield return new WaitForSeconds(0.3f);
            _hall.Interact(_interactor);
            Assert.IsTrue(_hall.Occupied);
            WarpStalker(_player.position + new Vector3(-3f, 0f, 0f));
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(_stalker.SeesPlayerNow);
        }

        [UnityTest]
        public IEnumerator EntityOpensAnEmptyCupboardWhileSearching()
        {
            Teleport(_player, new Vector3(0f, 0.1f, 2f));
            _stalker.InspectIntervalSeconds = 1f;
            WarpStalker(new Vector3(-13f, 0.1f, 16f));
            yield return new WaitForSeconds(0.3f);
            _hub.Emit(new Vector3(-13f, 0f, 15f), NoiseKind.DoorSlam);
            float start = Time.time;
            while (Time.time - start < 45f && _stalker.InspectedCount < 1) yield return null;
            Assert.GreaterOrEqual(_stalker.InspectedCount, 1, "it searches nearby hiding spots");
            Assert.IsFalse(_stacks.Occupied);
        }

        [UnityTest]
        public IEnumerator EntityEventuallyFindsAPlayerInAPoorSpotButNotInstantly()
        {
            _hall.Safety = 0.1f;
            Teleport(_player, _hall.ExitPoint.position);
            WarpStalker(new Vector3(-12f, 0.1f, 20f));
            yield return new WaitForSeconds(0.3f);
            _hall.Interact(_interactor);
            Assert.IsTrue(_hall.Occupied);
            _stalker.InspectIntervalSeconds = 1f;
            _stalker.InspectRepeatSeconds = 2f;
            _stalker.SetSeed(11);
            bool found = false;
            _stalker.FoundHidingPlayer += s => found = true;
            WarpStalker(new Vector3(2f, 0.1f, 14f));
            _hub.Emit(new Vector3(3f, 0f, 14f), NoiseKind.DoorSlam);
            float start = Time.time;
            while (Time.time - start < 90f && !found) yield return null;
            Assert.IsTrue(found, "a poor hiding spot is found within a long search");
            Assert.Greater(Time.time - start, 1.5f, "never instantly");
        }
    }
}
#endif
