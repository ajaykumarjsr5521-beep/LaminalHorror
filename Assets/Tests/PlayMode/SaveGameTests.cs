using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Inventory;
using NocturneAnnex.Puzzle;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.PlayMode
{
    public class SaveGameTests
    {
        GameObject _root;
        string _dir;
        ItemDatabase _db;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Root");
            _dir = Path.Combine(Path.GetTempPath(), "na_savegame_" + Path.GetRandomFileName());
            _db = ScriptableObject.CreateInstance<ItemDatabase>();
            foreach (var id in new[] { "brass_key", "memo_1" })
            {
                var d = ScriptableObject.CreateInstance<ItemDefinition>();
                d.Id = id;
                d.Kind = id == "brass_key" ? ItemKind.Key : ItemKind.Note;
                _db.Items.Add(d);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        class World
        {
            public SaveGame Save;
            public PlayerInventory Inv;
            public CodeLock Lock;
        }

        World Build(string lockId = "records_lock")
        {
            var w = new World();
            var g = new GameObject("World");
            g.SetActive(false);
            g.transform.SetParent(_root.transform);
            w.Inv = g.AddComponent<PlayerInventory>();
            w.Inv.Database = _db;
            var lg = new GameObject("Lock");
            lg.SetActive(false);
            lg.transform.SetParent(_root.transform);
            w.Lock = lg.AddComponent<CodeLock>();
            w.Lock.Code = "0719";
            w.Lock.PuzzleId = lockId;
            w.Save = g.AddComponent<SaveGame>();
            w.Save.Inventory = w.Inv;
            w.Save.Locks = new[] { w.Lock };
            w.Save.FilePathOverride = Path.Combine(_dir, "save.json");
            lg.SetActive(true);
            g.SetActive(true);
            return w;
        }

        [Test]
        public void SaveThenContinue_RestoresCheckpointInventoryAndSolvedPuzzle()
        {
            var a = Build();
            a.Inv.TryAdd("brass_key");
            a.Inv.TryAdd("memo_1");
            foreach (char c in "0719") a.Lock.Press(c);
            a.Lock.Submit();
            Assert.IsTrue(a.Save.SaveCheckpoint("lamp_room_2"));

            var b = Build();   // fresh session: nothing solved, empty inventory
            Assert.IsFalse(b.Lock.IsSolved);
            var r = b.Save.Continue();
            Assert.IsTrue(r.Ok);
            Assert.AreEqual("lamp_room_2", b.Save.LastCheckpointId);
            CollectionAssert.AreEquivalent(new[] { "brass_key", "memo_1" }, b.Inv.Ids);
            Assert.IsTrue(b.Lock.IsSolved);
        }

        [Test]
        public void Continue_WithNoSave_ReportsMissing_AndChangesNothing()
        {
            var w = Build();
            w.Inv.TryAdd("memo_1");
            var r = w.Save.Continue();
            Assert.AreEqual(LoadStatus.Missing, r.Status);
            Assert.IsTrue(w.Inv.Has("memo_1"));
        }

        [Test]
        public void Continue_WithCorruptSave_ReportsCorrupt_KeepsFile_AndChangesNothing()
        {
            var w = Build();
            Directory.CreateDirectory(_dir);
            File.WriteAllText(w.Save.Store.Path, "garbage");
            w.Inv.TryAdd("memo_1");
            var r = w.Save.Continue();
            Assert.AreEqual(LoadStatus.Corrupt, r.Status);
            Assert.IsTrue(File.Exists(w.Save.Store.Path));
            Assert.IsTrue(w.Inv.Has("memo_1"));
        }

        [Test]
        public void SaveFailure_RaisesSaveFailed_WithMessage()
        {
            var w = Build();
            Directory.CreateDirectory(w.Save.Store.Path + ".tmp");   // forces an IO error
            string msg = null;
            w.Save.SaveFailed += m => msg = m;
            Assert.IsFalse(w.Save.SaveCheckpoint("lamp_1"));
            Assert.IsNotNull(msg);
        }

        [Test]
        public void StartNewGame_DeletesHealthySave()
        {
            var w = Build();
            w.Save.SaveCheckpoint("lamp_1");
            Assert.IsTrue(w.Save.StartNewGame());
            Assert.AreEqual(LoadStatus.Missing, w.Save.Store.Load().Status);
        }

        [Test]
        public void StartNewGame_OnCorruptSave_PreservesItAside()
        {
            var w = Build();
            Directory.CreateDirectory(_dir);
            File.WriteAllText(w.Save.Store.Path, "garbage");
            Assert.IsTrue(w.Save.StartNewGame());
            Assert.IsFalse(File.Exists(w.Save.Store.Path));
            Assert.AreEqual(1, Directory.GetFiles(_dir, "save.json.corrupt-*").Length);
        }

        [Test]
        public void MissingPuzzleId_IsLoggedAsError()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("has no PuzzleId"));
            Build(lockId: "");
        }
    }
}
