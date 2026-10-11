#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
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
using NocturneAnnex.Interaction;
using NocturneAnnex.Inventory;
using NocturneAnnex.Level;
using NocturneAnnex.Player;
using NocturneAnnex.Puzzle;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>
    /// F-14f on the generated scenes Level_R02..R10: each loads, matches its RoomCatalog row, has a walkable way from the start to the
    /// progression door, and can be solved by a scripted player that only uses what the room and the carried keys provide.
    /// </summary>
    [Timeout(180000)]
    public class RoomSceneTests
    {
        public static IEnumerable<int> Rooms() => Enumerable.Range(2, 9);

        string _dir;
        SaveGame _save;
        Transform _player;
        Interactor _interactor;
        PlayerInventory _inventory;
        readonly List<string> _loaded = new List<string>();

        IEnumerator Load(int room)
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            RoomFlow.ResetForTests();
            _loaded.Clear();
            RoomFlow.SceneLoader = p => _loaded.Add(p);   // record the hand-over instead of loading a second scene
            LevelLaunch.RequestNewGame();
            _dir = Path.Combine(Path.GetTempPath(), "na_room_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RoomFlow.ScenePath(room), new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _save = Object.FindFirstObjectByType<SaveGame>();
            _save.FilePathOverride = Path.Combine(_dir, "save.json");
            _player = Object.FindFirstObjectByType<PlayerMotor>().transform;
            _interactor = _player.GetComponentInChildren<Interactor>();
            _inventory = _player.GetComponent<PlayerInventory>();
        }

        [TearDown]
        public void TearDown()
        {
            RoomFlow.ResetForTests();
            ModalGate.Reset();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Scene_MatchesItsCatalogRow([ValueSource(nameof(Rooms))] int room)
        {
            yield return Load(room);
            var def = RoomCatalog.Get(room);
            var doors = Object.FindObjectsByType<Door>(FindObjectsSortMode.None);
            Assert.AreEqual(def.Doors.Count, doors.Length, "doors");
            Assert.AreEqual(def.HideSpots, Object.FindObjectsByType<HideSpot>(FindObjectsSortMode.None).Length, "hide spots");
            var transitions = Object.FindObjectsByType<RoomTransition>(FindObjectsSortMode.None);
            Assert.AreEqual(1, transitions.Count(t => !t.Back && t.TargetRoom == room + 1), "one forward transition");
            Assert.AreEqual(1, transitions.Count(t => t.Back && t.TargetRoom == room - 1), "one back transition");
            foreach (var key in def.Keys)
                Assert.IsTrue(Object.FindObjectsByType<Pickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(p => p.ItemId == key.Id), "pickup " + key.Id);
            Assert.AreEqual(def.Code != null, Object.FindFirstObjectByType<CodeLock>() != null, "code lock");
            var stalker = Object.FindFirstObjectByType<StalkerAgent>();
            Assert.AreEqual(def.Difficulty.InspectIntervalSeconds, stalker.InspectIntervalSeconds, 0.0001f);
            Assert.AreEqual(def.Difficulty.InspectRange, stalker.InspectRange, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Stalker_StandsOnTheNavMesh_AndThereIsAWalkFromTheStartToEveryDoor([ValueSource(nameof(Rooms))] int room)
        {
            yield return Load(room);
            var stalker = Object.FindFirstObjectByType<StalkerAgent>();
            Assert.IsTrue(stalker.GetComponent<NavMeshAgent>().isOnNavMesh, "the entity is on the mesh");
            foreach (var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                var front = door.transform.position + (door.name == "Door_back" ? Vector3.forward : Vector3.back) * 1.2f;
                Assert.IsTrue(NavMesh.SamplePosition(front, out var to, 1.5f, NavMesh.AllAreas), door.name + ": the spot in front of it is off the mesh");
                Assert.IsTrue(NavMesh.SamplePosition(_player.position, out var from, 1.5f, NavMesh.AllAreas), "the start is off the mesh");
                var path = new NavMeshPath();
                Assert.IsTrue(NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
                    door.name + " has no walkable approach (" + path.status + ")");
            }
        }

        [UnityTest]
        public IEnumerator ScriptedPlayer_CanOpenTheProgressionDoor_AndLeave([ValueSource(nameof(Rooms))] int room)
        {
            yield return Load(room);
            var def = RoomCatalog.Get(room);

            // keys carried in from earlier rooms: those a door here needs that this room does not place
            foreach (var d in def.Doors.Where(d => d.Unlock == UnlockKind.Key && !def.Keys.Any(k => k.Id == d.UnlockId)))
                Assert.IsTrue(_inventory.TryAdd(d.UnlockId), "carried key " + d.UnlockId);

            var doors = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).ToDictionary(d => d.name);
            var codeLock = Object.FindFirstObjectByType<CodeLock>();
            bool Inside(Component c, out Door closetDoor)
            {
                closetDoor = null;
                var pos = c.transform.position;
                if (pos.z <= doors["Door_prog"].transform.position.z) return false;   // in the main room
                closetDoor = doors.Values.Where(d => d.name != "Door_back").OrderBy(d => Mathf.Abs(d.transform.position.x - pos.x)).First();
                return true;
            }

            for (int step = 0; step < 30 && !doors["Door_prog"].IsOpen; step++)
            {
                foreach (var p in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                    if (!Inside(p, out var gate) || gate.IsOpen) p.Interact(_interactor);
                foreach (var s in Object.FindObjectsByType<UnlockSwitch>(FindObjectsSortMode.None))
                    if (!Inside(s, out var gate) || gate.IsOpen) s.Interact(_interactor);
                if (codeLock != null && !codeLock.IsSolved)
                {
                    var clue = GameObject.Find("ClueBoard");
                    if (!Inside(clue.transform, out var gate) || gate.IsOpen)
                    {
                        foreach (char ch in def.Code.Code) codeLock.Press(ch);
                        codeLock.Submit();
                    }
                }
                foreach (var d in doors.Values.Where(d => d.name != "Door_back" && !d.IsOpen)) d.Interact(_interactor);
                yield return null;
            }
            Assert.IsTrue(doors["Door_prog"].IsOpen, "the progression door opened using only reachable items");
            foreach (var d in def.Doors.Where(d => d.Kind == DoorKind.Fake || d.Kind == DoorKind.Locked || d.Kind == DoorKind.Blocked))
                Assert.IsFalse(doors["Door_" + d.Id].IsOpen, d.Id + " must stay shut");

            // walk into the closet behind the door: the hand-over to the next room (or the ending) happens once
            var tr = Object.FindObjectsByType<RoomTransition>(FindObjectsSortMode.None).First(t => !t.Back);
            bool escaped = false;
            System.Action onEscaped = () => escaped = true;
            RoomFlow.Escaped += onEscaped;
            var cc = _player.GetComponent<CharacterController>();
            cc.enabled = false; _player.position = tr.transform.position; cc.enabled = true;
            yield return new WaitForSeconds(0.3f);
            RoomFlow.Escaped -= onEscaped;
            if (room < RoomCatalog.Count) CollectionAssert.AreEqual(new[] { RoomFlow.ScenePath(room + 1) }, _loaded, "moved on to the next room once");
            else Assert.IsTrue(escaped, "the last room ends the game");
            if (room < RoomCatalog.Count) Assert.AreEqual(room + 1, _save.Rooms.Current);
        }

        [UnityTest]
        public IEnumerator BackDoor_ReturnsToThePreviousRoom([ValueSource(nameof(Rooms))] int room)
        {
            yield return Load(room);
            var tr = Object.FindObjectsByType<RoomTransition>(FindObjectsSortMode.None).First(t => t.Back);
            var cc = _player.GetComponent<CharacterController>();
            cc.enabled = false; _player.position = tr.transform.position; cc.enabled = true;
            yield return new WaitForSeconds(0.3f);
            CollectionAssert.AreEqual(new[] { RoomFlow.ScenePath(room - 1) }, _loaded);
            Assert.AreEqual(room - 1, _save.Rooms.Current);
        }
    }
}
#endif
