using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Interaction;
using NocturneAnnex.Inventory;

namespace NocturneAnnex.Tests.PlayMode
{
    public class PlayerInventoryTests
    {
        GameObject _root;
        ItemDatabase _db;
        PlayerInventory _inv;
        Interactor _interactor;

        static ItemDefinition Def(string id, ItemKind kind, string name = "x", string body = "")
        {
            var d = ScriptableObject.CreateInstance<ItemDefinition>();
            d.Id = id; d.Kind = kind; d.DisplayName = name; d.Body = body;
            return d;
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Player");
            _db = ScriptableObject.CreateInstance<ItemDatabase>();
            _db.Items.Add(Def("brass_key", ItemKind.Key, "Brass Key"));
            _db.Items.Add(Def("memo_1", ItemKind.Note, "Memo", "Order of the dates..."));
            _db.Items.Add(Def("photo", ItemKind.Collectible));
            _inv = _root.AddComponent<PlayerInventory>();   // must exist before Interactor.Awake
            _inv.Database = _db;
            _interactor = _root.AddComponent<Interactor>();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_root);

        [Test]
        public void KnownItem_IsAdded_AndChangedFires()
        {
            int changed = 0;
            _inv.Changed += () => changed++;
            Assert.IsTrue(_inv.TryAdd("photo"));
            Assert.IsTrue(_inv.Has("photo"));
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void UnknownItem_IsRefusedAndLogged()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("unknown item id 'ghost'"));
            Assert.IsFalse(_inv.TryAdd("ghost"));
            Assert.AreEqual(0, _inv.Ids.Count);
        }

        [Test]
        public void HasKey_IsFalseForNonKeyItems()
        {
            _inv.TryAdd("photo");
            Assert.IsFalse(_inv.HasKey("photo"));
        }

        [Test]
        public void PickupKey_ThenOpenLockedDoor_ConsumesKey()
        {
            var pg = new GameObject("KeyPickup");
            pg.transform.SetParent(_root.transform);
            var pickup = pg.AddComponent<Pickup>();
            pickup.ItemId = "brass_key";

            var dg = new GameObject("Door");
            dg.SetActive(false);
            dg.transform.SetParent(_root.transform);
            var door = dg.AddComponent<Door>();
            door.RequiredKeyId = "brass_key";
            dg.SetActive(true);

            door.Interact(_interactor);
            Assert.IsTrue(door.IsLocked, "locked before the key is found");

            pickup.Interact(_interactor);
            Assert.IsTrue(_inv.HasKey("brass_key"));

            door.Interact(_interactor);
            Assert.IsTrue(door.IsOpen);
            Assert.IsFalse(_inv.Has("brass_key"), "used key must be removed");
        }

        [Test]
        public void Journal_ListsOnlyNotes_InPickupOrder()
        {
            _inv.TryAdd("photo");
            _inv.TryAdd("memo_1");
            var notes = _inv.GetJournalNotes();
            Assert.AreEqual(1, notes.Count);
            Assert.AreEqual("memo_1", notes[0].Id);
            Assert.AreEqual("Order of the dates...", notes[0].Body);
        }

        [Test]
        public void ReadingNote_AddsItToJournal_AndRereadingStillOpens()
        {
            var ng = new GameObject("Note");
            ng.transform.SetParent(_root.transform);
            var note = ng.AddComponent<Note>();
            note.ItemId = "memo_1";
            int opened = 0;
            void H(Note n) => opened++;
            Note.Opened += H;
            try
            {
                note.Interact(_interactor);
                note.Interact(_interactor);   // second read: already held, must still open
            }
            finally { Note.Opened -= H; }
            Assert.AreEqual(2, opened);
            Assert.AreEqual(1, _inv.GetJournalNotes().Count);
        }

        [Test]
        public void Notes_RemainReadableAfterOtherItemsAreUsed()
        {
            _inv.TryAdd("brass_key");
            _inv.TryAdd("memo_1");
            _inv.ConsumeKey("brass_key");
            Assert.AreEqual(1, _inv.GetJournalNotes().Count);
        }

        [Test]
        public void Snapshot_Restore_RoundTripsThroughAnotherInventory()
        {
            _inv.TryAdd("brass_key");
            _inv.TryAdd("memo_1");
            var other = new GameObject("Other").AddComponent<PlayerInventory>();
            other.Database = _db;
            other.Restore(_inv.Snapshot());
            CollectionAssert.AreEqual(_inv.Ids, other.Ids);
            Object.Destroy(other.gameObject);
        }

        [Test]
        public void Restore_DropsUnknownIds_AndLogsWarning()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("dropped 1 invalid or unknown"));
            _inv.Restore(new[] { "brass_key", "removed_item_from_old_build" });
            CollectionAssert.AreEqual(new[] { "brass_key" }, _inv.Ids);
        }

        [Test]
        public void Capacity_IsEnforced()
        {
            _inv.Capacity = 1;   // model is created lazily on first use
            Assert.IsTrue(_inv.TryAdd("photo"));
            Assert.IsFalse(_inv.TryAdd("memo_1"));
        }
    }
}
