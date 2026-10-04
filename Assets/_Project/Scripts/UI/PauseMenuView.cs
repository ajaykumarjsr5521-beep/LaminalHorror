using System;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Flow;

namespace NocturneAnnex.UI
{
    /// <summary>Shows the pause panel while the PauseController is paused. Scene changes are left to subscribers.</summary>
    public class PauseMenuView : MonoBehaviour
    {
        public GameObject Panel;
        public Button ResumeButton;
        public Button RestartButton;
        public Button SettingsButton;
        public Button MainMenuButton;
        public Button QuitButton;

        public event Action RestartRequested;
        public event Action SettingsRequested;
        public event Action MainMenuRequested;
        public event Action QuitRequested;

        PauseController _pause;

        static bool QuitAvailable =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        public void Bind(PauseController pause)
        {
            if (_pause != null) _pause.PausedChanged -= OnPausedChanged;
            _pause = pause;
            _pause.PausedChanged += OnPausedChanged;

            ResumeButton.onClick.AddListener(() => _pause.Resume());
            RestartButton.onClick.AddListener(() => RestartRequested?.Invoke());
            SettingsButton.onClick.AddListener(() => SettingsRequested?.Invoke());
            MainMenuButton.onClick.AddListener(() => MainMenuRequested?.Invoke());
            QuitButton.onClick.AddListener(() => QuitRequested?.Invoke());
            QuitButton.gameObject.SetActive(QuitAvailable);
            Panel.SetActive(_pause.IsPaused);
        }

        void OnPausedChanged(bool paused) => Panel.SetActive(paused);

        void OnDestroy()
        {
            if (_pause != null) _pause.PausedChanged -= OnPausedChanged;
        }
    }
}
