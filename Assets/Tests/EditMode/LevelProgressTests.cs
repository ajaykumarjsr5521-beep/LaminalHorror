using System;
using NUnit.Framework;
using NocturneAnnex.Flow;

namespace NocturneAnnex.Tests.EditMode
{
    public class LevelProgressTests
    {
        static LevelProgress Make() => new LevelProgress(new[] { "entrance", "hall", "records" });

        [Test]
        public void StartsAtFirstCheckpoint() => Assert.AreEqual("entrance", Make().CurrentId);

        [Test]
        public void ReachingLaterCheckpointAdvances()
        {
            var p = Make();
            Assert.IsTrue(p.TryReach("hall"));
            Assert.AreEqual("hall", p.CurrentId);
        }

        [Test]
        public void ReachingEarlierOrSameCheckpointDoesNotRollBack()
        {
            var p = Make();
            p.TryReach("records");
            Assert.IsFalse(p.TryReach("entrance"));
            Assert.IsFalse(p.TryReach("records"));
            Assert.AreEqual("records", p.CurrentId);
        }

        [Test]
        public void SkippingAheadIsAllowed()
        {
            var p = Make();
            Assert.IsTrue(p.TryReach("records"));
            Assert.IsTrue(p.HasReached("hall"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("nowhere")]
        public void UnknownIdsChangeNothing(string id)
        {
            var p = Make();
            p.TryReach("hall");
            Assert.IsFalse(p.TryReach(id));
            Assert.AreEqual("hall", p.CurrentId);
        }

        [Test]
        public void RestoreSetsSavedCheckpoint()
        {
            var p = Make();
            p.Restore("records");
            Assert.AreEqual("records", p.CurrentId);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("removed_in_later_version")]
        public void RestoreWithMissingOrUnknownIdFallsBackToStart(string id)
        {
            var p = Make();
            p.TryReach("records");
            p.Restore(id);
            Assert.AreEqual("entrance", p.CurrentId);
        }

        [Test]
        public void HasReachedIsFalseForLaterAndUnknown()
        {
            var p = Make();
            Assert.IsTrue(p.HasReached("entrance"));
            Assert.IsFalse(p.HasReached("hall"));
            Assert.IsFalse(p.HasReached("nowhere"));
        }

        [Test]
        public void ExitNeedsThePuzzleNotJustProgress()
        {
            var p = Make();
            p.TryReach("records");
            Assert.IsFalse(LevelProgress.CanExit(false));
            Assert.IsTrue(LevelProgress.CanExit(true));
        }

        [Test]
        public void BadConfigIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new LevelProgress(new string[0]));
            Assert.Throws<ArgumentException>(() => new LevelProgress(new[] { "a", "a" }));
            Assert.Throws<ArgumentException>(() => new LevelProgress(new[] { "a", "" }));
            Assert.Throws<ArgumentNullException>(() => new LevelProgress(null));
        }
    }
}
