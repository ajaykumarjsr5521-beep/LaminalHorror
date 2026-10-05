using NocturneAnnex.Core;

namespace NocturneAnnex.Horror
{
    /// <summary>
    /// Brightness multiplier for a flickering light. The waveform comes from SafeFlicker. With Reduce Flicker off every
    /// lit-to-dim change must first get a slot from the FlashBudget, so at most 3 happen in any second even when the
    /// authored frequency is higher; a refused change simply stays lit. With Reduce Flicker on the waveform is the slow,
    /// shallow fade and the budget is not needed. Plain C#, so the limits are unit-tested.
    /// </summary>
    public class LightFlickerEffect
    {
        readonly FlashBudget _budget;
        readonly float _frequencyHz;
        readonly float _depth;
        bool _dimmed;

        public LightFlickerEffect(FlashBudget budget, float frequencyHz, float depth)
        {
            _budget = budget ?? new FlashBudget();
            _frequencyHz = frequencyHz;
            _depth = depth;
        }

        /// <returns>Multiplier from 0 to 1 to apply to the light's base intensity.</returns>
        public float Multiplier(float timeSeconds, bool reduceFlicker)
        {
            float raw = SafeFlicker.Intensity(1f, timeSeconds, _frequencyHz, _depth, reduceFlicker);
            if (reduceFlicker) return raw;

            if (raw >= 1f) { _dimmed = false; return 1f; }
            if (_dimmed) return raw;
            if (!_budget.TryFlash(timeSeconds)) return 1f;   // over budget: skip this dip
            _dimmed = true;
            return raw;
        }
    }
}
