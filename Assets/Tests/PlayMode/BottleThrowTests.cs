#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Entity;
using NocturneAnnex.Flow;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>F-14d: bottles in the real Level_B1 can be held and thrown, and the landing is a noise the entity can hear.</summary>
    [Timeout(120000)]
    public class BottleThrowTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        NoiseHub _hub;
        PlayerThrower _thrower;
        StalkerAgent _stalker;
        ThrowableBottle[] _bottles;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _hub = Object.FindFirstObjectByType<NoiseHub>();
            _thrower = Object.FindFirstObjectByType<PlayerThrower>();
            _stalker = Object.FindFirstObjectByType<StalkerAgent>();
            _bottles = Object.FindObjectsByType<ThrowableBottle>(FindObjectsSortMode.None);
            if (_thrower != null) _thrower.InputProvider = () => default;   // no real keyboard during tests
        }

        [TearDown]
        public void TearDown() { ModalGate.Reset(); Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator BottlesAreInTheLevelAndRestOnTheFloor()
        {
            Assert.IsNotNull(_thrower);
            Assert.AreEqual(4, _bottles.Length);
            yield return new WaitForSeconds(2f);
            foreach (var b in _bottles) Assert.That(b.transform.position.y, Is.InRange(0f, 0.6f), b.name + " must settle on the floor");
        }

        [UnityTest]
        public IEnumerator ThrowingWithAnEmptyHandDoesNothing()
        {
            int before = _hub.Bus.EmittedCount;
            Assert.IsFalse(_thrower.Throw());
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(before, _hub.Bus.EmittedCount);
        }

        [UnityTest]
        public IEnumerator ThrownBottleMakesAThrownImpactNoise()
        {
            NoiseEvent heard = default; bool got = false;
            _hub.Bus.Emitted += e => { if (e.Kind == NoiseKind.ThrownImpact) { heard = e; got = true; } };
            Assert.IsTrue(_thrower.TryHold(_bottles[0]));
            Assert.IsFalse(_thrower.TryHold(_bottles[1]), "one bottle at a time");
            Assert.IsTrue(_thrower.Throw());
            float start = Time.time;
            while (!got && Time.time - start < 5f) yield return null;
            Assert.IsTrue(got, "landing must emit a ThrownImpact");
            Assert.AreEqual(NoiseTable.Radius(NoiseKind.ThrownImpact), heard.Radius, 1e-4f);
        }

        [UnityTest]
        public IEnumerator InputThrowPressedThrowsTheHeldBottle()
        {
            _thrower.TryHold(_bottles[0]);
            bool press = true;
            _thrower.InputProvider = () => { var s = new PlayerInputState { ThrowPressed = press }; press = false; return s; };
            yield return null;
            yield return null;
            Assert.IsNull(_thrower.Held);
            Assert.IsTrue(_bottles[0].gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator BottleLandingNearTheEntityMakesItLeavePatrol()
        {
            Assert.IsTrue(_thrower.TryHold(_bottles[0]));
            _stalker.SetSeed(5);
            var target = _stalker.transform.position + new Vector3(4f, 1.5f, 0f);
            var b = _bottles[0];
            b.Launch(target, Vector3.down * 4f);
            float start = Time.time;
            while (Time.time - start < 6f && _stalker.Brain.State == EntityState.Patrol) yield return null;
            Assert.AreNotEqual(EntityState.Patrol, _stalker.Brain.State);
        }
    }
}
#endif
