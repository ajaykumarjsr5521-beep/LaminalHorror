using System;

namespace NocturneAnnex.Strategy
{
    public enum StrategyRejection { None, UnknownStrategy, NoneStrategy, OutOfRange, BadDuration, BadReason }

    /// <summary>Gatekeeper for every command from the server or a fallback planner. Anything invalid is rejected, never clamped.</summary>
    public static class StrategyValidator
    {
        public const int MaxReasonLength = 48;

        public static StrategyRejection Check(StrategyCommand c)
        {
            if (!Enum.IsDefined(typeof(StrategyType), c.Strategy)) return StrategyRejection.UnknownStrategy;
            if (c.Strategy == StrategyType.None) return StrategyRejection.NoneStrategy;
            if (!In01(c.Priority) || !In01(c.Intensity) || !In01(c.Confidence)) return StrategyRejection.OutOfRange;
            if (float.IsNaN(c.DurationSeconds) || c.DurationSeconds < StrategyCommand.MinDuration || c.DurationSeconds > StrategyCommand.MaxDuration)
                return StrategyRejection.BadDuration;
            if (c.ReasonCode != null && c.ReasonCode.Length > MaxReasonLength) return StrategyRejection.BadReason;
            return StrategyRejection.None;
        }

        public static bool IsValid(StrategyCommand c) => Check(c) == StrategyRejection.None;

        static bool In01(float v) => !float.IsNaN(v) && v >= 0f && v <= 1f;
    }
}
