using System.IO;
using NUnit.Framework;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.EditMode
{
    public class SaveStoreTests
    {
        string _dir;
        string _file;
        SaveStore _store;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "na_save_" + Path.GetRandomFileName());
            _file = Path.Combine(_dir, "save.json");
            _store = new SaveStore(_file);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        static SaveData Sample(string checkpoint) => new SaveData
        {
            CheckpointId = checkpoint,
            InventoryIds = new[] { "brass_key" },
            SolvedPuzzleIds = new[] { "records_lock" }
        };

        [Test]
        public void Load_WhenNoFile_IsMissing() =>
            Assert.AreEqual(LoadStatus.Missing, _store.Load().Status);

        [Test]
        public void WriteThenLoad_RoundTrips_AndCreatesDirectory()
        {
            Assert.IsTrue(_store.Write(Sample("lamp_1")).Ok);
            var r = _store.Load();
            Assert.IsTrue(r.Ok);
            Assert.AreEqual("lamp_1", r.Data.CheckpointId);
            Assert.AreEqual("brass_key", r.Data.InventoryIds[0]);
            Assert.IsFalse(string.IsNullOrEmpty(r.Data.SavedAtUtc));
        }

        [Test]
        public void Overwrite_ReplacesPreviousSave_AndLeavesNoTempFile()
        {
            _store.Write(Sample("lamp_1"));
            _store.Write(Sample("lamp_2"));
            Assert.AreEqual("lamp_2", _store.Load().Data.CheckpointId);
            Assert.IsFalse(File.Exists(_file + ".tmp"));
        }

        [Test]
        public void InterruptedWrite_LeftoverTempFile_DoesNotAffectExistingSave()
        {
            _store.Write(Sample("lamp_1"));
            File.WriteAllText(_file + ".tmp", "{\"Version\":1,\"Check");   // simulated crash mid-write
            var r = _store.Load();
            Assert.IsTrue(r.Ok);
            Assert.AreEqual("lamp_1", r.Data.CheckpointId);
            Assert.IsTrue(_store.Write(Sample("lamp_2")).Ok, "next save must still succeed");
            Assert.AreEqual("lamp_2", _store.Load().Data.CheckpointId);
        }

        [Test]
        public void InterruptedFirstWrite_LeavesNoSave_NotACorruptOne()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file + ".tmp", "{\"Ver");
            Assert.AreEqual(LoadStatus.Missing, _store.Load().Status);
        }

        [Test]
        public void FailedWrite_ReportsError_AndKeepsExistingSave()
        {
            _store.Write(Sample("lamp_1"));
            Directory.CreateDirectory(_file + ".tmp");   // a directory in the temp file's place forces an IO failure
            var w = _store.Write(Sample("lamp_2"));
            Assert.IsFalse(w.Ok);
            StringAssert.Contains("previous save was kept", w.Error);
            Assert.AreEqual("lamp_1", _store.Load().Data.CheckpointId);
        }

        [Test]
        public void CorruptFile_IsReported_AndPreservedOnDisk()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file, "garbage");
            var r = _store.Load();
            Assert.AreEqual(LoadStatus.Corrupt, r.Status);
            Assert.IsNotEmpty(r.Message);
            Assert.AreEqual("garbage", File.ReadAllText(_file), "corrupt file must not be touched by Load");
        }

        [Test]
        public void Quarantine_MovesCorruptFileAside_WithoutDeletingIt()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file, "garbage");
            var moved = _store.Quarantine();
            Assert.IsNotNull(moved);
            Assert.IsFalse(File.Exists(_file));
            Assert.AreEqual("garbage", File.ReadAllText(moved));
            Assert.AreEqual(LoadStatus.Missing, _store.Load().Status);
        }

        [Test]
        public void Quarantine_WithNoFile_ReturnsNull() =>
            Assert.IsNull(_store.Quarantine());

        [Test]
        public void Delete_RemovesSave_AndIsSafeWhenAbsent()
        {
            _store.Write(Sample("lamp_1"));
            Assert.IsTrue(_store.Delete());
            Assert.IsFalse(_store.Exists);
            Assert.IsTrue(_store.Delete());
        }
    }
}
