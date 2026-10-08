using System;

namespace NocturneAnnex.Strategy
{
    /// <summary>Closed set of high-level strategies the strategic layer may ask for. Nothing here names a position or a target.</summary>
    public enum StrategyType
    {
        None = 0,
        IncreaseHidingPressure,
        IncreaseInvestigation,
        ChangePatrolPreference,
        ChangeSearchPriority,
        RelaxPressure,
    }

    /// <summary>A time-limited request from the strategic AI. Unity validates it before use (StrategyValidator).</summary>
    [Serializable]
    public struct StrategyCommand
    {
        public StrategyType Strategy;
        public float Priority;        // 0..1
        public float Intensity;       // 0..1
        public float DurationSeconds; // MinDuration..MaxDuration
        public float Confidence;      // 0..1
        public string ReasonCode;     // short code for the debug panel, e.g. REPEATED_SUCCESSFUL_HIDING

        public const float MinDuration = 10f;
        public const float MaxDuration = 180f;
    }
}
