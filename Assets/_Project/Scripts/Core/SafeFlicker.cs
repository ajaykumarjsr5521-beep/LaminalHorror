using UnityEngine;

namespace NocturneAnnex.Core
{
    /// <summary>
    /// Light-intensity waveform for flickering lights. Normally a fast on/off square wave at FrequencyHz.
    /// With Reduce Flicker on it becomes a slow, shallow fade (at most 0.5 Hz, at most 25 percent deep), so no state
    /// change happens more than 3 times per second and no abrupt large brightness jump occurs.
    /// </summary>
    public static class SafeFlicker
    {
        public const float ReducedFrequencyHz = 0.5f;
        public const float ReducedMaxDepth = 0.25f;

        /// <param name="baseIntensity">Full brightness.</param>
        /// <param name="timeSeconds">Current time in seconds.</param>
        /// <param name="frequencyHz">Flicker rate when not reduced.</param>
        /// <param name="depth">0 to 1: how far brightness dips below base.</param>
        public static float Intensity(float baseIntensity, float timeSeconds, float frequencyHz, float depth, bool reduceFlicker)
        {
            depth = Mathf.Clamp01(depth);
            if (reduceFlicker)
            {
                float slow = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * ReducedFrequencyHz * timeSeconds);
                return baseIntensity * (1f - Mathf.Min(depth, ReducedMaxDepth) * slow);
            }

            bool dipped = Mathf.Sin(2f * Mathf.PI * frequencyHz * timeSeconds) < 0f;
            return baseIntensity * (dipped ? 1f - depth : 1f);
        }
    }
}
