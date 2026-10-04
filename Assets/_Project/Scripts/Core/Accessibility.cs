using System;
using UnityEngine;

namespace NocturneAnnex.Core
{
    /// <summary>
    /// Current accessibility preferences, set by the settings applier. Game systems (captions, lighting events,
    /// camera effects, UI text) read these and never read SettingsData directly.
    /// </summary>
    public static class Accessibility
    {
        public const int SmallText = 0;
        public const int MediumText = 1;
        public const int LargeText = 2;

        static readonly float[] TextScales = { 0.85f, 1f, 1.25f };

        public static bool CaptionsEnabled { get; private set; } = true;
        public static int TextSize { get; private set; } = MediumText;
        public static bool ReduceFlicker { get; private set; }
        public static bool ReduceMotion { get; private set; }

        public static float TextScale => TextScales[TextSize];

        /// <summary>1 normally, 0 when Reduce Camera Motion is on. Multiply shake, bob and sway by this.</summary>
        public static float MotionScale => ReduceMotion ? 0f : 1f;

        /// <summary>Raised only when a value actually changed.</summary>
        public static event Action Changed;

        /// <summary>Out-of-range text sizes are clamped to the nearest valid size.</summary>
        public static void Set(bool captionsEnabled, int textSize, bool reduceFlicker, bool reduceMotion)
        {
            textSize = Mathf.Clamp(textSize, SmallText, LargeText);
            bool changed = captionsEnabled != CaptionsEnabled || textSize != TextSize
                           || reduceFlicker != ReduceFlicker || reduceMotion != ReduceMotion;
            CaptionsEnabled = captionsEnabled;
            TextSize = textSize;
            ReduceFlicker = reduceFlicker;
            ReduceMotion = reduceMotion;
            if (changed) Changed?.Invoke();
        }

        public static void Reset()
        {
            Set(true, MediumText, false, false);
        }

        // Keeps state correct when Enter Play Mode runs without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => Reset();
    }
}
