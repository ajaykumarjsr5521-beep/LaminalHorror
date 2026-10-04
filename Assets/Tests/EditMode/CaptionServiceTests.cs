using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class CaptionServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            Accessibility.Reset();
            Loc.SetTable(new Dictionary<string, string>
            {
                { "c.creak", "[door creaks]" },
                { "c.step", "[footsteps nearby]" },
                { "c.whisper", "[whispering]" },
                { "c.thud", "[distant thud]" },
                { "c.dist", "[something {0} metres away]" },
            });
        }

        [TearDown]
        public void TearDown()
        {
            Loc.ResetToDefault();
            Accessibility.Reset();
        }

        [Test]
        public void PostedCue_ShowsUntilItsDurationEnds()
        {
            var s = new CaptionService();
            s.Post("c.creak", now: 10f, durationSeconds: 2f);
            Assert.AreEqual(new[] { "[door creaks]" }, s.Lines);
            s.Tick(11.9f);
            Assert.AreEqual(1, s.Count);
            s.Tick(12f);
            Assert.AreEqual(0, s.Count);
        }

        [Test]
        public void SameCuePostedAgain_RefreshesInsteadOfStacking()
        {
            var s = new CaptionService();
            s.Post("c.creak", 0f, 2f);
            s.Post("c.creak", 1.5f, 2f);
            Assert.AreEqual(1, s.Count);
            s.Tick(3f);
            Assert.AreEqual(1, s.Count, "the refresh extended it to t=3.5");
            s.Tick(3.5f);
            Assert.AreEqual(0, s.Count);
        }

        [Test]
        public void MoreThanMaxLines_DropsLowestPriority_ThenOldest()
        {
            var s = new CaptionService { MaxLines = 3 };
            s.Post("c.creak", 0f, 10f, priority: 1);
            s.Post("c.step", 0f, 10f, priority: 0);
            s.Post("c.whisper", 0f, 10f, priority: 1);
            s.Post("c.thud", 0f, 10f, priority: 2);
            CollectionAssert.AreEqual(new[] { "[door creaks]", "[whispering]", "[distant thud]" }, s.Lines);
        }

        [Test]
        public void LowPriorityCue_IsItselfDropped_WhenFullOfHigherPriority()
        {
            var s = new CaptionService { MaxLines = 2 };
            s.Post("c.creak", 0f, 10f, priority: 5);
            s.Post("c.step", 0f, 10f, priority: 5);
            s.Post("c.whisper", 0f, 10f, priority: 0);
            CollectionAssert.AreEqual(new[] { "[door creaks]", "[footsteps nearby]" }, s.Lines);
        }

        [Test]
        public void SamePriority_DropsTheOldest()
        {
            var s = new CaptionService { MaxLines = 2 };
            s.Post("c.creak", 0f, 10f);
            s.Post("c.step", 0f, 10f);
            s.Post("c.whisper", 0f, 10f);
            CollectionAssert.AreEqual(new[] { "[footsteps nearby]", "[whispering]" }, s.Lines);
        }

        [Test]
        public void CaptionsOff_ShowsNothing_AndTickClearsWhatWasThere()
        {
            var s = new CaptionService();
            s.Post("c.creak", 0f, 10f);
            Accessibility.Set(false, Accessibility.MediumText, false, false);
            s.Tick(1f);
            Assert.AreEqual(0, s.Count);
            s.Post("c.step", 1f, 10f);
            Assert.AreEqual(0, s.Count);
        }

        [Test]
        public void Post_WithArguments_FormatsText()
        {
            var s = new CaptionService();
            s.Post("c.dist", 0f, 3f, 0, 12);
            Assert.AreEqual("[something 12 metres away]", s.Lines[0]);
        }

        [Test]
        public void MissingKey_IsShownAsBracketedKey_AndReportedNotHidden()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("missing string key 'c.nope'"));
            var s = new CaptionService();
            s.Post("c.nope", 0f);
            Assert.AreEqual("[c.nope]", s.Lines[0]);
        }

        [Test]
        public void Changed_FiresOnPostRefreshExpiryAndClear_ButNotOnIdleTick()
        {
            var s = new CaptionService();
            int fired = 0;
            s.Changed += () => fired++;
            s.Post("c.creak", 0f, 1f);
            s.Tick(0.5f);
            Assert.AreEqual(1, fired, "idle tick must not raise Changed");
            s.Post("c.creak", 0.5f, 1f);
            Assert.AreEqual(2, fired);
            s.Tick(5f);
            Assert.AreEqual(3, fired);
            s.Clear();
            Assert.AreEqual(3, fired, "clearing an empty queue is not a change");
        }

        [Test]
        public void MaxLinesBelowOne_StillShowsOneLine()
        {
            var s = new CaptionService { MaxLines = 0 };
            s.Post("c.creak", 0f);
            Assert.AreEqual(1, s.Count);
        }
    }
}
