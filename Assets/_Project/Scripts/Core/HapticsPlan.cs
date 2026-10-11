using System;
using System.Collections.Generic;

namespace NocturneAnnex.Core
{
    public enum HapticEvent { Footstep, Heartbeat, Hit, Scare }

    /// <summary>One motor pulse: low (heavy) and high (light) motor speeds, 0 to 1, and how long it lasts.</summary>
    public readonly struct HapticPulse
    {
        public readonly float Low, High, Seconds;
        public HapticPulse(float low, float high, float seconds) { Low = low; High = high; Seconds = seconds; }
    }

    /// <summary>Something that can shake: a gamepad, a phone.</summary>
    public interface IHapticsDevice
    {
        void Pulse(HapticEvent kind, HapticPulse pulse);
    }

    /// <summary>The player's haptics choice, set from Settings.</summary>
    public static class HapticSettings
    {
        public static bool Enabled { get; private set; } = true;
        public static float Strength { get; private set; } = 0.7f;

        public static void Set(bool enabled, float strength)
        {
            Enabled = enabled;
            Strength = float.IsNaN(strength) ? 0.7f : Math.Max(0f, Math.Min(1f, strength));
        }
    }

    /// <summary>The pulse for each game event, scaled by strength. Off, or a strength under 5%, means no pulse at all.</summary>
    public static class HapticsPlan
    {
        public const float MinStrength = 0.05f;

        static readonly Dictionary<HapticEvent, HapticPulse> Table = new Dictionary<HapticEvent, HapticPulse>
        {
            { HapticEvent.Footstep, new HapticPulse(0.15f, 0.05f, 0.06f) },
            { HapticEvent.Heartbeat, new HapticPulse(0.30f, 0.10f, 0.12f) },
            { HapticEvent.Hit, new HapticPulse(1.00f, 0.80f, 0.35f) },
            { HapticEvent.Scare, new HapticPulse(0.60f, 0.40f, 0.25f) },
        };

        public static HapticPulse? For(HapticEvent kind, bool enabled, float strength)
        {
            if (!enabled || float.IsNaN(strength) || strength < MinStrength) return null;
            strength = Math.Min(1f, strength);
            var p = Table[kind];
            return new HapticPulse(p.Low * strength, p.High * strength, p.Seconds);
        }

        /// <summary>How close the entity's footsteps must be to be felt, in metres.</summary>
        public const float FootstepRange = 10f;

        /// <summary>Footstep strength fades with distance: full at the entity's feet, nothing at FootstepRange.</summary>
        public static float FootstepScale(float distance) =>
            distance >= FootstepRange ? 0f : 1f - Math.Max(0f, distance) / FootstepRange;
    }

    /// <summary>A minimum gap between pulses of the same kind, so a heartbeat or a run of footsteps never becomes a constant buzz.</summary>
    public sealed class HapticLimiter
    {
        static readonly Dictionary<HapticEvent, float> Gaps = new Dictionary<HapticEvent, float>
        {
            { HapticEvent.Footstep, 0.25f }, { HapticEvent.Heartbeat, 0.3f }, { HapticEvent.Hit, 0.5f }, { HapticEvent.Scare, 1f },
        };

        readonly Dictionary<HapticEvent, float> _last = new Dictionary<HapticEvent, float>();

        public bool TryAcquire(HapticEvent kind, float now)
        {
            if (_last.TryGetValue(kind, out var t) && now - t < Gaps[kind]) return false;
            _last[kind] = now;
            return true;
        }

        public void Reset() => _last.Clear();
    }
}
