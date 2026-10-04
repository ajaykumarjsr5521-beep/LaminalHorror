using System;
using UnityEngine;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Settings
{
    /// <summary>Player preferences. Plain data; Clamp keeps every value in a safe range.</summary>
    [Serializable]
    public class SettingsData
    {
        public const int CurrentVersion = 1;
        public const float MinSensitivity = 0.2f;
        public const float MaxSensitivity = 3f;

        // Defaults to 0 on purpose: a file without a Version must read as corrupt (SettingsStore stamps it on write).
        public int Version;

        public float LookSensitivity = 1f;
        public bool InvertY;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public bool CaptionsEnabled = true;
        public float TouchControlsScale = 1f;
        public bool StoryMode;

        /// <summary>0 small, 1 medium, 2 large (see Accessibility). Added after v1; older files load the default.</summary>
        public int TextSize = 1;
        public bool ReduceFlicker;
        public bool ReduceMotion;

        /// <summary>-1 means "use the project default quality level".</summary>
        public int QualityLevel = -1;

        public static SettingsData CreateDefault() => new SettingsData();

        public SettingsData Clone() => (SettingsData)MemberwiseClone();

        /// <summary>Copies all user-facing values in place, so UI bound to this object keeps working.</summary>
        public void CopyFrom(SettingsData o)
        {
            LookSensitivity = o.LookSensitivity;
            InvertY = o.InvertY;
            MasterVolume = o.MasterVolume;
            MusicVolume = o.MusicVolume;
            SfxVolume = o.SfxVolume;
            CaptionsEnabled = o.CaptionsEnabled;
            TouchControlsScale = o.TouchControlsScale;
            StoryMode = o.StoryMode;
            QualityLevel = o.QualityLevel;
            TextSize = o.TextSize;
            ReduceFlicker = o.ReduceFlicker;
            ReduceMotion = o.ReduceMotion;
        }

        /// <summary>True if every user-facing value matches. Version is ignored.</summary>
        public bool ValueEquals(SettingsData o) =>
            o != null
            && LookSensitivity == o.LookSensitivity && InvertY == o.InvertY
            && MasterVolume == o.MasterVolume && MusicVolume == o.MusicVolume && SfxVolume == o.SfxVolume
            && CaptionsEnabled == o.CaptionsEnabled && TouchControlsScale == o.TouchControlsScale
            && StoryMode == o.StoryMode && QualityLevel == o.QualityLevel
            && TextSize == o.TextSize && ReduceFlicker == o.ReduceFlicker && ReduceMotion == o.ReduceMotion;

        /// <summary>Forces all values into range. NaN and infinity fall back to the default value.</summary>
        public void Clamp(int qualityLevelCount)
        {
            var d = CreateDefault();
            LookSensitivity = Clamp(LookSensitivity, MinSensitivity, MaxSensitivity, d.LookSensitivity);
            MasterVolume = Clamp(MasterVolume, 0f, 1f, d.MasterVolume);
            MusicVolume = Clamp(MusicVolume, 0f, 1f, d.MusicVolume);
            SfxVolume = Clamp(SfxVolume, 0f, 1f, d.SfxVolume);
            TouchControlsScale = Clamp(TouchControlsScale, InputMath.MinControlsScale, InputMath.MaxControlsScale, d.TouchControlsScale);
            if (QualityLevel < -1 || QualityLevel >= qualityLevelCount) QualityLevel = -1;
            TextSize = Mathf.Clamp(TextSize, 0, 2);
        }

        static float Clamp(float v, float min, float max, float fallback) =>
            float.IsNaN(v) || float.IsInfinity(v) ? fallback : Mathf.Clamp(v, min, max);
    }
}
