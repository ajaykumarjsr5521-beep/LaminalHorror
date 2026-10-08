namespace NocturneAnnex.Entity
{
    public enum StrikePhase { Idle, Windup, Strike, Recover }

    /// <summary>
    /// The entity's attack timeline. A hit needs a full windup and swing and the player still within reach at the end of the swing,
    /// so a chase contact is never an instant death and moving away is a fair way to survive. Plain C# so it is tested without a scene.
    /// </summary>
    public sealed class StrikeModel
    {
        public const float WindupSeconds = 0.5f;
        public const float StrikeSeconds = 0.25f;
        public const float RecoverSeconds = 1.0f;
        public const float StrikeDistance = 1.8f;
        public const float HitReach = 2.0f;

        public StrikePhase Phase { get; private set; } = StrikePhase.Idle;
        public float PhaseTime { get; private set; }
        public bool LastWasHit { get; private set; }

        /// <summary>0 at rest, 1 at the top of the windup, back to 0 after the swing: drives the arm pose.</summary>
        public float Raise
        {
            get
            {
                switch (Phase)
                {
                    case StrikePhase.Windup: return PhaseTime / WindupSeconds;
                    case StrikePhase.Strike: return 1f - PhaseTime / StrikeSeconds;
                    default: return 0f;
                }
            }
        }

        public bool CanStart(float distance) => Phase == StrikePhase.Idle && distance <= StrikeDistance;

        public void Start() { Phase = StrikePhase.Windup; PhaseTime = 0f; LastWasHit = false; }

        public void Cancel() { Phase = StrikePhase.Idle; PhaseTime = 0f; }

        /// <summary>Advances the swing. Returns true on the frame the swing ends in a hit.</summary>
        public bool Step(float dt, float distanceToPlayer)
        {
            if (Phase == StrikePhase.Idle) return false;
            PhaseTime += dt;
            switch (Phase)
            {
                case StrikePhase.Windup:
                    if (PhaseTime >= WindupSeconds) { Phase = StrikePhase.Strike; PhaseTime = 0f; }
                    break;
                case StrikePhase.Strike:
                    if (PhaseTime >= StrikeSeconds)
                    {
                        LastWasHit = distanceToPlayer <= HitReach;
                        Phase = LastWasHit ? StrikePhase.Idle : StrikePhase.Recover;
                        PhaseTime = 0f;
                        return LastWasHit;
                    }
                    break;
                case StrikePhase.Recover:
                    if (PhaseTime >= RecoverSeconds) { Phase = StrikePhase.Idle; PhaseTime = 0f; }
                    break;
            }
            return false;
        }
    }
}
