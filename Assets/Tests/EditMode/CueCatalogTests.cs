using NUnit.Framework;
using NocturneAnnex.Audio;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class CueCatalogTests
    {
        [Test]
        public void DefaultCatalog_IsValid_AndEveryGameplayCueHasAnExistingCaption()
        {
            var cat = CueCatalog.CreateDefault();
            CollectionAssert.IsEmpty(cat.Validate(Loc.Has));
            int relevant = 0;
            foreach (var c in cat.Cues)
                if (c.GameplayRelevant) { relevant++; Assert.IsTrue(Loc.Has(c.CaptionKey), c.Id); }
            Assert.AreEqual(7, relevant, "six events plus the checkpoint chime");
        }

        [Test]
        public void GameplayCueWithoutCaption_FailsValidation()
        {
            var cat = CueCatalog.CreateDefault();
            cat.Cues.Add(new CueDefinition("new.growl", CueCatalog.Sfx, true));
            var problems = cat.Validate(Loc.Has);
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("new.growl", problems[0]);
        }

        [Test]
        public void UnknownCaptionKey_DuplicateId_AndBadGroup_AreReported()
        {
            var cat = CueCatalog.CreateDefault();
            cat.Cues.Add(new CueDefinition("event.figure", CueCatalog.Sfx, true, "event.figure"));
            cat.Cues.Add(new CueDefinition("x.one", "Loud", false));
            cat.Cues.Add(new CueDefinition("x.two", CueCatalog.Sfx, true, "no.such.key"));
            Assert.AreEqual(3, cat.Validate(Loc.Has).Count);
        }

        [Test]
        public void Find_ReturnsTheCue_OrNull()
        {
            var cat = CueCatalog.CreateDefault();
            Assert.AreEqual("event.door_slam", cat.Find("event.door_slam").CaptionKey);
            Assert.IsNull(cat.Find("nope"));
        }
    }
}
