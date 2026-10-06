using System.Linq;
using NUnit.Framework;
using UnityEditor;
using NocturneAnnex.Audio;
using NocturneAnnex.Core;
using NocturneAnnex.Editor;

namespace NocturneAnnex.Tests.EditMode
{
    /// <summary>The authored catalog asset itself (not the code default): valid, every cue has a clip, every clip is in the register.</summary>
    public class CueCatalogAssetTests
    {
        static CueCatalog Catalog => AssetDatabase.LoadAssetAtPath<CueCatalog>(LevelAudio.CatalogPath);

        [Test]
        public void CatalogAsset_IsValid_AndHasEveryDefaultCue()
        {
            Assert.IsNotNull(Catalog, "run Build > Create Level B1 Scene");
            CollectionAssert.IsEmpty(Catalog.Validate(Loc.Has));
            foreach (var cue in CueCatalog.CreateDefault().Cues) Assert.IsNotNull(Catalog.Find(cue.Id), cue.Id);
        }

        [Test]
        public void EveryCue_HasAtLeastOneClip()
        {
            var empty = Catalog.Cues.Where(c => c.Clips == null || c.Clips.Length == 0 || c.Clips.Any(x => x == null)).Select(c => c.Id).ToList();
            CollectionAssert.IsEmpty(empty, "cues without clips: " + string.Join(", ", empty));
        }

        [Test]
        public void EveryClipInTheCatalog_IsInTheAssetRegister()
        {
            var paths = Catalog.Cues.SelectMany(c => c.Clips).Distinct().Select(AssetDatabase.GetAssetPath).ToList();
            CollectionAssert.IsEmpty(AssetRegister.AudioProblems(AssetRegister.Load(), paths));
        }
    }
}
