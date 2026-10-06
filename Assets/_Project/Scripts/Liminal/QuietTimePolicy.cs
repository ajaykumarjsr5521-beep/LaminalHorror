namespace NocturneAnnex.Liminal
{
    /// <summary>How much quiet must pass before the next manifestation. Long quiet is a requirement of the design, not a leftover.</summary>
    public static class QuietTimePolicy
    {
        public const float BeforePresenceSeconds = 90f;
        public const float HuntSeconds = 45f;
        public const float AfterAftermathSeconds = 180f;

        /// <summary>Minimum seconds since the previous manifestation for this phase.</summary>
        public static float MinGapSeconds(EntityPhase phase) =>
            phase == EntityPhase.Hunt ? HuntSeconds : phase == EntityPhase.Aftermath ? AfterAftermathSeconds : BeforePresenceSeconds;

        /// <summary>
        /// Whether a manifestation may run now. Never while blocked, never during Survival (the player catches their breath),
        /// never within the quiet window after Aftermath began.
        /// </summary>
        public static bool CanManifest(EntityPhase phase, float secondsSinceLast, float secondsSinceAftermath, bool blocked)
        {
            if (blocked || phase == EntityPhase.Survival) return false;
            if (phase == EntityPhase.Aftermath && secondsSinceAftermath < AfterAftermathSeconds) return false;
            return secondsSinceLast >= MinGapSeconds(phase);
        }
    }
}
