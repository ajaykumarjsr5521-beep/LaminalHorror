using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class LocTests
    {
        [TearDown]
        public void TearDown() => Loc.ResetToDefault();

        [Test]
        public void EveryDefaultKey_HasNonEmptyText()
        {
            foreach (var kv in DefaultStrings.English)
                Assert.IsFalse(string.IsNullOrWhiteSpace(kv.Value), $"key '{kv.Key}' is empty");
        }

        [Test]
        public void Get_KnownKey_ReturnsText() =>
            Assert.AreEqual("Open", Loc.Get("door.prompt.open"));

        [Test]
        public void Get_MissingKey_ReturnsBracketedKey_AndLogsOncePerKey()
        {
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("missing string key 'no.such.key'"));
            Assert.AreEqual("[no.such.key]", Loc.Get("no.such.key"));
            Assert.AreEqual("[no.such.key]", Loc.Get("no.such.key"));   // second call: no new log (test fails on unexpected logs)
        }

        [Test]
        public void Format_FillsArguments() =>
            Assert.AreEqual("Take Brass Key", Loc.Format("pickup.prompt", "Brass Key"));

        [Test]
        public void Format_BadFormatString_IsReported_AndTextStillReturned()
        {
            Loc.SetTable(new Dictionary<string, string> { { "x", "broken {0" } });
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("bad format string"));
            Assert.AreEqual("broken {0", Loc.Format("x", "a"));
        }

        [Test]
        public void SetTable_ChangesDisplayedText()
        {
            Loc.SetTable(new Dictionary<string, string> { { "door.prompt.open", "Ouvrir" } });
            Assert.AreEqual("Ouvrir", Loc.Get("door.prompt.open"));
            Loc.ResetToDefault();
            Assert.AreEqual("Open", Loc.Get("door.prompt.open"));
        }

        [Test]
        public void SetTable_Null_Throws() =>
            Assert.Throws<System.ArgumentNullException>(() => Loc.SetTable(null));

        [Test]
        public void Has_ReflectsTable()
        {
            Assert.IsTrue(Loc.Has("note.prompt"));
            Assert.IsFalse(Loc.Has("nope"));
            Assert.IsFalse(Loc.Has(null));
        }
    }
}
