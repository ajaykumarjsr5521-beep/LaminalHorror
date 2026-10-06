using UnityEngine;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;

namespace NocturneAnnex.Settings
{
    /// <summary>
    /// Pushes settings into the live systems. Music and SFX volumes go to AudioLevels, which the audio
    /// director follows (ambience uses the SFX level).
    /// </summary>
    public static class SettingsApplier
    {
        public static void Apply(SettingsData settings, InputRouter router)
        {
            settings.Clamp(QualitySettings.names.Length);

            if (router != null)
            {
                router.LookSensitivity = settings.LookSensitivity;
                router.InvertY = settings.InvertY;
            }

            ControlsLayout.Scale = settings.TouchControlsScale;
            Accessibility.Set(settings.CaptionsEnabled, settings.TextSize, settings.ReduceFlicker, settings.ReduceMotion);
            AudioListener.volume = settings.MasterVolume;
            AudioLevels.Set(settings.MusicVolume, settings.SfxVolume);

            if (settings.QualityLevel >= 0)
                QualitySettings.SetQualityLevel(settings.QualityLevel, true);
        }
    }
}
