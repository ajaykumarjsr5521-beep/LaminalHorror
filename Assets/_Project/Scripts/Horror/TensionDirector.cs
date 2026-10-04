using System;

namespace NocturneAnnex.Horror
{
    /// <summary>
    /// Tracks a 0-1 tension value and decides when a scare event is allowed. Tension rises while the player is in an
    /// unsafe zone and falls in a safe one. While gameplay is blocked (pause, modal screen, menu) nothing changes, so
    /// reading a note never builds up a scare. Events are spaced by a minimum gap measured in unblocked game time.
    /// </summary>
    public class TensionDirector
    {
        readonly TensionSettings _settings;
        float _sinceLastEvent;

        public TensionDirector(TensionSettings settings, bool storyMode = false)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();
            StoryMode = storyMode;
            _sinceLastEvent = float.PositiveInfinity;   // the very first event is not held back by a gap
        }

        public float Tension { get; private set; }
        public bool StoryMode { get; set; }

        public float EffectiveGapSeconds => _settings.MinGapSeconds * (StoryMode ? TensionSettings.StoryGapMultiplier : 1f);
        float EffectiveRise => _settings.RisePerSecondUnsafe * (StoryMode ? TensionSettings.StoryRiseMultiplier : 1f);

        /// <param name="deltaSeconds">Game time since the last call (not real time while paused).</param>
        public void Tick(float deltaSeconds, bool inUnsafeZone, bool blocked)
        {
            if (blocked || deltaSeconds <= 0f) return;
            _sinceLastEvent += deltaSeconds;
            float change = inUnsafeZone ? EffectiveRise * deltaSeconds : -_settings.DecayPerSecondSafe * deltaSeconds;
            Tension = Math.Min(1f, Math.Max(0f, Tension + change));
        }

        public bool CanFire => Tension >= _settings.FireThreshold && _sinceLastEvent >= EffectiveGapSeconds;

        /// <summary>Claims the next event slot. Returns false when it is too soon or tension is too low.</summary>
        public bool TryTakeEvent()
        {
            if (!CanFire) return false;
            _sinceLastEvent = 0f;
            Tension = Math.Max(0f, Tension - _settings.ReliefAfterEvent);
            return true;
        }

        /// <summary>For a fresh run or a checkpoint respawn: no leftover tension, and the first event is not delayed.</summary>
        public void Reset()
        {
            Tension = 0f;
            _sinceLastEvent = float.PositiveInfinity;
        }
    }
}
