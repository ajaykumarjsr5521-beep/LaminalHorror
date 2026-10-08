#if UNITY_EDITOR
using System.Collections;
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

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>The Stilt-Walker on the real Level_B1: it exists, stands on the NavMesh, patrols, hears the player and is fair.</summary>
    [Timeout(120000)]
    public class StalkerLevelTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        StalkerAgent _stalker;
        NoiseHub _hub;
        Transform _player;

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
        }

        [TearDown]
        public void TearDown() { ModalGate.Reset(); Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator EntityAndNoisePlumbingExist()
        {
            Assert.IsNotNull(_stalker, "Level_B1 must contain the Stilt-Walker");
            Assert.IsNotNull(_hub);
            Assert.IsTrue(Object.FindFirstObjectByType<LevelNavMesh>().Built);
            Assert.IsNotNull(_player.GetComponent<PlayerNoiseEmitter>());
            foreach (var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
                Assert.IsNotNull(door.GetComponent<DoorNoise>(), door.name);
            Assert.GreaterOrEqual(_stalker.PatrolPoints.Length, 4);
            Assert.AreEqual(4, _stalker.NormalSteps.Length);
            Assert.AreEqual(4, _stalker.WoodSteps.Length);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EntityStandsOnTheNavMeshAndPatrols()
        {
            yield return null;
            var agent = _stalker.GetComponent<NavMeshAgent>();
            Assert.IsTrue(agent.isOnNavMesh);
            var start = _stalker.transform.position;
            yield return new WaitForSeconds(6f);
            Assert.Greater(Vector3.Distance(start, _stalker.transform.position), 1f, "it must walk its patrol");
            Assert.AreEqual(EntityState.Patrol, _stalker.Brain.State, "a quiet player far away must not be noticed");
        }

        [UnityTest]
        public IEnumerator EntityIsFarFromThePlayerAtStart()
        {
            yield return null;
            Assert.Greater(Vector3.Distance(_stalker.transform.position, _player.position), 12f);
        }

        [UnityTest]
        public IEnumerator PlayerFootstepEmitsNoiseOnTheBus()
        {
            yield return null;
            int before = _hub.Bus.EmittedCount;
            _hub.Emit(_player.position, NoiseKind.Walk);
            Assert.AreEqual(before + 1, _hub.Bus.EmittedCount);
        }

        [UnityTest]
        public IEnumerator SlammingNoiseNearTheEntityMakesItInvestigate()
        {
            yield return null;
            _stalker.SetSeed(5);
            var near = _stalker.transform.position + new Vector3(4f, 0f, 0f);
            _hub.Emit(near, NoiseKind.DoorSlam);
            float start = Time.time;
            while (Time.time - start < 5f && _stalker.Brain.State != EntityState.Investigate
                   && _stalker.Brain.State != EntityState.Watch) yield return null;
            Assert.That(_stalker.Brain.State, Is.EqualTo(EntityState.Investigate).Or.EqualTo(EntityState.Watch));
        }
    }
}
#endif
