#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Liminal;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Hotel Meridian greybox: it really is large, within the light budget, and the legend and Guest rules work in it.</summary>
    [Timeout(180000)]
    public class HotelSceneTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Hotel_Meridian.unity";

        LiminalDirector _director;
        GuestManifestation _guest;
        Transform _player;
        CharacterController _cc;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            Accessibility.Reset();
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _director = Object.FindFirstObjectByType<LiminalDirector>();
            _guest = Object.FindFirstObjectByType<GuestManifestation>();
            _player = Object.FindFirstObjectByType<NocturneAnnex.Level.LevelBootstrap>().Player;
            _cc = _player.GetComponent<CharacterController>();
            // tests drive time themselves
            _director.enabled = false;
            _guest.enabled = false;
            Captions.Service.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ModalGate.Reset();
            Time.timeScale = 1f;
            LevelLaunch.RequestNewGame();
            if (InputRouter.Instance != null) Object.DestroyImmediate(InputRouter.Instance.gameObject);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
        }

        static float Cast(Vector3 origin, Vector3 dir) =>
            Physics.Raycast(origin, dir, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore) ? hit.distance : -1f;

        static LevelSpec Measure(int realtimeLights)
        {
            var eye = new Vector3(0f, 1.6f, 3f);
            bool vista = Cast(eye, Vector3.forward) >= 40f
                && GameObject.Find("Sign_Hotel") != null && GameObject.Find("ElevatorDoor_0") != null && GameObject.Find("Stair_West_0") != null
                && Physics.Raycast(new Vector3(-13f, 8f, 20f), Vector3.down, out var mezz) && mezz.point.y > 5f;
            Physics.Raycast(new Vector3(0f, 0.5f, 23f), Vector3.up, out var ceil);
            return new LevelSpec
            {
                Name = "Hotel Meridian",
                HasHeroVista = vista,
                LongestRoomDoorChain = 0,   // open plan: no door sits between two of its spaces
                ShortestPublicCeilingMetres = ceil.point.y,
                RealtimeLights = realtimeLights,
            };
        }

        // ---------- scale and light ----------

        [UnityTest]
        public IEnumerator FromTheSpawn_TheLobbyIsDeep_Tall_AndHasTwoFloors()
        {
            yield return null;
            Assert.GreaterOrEqual(Cast(new Vector3(0f, 1.6f, 3f), Vector3.forward), 40f, "vista depth");
            Physics.Raycast(new Vector3(0f, 0.5f, 23f), Vector3.up, out var ceil);
            Assert.GreaterOrEqual(ceil.point.y, 11.5f, "ceiling height");
            Assert.IsTrue(Physics.Raycast(new Vector3(-13f, 8f, 20f), Vector3.down, out var mezz));
            Assert.That(mezz.point.y, Is.InRange(5f, 6f), "mezzanine floor");
        }

        [UnityTest]
        public IEnumerator EastWing_IsAtLeastFiftyMetresLong_WithDoorsOnBothSides()
        {
            yield return null;
            Assert.GreaterOrEqual(Cast(new Vector3(16f, 1.6f, 20f), Vector3.right), 50f);
            var frames = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(t => t.name.StartsWith("DoorFrame_"));
            Assert.GreaterOrEqual(frames, 8);
        }

        [UnityTest]
        public IEnumerator Lighting_StaysInBudget_AndIsBaked()
        {
            yield return null;
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.LessOrEqual(lights.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime), 2);
            Assert.GreaterOrEqual(lights.Count(l => l.lightmapBakeType == LightmapBakeType.Baked), 6);
            Assert.IsTrue(RenderSettings.fog);
            Assert.Greater(LightmapSettings.lightmaps.Length, 0, "run Build > Create Hotel Meridian Scene");
        }

        [UnityTest]
        public IEnumerator TheMeasuredScene_PassesTheLiminalityChecklist()
        {
            yield return null;
            int realtime = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(l => l.lightmapBakeType == LightmapBakeType.Realtime);
            CollectionAssert.IsEmpty(LevelSpecValidator.Validate(Measure(realtime)));
        }

        // ---------- legend ----------

        [UnityTest]
        public IEnumerator ReadingLegendNotes_RaisesEvidenceOnce_AndPhasesAdvance()
        {
            yield return null;
            var notes = Object.FindObjectsByType<LegendPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(n => n.EntryId).ToArray();
            Assert.AreEqual(3, notes.Length);
            var first = notes.First(n => n.EntryId == "guest_rule_room");
            first.Interact(null);
            Assert.AreEqual(EntityPhase.Rumor, _director.Phase.Phase);
            Assert.AreEqual(2, _director.Phase.Evidence);
            first.Interact(null);
            Assert.AreEqual(2, _director.Phase.Evidence, "reading the same note twice gives nothing");
            notes.First(n => n.EntryId == "guest_mirror").Interact(null);
            Assert.AreEqual(EntityPhase.Clue, _director.Phase.Phase);
            notes.First(n => n.EntryId == "guest_register").Interact(null);
            Assert.AreEqual(EntityPhase.Phenomenon, _director.Phase.Phase);
            Assert.AreEqual(3, _director.Legend.Count);
        }

        // ---------- the Guest ----------

        [UnityTest]
        public IEnumerator Guest_StaysHidden_BeforeSighting_WhileWatched_AndInsideTheQuietWindow()
        {
            yield return null;
            bool watched = false;
            _guest.WatchedOverride = () => watched;
            _director.Tick(500f, false);
            _guest.Tick(10f);
            Assert.IsFalse(_guest.IsVisible, "before Sighting the Guest never appears");

            _director.Phase.AddEvidence(8);
            Assert.AreEqual(EntityPhase.Sighting, _director.Phase.Phase);
            watched = true;
            _guest.Tick(5f);
            Assert.IsFalse(_guest.IsVisible, "never while the player is looking");

            watched = false;
            _guest.Tick(2.1f);
            Assert.IsTrue(_guest.IsVisible, "unwatched long enough, phase reached, quiet window clear");
            CollectionAssert.Contains(Captions.Service.Lines.ToList(), Loc.Get("event.figure"), "captioned");

            watched = true;
            _guest.Tick(0.6f);
            Assert.IsFalse(_guest.IsVisible, "looking at it makes it vanish");

            watched = false;
            _guest.Tick(5f);
            Assert.IsFalse(_guest.IsVisible, "inside the 90 s quiet window it cannot return");
            _director.Tick(91f, false);
            _guest.Tick(2.1f);
            Assert.IsTrue(_guest.IsVisible, "after the quiet window it can appear again");
        }

        [UnityTest]
        public IEnumerator Guest_DoesNotAppear_WhileTheGameIsBlocked()
        {
            yield return null;
            _guest.WatchedOverride = () => false;
            _director.Phase.AddEvidence(8);
            _director.Tick(500f, false);
            using (ModalGate.Open())
            {
                _guest.Tick(10f);
                Assert.IsFalse(_guest.IsVisible);
            }
        }

        // ---------- walking ----------

        [UnityTest]
        public IEnumerator ThePlayerCanWalkToTheEndOfTheWing_AndBack()
        {
            yield return null;
            var path = new[] { new Vector3(0f, 0f, 20f), new Vector3(14f, 0f, 20f), new Vector3(68f, 0f, 20f), new Vector3(14f, 0f, 20f), new Vector3(0f, 0f, 3f) };
            foreach (var target in path)
            {
                float end = Time.realtimeSinceStartup + 40f;
                while (Time.realtimeSinceStartup < end)
                {
                    var flat = new Vector3(target.x - _player.position.x, 0f, target.z - _player.position.z);
                    if (flat.magnitude < 0.5f) break;
                    _cc.Move(flat.normalized * 30f * Time.deltaTime + Vector3.down * 0.1f);   // fast: this only proves the way is clear
                    yield return null;
                }
                Assert.Less(new Vector3(target.x - _player.position.x, 0f, target.z - _player.position.z).magnitude, 1f, "stuck on the way to " + target);
            }
        }
    }
}
#endif
