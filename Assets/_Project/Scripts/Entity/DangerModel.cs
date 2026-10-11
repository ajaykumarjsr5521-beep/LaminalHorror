namespace NocturneAnnex.Entity
{
    /// <summary>
    /// DangerLevel 0-4 from entity distance and state. Rises at once, falls only after the lower level has held for FallDelay seconds,
    /// so the heartbeat does not flap at a threshold. Pure C#; no positions leave it, only the level.
    /// </summary>
    public sealed class DangerModel
    {
        public const int Safe = 0, Aware = 1, Near = 2, Close = 3, Chase = 4;
        public const float AwareRange = 25f, NearRange = 15f, CloseRange = 8f, ChaseRange = 3f;

        public float FallDelay = 2f;
        public int Level { get; private set; }

        float _lowerFor;

        /// <summary>The level for this instant, ignoring hysteresis.</summary>
        public static int Raw(float distance, EntityState state)
        {
            if (state == EntityState.Chase || distance <= ChaseRange) return Chase;
            if (state == EntityState.Listen || state == EntityState.Watch || distance <= CloseRange) return Close;
            if (state == EntityState.Investigate || state == EntityState.Search || distance <= NearRange) return Near;
            if (distance <= AwareRange) return Aware;
            return Safe;
        }

        public int Step(float dt, float distance, EntityState state)
        {
            int raw = Raw(distance, state);
            if (raw >= Level) { Level = raw; _lowerFor = 0f; }
            else
            {
                _lowerFor += dt;
                if (_lowerFor >= FallDelay) { Level = raw; _lowerFor = 0f; }
            }
            return Level;
        }

        public void Reset() { Level = Safe; _lowerFor = 0f; }
    }
}
