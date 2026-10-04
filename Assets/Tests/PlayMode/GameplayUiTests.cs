#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;
using NocturneAnnex.Inventory;
using NocturneAnnex.Puzzle;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Drives the generated keypad, note reader and journal prefabs with real game objects.</summary>
    public class GameplayUiTests
    {
        const string KeypadPath = "Assets/_Project/Prefabs/UI/Keypad.prefab";
        const string NoteReaderPath = "Assets/_Project/Prefabs/UI/NoteReader.prefab";
        const string JournalPath = "Assets/_Project/Prefabs/UI/Journal.prefab";

        GameObject _root;

        [SetUp]
        public void SetUp()
        {
            ModalGate.Reset();
            _root = new GameObject("Canvas", typeof(Canvas));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            ModalGate.Reset();
        }

        T Spawn<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, "missing prefab " + path + ": run GameplayUiBuilder.CreateAll");
            return Object.Instantiate(prefab, _root.transform).GetComponent<T>();
        }

        CodeLock MakeLock(string code, Door door = null)
        {
            var g = new GameObject("Lock");
            g.SetActive(false);
            g.transform.SetParent(_root.transform);
            var l = g.AddComponent<CodeLock>();
            l.Code = code;
            l.TargetDoor = door;
            g.SetActive(true);
            return l;
        }

        static void Tap(Button b) => b.onClick.Invoke();

        static void TypeDigits(KeypadView k, string digits)
        {
            foreach (char c in digits) Tap(k.DigitButtons[c - '0']);
        }

        // ---------- Keypad ----------

        [Test]
        public void Keypad_StartsHidden_AndOpensWhenTheLockAsks()
        {
            var keypad = Spawn<KeypadView>(KeypadPath);
            var codeLock = MakeLock("0719");
            Assert.IsFalse(keypad.gameObject.activeSelf);
            keypad.Watch(codeLock);
            codeLock.Interact(null);
            Assert.IsTrue(keypad.IsOpen);
            Assert.IsTrue(ModalGate.IsOpen);
        }

        [Test]
        public void Keypad_ShowsTypedDigitsAndEmptySlots_AndDeleteRemovesOne()
        {
            var keypad = Spawn<KeypadView>(KeypadPath);
            keypad.Open(MakeLock("0719"));
            TypeDigits(keypad, "07");
            Assert.AreEqual("0 7 _ _", keypad.Display.text);
            Tap(keypad.BackspaceButton);
            Assert.AreEqual("0 _ _ _", keypad.Display.text);
        }

        [Test]
        public void Keypad_WrongCode_ShowsNeutralMessage_ClearsEntry_AndAllowsImmediateRetry()
        {
            var door = new GameObject("Door").AddComponent<Door>();
            door.RequiredKeyId = "never";
            var codeLock = MakeLock("0719", door);
            var keypad = Spawn<KeypadView>(KeypadPath);
            keypad.Open(codeLock);

            for (int attempt = 0; attempt < 5; attempt++)
            {
                TypeDigits(keypad, "1111");
                Tap(keypad.EnterButton);
                Assert.AreEqual(Loc.Get("keypad.incorrect"), keypad.Message.text);
                Assert.AreEqual("_ _ _ _", keypad.Display.text);
            }
            Assert.IsTrue(keypad.IsOpen, "a wrong code must never lock the player out or close the keypad");
            StringAssert.DoesNotContain("0719", keypad.Message.text);
        }

        [UnityTest]
        public IEnumerator Keypad_CorrectCode_UnlocksDoor_ShowsConfirmation_ThenClosesAndReleasesGate()
        {
            var door = new GameObject("Door").AddComponent<Door>();
            door.RequiredKeyId = "never";
            var codeLock = MakeLock("0719", door);
            var keypad = Spawn<KeypadView>(KeypadPath);
            keypad.Open(codeLock);
            TypeDigits(keypad, "0719");
            Tap(keypad.EnterButton);

            Assert.IsFalse(door.IsLocked);
            Assert.AreEqual(Loc.Get("keypad.solved"), keypad.Message.text);
            Assert.IsTrue(keypad.IsOpen, "stays up briefly so the player sees the confirmation");

            yield return new WaitForSecondsRealtime(KeypadView.SolvedCloseDelaySeconds + 0.3f);
            Assert.IsFalse(keypad.IsOpen);
            Assert.IsFalse(ModalGate.IsOpen);
        }

        [Test]
        public void Keypad_CloseButton_ReleasesGate()
        {
            var keypad = Spawn<KeypadView>(KeypadPath);
            keypad.Open(MakeLock("12"));
            Tap(keypad.CloseButton);
            Assert.IsFalse(keypad.IsOpen);
            Assert.IsFalse(ModalGate.IsOpen);
        }

        [Test]
        public void Keypad_DestroyedWhileOpen_DoesNotLeaveGateStuck()
        {
            var keypad = Spawn<KeypadView>(KeypadPath);
            keypad.Open(MakeLock("12"));
            Assert.IsTrue(ModalGate.IsOpen);
            Object.DestroyImmediate(keypad.gameObject);
            Assert.IsFalse(ModalGate.IsOpen);
        }

        // ---------- Note reader ----------

        [Test]
        public void NoteReader_ShowsTheNoteJustRead_AndHoldsGateUntilClosed()
        {
            var reader = Spawn<NoteReaderView>(NoteReaderPath);
            var note = new GameObject("Note").AddComponent<Note>();
            note.Title = "Memo";
            note.Body = "The first date is circled.";
            note.transform.SetParent(_root.transform);

            note.Interact(null);
            Assert.IsTrue(reader.IsOpen);
            Assert.AreEqual("Memo", reader.Title.text);
            Assert.AreEqual("The first date is circled.", reader.Body.text);
            Assert.IsTrue(ModalGate.IsOpen);

            Tap(reader.CloseButton);
            Assert.IsFalse(reader.IsOpen);
            Assert.IsFalse(ModalGate.IsOpen);
        }

        [Test]
        public void NoteReader_ReadingTwice_DoesNotStackTheGate()
        {
            var reader = Spawn<NoteReaderView>(NoteReaderPath);
            reader.Show("A", "a");
            reader.Show("B", "b");
            Tap(reader.CloseButton);
            Assert.IsFalse(ModalGate.IsOpen, "one close must release the gate however often it was shown");
        }

        [Test]
        public void NoteReader_DestroyedWhileOpen_DoesNotLeaveGateStuck()
        {
            var reader = Spawn<NoteReaderView>(NoteReaderPath);
            reader.Show("A", "a");
            Object.DestroyImmediate(reader.gameObject);
            Assert.IsFalse(ModalGate.IsOpen);
        }

        // ---------- Journal ----------

        PlayerInventory MakeInventory(params (string id, string title, string body, ItemKind kind)[] items)
        {
            var db = ScriptableObject.CreateInstance<ItemDatabase>();
            foreach (var (id, title, body, kind) in items)
            {
                var d = ScriptableObject.CreateInstance<ItemDefinition>();
                d.Id = id; d.DisplayName = title; d.Body = body; d.Kind = kind;
                db.Items.Add(d);
            }
            var g = new GameObject("Inventory");
            g.transform.SetParent(_root.transform);
            var inv = g.AddComponent<PlayerInventory>();
            inv.Database = db;
            foreach (var i in items) inv.TryAdd(i.id);
            return inv;
        }

        [Test]
        public void Journal_ListsOnlyNotes_AndAnyNoteIsReadableInTwoTaps()
        {
            var inv = MakeInventory(
                ("brass_key", "Brass Key", "", ItemKind.Key),
                ("memo_1", "Night Shift Memo", "Dates, not names.", ItemKind.Note),
                ("ledger_12", "Ledger Page 12", "Struck through twice.", ItemKind.Note));
            var journal = Spawn<JournalView>(JournalPath);
            journal.Bind(inv);

            journal.Open();                                         // tap 1: open the journal
            Assert.AreEqual(2, journal.Rows.Count, "the key is not a note");
            journal.Rows[1].GetComponent<Button>().onClick.Invoke(); // tap 2: tap the note
            Assert.AreEqual("Ledger Page 12", journal.ReaderTitle.text);
            Assert.AreEqual("Struck through twice.", journal.ReaderBody.text);
        }

        [Test]
        public void Journal_WithNoNotes_ShowsEmptyState()
        {
            var inv = MakeInventory(("brass_key", "Brass Key", "", ItemKind.Key));
            var journal = Spawn<JournalView>(JournalPath);
            journal.Bind(inv);
            journal.Open();
            Assert.AreEqual(0, journal.Rows.Count);
            Assert.IsTrue(journal.EmptyText.gameObject.activeSelf);
            Assert.AreEqual(Loc.Get("journal.empty"), journal.EmptyText.text);
        }

        [Test]
        public void Journal_BeforeSelecting_ShowsHint_AfterSelecting_HidesIt()
        {
            var inv = MakeInventory(("memo_1", "Memo", "Body", ItemKind.Note));
            var journal = Spawn<JournalView>(JournalPath);
            journal.Bind(inv);
            journal.Open();
            Assert.IsTrue(journal.HintText.gameObject.activeSelf);
            journal.Rows[0].GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(journal.HintText.gameObject.activeSelf);
        }

        [Test]
        public void Journal_PicksUpNewNotesOnReopen()
        {
            var inv = MakeInventory(
                ("memo_1", "Memo", "Body", ItemKind.Note),
                ("memo_2", "Second", "More", ItemKind.Note));
            inv.Restore(new[] { "memo_1" });        // the player has only found the first note so far
            var journal = Spawn<JournalView>(JournalPath);
            journal.Bind(inv);
            journal.Open();
            Assert.AreEqual(1, journal.Rows.Count);
            journal.Close();

            inv.TryAdd("memo_2");                   // finds the second note
            journal.Open();
            Assert.AreEqual(2, journal.Rows.Count);
        }

        [Test]
        public void Journal_HoldsGateWhileOpen_ReleasesOnCloseAndOnDestroy()
        {
            var inv = MakeInventory(("memo_1", "Memo", "Body", ItemKind.Note));
            var journal = Spawn<JournalView>(JournalPath);
            journal.Bind(inv);
            journal.Open();
            Assert.IsTrue(ModalGate.IsOpen);
            Tap(journal.CloseButton);
            Assert.IsFalse(ModalGate.IsOpen);

            journal.Open();
            Object.DestroyImmediate(journal.gameObject);
            Assert.IsFalse(ModalGate.IsOpen);
        }
    }
}
#endif
