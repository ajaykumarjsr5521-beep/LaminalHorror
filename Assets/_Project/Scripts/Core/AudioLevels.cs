using System;
using UnityEngine;

namespace NocturneAnnex.Core
{
    /// <summary>The player's Music and SFX volume settings, shared so the audio director can follow them without referencing Settings.</summary>
    public static class AudioLevels
    {
        public const float DefaultMusic = 0.8f, DefaultSfx = 1f;

        public static float Music { get; private set; } = DefaultMusic;
        public static float Sfx { get; private set; } = DefaultSfx;

        /// <summary>Raised after the levels change.</summary>
        public static event Action Changed;

        public static void Set(float music, float sfx)
        {
            Music = Mathf.Clamp01(music);
            Sfx = Mathf.Clamp01(sfx);
            Changed?.Invoke();
        }

        public static void Reset() => Set(DefaultMusic, DefaultSfx);

        // Keeps state correct when Enter Play Mode runs without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad()
        {
            Music = DefaultMusic;
            Sfx = DefaultSfx;
        }
    }
}
