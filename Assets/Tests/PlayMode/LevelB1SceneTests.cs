#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Interaction;
using NocturneAnnex.Inventory;
using NocturneAnnex.Level;
using NocturneAnnex.Puzzle;
using NocturneAnnex.Save;
using NocturneAnnex.Settings;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Drives the real generated Level_B1 scene end to end: the puzzle, gating, checkpoints, resume and HUD.</summary>
    public class LevelB1SceneTests
    {
        // Item ids from the level data; the generator itself lives in the Editor assembly, which PlayMode tests cannot reference.
        static class Ids
        {
            public const string MemoId = "note_memo", StampId = "brass_stamp", TapeId = "note_tape";
        }

        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        static readonly Vector3 Hall = new Vector3(0f, 0.1f, 13f);
        static readonly Vector3 Records = new Vector3(10f, 0.1f, 16f);
        static readonly Vector3 Dock = new Vector3(0f, 0.1f, 29f);
        static readonly Vector3 ExitSpot = new Vector3(0f, 0.1f, 40.6f);

        string _dir;
        string _savePath;
        LevelBootstrap _level;
        LevelHud _hud;
        SaveGame _save;
        Transform _player;
        CharacterController _cc;
        Interactor _interactor;
        PlayerInventory _inventory;
        CodeLock _lock;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            LevelLaunch.RequestNewGame();
            _dir = Path.Combine(Path.GetTempPath(), "na_level_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            _savePath = Path.Combine(_dir, "save.json");

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            _level = Object.FindFirstObjectByType<LevelBootstrap>();
            Assert.IsNotNull(_level, "Level_B1 must contain a LevelBootstrap");
            _hud = Object.FindFirstObjectByType<LevelHud>();
            _save = _level.SaveGame;
            _save.FilePathOverride = _savePath;   // before the first Store use, so the real save is never touched
            _player = _level.Player;
            _cc = _player.GetComponent<CharacterController>();
            _interactor = Object.FindFirstObjectByType<Interactor>();
            _inventory = _player.GetComponent<PlayerInventory>();
            _lock = _level.FinalLock;
            _hud.SceneLoader = _ => { };
            Captions.Service.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hud != null && _hud.Pause != null) _hud.Pause.Resume();
            Time.timeScale = 1f;
            ModalGate.Reset();
            Accessibility.Reset();
            LevelLaunch.RequestNewGame();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // The player origin sits at the capsule centre, so compare on the floor plane only.
        static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        IEnumerator Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            _player.position = pos;
            _cc.enabled = true;
            _cc.Move(Vector3.down * 0.01f);   // a move is what makes the controller report trigger entry
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        static T Named<T>(string name) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == name);

        static string DigitsOf(string name)
        {
            var text = Named<TextMeshPro>(name + "_Text").text;
            return Regex.Replace(Regex.Replace(text, "<.*?>", ""), "[^0-9]", "");
        }

        // ---------- structure ----------

        [UnityTest]
        public IEnumerator Scene_HasFourCheckpointsInOrder_AndStartsAtTheEntrance()
        {
            yield return null;
            CollectionAssert.AreEqual(new[] { "entrance", "hall", "records", "dock" }, _level.Checkpoints.Select(c => c.Id).ToArray());
            Assert.AreEqual("entrance", _level.Progress.CurrentId);
            Assert.Less(Flat(_player.position, _level.Checkpoints[0].Spawn.position), 0.5f);
        }

        // ---------- puzzle ----------

        [UnityTest]
        public IEnumerator PuzzleCode_IsTheThreeDatesFoundOnTheInWorldProps_InOrder()
        {
            yield return null;
            string fromWorld = DigitsOf("Calendar_Lights") + DigitsOf("LedgerSpine") + DigitsOf("Calendar_Dock");
            Assert.AreEqual(fromWorld, _lock.Code, "the code must be readable from the props, in the order the memo describes");
        }

        [UnityTest]
        public IEnumerator Notes_NeverContainTheCodeDigitsDirectly()
        {
            yield return null;
            foreach (var note in Object.FindObjectsByType<Note>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Assert.IsFalse(Regex.IsMatch(note.Body, "[0-9]"), $"note '{note.Title}' leaks digits; clues must point at props instead");
        }

        // ---------- scripted full run ----------

        [UnityTest]
        public IEnumerator FullRun_NotesStampKeypadDoorAndExit()
        {
            yield return null;

            yield return Teleport(Hall);
            Assert.AreEqual("hall", _level.Progress.CurrentId);
            Assert.AreEqual("hall", new SaveStore(_savePath).Load().Data.CheckpointId, "reaching a checkpoint must save");

            Named<Note>("Note_" + Ids.MemoId).Interact(_interactor);
            Assert.IsTrue(_inventory.Has(Ids.MemoId));

            var recordsDoor = Named<Door>("Door_HallToRecords");
            recordsDoor.Interact(_interactor);
            Assert.IsFalse(recordsDoor.IsOpen, "the Records Office door needs the stamp");

            Named<Pickup>("BrassStamp").Interact(_interactor);
            Assert.IsTrue(_inventory.Has(Ids.StampId));
            recordsDoor.Interact(_interactor);
            Assert.IsTrue(recordsDoor.IsOpen);
            Assert.IsFalse(_inventory.Has(Ids.StampId), "the used stamp is consumed");

            yield return Teleport(Records);
            Assert.AreEqual("records", _level.Progress.CurrentId);

            // keypad: wrong code first, then the code read from the props
            foreach (char c in "0000") _lock.Press(c);
            Assert.AreEqual(SubmitResult.Wrong, _lock.Submit());
            string code = DigitsOf("Calendar_Lights") + DigitsOf("LedgerSpine") + DigitsOf("Calendar_Dock");
            foreach (char c in code) _lock.Press(c);
            Assert.AreEqual(SubmitResult.Correct, _lock.Submit());
            Assert.IsFalse(Named<Door>("Door_HallToDock").IsLocked);

            yield return Teleport(Dock);
            Assert.AreEqual("dock", _level.Progress.CurrentId);

            bool completed = false;
            _level.LevelCompleted += () => completed = true;
            yield return Teleport(ExitSpot);
            Assert.IsTrue(completed);
            Assert.IsTrue(_hud.EndCard.activeSelf, "the end card shows on completion");
            Assert.IsTrue(ModalGate.IsOpen, "gameplay input is blocked behind the end card");
        }

        [UnityTest]
        public IEnumerator Exit_DoesNotCountUntilThePuzzleIsSolved()
        {
            yield return null;
            bool completed = false;
            _level.LevelCompleted += () => completed = true;
            yield return Teleport(ExitSpot);
            Assert.IsFalse(completed);
            Assert.IsFalse(_level.Completed);
            CollectionAssert.Contains(Captions.Service.Lines.ToList(), Loc.Get("level.exit_locked"));
        }

        [UnityTest]
        public IEnumerator LockedDock_BlocksWalkingThrough_UntilSolvedAndOpened()
        {
            yield return null;
            var door = Named<Door>("Door_HallToDock");
            yield return Teleport(new Vector3(0f, 0.1f, 23f));
            for (int i = 0; i < 60; i++) { _cc.Move(Vector3.forward * 0.1f); yield return null; }
            Assert.Less(_player.position.z, 26f, "a locked door must block the way");

            foreach (char c in _lock.Code) _lock.Press(c);
            _lock.Submit();
            door.Interact(_interactor);
            yield return new WaitForSeconds(1.5f);
            for (int i = 0; i < 60; i++) { _cc.Move(Vector3.forward * 0.1f); yield return null; }
            Assert.Greater(_player.position.z, 27f, "the opened door must let the player through");
        }

        // ---------- save and resume ----------

        [UnityTest]
        public IEnumerator Resume_RestoresCheckpointInventoryPuzzleAndUnlockedDoor_WithoutDuplicates()
        {
            yield return null;
            new SaveStore(_savePath).Write(new SaveData
            {
                CheckpointId = "records",
                InventoryIds = new[] { Ids.MemoId },
                SolvedPuzzleIds = new[] { "three_dates" },
            });

            _level.Begin(resume: true);
            yield return null;

            Assert.AreEqual("records", _level.Progress.CurrentId);
            Assert.Less(Flat(_player.position, Records), 0.5f);
            Assert.IsTrue(_lock.IsSolved);
            CollectionAssert.AreEqual(new[] { Ids.MemoId }, _inventory.Ids.ToArray());
            Assert.IsFalse(Named<Door>("Door_HallToRecords").IsLocked, "resuming inside the office must not lock the player in");
            Assert.IsFalse(Named<Pickup>("BrassStamp").gameObject.activeSelf, "the consumed stamp must not reappear");
        }

        [UnityTest]
        public IEnumerator Resume_WithUnknownCheckpoint_StartsAtTheEntrance()
        {
            yield return null;
            new SaveStore(_savePath).Write(new SaveData { CheckpointId = "removed_in_a_later_version" });
            _level.Begin(resume: true);
            yield return null;
            Assert.AreEqual("entrance", _level.Progress.CurrentId);
        }

        [UnityTest]
        public IEnumerator Resume_WithMissingSave_StartsFreshAndWarns()
        {
            yield return null;
            LogAssert.Expect(LogType.Warning, new Regex("could not resume"));
            _level.Begin(resume: true);
            yield return null;
            Assert.AreEqual("entrance", _level.Progress.CurrentId);
        }

        [UnityTest]
        public IEnumerator Begin_CalledTwice_DoesNotSaveTwicePerCheckpoint()
        {
            yield return null;
            _level.Begin(resume: false);
            yield return Teleport(Hall);
            Assert.AreEqual("hall", _level.Progress.CurrentId);
            Assert.AreEqual(1, Captions.Service.Count, "one checkpoint message, not one per subscription");
        }

        // ---------- HUD ----------

        [UnityTest]
        public IEnumerator PauseButton_Pauses_AndResumeRestoresTime()
        {
            yield return null;
            _hud.PauseButton.onClick.Invoke();
            Assert.IsTrue(_hud.Pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(_hud.PauseMenu.Panel.activeSelf);
            _hud.PauseMenu.ResumeButton.onClick.Invoke();
            Assert.IsFalse(_hud.Pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator JournalButton_OpensJournal_ButNotOverAnotherScreen()
        {
            yield return null;
            _hud.JournalButton.onClick.Invoke();
            Assert.IsTrue(_hud.Journal.IsOpen);
            _hud.Journal.Close();

            var gate = ModalGate.Open();   // e.g. the keypad is open
            _hud.JournalButton.onClick.Invoke();
            Assert.IsFalse(_hud.Journal.IsOpen);
            gate.Dispose();

            _hud.Pause.Pause();
            _hud.JournalButton.onClick.Invoke();
            Assert.IsFalse(_hud.Journal.IsOpen, "no journal while paused");
        }

        [UnityTest]
        public IEnumerator PauseMenu_MainMenuAndRestart_RouteToTheRightScenes()
        {
            yield return null;
            string loaded = null;
            _hud.SceneLoader = s => loaded = s;

            _hud.Pause.Pause();
            _hud.PauseMenu.MainMenuButton.onClick.Invoke();
            Assert.AreEqual(LevelHud.MenuSceneName, loaded);
            Assert.IsFalse(LevelLaunch.ContinueSave);

            _hud.PauseMenu.RestartButton.onClick.Invoke();
            Assert.AreEqual("Level_B1", loaded);
            Assert.IsTrue(LevelLaunch.ContinueSave, "restart resumes from the last checkpoint");
        }

        [UnityTest]
        public IEnumerator Interacting_WithTheKeypad_OpensIt_AndHoldsTheModalGate()
        {
            yield return null;
            var keypad = Object.FindFirstObjectByType<KeypadView>(FindObjectsInactive.Include);
            _lock.Interact(_interactor);
            Assert.IsTrue(keypad.IsOpen);
            Assert.IsTrue(ModalGate.IsOpen);
            keypad.Close();
            Assert.IsFalse(ModalGate.IsOpen, "closing the keypad must release gameplay input");
        }

        [UnityTest]
        public IEnumerator ReadingANote_OpensTheReader_AndClosingReleasesInput()
        {
            yield return null;
            var reader = Object.FindFirstObjectByType<NoteReaderView>(FindObjectsInactive.Include);
            Named<Note>("Note_" + Ids.TapeId).Interact(_interactor);
            Assert.IsTrue(reader.IsOpen);
            Assert.IsTrue(ModalGate.IsOpen);
            reader.Close();
            Assert.IsFalse(ModalGate.IsOpen);
        }
    }
}
#endif
