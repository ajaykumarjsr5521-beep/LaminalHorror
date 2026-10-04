using System.IO;
using NUnit.Framework;
using NocturneAnnex.Core;
using NocturneAnnex.Settings;

namespace NocturneAnnex.Tests.EditMode
{
    public class SettingsStoreTests
    {
        string _dir;
        string _file;
        SettingsStore _store;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "na_settings_" + Path.GetRandomFileName());
            _file = Path.Combine(_dir, "settings.json");
            _store = new SettingsStore(_file, 3);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void MissingFile_IsFirstRun_WithDefaults_AndNoWarning()
        {
            var r = _store.Load();
            Assert.AreEqual(SettingsLoadStatus.FirstRun, r.Status);
            Assert.IsNull(r.Warning);
            Assert.AreEqual(1f, r.Data.LookSensitivity);
        }

        [Test]
        public void WriteThenLoad_RoundTripsEveryField()
        {
            var s = new SettingsData
            {
                LookSensitivity = 1.7f, InvertY = true, MasterVolume = 0.5f, MusicVolume = 0.3f, SfxVolume = 0.9f,
                CaptionsEnabled = false, TouchControlsScale = 1.2f, StoryMode = true, QualityLevel = 1
            };
            Assert.IsTrue(_store.Write(s).Ok);
            var r = _store.Load();
            Assert.AreEqual(SettingsLoadStatus.Ok, r.Status);
            Assert.AreEqual(1.7f, r.Data.LookSensitivity, 1e-4f);
            Assert.IsTrue(r.Data.InvertY);
            Assert.AreEqual(0.5f, r.Data.MasterVolume, 1e-4f);
            Assert.AreEqual(0.3f, r.Data.MusicVolume, 1e-4f);
            Assert.AreEqual(0.9f, r.Data.SfxVolume, 1e-4f);
            Assert.IsFalse(r.Data.CaptionsEnabled);
            Assert.AreEqual(1.2f, r.Data.TouchControlsScale, 1e-4f);
            Assert.IsTrue(r.Data.StoryMode);
            Assert.AreEqual(1, r.Data.QualityLevel);
        }

        [Test]
        public void Write_ClampsOutOfRangeValuesBeforeSaving()
        {
            _store.Write(new SettingsData { LookSensitivity = 99f, QualityLevel = 40 });
            var r = _store.Load();
            Assert.AreEqual(SettingsData.MaxSensitivity, r.Data.LookSensitivity);
            Assert.AreEqual(-1, r.Data.QualityLevel);
        }

        [Test]
        public void HandEditedOutOfRangeFile_IsClampedOnLoad()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file, "{\"Version\":1,\"LookSensitivity\":-4,\"MasterVolume\":9}");
            var r = _store.Load();
            Assert.AreEqual(SettingsLoadStatus.Ok, r.Status);
            Assert.AreEqual(SettingsData.MinSensitivity, r.Data.LookSensitivity);
            Assert.AreEqual(1f, r.Data.MasterVolume);
        }

        [Test]
        public void FileFromEarlierBuild_WithoutAccessibilityFields_LoadsDefaultsForThem()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file, "{\"Version\":1,\"LookSensitivity\":2}");
            var r = _store.Load();
            Assert.AreEqual(SettingsLoadStatus.Ok, r.Status);
            Assert.AreEqual(2f, r.Data.LookSensitivity, 1e-4f);
            Assert.AreEqual(1, r.Data.TextSize);
            Assert.IsFalse(r.Data.ReduceFlicker);
            Assert.IsFalse(r.Data.ReduceMotion);
        }

        [Test]
        public void AccessibilityFields_RoundTrip()
        {
            _store.Write(new SettingsData { TextSize = 2, ReduceFlicker = true, ReduceMotion = true });
            var r = _store.Load();
            Assert.AreEqual(2, r.Data.TextSize);
            Assert.IsTrue(r.Data.ReduceFlicker);
            Assert.IsTrue(r.Data.ReduceMotion);
        }

        [TestCase("garbage")]
        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"Version\":")]
        public void CorruptFile_GivesDefaultsAndWarning_AndFileIsKept(string content)
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file, content);
            var r = _store.Load();
            Assert.AreEqual(SettingsLoadStatus.Corrupt, r.Status);
            Assert.AreEqual(Loc.Get("settings.corrupt"), r.Warning);
            Assert.AreEqual(1f, r.Data.LookSensitivity);
            Assert.AreEqual(content, File.ReadAllText(_file), "bad file must not be modified");
        }

        [Test]
        public void NewerVersionFile_GivesDefaultsAndWarning_AndFileIsKept()
        {
            Directory.CreateDirectory(_dir);
            const string content = "{\"Version\":99,\"LookSensitivity\":2}";
            File.WriteAllText(_file, content);
            var r = _store.Load();
            Assert.AreEqual(SettingsLoadStatus.UnsupportedVersion, r.Status);
            Assert.AreEqual(Loc.Get("settings.newer"), r.Warning);
            Assert.AreEqual(1f, r.Data.LookSensitivity);
            Assert.AreEqual(content, File.ReadAllText(_file));
        }

        [Test]
        public void FailedWrite_ReturnsMessage_AndKeepsExistingFile()
        {
            _store.Write(new SettingsData { LookSensitivity = 2f });
            Directory.CreateDirectory(AtomicFile.TempPathFor(_file));   // forces an IO failure
            var w = _store.Write(new SettingsData { LookSensitivity = 3f });
            Assert.IsFalse(w.Ok);
            Assert.IsNotEmpty(w.Error);
            Assert.AreEqual(2f, _store.Load().Data.LookSensitivity, 1e-4f);
        }
    }
}
