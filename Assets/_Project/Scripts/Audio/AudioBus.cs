using System;

namespace NocturneAnnex.Audio
{
    /// <summary>
    /// Volume state for the three buses. Each bus has a player slider (0-1). Music and Ambience are also ducked
    /// while a gameplay cue plays, so the cue is heard over them. Plain C#, driven by Tick.
    /// </summary>
    public class AudioBus
    {
        public const float DefaultDuckAmount = 0.5f;   // duck factor 0.5 means half volume
        const float AttackPerSecond = 8f, ReleasePerSecond = 2f;

        float _music = 1f, _ambience = 1f, _sfx = 1f;
        float _duck = 1f, _duckLevel = 1f, _duckRemaining;

        /// <summary>Current duck factor applied to Music and Ambience, 1 when not ducking.</summary>
        public float DuckFactor => _duck;

        public float Slider(string bus) => bus switch
        {
            CueCatalog.Music => _music,
            CueCatalog.Ambience => _ambience,
            CueCatalog.Sfx => _sfx,
            _ => throw new ArgumentException($"Unknown bus '{bus}'.", nameof(bus))
        };

        public void SetSlider(string bus, float value)
        {
            value = Math.Max(0f, Math.Min(1f, value));
            switch (bus)
            {
                case CueCatalog.Music: _music = value; break;
                case CueCatalog.Ambience: _ambience = value; break;
                case CueCatalog.Sfx: _sfx = value; break;
                default: throw new ArgumentException($"Unknown bus '{bus}'.", nameof(bus));
            }
        }

        /// <summary>Final gain for a bus: its slider, times the duck factor for Music and Ambience.</summary>
        public float Gain(string bus) => bus == CueCatalog.Sfx ? Slider(bus) : Slider(bus) * _duck;

        /// <summary>Starts or extends a duck. The factor is 1 - amount, held for at least <paramref name="seconds"/>.</summary>
        public void Duck(float amount, float seconds)
        {
            _duckLevel = 1f - Math.Max(0f, Math.Min(1f, amount));
            _duckRemaining = Math.Max(_duckRemaining, Math.Max(0f, seconds));
        }

        public void Tick(float deltaSeconds)
        {
            _duckRemaining = Math.Max(0f, _duckRemaining - deltaSeconds);
            float target = _duckRemaining > 0f ? _duckLevel : 1f;
            float rate = (target < _duck ? AttackPerSecond : ReleasePerSecond) * deltaSeconds;
            _duck = target < _duck ? Math.Max(target, _duck - rate) : Math.Min(target, _duck + rate);
        }
    }
}
