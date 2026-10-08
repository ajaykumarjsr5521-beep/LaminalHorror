using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.PlayMode
{
    public class StalkerAgentTests
    {
        GameObject _root;
        NoiseHub _hub;
        StalkerAgent _stalker;
        Transform _player;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Arena");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(_root.transform);
            floor.transform.localScale = new Vector3(60f, 1f, 60f);
            floor.transform.position = new Vector3(0, -0.5f, 0);

            var player = new GameObject("Player");
            player.transform.SetParent(_root.transform);
            player.transform.position = new Vector3(20f, 0, 0);
            _player = player.transform;

            _root.AddComponent<LevelNavMesh>().Player = _player;
            _hub = _root.AddComponent<NoiseHub>();
        }

        StalkerAgent SpawnStalker(Vector3 pos)
        {
            var go = new GameObject("Stalker");
            go.transform.SetParent(_root.transform);
            go.transform.position = pos;
            go.AddComponent<NavMeshAgent>();
            go.SetActive(false);
            var s = go.AddComponent<StalkerAgent>();
            s.Hub = _hub; s.Player = _player; s.SetSeed(1);
            go.SetActive(true);
            return s;
        }

        [TearDown]
        public void TearDown() { if (_root != null) Object.Destroy(_root); }

        [UnityTest]
        public IEnumerator NavMeshIsBuilt()
        {
            yield return null;
            Assert.IsTrue(_root.GetComponent<LevelNavMesh>().Built);
            Assert.IsTrue(NavMesh.SamplePosition(Vector3.zero, out _, 1f, NavMesh.AllAreas));
        }

        [UnityTest]
        public IEnumerator EntityWalksToARunNoiseAndInvestigates()
        {
            yield return null;
            _stalker = SpawnStalker(new Vector3(-10f, 0, 0));
            yield return null;
            _hub.Emit(new Vector3(-6f, 0, 0), NoiseKind.Run);   // 4 m away, strong run noise
            float start = Time.time;
            while (Time.time - start < 4f && _stalker.Brain.State != EntityState.Investigate) yield return null;
            Assert.AreEqual(EntityState.Investigate, _stalker.Brain.State);
            Assert.AreEqual(-6f, _stalker.Brain.Target.x, 0.1f);
            yield return new WaitForSeconds(6f);
            Assert.Less(Vector3.Distance(_stalker.transform.position, new Vector3(-6f, 0, 0)), 2.5f);
        }

        [UnityTest]
        public IEnumerator CrouchNoiseFarAwayIsIgnored()
        {
            yield return null;
            _stalker = SpawnStalker(new Vector3(-10f, 0, 0));
            yield return null;
            _hub.Emit(new Vector3(0f, 0, 0), NoiseKind.Crouch);   // 10 m away, radius is 2 m
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(EntityState.Patrol, _stalker.Brain.State);
        }

        [UnityTest]
        public IEnumerator WallBlocksSight()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(_root.transform);
            wall.transform.position = new Vector3(5f, 1.5f, 0);
            wall.transform.localScale = new Vector3(0.3f, 3f, 20f);
            _player.position = new Vector3(7f, 0, 0);
            Physics.SyncTransforms();
            yield return null;
            _stalker = SpawnStalker(new Vector3(2f, 0, 0));
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(EntityState.Patrol, _stalker.Brain.State, "a wall between must hide the player");
            Object.Destroy(wall);
            yield return new WaitForSeconds(0.5f);
            Assert.AreNotEqual(EntityState.Patrol, _stalker.Brain.State, "without the wall the entity notices the player");
        }

        [UnityTest]
        public IEnumerator ChaseCatchesAStandingPlayerAndRaisesTheEvent()
        {
            _player.position = new Vector3(4f, 0, 0);
            yield return null;
            _stalker = SpawnStalker(new Vector3(0f, 0, 0));
            bool caught = false;
            _stalker.CaughtPlayer += () => caught = true;
            float start = Time.time;
            while (Time.time - start < 8f && !caught) yield return null;
            Assert.IsTrue(caught);
        }
    }
}
