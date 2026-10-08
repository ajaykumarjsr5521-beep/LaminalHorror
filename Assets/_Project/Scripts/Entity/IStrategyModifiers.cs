namespace NocturneAnnex.Entity
{
    /// <summary>
    /// The only things the strategic layer may change in the real-time AI: probabilities and durations, within validator limits.
    /// Entity code reads this; the Strategy assembly implements it. Null means baseline.
    /// </summary>
    public interface IStrategyModifiers
    {
        /// <summary>Added to a hide spot's inspection chance (never turns a safe spot into a sure find).</summary>
        float HideBonus { get; }

        /// <summary>Scales how long the entity searches after losing a target. 1 = baseline.</summary>
        float InvestigationMultiplier { get; }
    }
}
