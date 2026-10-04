using System;
using UnityEngine;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Flow
{
    /// <summary>
    /// Pauses by freezing time and audio, and restores exactly what it changed on resume.
    /// Pauses automatically when the app loses focus. The pause button (Escape, gamepad Start, touch button,
    /// and Android Back, which Unity maps to Escape) toggles it. Resuming after focus returns is always the player's choice.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        /// <summary>Overridable for tests; defaults to the scene InputRouter.</summary>
        public Func<PlayerInputState> InputProvider;

        public bool IsPaused { get; private set; }
        public event Action<bool> PausedChanged;

        float _timeScaleBeforePause = 1f;
        bool _audioPausedBefore;

        void Update()
        {
            var input = (InputProvider ?? DefaultProvider)();
            if (input.PausePressed) Toggle();
        }

        public void Toggle()
        {
            if (IsPaused) Resume(); else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;   // do not overwrite the remembered time scale with 0
            _timeScaleBeforePause = Time.timeScale;
            _audioPausedBefore = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            InputRouter.Instance?.ResetInput();
            IsPaused = true;
            PausedChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            Restore();
            InputRouter.Instance?.ResetInput();
            IsPaused = false;
            PausedChanged?.Invoke(false);
        }

        /// <summary>Called for focus changes. Losing focus pauses; regaining focus never auto-resumes.</summary>
        public void HandleApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Pause();
        }

        public void HandleApplicationPause(bool paused)
        {
            if (paused) Pause();
        }

        void OnApplicationFocus(bool hasFocus) => HandleApplicationFocus(hasFocus);
        void OnApplicationPause(bool paused) => HandleApplicationPause(paused);

        // Leaving the scene while paused must not leave the next scene frozen.
        void OnDisable()
        {
            if (!IsPaused) return;
            Restore();
            IsPaused = false;
        }

        void Restore()
        {
            Time.timeScale = _timeScaleBeforePause;
            AudioListener.pause = _audioPausedBefore;
        }

        static PlayerInputState DefaultProvider() =>
            InputRouter.Instance != null ? InputRouter.Instance.Current : default;
    }
}
