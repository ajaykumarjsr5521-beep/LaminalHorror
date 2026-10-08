using NUnit.Framework;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class HideInspectionTests
    {
        [Test]
        public void SaferSpotsAreInspectedLessOften()
        {
            Assert.Greater(HideInspection.Chance(0.2f, 0f, 0f), HideInspection.Chance(0.9f, 0f, 0f));
        }

        [Test]
        public void NoisyEntryRaisesTheChance()
        {
            Assert.Greater(HideInspection.Chance(0.5f, 1f, 0f), HideInspection.Chance(0.5f, 0f, 0f));
        }

        [Test]
        public void MemoryRaisesTheChanceButIsCapped()
        {
            float none = HideInspection.Chance(0.5f, 0.2f, 0f);
            Assert.Greater(HideInspection.Chance(0.5f, 0.2f, 0.25f), none);
            Assert.AreEqual(HideInspection.Chance(0.5f, 0.2f, 0.25f), HideInspection.Chance(0.5f, 0.2f, 5f), 1e-5f);
        }

        [Test]
        public void ChanceNeverReachesCertainty()
        {
            Assert.LessOrEqual(HideInspection.Chance(0f, 1f, 1f), HideInspection.MaxChance);
            Assert.Less(HideInspection.MaxChance, 1f);
            Assert.Greater(HideInspection.Chance(1f, 0f, 0f), 0f);
        }

        [Test]
        public void ResolveFindsOnlyWhenInspectedAndInside()
        {
            Assert.AreEqual(InspectOutcome.FoundPlayer, HideInspection.Resolve(0.5f, 0.1f, true));
            Assert.AreEqual(InspectOutcome.Missed, HideInspection.Resolve(0.5f, 0.1f, false));
            Assert.AreEqual(InspectOutcome.Skipped, HideInspection.Resolve(0.5f, 0.7f, true));
        }

        [Test]
        public void SimulationMatchesTheChance()
        {
            var rng = new System.Random(3);
            float chance = HideInspection.Chance(0.5f, 0.2f, 0f);
            int found = 0, n = 20000;
            for (int i = 0; i < n; i++)
                if (HideInspection.Resolve(chance, (float)rng.NextDouble(), true) == InspectOutcome.FoundPlayer) found++;
            Assert.AreEqual(chance, found / (float)n, 0.02f);
        }
    }
}
