using UnityEngine;

namespace NocturneAnnex.Entity
{
    /// <summary>Screen-effect strengths per danger level. Reduce motion removes pulse and shake; reduce flicker keeps only a steady tint.</summary>
    public static class DangerPulseModel
    {
        public static readonly float[] Vignette = { 0f, 0.05f, 0.15f, 0.3f, 0.5f };
        public static readonly float[] ShakeMetres = { 0f, 0f, 0f, 0.004f, 0.01f };

        public static float VignetteAlpha(int level, float beatPhase, bool reduceMotion, bool reduceFlicker)
        {
            float baseAlpha = Vignette[Mathf.Clamp(level, 0, 4)];
            if (reduceMotion || reduceFlicker) return baseAlpha;   // steady, no pulse
            return baseAlpha * (0.8f + 0.2f * Mathf.Sin(beatPhase * Mathf.PI * 2f));
        }

        public static float ShakeAmplitude(int level, bool reduceMotion) =>
            reduceMotion ? 0f : ShakeMetres[Mathf.Clamp(level, 0, 4)];
    }
}
