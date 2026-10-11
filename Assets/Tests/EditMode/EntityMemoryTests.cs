using NUnit.Framework;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.EditMode
{
    public class EntityMemoryTests
    {
        static DeathRecord Death(string spot = "", string checkpoint = "start") => new DeathRecord { HidingSpot = spot, Checkpoint = checkpoint };

        [Test]
        public void SnapshotRestore_IsExact()
        {
            var a = new EntityMemory();
            a.RecordHide("Cupboard_Hall"); a.RecordHide("Cupboard_Hall"); a.RecordHide("Cupboard_Stacks");
            a.RecordDeath(Death("Cupboard_Hall", "records")); a.RecordRun(); a.RecordThrow();
            var b = new EntityMemory();
            b.Restore(a.Snapshot());
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(a.Snapshot()), UnityEngine.JsonUtility.ToJson(b.Snapshot()));
            Assert.AreEqual(a.Level, b.Level);
            Assert.AreEqual(a.HideBonus("Cupboard_Hall", 0.25f), b.HideBonus("Cupboard_Hall", 0.25f));
        }

        [Test]
        public void Restore_Null_IsEmpty()
        {
            var m = new EntityMemory();
            m.RecordDeath(Death());
            m.Restore(null);
            Assert.AreEqual(0, m.Deaths);
            Assert.AreEqual(0, m.Level);
        }

        [Test]
        public void Level_RisesOnlyThroughRecordedEvents_AndNeverFalls()
        {
            var m = new EntityMemory();
            Assert.AreEqual(0, m.Level);
            int last = 0;
            for (int i = 0; i < 20; i++)
            {
                m.RecordHide("s"); m.RecordDeath(Death());
                Assert.GreaterOrEqual(m.Level, last);
                last = m.Level;
            }
            Assert.AreEqual(EntityMemory.MaxLevel, m.Level);
            Assert.AreEqual(last, m.Level, "no event, no change");
        }

        [Test]
        public void HideBonus_PastDeathSpotIsHigher_ButCappedBelowCertainty()
        {
            var m = new EntityMemory();
            m.RecordHide("A"); m.RecordHide("B");
            m.RecordDeath(Death("A"));
            Assert.Greater(m.HideBonus("A", 0.25f), m.HideBonus("B", 0.25f));
            Assert.Greater(m.HideBonus("B", 0.25f), 0f);
            for (int i = 0; i < 50; i++) { m.RecordHide("A"); m.RecordDeath(Death("A")); }
            Assert.AreEqual(0.25f, m.HideBonus("A", 0.25f), 0.0001f, "capped");
            Assert.AreEqual(0f, m.HideBonus("never-seen-spot-with-zero-level", 0f));
        }

        [Test]
        public void HideBonus_UnknownSpot_OnlyGetsTheLevelPart()
        {
            var m = new EntityMemory();
            m.RecordDeath(Death("A")); m.RecordDeath(Death("A"));   // level 2
            Assert.AreEqual(0.04f * m.Level, m.HideBonus("Z", 0.25f), 0.0001f);
        }

        [Test]
        public void Lines_RespectTheRateLimit_TheLevelGate_AndTheSetting()
        {
            var m = new EntityMemory();
            Assert.IsNull(m.NextLine(0f, true), "level 0");
            m.RecordDeath(Death()); m.RecordDeath(Death());   // 4 points = level 2
            Assert.IsNotNull(m.NextLine(10f, true));
            Assert.IsNull(m.NextLine(300f, true), "inside 10 minutes");
            Assert.IsNull(m.NextLine(10f + EntityMemory.LineIntervalSeconds, false), "lines off");
            Assert.IsNotNull(m.NextLine(10f + EntityMemory.LineIntervalSeconds, true));
        }

        [Test]
        public void Clear_ForgetsEverything()
        {
            var m = new EntityMemory();
            m.RecordDeath(Death("A")); m.RecordHide("A");
            m.Clear();
            Assert.AreEqual(0, m.Deaths);
            Assert.AreEqual(0f, m.HideBonus("A", 0.25f));
        }

        [Test]
        public void Memory_SurvivesTheSaveFile()
        {
            var a = new EntityMemory();
            a.RecordHide("Cupboard_Hall"); a.RecordDeath(Death("Cupboard_Hall", "records")); a.RecordThrow();
            var result = SaveSerializer.FromJson(SaveSerializer.ToJson(new SaveData { CheckpointId = "records", Memory = a.Snapshot() }));
            Assert.IsTrue(result.Ok);
            var b = new EntityMemory();
            b.Restore(result.Data.Memory);
            Assert.AreEqual(a.Level, b.Level);
            Assert.AreEqual(a.HideBonus("Cupboard_Hall", 0.25f), b.HideBonus("Cupboard_Hall", 0.25f), 0.0001f);
        }

        [Test]
        public void OlderSaveWithoutMemory_ReadsAsEmpty()
        {
            const string old = "{\"Version\":1,\"CheckpointId\":\"hall\",\"InventoryIds\":[],\"SolvedPuzzleIds\":[],\"FiredEventIds\":[]}";
            var result = SaveSerializer.FromJson(old);
            Assert.IsTrue(result.Ok);
            var m = new EntityMemory();
            m.Restore(result.Data.Memory);
            Assert.AreEqual(0, m.Level);
        }
    }
}
