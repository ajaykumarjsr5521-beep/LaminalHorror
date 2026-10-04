using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Inventory;

namespace NocturneAnnex.Tests.EditMode
{
    public class ItemDatabaseTests
    {
        static ItemDefinition Item(string id)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.Id = id;
            return i;
        }

        [Test]
        public void TryGet_FindsById_AndMissesUnknown()
        {
            var db = ScriptableObject.CreateInstance<ItemDatabase>();
            db.Items.Add(Item("brass_key"));
            Assert.IsTrue(db.TryGet("brass_key", out var found));
            Assert.AreEqual("brass_key", found.Id);
            Assert.IsFalse(db.TryGet("ghost", out _));
            Assert.IsFalse(db.TryGet(null, out _));
        }

        [Test]
        public void Validate_ReportsDuplicateEmptyAndNull()
        {
            var db = ScriptableObject.CreateInstance<ItemDatabase>();
            db.Items.Add(Item("a"));
            db.Items.Add(Item("a"));
            db.Items.Add(Item(""));
            db.Items.Add(null);
            Assert.AreEqual(3, db.Validate().Count);
        }

        [Test]
        public void Validate_CleanDatabase_IsEmpty()
        {
            var db = ScriptableObject.CreateInstance<ItemDatabase>();
            db.Items.Add(Item("a"));
            db.Items.Add(Item("b"));
            Assert.IsEmpty(db.Validate());
        }
    }
}
