using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>F-14m: contact in a chase starts a visible swing; the hit lands after it, and moving away makes it a miss.</summary>
    public class StalkerStrikeTests
    {
        GameObject _root;
        NoiseHub _hub;
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
            _player = player.transform;
            _root.AddComponent<LevelNavMesh>().Player = _player;
            _hub = _root.AddComponent<NoiseHub>();
        }

        [TearDown]
        public void TearDown() { if (_root != null) Object.Destroy(_root); }

        StalkerAgent Spawn(Vector3 pos, out StalkerArms arms)
        {
            var go = new GameObject("Stalker");
            go.transform.SetParent(_root.transform);
            go.transform.position = pos;
            go.AddComponent<NavMeshAgent>();
            var l = new GameObject("ShoulderL").transform; l.SetParent(go.transform, false);
            var r = new GameObject("ShoulderR").transform; r.SetParent(go.transform, false);
            arms = go.AddComponent<StalkerArms>(); arms.ShoulderL = l; arms.ShoulderR = r;
            go.SetActive(false);
            var s = go.AddComponent<StalkerAgent>();
            s.Hub = _hub; s.Player = _player; s.Arms = arms; s.SetSeed(1);
            go.SetActive(true);
            return s;
        }

        [UnityTest]
        public IEnumerator StandingPlayerIsHitOnlyAfterAVisibleWindup()
        {
            _player.position = new Vector3(4f, 0, 0);
            yield return null;
            var stalker = Spawn(Vector3.zero, out var arms);
            bool caught = false; float windupAt = -1f, caughtAt = -1f, maxRaise = 0f;
            stalker.CaughtPlayer += () => { caught = true; caughtAt = Time.time; };
            float start = Time.time;
            while (Time.time - start < 10f && !caught)
            {
                if (windupAt < 0f && stalker.Strike.Phase != StrikePhase.Idle) windupAt = Time.time;
                maxRaise = Mathf.Max(maxRaise, arms.Raise);
                yield return null;
            }
            Assert.IsTrue(caught, "a standing player is still caught");
            Assert.GreaterOrEqual(windupAt, 0f, "the swing started");
            Assert.GreaterOrEqual(caughtAt - windupAt, StrikeModel.WindupSeconds + StrikeModel.StrikeSeconds - 0.1f, "no instant death");
            Assert.Greater(maxRaise, 0.8f, "the arm visibly raised");
            Assert.Greater(Quaternion.Angle(Quaternion.identity, arms.ShoulderR.localRotation) + maxRaise, 1f);
        }

        [UnityTest]
        public IEnumerator PlayerWhoMovesOutOfReachDuringTheSwingIsMissed()
        {
            _player.position = new Vector3(4f, 0, 0);
            yield return null;
            var stalker = Spawn(Vector3.zero, out _);
            bool caught = false;
            stalker.CaughtPlayer += () => caught = true;
            float start = Time.time;
            while (Time.time - start < 10f && stalker.Strike.Phase == StrikePhase.Idle) yield return null;
            Assert.AreNotEqual(StrikePhase.Idle, stalker.Strike.Phase, "swing began");
            _player.position = new Vector3(20f, 0, 0);   // out of reach before the swing lands
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(caught, "moving away during the swing is a miss");
            Assert.IsFalse(stalker.Strike.LastWasHit);
        }
    }
}
