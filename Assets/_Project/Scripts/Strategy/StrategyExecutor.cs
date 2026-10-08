using NocturneAnnex.Entity;

namespace NocturneAnnex.Strategy
{
    /// <summary>
    /// Holds the one active strategy and turns it into small, capped biases for the real-time AI. It exposes numbers only:
    /// no position, target, speed or transform. When the strategy expires (or none is set) every bias returns to baseline.
    /// </summary>
    public sealed class StrategyExecutor
    {
        public const float MaxInvestigationMultiplier = 1.5f;
        public const float MinInvestigationMultiplier = 0.75f;

        StrategyCommand _active;
        float _expiresAt;

        public bool HasActive { get; private set; }
        public StrategyCommand Active => _active;
        public StrategyRejection LastRejection { get; private set; }

        /// <summary>Extra hide-spot inspect chance, 0..HideInspection.MaxMemoryBonus. Feed it to HideInspection.Chance.</summary>
        public float HideBonus { get; private set; }
        /// <summary>Multiplier on the utility score of investigating a sound, 0.75..1.5.</summary>
        public float InvestigationMultiplier { get; private set; } = 1f;
        /// <summary>0..1 preference for the favoured patrol route group (route choice stays with the AI).</summary>
        public float PatrolBias { get; private set; }
        /// <summary>0..1 preference for searching near the last sound over searching elsewhere.</summary>
        public float SearchBias { get; private set; }

        public float Remaining(float now) => HasActive ? System.Math.Max(0f, _expiresAt - now) : 0f;

        /// <summary>Validates and applies. An invalid command is rejected and the current strategy stays.</summary>
        public bool TryApply(StrategyCommand c, float now)
        {
            LastRejection = StrategyValidator.Check(c);
            if (LastRejection != StrategyRejection.None) return false;
            _active = c; _expiresAt = now + c.DurationSeconds; HasActive = true;
            Recompute();
            return true;
        }

        /// <summary>Call from a slow tick (about once a second), not every frame.</summary>
        public void Tick(float now)
        {
            if (HasActive && now >= _expiresAt) Clear();
        }

        public void Clear()
        {
            HasActive = false; _active = default;
            HideBonus = 0f; InvestigationMultiplier = 1f; PatrolBias = 0f; SearchBias = 0f;
        }

        void Recompute()
        {
            float w = _active.Intensity * _active.Priority;
            HideBonus = 0f; InvestigationMultiplier = 1f; PatrolBias = 0f; SearchBias = 0f;
            switch (_active.Strategy)
            {
                case StrategyType.IncreaseHidingPressure: HideBonus = w * HideInspection.MaxMemoryBonus; break;
                case StrategyType.IncreaseInvestigation: InvestigationMultiplier = 1f + w * (MaxInvestigationMultiplier - 1f); break;
                case StrategyType.ChangePatrolPreference: PatrolBias = w; break;
                case StrategyType.ChangeSearchPriority: SearchBias = w; break;
                case StrategyType.RelaxPressure: InvestigationMultiplier = 1f - w * (1f - MinInvestigationMultiplier); break;
            }
        }
    }
}
