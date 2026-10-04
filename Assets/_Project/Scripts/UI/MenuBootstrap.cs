using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Controls;
using NocturneAnnex.Save;
using NocturneAnnex.Settings;

namespace NocturneAnnex.UI
{
    /// <summary>
    /// Runtime glue for the main menu scene: loads settings, builds the models, binds the views and switches panels.
    /// Loading the game scene is left to whoever subscribes to the events.
    /// </summary>
    public class MenuBootstrap : MonoBehaviour
    {
        public MainMenuView MainMenu;
        public SettingsView Settings;
        public GameObject MainPanel;
        public GameObject SettingsPanel;
        public GameObject CreditsPanel;
        public Button CreditsBackButton;
        public GameObject NoticePanel;
        public Button NoticeOkButton;
        public Button CreditsNoticeButton;

        /// <summary>Overridable for tests and screenshot tools; default to files in persistentDataPath.</summary>
        public SaveStore SaveStore;
        public SettingsStore SettingsStore;

        public event Action ContinueRequested;
        public event Action NewGameRequested;
        public event Action QuitRequested;

        SettingsData _settings;
        string _settingsWarning;
        bool _noticeIsFirstRun;
        bool _wired;

        void Start()
        {
            if (_settings == null) Initialize();
        }

        public void Initialize()
        {
            SaveStore ??= new SaveStore(Path.Combine(Application.persistentDataPath, "save.json"));
            SettingsStore ??= new SettingsStore(Path.Combine(Application.persistentDataPath, "settings.json"), QualitySettings.names.Length);

            var loaded = SettingsStore.Load();
            _settings = loaded.Data;
            SettingsApplier.Apply(_settings.Clone(), InputRouter.Instance);

            var model = new MainMenuModel(() => SaveStore.Load(), () => SaveStore.ClearForNewGame());
            MainMenu.Bind(model);
            _settingsWarning = loaded.Warning;
            MainMenu.ExtraWarning = _settingsWarning;

            if (!_wired)
            {
                MainMenu.ContinueRequested += () => ContinueRequested?.Invoke();
                MainMenu.NewGameStarted += () => NewGameRequested?.Invoke();
                MainMenu.QuitRequested += () => QuitRequested?.Invoke();
                MainMenu.SettingsRequested += ShowSettings;
                MainMenu.CreditsRequested += ShowCredits;
                Settings.Closed += ShowMain;
                CreditsBackButton.onClick.AddListener(ShowMain);
                CreditsNoticeButton.onClick.AddListener(() => ShowNotice(firstRun: false));
                NoticeOkButton.onClick.AddListener(OnNoticeAcknowledged);
                _wired = true;
            }

            // The content notice comes before anything else until the player has seen it once.
            if (!_settings.ContentNoticeAccepted) ShowNotice(firstRun: true);
            else ShowMain();
        }

        public void ShowMain() => Show(MainPanel);
        public void ShowCredits() => Show(CreditsPanel);

        void ShowNotice(bool firstRun)
        {
            _noticeIsFirstRun = firstRun;
            Show(NoticePanel);
        }

        void OnNoticeAcknowledged()
        {
            if (!_noticeIsFirstRun)
            {
                ShowCredits();   // re-reading from the credits screen changes nothing
                return;
            }

            _settings.ContentNoticeAccepted = true;
            var result = SettingsStore.Write(_settings.Clone());
            if (!result.Ok)
                MainMenu.ExtraWarning = string.IsNullOrEmpty(_settingsWarning) ? result.Error : string.Join("\n", _settingsWarning, result.Error);
            ShowMain();
        }

        public void ShowSettings()
        {
            var model = new SettingsScreenModel(_settings, QualitySettings.names.Length,
                d =>
                {
                    var result = SettingsStore.Write(d);
                    if (result.Ok) _settings = d.Clone();
                    return result;
                },
                d => SettingsApplier.Apply(d, InputRouter.Instance));
            Settings.Bind(model, QualitySettings.names);
            Show(SettingsPanel);
        }

        void Show(GameObject panel)
        {
            MainPanel.SetActive(panel == MainPanel);
            SettingsPanel.SetActive(panel == SettingsPanel);
            CreditsPanel.SetActive(panel == CreditsPanel);
            NoticePanel.SetActive(panel == NoticePanel);
        }
    }
}
