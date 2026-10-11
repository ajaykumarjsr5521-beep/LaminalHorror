using NUnit.Framework;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.EditMode
{
    public class RoomVisitsTests
    {
        [Test]
        public void FirstEntry_DoesNotChange_SecondEntryChangesOnce()
        {
            var v = new RoomVisits();
            Assert.IsFalse(v.Enter(4), "first visit");
            Assert.IsTrue(v.Enter(4), "second visit fires the change");
            Assert.IsFalse(v.Enter(4), "third visit: already fired");
            Assert.IsTrue(v.HasChanged(4));
            Assert.AreEqual(3, v.VisitCount(4));
        }

        [Test]
        public void RoomsAreIndependent()
        {
            var v = new RoomVisits();
            v.Enter(4); v.Enter(5);
            Assert.IsFalse(v.HasChanged(4));
            Assert.IsTrue(v.Enter(4));
            Assert.IsFalse(v.HasChanged(5));
            Assert.AreEqual(4, v.Current);
        }

        [Test]
        public void SnapshotRestore_KeepsChangesFired_SoTheyNeverFireAgain()
        {
            var a = new RoomVisits();
            a.Enter(4); a.Enter(5); a.Enter(4);   // room 4 changed
            var b = new RoomVisits();
            b.Restore(a.Snapshot());
            Assert.IsTrue(b.HasChanged(4));
            Assert.AreEqual(4, b.Current);
            Assert.IsFalse(b.Enter(4), "already fired before the save");
            Assert.AreEqual(3, b.VisitCount(4));
        }

        [Test]
        public void SurvivesTheSaveFile_AndOlderSavesStartInRoom1()
        {
            var a = new RoomVisits();
            a.Enter(2); a.Enter(2);
            var result = SaveSerializer.FromJson(SaveSerializer.ToJson(new SaveData { CheckpointId = "x", Rooms = a.Snapshot() }));
            Assert.IsTrue(result.Ok);
            var b = new RoomVisits();
            b.Restore(result.Data.Rooms);
            Assert.IsTrue(b.HasChanged(2));

            var old = SaveSerializer.FromJson("{\"Version\":1,\"CheckpointId\":\"hall\",\"InventoryIds\":[],\"SolvedPuzzleIds\":[],\"FiredEventIds\":[]}");
            var c = new RoomVisits();
            c.Restore(old.Data.Rooms);
            Assert.AreEqual(1, c.Current);
            Assert.AreEqual(0, c.VisitCount(1));
        }

        [Test]
        public void OpenedDoorsAndTakenItems_AreRememberedAcrossSaveAndLoad()
        {
            var a = new RoomVisits();
            a.MarkDoorOpened(4, "prog"); a.MarkItemTaken(4, "key_r4a");
            var b = new RoomVisits();
            b.Restore(a.Snapshot());
            Assert.IsTrue(b.IsDoorOpened(4, "prog"));
            Assert.IsFalse(b.IsDoorOpened(5, "prog"), "rooms are independent");
            Assert.IsTrue(b.IsItemTaken(4, "key_r4a"));
            b.Clear();
            Assert.IsFalse(b.IsDoorOpened(4, "prog"));
        }

        [Test]
        public void Clear_StartsOver()
        {
            var v = new RoomVisits();
            v.Enter(3); v.Enter(3);
            v.Clear();
            Assert.AreEqual(1, v.Current);
            Assert.IsFalse(v.HasChanged(3));
            Assert.AreEqual(0, v.VisitCount(3));
        }
    }
}
