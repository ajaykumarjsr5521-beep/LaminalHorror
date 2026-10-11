using NUnit.Framework;
using NocturneAnnex.Core;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.EditMode
{
    public class EndingStatsTests
    {
        [Test]
        public void RunStats_CountsSecondsAndSecrets_IgnoringBadValues()
        {
            var s = new RunStats();
            s.AddSeconds(1.5f); s.AddSeconds(-3f); s.AddSeconds(float.NaN); s.AddSeconds(float.PositiveInfinity); s.AddSeconds(0.5f);
            Assert.AreEqual(2f, s.Seconds, 0.0001f);
            Assert.IsTrue(s.AddSecret("switch:s6"));
            Assert.IsFalse(s.AddSecret("switch:s6"), "once");
            Assert.IsFalse(s.AddSecret(""));
            Assert.IsFalse(s.AddSecret(null));
            Assert.AreEqual(1, s.SecretCount);
        }

        [Test]
        public void RunStats_SnapshotRestore_IsExact_AndSurvivesTheSaveFile()
        {
            var a = new RunStats();
            a.AddSeconds(125.25f); a.AddSecret("toybox:3"); a.AddSecret("misfile");
            var result = SaveSerializer.FromJson(SaveSerializer.ToJson(new SaveData { CheckpointId = "x", Stats = a.Snapshot() }));
            Assert.IsTrue(result.Ok);
            var b = new RunStats();
            b.Restore(result.Data.Stats);
            Assert.AreEqual(125.25f, b.Seconds, 0.001f);
            Assert.AreEqual(2, b.SecretCount);
            Assert.IsTrue(b.HasSecret("misfile"));
            b.Clear();
            Assert.AreEqual(0f, b.Seconds);
            Assert.AreEqual(0, b.SecretCount);
        }

        [Test]
        public void RunStats_OlderSaveWithoutStats_ReadsAsFresh()
        {
            var old = SaveSerializer.FromJson("{\"Version\":1,\"CheckpointId\":\"hall\",\"InventoryIds\":[],\"SolvedPuzzleIds\":[],\"FiredEventIds\":[]}");
            var s = new RunStats();
            s.Restore(old.Data.Stats);
            Assert.AreEqual(0f, s.Seconds);
            Assert.AreEqual(0, s.SecretCount);
            s.Restore(null);
            Assert.AreEqual(0, s.SecretCount);
        }

        [Test]
        public void HighestRoom_IsTheFurthestRoomEntered()
        {
            var v = new RoomVisits();
            Assert.AreEqual(0, v.HighestRoom);
            v.Enter(2); v.Enter(5); v.Enter(3);
            Assert.AreEqual(5, v.HighestRoom);
        }

        [TestCase(0f, "0:00")]
        [TestCase(59.9f, "0:59")]
        [TestCase(60f, "1:00")]
        [TestCase(754f, "12:34")]
        [TestCase(3600f, "1:00:00")]
        [TestCase(3725f, "1:02:05")]
        [TestCase(-5f, "0:00")]
        [TestCase(float.NaN, "0:00")]
        public void FormatTime(float seconds, string expected) => Assert.AreEqual(expected, EndingText.FormatTime(seconds));

        [Test]
        public void TheCardLines_MatchTheRunNumbers()
        {
            var lines = EndingText.Lines(new EndingStats(754f, 1, 1, 2, 10, 10, 4));
            CollectionAssert.AreEqual(new[]
            {
                "Time: 12:34", "Deaths: 1", "Lives left: 1 of 2", "Rooms: 10 of 10", "Secrets found: 4"
            }, lines);
        }
    }
}
