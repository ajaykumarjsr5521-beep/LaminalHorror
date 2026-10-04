using System;
using NUnit.Framework;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Tests.EditMode
{
    public class EventPickerTests
    {
        static EventCandidate E(string id, bool once = false, float cooldown = 0f, float min = 0f) =>
            new EventCandidate(id, once, cooldown, min);

        [Test]
        public void NothingEligible_ReturnsFalse()
        {
            var p = new EventPicker(new[] { E("a", min: 0.8f) });
            Assert.IsFalse(p.TryPick(0.5f, 0f, out var id));
            Assert.IsNull(id);
        }

        [Test]
        public void EmptyList_ReturnsFalse()
        {
            Assert.IsFalse(new EventPicker(new EventCandidate[0]).TryPick(1f, 0f, out _));
        }

        [Test]
        public void OneShot_NeverRepeats()
        {
            var p = new EventPicker(new[] { E("slam", once: true) });
            Assert.IsTrue(p.TryPick(1f, 0f, out var first));
            Assert.AreEqual("slam", first);
            Assert.IsFalse(p.TryPick(1f, 10000f, out _));
        }

        [Test]
        public void Cooldown_BlocksUntilItHasPassed()
        {
            var p = new EventPicker(new[] { E("flicker", cooldown: 60f) });
            Assert.IsTrue(p.TryPick(1f, 100f, out _));
            Assert.IsFalse(p.TryPick(1f, 159f, out _));
            Assert.IsTrue(p.TryPick(1f, 160f, out _));
        }

        [Test]
        public void HigherMinimumTensionWins_AmongEligible()
        {
            var p = new EventPicker(new[] { E("mild", min: 0.2f), E("strong", min: 0.7f), E("medium", min: 0.5f) });
            Assert.IsTrue(p.TryPick(0.9f, 0f, out var id));
            Assert.AreEqual("strong", id);
            Assert.IsTrue(p.TryPick(0.6f, 0f, out id), "strong is on no cooldown but needs more tension than 0.6");
            Assert.AreEqual("medium", id);
        }

        [Test]
        public void Tie_GoesToTheEarlierEntry()
        {
            var p = new EventPicker(new[] { E("first", min: 0.5f), E("second", min: 0.5f) });
            Assert.IsTrue(p.TryPick(0.8f, 0f, out var id));
            Assert.AreEqual("first", id);
        }

        [Test]
        public void WhenTheTopChoiceIsSpent_TheNextEligibleIsUsed()
        {
            var p = new EventPicker(new[] { E("big", once: true, min: 0.8f), E("small", min: 0.1f) });
            p.TryPick(1f, 0f, out var id);
            Assert.AreEqual("big", id);
            p.TryPick(1f, 1f, out id);
            Assert.AreEqual("small", id);
        }

        [Test]
        public void SnapshotAndRestore_KeepOneShotsFired_AndIgnoreUnknownIds()
        {
            var list = new[] { E("slam", once: true), E("flicker") };
            var a = new EventPicker(list);
            a.TryPick(1f, 0f, out _);   // fires "slam" first? highest min tension ties: earlier entry, so slam
            CollectionAssert.AreEqual(new[] { "slam" }, a.SnapshotFiredOnce());

            var b = new EventPicker(list);
            b.RestoreFiredOnce(new[] { "slam", "removed_in_later_version" });
            b.TryPick(1f, 0f, out var id);
            Assert.AreEqual("flicker", id, "a restored one-shot must not fire again");
        }

        [Test]
        public void BadConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EventPicker(null));
            Assert.Throws<ArgumentException>(() => new EventPicker(new[] { E("") }));
            Assert.Throws<ArgumentException>(() => new EventPicker(new[] { E("a"), E("a") }));
            Assert.Throws<ArgumentException>(() => new EventPicker(new[] { E("a", cooldown: -1f) }));
            Assert.Throws<ArgumentException>(() => new EventPicker(new[] { E("a", min: 1.5f) }));
        }
    }
}
