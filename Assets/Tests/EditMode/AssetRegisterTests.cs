using System.Linq;
using NUnit.Framework;
using UnityEditor;
using NocturneAnnex.Editor;

namespace NocturneAnnex.Tests.EditMode
{
    public class AssetRegisterTests
    {
        const string Sample =
            "| ID | Asset | Type | Source | Licence | Commercial | Attribution | Modify | Used in | Status | Reviewed |\n" +
            "|---|---|---|---|---|---|---|---|---|---|---|\n" +
            "| AU-01 | creak.ogg | SFX | url | CC0 | Yes | No | Yes | Doors | CLEARED | me |\n" +
            "| AU-02 | drone.wav | Music | url | CC0 | Yes | No | Yes | Music | REQUIRES_REVIEW | — |\n" +
            "| AU-03 | odd.wav | SFX | url | CC0 | Yes | No | Yes | Doors | MAYBE | — |\n" +
            "Some other line | not | a row\n";

        [Test]
        public void Parse_ReadsRows_AndIgnoresEverythingElse()
        {
            var rows = AssetRegister.Parse(Sample);
            CollectionAssert.AreEqual(new[] { "AU-01", "AU-02", "AU-03" }, rows.Select(r => r.Id).ToArray());
            Assert.AreEqual("CLEARED", rows[0].Status);
            Assert.AreEqual("Music", rows[1].UsedIn);
        }

        [Test]
        public void AudioFile_NeedsAShippableRow()
        {
            var rows = AssetRegister.Parse(Sample);
            var problems = AssetRegister.AudioProblems(rows, new[] { "Assets/Audio/creak.ogg", "Assets/Audio/drone.wav", "Assets/Audio/new.wav", "Assets/Art/pic.png" });
            Assert.AreEqual(2, problems.Count);
            StringAssert.Contains("drone.wav", problems.First(p => p.Contains("drone")));
            StringAssert.Contains("no register row", problems.First(p => p.Contains("new.wav")));
        }

        [Test]
        public void StatusAndReleaseChecks_FlagAnythingNotClearedOrOriginal()
        {
            var rows = AssetRegister.Parse(Sample);
            Assert.AreEqual(1, AssetRegister.StatusProblems(rows).Count);
            Assert.AreEqual(2, AssetRegister.ReleaseProblems(rows).Count, "REQUIRES_REVIEW and the unknown status both block");
        }

        [Test]
        public void TheRealRegister_ParsesWithValidStatuses()
        {
            var rows = AssetRegister.Load();
            Assert.GreaterOrEqual(rows.Count, 4);
            CollectionAssert.IsEmpty(AssetRegister.StatusProblems(rows));
        }

        [Test]
        public void EveryAudioFileInTheProject_HasAClearedOrOriginalRegisterRow()
        {
            var files = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            CollectionAssert.IsEmpty(AssetRegister.AudioProblems(AssetRegister.Load(), files), "add a row to docs/09 for each audio file");
        }
    }
}
