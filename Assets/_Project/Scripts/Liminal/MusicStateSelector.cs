namespace NocturneAnnex.Liminal
{
    public enum MusicState { Exploration, Wonder, Unease, Presence, EntityNear, Hunting, Silence }

    /// <summary>What the music director needs to decide the music state.</summary>
    public struct ExperienceState
    {
        public EntityPhase Phase;
        public float Tension;                    // 0 to 1
        public float EntityDistance;             // metres; use float.PositiveInfinity when unknown
        public float SecondsSinceAftermath;      // large when not in Aftermath
        public bool AtVista;
    }

    /// <summary>Maps the experience to a music state. Deterministic, so it can be tested; the music director adds the crossfades.</summary>
    public static class MusicStateSelector
    {
        public const float EntityNearMetres = 15f;
        public const float SilenceSecondsAfterAftermath = 60f;
        public const float UneaseTension = 0.5f;

        public static MusicState Select(ExperienceState s)
        {
            if (s.Phase == EntityPhase.Hunt) return MusicState.Hunting;
            if (s.Phase == EntityPhase.Survival) return MusicState.Silence;
            if (s.Phase == EntityPhase.Aftermath && s.SecondsSinceAftermath < SilenceSecondsAfterAftermath) return MusicState.Silence;
            if (s.Phase >= EntityPhase.Presence && s.EntityDistance < EntityNearMetres) return MusicState.EntityNear;
            if (s.Phase == EntityPhase.Presence) return MusicState.Presence;
            if (s.AtVista && s.Phase < EntityPhase.Presence) return MusicState.Wonder;
            if ((s.Phase >= EntityPhase.Clue && s.Phase < EntityPhase.Presence) || s.Tension >= UneaseTension) return MusicState.Unease;
            return MusicState.Exploration;
        }
    }
}
