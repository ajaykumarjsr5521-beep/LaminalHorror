using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NocturneAnnex.UI
{
    /// <summary>Binds MainMenuModel to buttons. Scene loading is done by whoever subscribes to the events.</summary>
    public class MainMenuView : MonoBehaviour
    {
        public Button ContinueButton;
        public Button NewGameButton;
        public Button SettingsButton;
        public Button CreditsButton;
        public Button QuitButton;
        public TMP_Text WarningText;
        public TMP_Text ErrorText;
        public ConfirmDialogView Confirm;

        public event Action ContinueRequested;
        public event Action NewGameStarted;
        public event Action SettingsRequested;
        public event Action CreditsRequested;
        public event Action QuitRequested;

        MainMenuModel _model;
        bool _wired;
        string _extraWarning;

        /// <summary>Additional warning shown with the save warning, e.g. an unreadable settings file.</summary>
        public string ExtraWarning
        {
            get => _extraWarning;
            set { _extraWarning = value; if (_model != null) Render(); }
        }

        /// <summary>Quit is offered on Windows only; mobile platforms leave the app through the OS.</summary>
        static bool QuitAvailable =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        public void Bind(MainMenuModel model)
        {
            _model = model;
            if (!_wired)
            {
                ContinueButton.onClick.AddListener(OnContinue);
                NewGameButton.onClick.AddListener(OnNewGame);
                SettingsButton.onClick.AddListener(() => SettingsRequested?.Invoke());
                CreditsButton.onClick.AddListener(() => CreditsRequested?.Invoke());
                QuitButton.onClick.AddListener(() => QuitRequested?.Invoke());
                _wired = true;
            }
            _model.Refresh();
            Render();
        }

        void OnEnable()
        {
            if (_model == null) return;
            _model.Refresh();
            Render();
        }

        void OnContinue()
        {
            if (_model.RequestContinue() == MenuOutcome.Continue) ContinueRequested?.Invoke();
            Render();
        }

        void OnNewGame()
        {
            var outcome = _model.RequestNewGame();
            if (outcome == MenuOutcome.NeedsConfirmation) Confirm.Show(OnConfirmed, () => { _model.CancelNewGame(); Render(); });
            else Handle(outcome);
        }

        void OnConfirmed() => Handle(_model.ConfirmNewGame());

        void Handle(MenuOutcome outcome)
        {
            if (outcome == MenuOutcome.StartNewGame) NewGameStarted?.Invoke();
            Render();
        }

        void Render()
        {
            ContinueButton.interactable = _model.ContinueAvailable;
            QuitButton.gameObject.SetActive(QuitAvailable);
            var warning = string.IsNullOrEmpty(_extraWarning) ? _model.Warning
                : string.IsNullOrEmpty(_model.Warning) ? _extraWarning
                : _model.Warning + "\n" + _extraWarning;
            SetMessage(WarningText, warning);
            SetMessage(ErrorText, _model.Error);
        }

        static void SetMessage(TMP_Text label, string message)
        {
            label.text = message ?? string.Empty;
            label.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}
