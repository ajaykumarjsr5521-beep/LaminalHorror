using System;
using System.IO;
using NUnit.Framework;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class AtomicFileTests
    {
        string _dir;
        string _file;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "na_atomic_" + Path.GetRandomFileName());
            _file = Path.Combine(_dir, "data.txt");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void Write_CreatesDirectoryAndFile_AndLeavesNoTemp()
        {
            AtomicFile.Write(_file, "hello");
            Assert.AreEqual("hello", File.ReadAllText(_file));
            Assert.IsFalse(File.Exists(AtomicFile.TempPathFor(_file)));
        }

        [Test]
        public void Write_Overwrites_ExistingContent()
        {
            AtomicFile.Write(_file, "one");
            AtomicFile.Write(_file, "two");
            Assert.AreEqual("two", File.ReadAllText(_file));
        }

        [Test]
        public void FailedVerification_Throws_AndKeepsExistingFile()
        {
            AtomicFile.Write(_file, "good");
            Assert.Throws<InvalidDataException>(() => AtomicFile.Write(_file, "bad", _ => false));
            Assert.AreEqual("good", File.ReadAllText(_file));
        }

        [Test]
        public void VerifyReceivesWhatWasActuallyWritten()
        {
            string seen = null;
            AtomicFile.Write(_file, "payload", c => { seen = c; return true; });
            Assert.AreEqual("payload", seen);
        }

        [Test]
        public void IoFailure_Throws_AndKeepsExistingFile()
        {
            AtomicFile.Write(_file, "good");
            Directory.CreateDirectory(AtomicFile.TempPathFor(_file));   // a directory where the temp file must go
            Assert.Catch<Exception>(() => AtomicFile.Write(_file, "new"));
            Assert.AreEqual("good", File.ReadAllText(_file));
        }

        [Test]
        public void LeftoverTempFromEarlierCrash_DoesNotBlockNextWrite()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(AtomicFile.TempPathFor(_file), "half-writ");
            AtomicFile.Write(_file, "complete");
            Assert.AreEqual("complete", File.ReadAllText(_file));
        }
    }
}
