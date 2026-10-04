using System;
using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.EditMode
{
    public class JournalModelTests
    {
        List<JournalEntry> _entries;

        [SetUp]
        public void SetUp() => _entries = new List<JournalEntry>();

        JournalModel Make() => new JournalModel(() => _entries);

        static JournalEntry E(string id) => new JournalEntry(id, "Title " + id, "Body " + id);

        [Test]
        public void Empty_ReportsEmpty_AndNothingSelected()
        {
            var m = Make();
            Assert.IsTrue(m.IsEmpty);
            Assert.IsFalse(m.HasSelection);
            Assert.AreEqual(-1, m.SelectedIndex);
        }

        [Test]
        public void Entries_KeepSourceOrder()
        {
            _entries.Add(E("a"));
            _entries.Add(E("b"));
            var m = Make();
            Assert.AreEqual("a", m.Entries[0].Id);
            Assert.AreEqual("b", m.Entries[1].Id);
        }

        [Test]
        public void Select_ValidIndex_ExposesThatNote()
        {
            _entries.Add(E("a"));
            _entries.Add(E("b"));
            var m = Make();
            Assert.IsTrue(m.Select(1));
            Assert.AreEqual("Body b", m.Selected.Body);
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(99)]
        public void Select_OutOfRange_IsRefused_AndKeepsPreviousSelection(int index)
        {
            _entries.Add(E("a"));
            _entries.Add(E("b"));
            var m = Make();
            m.Select(0);
            Assert.IsFalse(m.Select(index));
            Assert.AreEqual(0, m.SelectedIndex);
        }

        [Test]
        public void SelectById_FindsNote_AndMissReturnsFalse()
        {
            _entries.Add(E("a"));
            _entries.Add(E("b"));
            var m = Make();
            Assert.IsTrue(m.SelectById("b"));
            Assert.AreEqual(1, m.SelectedIndex);
            Assert.IsFalse(m.SelectById("ghost"));
        }

        [Test]
        public void Refresh_PicksUpNewNotes_AndKeepsSelectionOfTheSameNote()
        {
            _entries.Add(E("b"));
            var m = Make();
            m.SelectById("b");
            _entries.Insert(0, E("a"));      // order changed: "b" is now index 1
            m.Refresh();
            Assert.AreEqual(2, m.Entries.Count);
            Assert.AreEqual("b", m.Selected.Id);
        }

        [Test]
        public void Refresh_DropsSelection_WhenTheNoteIsGone()
        {
            _entries.Add(E("a"));
            var m = Make();
            m.Select(0);
            _entries.Clear();
            m.Refresh();
            Assert.IsFalse(m.HasSelection);
            Assert.IsTrue(m.IsEmpty);
        }

        [Test]
        public void ClearSelection_Works()
        {
            _entries.Add(E("a"));
            var m = Make();
            m.Select(0);
            m.ClearSelection();
            Assert.IsFalse(m.HasSelection);
        }

        [Test]
        public void NullSource_IsRejected_AndNullResultTreatedAsEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => new JournalModel(null));
            var m = new JournalModel(() => null);
            Assert.IsTrue(m.IsEmpty);
        }
    }
}
