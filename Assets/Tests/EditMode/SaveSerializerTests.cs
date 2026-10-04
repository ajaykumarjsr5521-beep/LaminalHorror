using NUnit.Framework;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.EditMode
{
    public class SaveSerializerTests
    {
        [Test]
        public void RoundTrip_KeepsAllFields()
        {
            var d = new SaveData
            {
                CheckpointId = "lamp_room_2",
                InventoryIds = new[] { "brass_key", "memo_1" },
                SolvedPuzzleIds = new[] { "records_lock" },
                SavedAtUtc = "2026-10-04T10:00:00Z"
            };
            var r = SaveSerializer.FromJson(SaveSerializer.ToJson(d));
            Assert.IsTrue(r.Ok);
            Assert.AreEqual("lamp_room_2", r.Data.CheckpointId);
            CollectionAssert.AreEqual(d.InventoryIds, r.Data.InventoryIds);
            CollectionAssert.AreEqual(d.SolvedPuzzleIds, r.Data.SolvedPuzzleIds);
            Assert.AreEqual(SaveData.CurrentVersion, r.Data.Version);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not json at all")]
        [TestCase("{\"Version\":")]
        [TestCase("{}")]
        [TestCase("[1,2,3]")]
        public void BadContent_IsCorrupt_WithMessage(string json)
        {
            var r = SaveSerializer.FromJson(json);
            Assert.AreEqual(LoadStatus.Corrupt, r.Status);
            Assert.IsNull(r.Data);
            Assert.IsNotEmpty(r.Message);
        }

        [Test]
        public void NewerVersion_IsRefused_NotParsedBlindly()
        {
            var r = SaveSerializer.FromJson("{\"Version\":99,\"CheckpointId\":\"x\"}");
            Assert.AreEqual(LoadStatus.UnsupportedVersion, r.Status);
            Assert.IsNull(r.Data);
            StringAssert.Contains("newer", r.Message);
        }

        [Test]
        public void OlderVersion_IsReportedUnsupported_UntilMigrationExists()
        {
            // CurrentVersion is 1, so there is no older valid version; this guards the branch for the future.
            if (SaveData.CurrentVersion == 1) Assert.Pass("no older version exists yet");
            var r = SaveSerializer.FromJson("{\"Version\":1}");
            Assert.AreEqual(LoadStatus.UnsupportedVersion, r.Status);
        }

        [Test]
        public void MissingArrays_BecomeEmpty_NotNull()
        {
            var r = SaveSerializer.FromJson("{\"Version\":1,\"CheckpointId\":\"a\"}");
            Assert.IsTrue(r.Ok);
            Assert.IsNotNull(r.Data.InventoryIds);
            Assert.IsNotNull(r.Data.SolvedPuzzleIds);
        }
    }
}
