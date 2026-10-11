using NocturneAnnex.Level;

namespace NocturneAnnex.Entity
{
    /// <summary>Applies a room's difficulty row to the stalker. Only how often and how far it checks hiding spots changes; speed never does.</summary>
    public static class RoomDifficulty
    {
        public static void Apply(StalkerAgent stalker, DifficultySpec spec)
        {
            if (stalker == null || spec == null) return;
            stalker.InspectIntervalSeconds = spec.InspectIntervalSeconds;
            stalker.InspectRange = spec.InspectRange;
        }
    }
}
