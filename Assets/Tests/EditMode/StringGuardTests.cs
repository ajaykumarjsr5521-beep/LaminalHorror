using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    /// <summary>
    /// Guard rails for X-04: every Loc key used in code exists in the default table, and the migrated
    /// areas do not reintroduce literal player-facing text. Pattern based, so it catches common slips, not every case.
    /// </summary>
    public class StringGuardTests
    {
        static readonly string[] GuardedAreas = { "Interaction", "Puzzle", "Save", "Inventory", "Settings", "Level" };

        static string ScriptsRoot => Path.Combine(Application.dataPath, "_Project", "Scripts");

        static System.Collections.Generic.IEnumerable<string> GuardedFiles()
        {
            foreach (var area in GuardedAreas)
            {
                var dir = Path.Combine(ScriptsRoot, area);
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)) yield return f;
            }
        }

        [Test]
        public void EveryLocKeyUsedInCode_ExistsInDefaultTable()
        {
            var rx = new Regex("Loc\\.(?:Get|Format)\\(\\s*\"([^\"]+)\"");
            int found = 0;
            foreach (var file in Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                foreach (Match m in rx.Matches(File.ReadAllText(file)))
                {
                    found++;
                    Assert.IsTrue(DefaultStrings.English.ContainsKey(m.Groups[1].Value),
                        $"{Path.GetFileName(file)} uses unknown Loc key '{m.Groups[1].Value}'");
                }
            }
            Assert.Greater(found, 0, "scan found no Loc usages; the guard itself is broken");
        }

        static readonly Regex[] Forbidden = new[]
        {
            new Regex("Prompt\\s*=>\\s*\""),                       // Prompt => "Open"
            new Regex("Prompt\\s*=>(?![^;]*Loc\\.)[^;]*\""),       // Prompt => any literal without a Loc call
            new Regex("OnMessage\\.Invoke\\(\\s*\""),              // OnMessage.Invoke("...")
            new Regex("Refused\\?\\.Invoke\\([^,]+,\\s*\""),       // Refused?.Invoke(this, "...")
            new Regex("new LoadResult\\([^;]*,\\s*\""),             // new LoadResult(status, null, "text")
            new Regex("new WriteResult\\(false,\\s*\""),           // new WriteResult(false, "text")
        };

        [Test]
        public void GuardedAreas_HaveNoLiteralPromptsOrPlayerMessages()
        {
            foreach (var file in GuardedFiles())
            {
                var text = File.ReadAllText(file);
                foreach (var rx in Forbidden)
                    Assert.IsFalse(rx.IsMatch(text), $"{Path.GetFileName(file)} contains a literal player-facing string ({rx}); use Loc");
            }
        }

        [TestCase("public string Prompt => \"Open\";")]
        [TestCase("public string Prompt => IsOpen ? \"Close\" : \"Open\";")]
        [TestCase("OnMessage.Invoke(\"It is locked\");")]
        [TestCase("Refused?.Invoke(this, \"Too heavy\");")]
        [TestCase("return new LoadResult(LoadStatus.Corrupt, null, \"Bad file\");")]
        [TestCase("return new WriteResult(false, \"Disk full\");")]
        public void Guard_DetectsLiteralPlayerText(string badLine)
        {
            Assert.IsTrue(System.Array.Exists(Forbidden, rx => rx.IsMatch(badLine)), "guard missed: " + badLine);
        }

        [TestCase("public string Prompt => Loc.Get(\"note.prompt\");")]
        [TestCase("public string Prompt => !IsConfigured ? \"\" : Loc.Get(IsSolved ? \"a\" : \"b\");")]
        [TestCase("return new LoadResult(LoadStatus.Corrupt, null, Loc.Get(\"save.empty\"));")]
        public void Guard_AllowsLocUsage(string goodLine)
        {
            Assert.IsFalse(System.Array.Exists(Forbidden, rx => rx.IsMatch(goodLine)), "guard false positive: " + goodLine);
        }
    }
}
