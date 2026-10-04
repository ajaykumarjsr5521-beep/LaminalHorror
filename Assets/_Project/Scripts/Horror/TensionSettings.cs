using System;

namespace NocturneAnnex.Horror
{
    /// <summary>Tuning for the tension director. Plain data so designers can change it without code.</summary>
    [Serializable]
    public class TensionSettings
    {
        public float RisePerSecondUnsafe = 0.02f;   // 50 s of unsafe time to go from 0 to 1
        public float DecayPerSecondSafe = 0.05f;
        public float FireThreshold = 0.6f;
        public float ReliefAfterEvent = 0.5f;
        public float MinGapSeconds = 45f;

        /// <summary>Story mode: gentler pacing. Halves the rise and doubles the gap.</summary>
        public const float StoryRiseMultiplier = 0.5f;
        public const float StoryGapMultiplier = 2f;

        public void Validate()
        {
            if (RisePerSecondUnsafe <= 0f) throw new ArgumentException("RisePerSecondUnsafe must be positive.");
            if (DecayPerSecondSafe <= 0f) throw new ArgumentException("DecayPerSecondSafe must be positive.");
            if (MinGapSeconds <= 0f) throw new ArgumentException("MinGapSeconds must be positive.");
            if (FireThreshold < 0f || FireThreshold > 1f) throw new ArgumentException("FireThreshold must be between 0 and 1.");
            if (ReliefAfterEvent < 0f || ReliefAfterEvent > 1f) throw new ArgumentException("ReliefAfterEvent must be between 0 and 1.");
        }
    }
}
