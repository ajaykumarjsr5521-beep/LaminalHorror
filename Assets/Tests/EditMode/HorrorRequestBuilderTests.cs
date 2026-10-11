using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.EditMode
{
    public class HorrorRequestBuilderTests
    {
        [Test]
        public void Build_ClampsOutOfRangeAndNonFiniteValues()
        {
            var i = new HorrorInputs { Stress = 3f, EntityProximity = -1f, Darkness = float.NaN, SecondsSinceScare = -5f, RecentScares = 99 };
            var json = HorrorRequestBuilder.Build("r1", i, new PlayerProfile { DeathCount = 2, HideSuccessRate = 4f }, 7f);
            StringAssert.Contains("\"stress\":1", json);
            StringAssert.Contains("\"entity_proximity\":0", json);
            StringAssert.Contains("\"darkness\":0", json);
            StringAssert.Contains("\"seconds_since_scare\":0", json);
            StringAssert.Contains("\"recent_scares\":20", json);
            StringAssert.Contains("\"death_count\":2", json);
            StringAssert.Contains("\"memory_pressure\":1", json);
            StringAssert.Contains("\"hide_success_rate\":1", json);
        }

        [Test]
        public void Build_NullProfile_UsesZeros()
        {
            var json = HorrorRequestBuilder.Build("r2", default, null);
            StringAssert.Contains("\"death_count\":0", json);
            StringAssert.Contains("\"hide_success_rate\":0", json);
        }

        [Test]
        public void Build_HasOnlyTheServerFields_NoPositions()
        {
            var json = HorrorRequestBuilder.Build("r3", default, null);
            foreach (var banned in new[] { "position", "\"x\"", "\"y\"", "\"z\"", "room", "spot" })
                StringAssert.DoesNotContain(banned, json);
            Assert.AreEqual(14, CountKeys(json), "exactly the 14 fields the server accepts");
        }

        static int CountKeys(string json) => json.Split(new[] { "\":" }, System.StringSplitOptions.None).Length - 1;
    }
}
