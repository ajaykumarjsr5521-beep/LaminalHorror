using NUnit.Framework;
using NocturneAnnex.Inventory;

namespace NocturneAnnex.Tests.EditMode
{
    public class InventoryModelTests
    {
        [Test]
        public void Add_ThenHas_IsTrue()
        {
            var inv = new InventoryModel(4);
            Assert.IsTrue(inv.TryAdd("key_a"));
            Assert.IsTrue(inv.Has("key_a"));
            Assert.AreEqual(1, inv.Count);
        }

        [Test]
        public void Add_Duplicate_IsRefused()
        {
            var inv = new InventoryModel(4);
            inv.TryAdd("key_a");
            Assert.IsFalse(inv.TryAdd("key_a"));
            Assert.AreEqual(1, inv.Count);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Add_EmptyId_IsRefused(string id) =>
            Assert.IsFalse(new InventoryModel(4).TryAdd(id));

        [Test]
        public void Add_BeyondCapacity_IsRefused()
        {
            var inv = new InventoryModel(2);
            inv.TryAdd("a");
            inv.TryAdd("b");
            Assert.IsFalse(inv.TryAdd("c"));
            Assert.IsFalse(inv.Has("c"));
        }

        [Test]
        public void Remove_ExistingItem_FreesSlot()
        {
            var inv = new InventoryModel(1);
            inv.TryAdd("a");
            Assert.IsTrue(inv.Remove("a"));
            Assert.IsFalse(inv.Has("a"));
            Assert.IsTrue(inv.TryAdd("b"));
        }

        [Test]
        public void Remove_Missing_ReturnsFalse() =>
            Assert.IsFalse(new InventoryModel(2).Remove("nope"));

        [Test]
        public void Snapshot_Restore_RoundTrips()
        {
            var a = new InventoryModel(5);
            a.TryAdd("x");
            a.TryAdd("y");
            var b = new InventoryModel(5);
            int dropped = b.Restore(a.Snapshot());
            Assert.AreEqual(0, dropped);
            CollectionAssert.AreEqual(a.Ids, b.Ids);
        }

        [Test]
        public void Restore_DropsInvalidEntriesAndReportsCount()
        {
            var inv = new InventoryModel(2);
            int dropped = inv.Restore(new[] { "a", "a", "", "b", "c" });
            Assert.AreEqual(3, dropped);   // duplicate, empty, over capacity
            CollectionAssert.AreEqual(new[] { "a", "b" }, inv.Ids);
        }

        [Test]
        public void Restore_ReplacesPreviousContents()
        {
            var inv = new InventoryModel(3);
            inv.TryAdd("old");
            inv.Restore(new[] { "new" });
            Assert.IsFalse(inv.Has("old"));
            Assert.IsTrue(inv.Has("new"));
        }
    }
}
