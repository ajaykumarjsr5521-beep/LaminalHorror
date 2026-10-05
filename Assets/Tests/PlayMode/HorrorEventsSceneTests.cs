#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Horror;
using NocturneAnnex.Interaction;
using NocturneAnnex.Level;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Fires every horror event kind on the real Level_B1 scene and checks pacing, blocking, accessibility and saving.</summary>
    [Timeout(120000)]   // ms per test: a hung run fails instead of freezing
    public class HorrorEventsSceneTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";
        const float RealTimeLimit = 20f;   // real seconds; a loop on game time must never outlive this
        static readonly string[] OneShots = { "prop_shift_counter", "door_slam_stacks", "shadow_hall", "misfile_alcove" };

        string _dir;
        HorrorEventRunner _runner;
        LevelBootstrap _level;
        Transform _player;
        CharacterController _cc;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;   // another fixture may have left the game paused
            ModalGate.Reset();
            LevelLaunch.RequestNewGame();
            _dir = Path.Combine(Path.GetTempPath(), "na_horror_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindFirstObjectByType<HorrorEventRunner>();
            Assert.IsNotNull(_runner, "Level_B1 must contain a HorrorEventRunner");
            _level = Object.FindFirstObjectByType<LevelBootstrap>();
            _level.SaveGame.FilePathOverride = Path.Combine(_dir, "save.json");
            _player = _level.Player;
            _cc = _player.GetComponent<CharacterController>();
            Captions.Service.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Accessibility.Reset();
            ModalGate.Reset();
            Time.timeScale = 1f;
            LevelLaunch.RequestNewGame();
            // the level scene stays loaded after the test; its InputRouter singleton would make the next test's router destroy itself
            if (InputRouter.Instance != null) Object.DestroyImmediate(InputRouter.Instance.gameObject);
            // and leave no level objects (player, runner, UI) alive under the next fixture
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        static T Named<T>(string name) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == name);

        HorrorEventSpot SpotFor(string id) => _runner.Spots.First(s => s.Event.Id == id);

        IEnumerator Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            _player.position = pos;
            _cc.enabled = true;
            _cc.Move(Vector3.down * 0.01f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        // ---------- the six kinds ----------

        [UnityTest]
        public IEnumerator Data_HasAtLeastSixKinds_WithUniqueIds()
        {
            yield return null;
            var kinds = _runner.Spots.Select(s => s.Event.Kind).Distinct().ToList();
            Assert.GreaterOrEqual(kinds.Count, 6);
            Assert.AreEqual(_runner.Spots.Length, _runner.Spots.Select(s => s.Event.Id).Distinct().Count());
        }

        [UnityTest]
        public IEnumerator LightFlicker_ChangesBrightness_ThenRestoresIt()
        {
            var spot = SpotFor("flicker_hall");
            var light = spot.Lights[0];
            float baseIntensity = light.intensity;
            float min = baseIntensity;
            _runner.Fire("flicker_hall");
            yield return null;
            CollectionAssert.Contains(Captions.Service.Lines.ToList(), Loc.Get("event.light_buzz"));   // before it expires
            float end = Time.time + spot.Event.DurationSeconds + 0.5f;
            float guard = Time.realtimeSinceStartup + RealTimeLimit;
            while (Time.time < end)
            {
                Assert.Less(Time.realtimeSinceStartup, guard, "game time stopped advancing (timeScale=" + Time.timeScale + ")");
                min = Mathf.Min(min, light.intensity);
                yield return null;
            }
            Assert.Less(min, baseIntensity * 0.5f, "the light must visibly dip");
            Assert.AreEqual(baseIntensity, light.intensity, 0.0001f, "brightness must be restored");
        }

        [UnityTest]
        public IEnumerator LightFlicker_WithReduceFlicker_StaysShallow()
        {
            Accessibility.Set(true, Accessibility.MediumText, reduceFlicker: true, reduceMotion: false);
            var spot = SpotFor("flicker_hall");
            var light = spot.Lights[0];
            float baseIntensity = light.intensity;
            float min = baseIntensity;
            _runner.Fire("flicker_hall");
            float end = Time.time + spot.Event.DurationSeconds + 0.3f;
            float guard = Time.realtimeSinceStartup + RealTimeLimit;
            while (Time.time < end)
            {
                Assert.Less(Time.realtimeSinceStartup, guard, "game time stopped advancing (timeScale=" + Time.timeScale + ")");
                min = Mathf.Min(min, light.intensity);
                yield return null;
            }
            Assert.GreaterOrEqual(min, baseIntensity * 0.75f - 0.001f, "at most 25 percent deep");
        }

        [UnityTest]
        public IEnumerator DoorSlam_ClosesAnOpenDoor_AndRestoresItsSpeed()
        {
            var door = Named<Door>("Door_HallToStacks");
            float speed = door.AngularSpeed;
            door.Interact(null);
            Assert.IsTrue(door.IsOpen);
            _runner.Fire("door_slam_stacks");
            yield return null;
            Assert.IsFalse(door.IsOpen);
            yield return new WaitForSeconds(SpotFor("door_slam_stacks").Event.DurationSeconds + 0.7f);
            Assert.AreEqual(speed, door.AngularSpeed, 0.0001f);
            CollectionAssert.Contains(Captions.Service.Lines.ToList(), Loc.Get("event.door_slam"));
        }

        [UnityTest]
        public IEnumerator PropShift_MovesTheProp()
        {
            var prop = SpotFor("prop_shift_counter").Prop;
            var before = prop.position;
            _runner.Fire("prop_shift_counter");
            yield return null;
            Assert.AreEqual(0.5f, (prop.position - before).x, 0.0001f);
        }

        [UnityTest]
        public IEnumerator AudioCue_PostsItsCaption_EvenWithoutAClip()
        {
            _runner.Fire("whisper_stacks");
            yield return null;
            CollectionAssert.Contains(Captions.Service.Lines.ToList(), Loc.Get("event.whisper"));
        }

        [UnityTest]
        public IEnumerator MisfileReveal_EnablesTheHiddenSection()
        {
            var alcove = SpotFor("misfile_alcove").Reveal;
            Assert.IsFalse(alcove.activeSelf);
            _runner.Fire("misfile_alcove");
            yield return null;
            Assert.IsTrue(alcove.activeSelf);
        }

        [UnityTest]
        public IEnumerator ShadowFigure_AppearsThenVanishes()
        {
            var figure = SpotFor("shadow_hall").Figure;
            Assert.IsFalse(figure.activeSelf);
            _runner.Fire("shadow_hall");
            yield return null;
            Assert.IsTrue(figure.activeSelf);
            yield return new WaitForSeconds(SpotFor("shadow_hall").Event.DurationSeconds + 0.4f);
            Assert.IsFalse(figure.activeSelf);
        }

        // ---------- pacing and blocking ----------

        [UnityTest]
        public IEnumerator Blocked_NeverBuildsTensionOrFiresAnything()
        {
            yield return null;
            int fired = 0;
            _runner.EventFired += _ => fired++;
            for (int i = 0; i < 4000; i++) _runner.Advance(0.5f, inSafeZone: false, blocked: true);
            Assert.AreEqual(0f, _runner.Director.Tension);
            Assert.AreEqual(0, fired);
        }

        [UnityTest]
        public IEnumerator OpenModalScreen_CountsAsBlocked()
        {
            yield return null;
            Assert.IsFalse(_runner.Blocked);
            using (var gate = ModalGate.Open().AsDisposable()) Assert.IsTrue(_runner.Blocked);
            Time.timeScale = 0f;
            Assert.IsTrue(_runner.Blocked, "a paused game is blocked");
        }

        [UnityTest]
        public IEnumerator OneHourOfUnsafePlay_NeverPutsTwoEventsCloserThanTheGap()
        {
            yield return null;
            var times = new List<float>();
            _runner.EventFired += _ => times.Add(_runner.GameTime);
            for (int i = 0; i < 7200; i++) _runner.Advance(0.5f, inSafeZone: false, blocked: false);
            Assert.GreaterOrEqual(times.Count, 3);
            for (int i = 1; i < times.Count; i++)
                Assert.GreaterOrEqual(times[i] - times[i - 1], _runner.Director.EffectiveGapSeconds - 0.001f);
        }

        [UnityTest]
        public IEnumerator SafeZones_AreDetected_InTheBreakRoomAndNotInTheHall()
        {
            yield return null;
            yield return Teleport(new Vector3(0f, 0.1f, 3f));
            Assert.IsTrue(_runner.PlayerInSafeZone);
            yield return Teleport(new Vector3(0f, 0.1f, 17f));
            Assert.IsFalse(_runner.PlayerInSafeZone);
        }

        // ---------- accessibility ----------

        [UnityTest]
        public IEnumerator CameraShake_IsOff_WhenReduceMotionIsOn()
        {
            yield return null;
            var shake = _runner.Shake;
            Accessibility.Set(true, Accessibility.MediumText, reduceFlicker: false, reduceMotion: true);
            Assert.AreEqual(0f, CameraShake.MaxOffset(1f));
            shake.Shake(1f, 0.5f);
            Assert.IsFalse(shake.IsShaking);

            Accessibility.Set(true, Accessibility.MediumText, reduceFlicker: false, reduceMotion: false);
            var rest = shake.transform.localPosition;
            shake.Shake(1f, 0.2f);
            Assert.IsTrue(shake.IsShaking);
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(shake.IsShaking);
            Assert.AreEqual(rest, shake.transform.localPosition, "the camera must return to rest");
        }

        // ---------- saving ----------

        [UnityTest]
        public IEnumerator FiredOneShots_AreSaved_AndDoNotRepeatAfterContinue()
        {
            yield return null;
            // use every one-shot
            for (int i = 0; i < 10 && _runner.Picker.TryPick(1f, i, out _); i++) { }
            var saved = _level.SaveGame.Capture("hall");
            foreach (var id in OneShots) CollectionAssert.Contains(saved.FiredEventIds, id);
            Assert.IsTrue(_level.SaveGame.Store.Write(saved).Ok);

            _level.Begin(resume: true);
            yield return null;

            var picked = new List<string>();
            for (int i = 0; i < 10 && _runner.Picker.TryPick(1f, 1000f * (i + 1), out var id); i++) picked.Add(id);
            foreach (var id in OneShots) CollectionAssert.DoesNotContain(picked, id, "a one-shot that fired must stay fired");
        }

        [UnityTest]
        public IEnumerator Respawn_ClearsLeftoverTension()
        {
            yield return null;
            for (int i = 0; i < 100; i++) _runner.Director.Tick(0.5f, true, false);
            Assert.Greater(_runner.Director.Tension, 0.3f);
            _level.Begin(resume: false);
            Assert.AreEqual(0f, _runner.Director.Tension);
        }

        // ---------- bad data ----------

        [UnityTest]
        public IEnumerator DuplicateEventIds_AreReported_AndTheSecondIsSkipped()
        {
            yield return null;
            var go = new GameObject("TestRunner");
            var runner = go.AddComponent<HorrorEventRunner>();
            var asset = ScriptableObject.CreateInstance<HorrorEventAsset>();
            asset.Id = "dup";
            var a = new GameObject("A").AddComponent<HorrorEventSpot>();
            var b = new GameObject("B").AddComponent<HorrorEventSpot>();
            a.Event = asset; b.Event = asset;
            runner.Spots = new[] { a, b };
            LogAssert.Expect(LogType.Error, new Regex("duplicate event id 'dup'"));
            runner.Build();
            Object.Destroy(go);
            Object.Destroy(a.gameObject);
            Object.Destroy(b.gameObject);
        }
    }

    static class GateExtensions
    {
        public static System.IDisposable AsDisposable(this ModalGate.Handle h) => h;
    }
}
#endif
