using System;

namespace NocturneAnnex.Entity
{
    public enum InspectOutcome { Skipped, FoundPlayer, Missed }

    /// <summary>
    /// Decides whether the entity opens a hiding spot while it searches nearby, and what it finds. The chance grows when the
    /// spot is unsafe, when the player made noise getting in, and when memory says the player hid here before. It is capped
    /// below 1 so a hiding player is never certain to be found, and the memory bonus can never reach certainty.
    /// </summary>
    public static class HideInspection
    {
        public const float MaxChance = 0.9f;
        public const float MaxMemoryBonus = 0.25f;
        public const float BaseChance = 0.15f;

        /// <param name="safety">0 (poor) to 1 (excellent) as authored on the spot.</param>
        /// <param name="entryNoise">0 (silent) to 1 (very loud) from the noise made entering.</param>
        /// <param name="memoryBonus">0 to MaxMemoryBonus from EntityMemory (F-14i).</param>
        public static float Chance(float safety, float entryNoise, float memoryBonus)
        {
            safety = Clamp01(safety); entryNoise = Clamp01(entryNoise);
            memoryBonus = Math.Max(0f, Math.Min(MaxMemoryBonus, memoryBonus));
            float c = BaseChance + (1f - safety) * 0.5f + entryNoise * 0.3f + memoryBonus;
            return Math.Min(MaxChance, c);
        }

        /// <param name="random01">0..1 for the inspect decision.</param>
        /// <param name="playerInside">Whether the player is in this spot.</param>
        public static InspectOutcome Resolve(float chance, float random01, bool playerInside)
        {
            if (random01 >= chance) return InspectOutcome.Skipped;
            return playerInside ? InspectOutcome.FoundPlayer : InspectOutcome.Missed;
        }

        static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
    }
}
