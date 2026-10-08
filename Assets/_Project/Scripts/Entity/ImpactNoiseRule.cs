namespace NocturneAnnex.Entity
{
    /// <summary>Decides whether a prop hitting something is audible, and as what. Plain C# so it is tested without a scene.</summary>
    public static class ImpactNoiseRule
    {
        public const float MinSpeed = 1.5f;
        public const float MinGap = 0.5f;

        /// <summary>Returns false for taps and for repeats inside <see cref="MinGap"/> of the last impact.</summary>
        public static bool TryGetKind(float impactSpeed, bool wasThrown, float secondsSinceLast, out NoiseKind kind)
        {
            kind = wasThrown ? NoiseKind.ThrownImpact : NoiseKind.Drop;
            return impactSpeed >= MinSpeed && secondsSinceLast >= MinGap;
        }
    }
}
