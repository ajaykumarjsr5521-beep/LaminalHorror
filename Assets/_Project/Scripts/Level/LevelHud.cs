using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Inventory;
using NocturneAnnex.UI;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// Connects the on-screen buttons and overlays of a level: pause menu actions, the journal button and the end card.
    /// Scene changes go through SceneLoader so tests can observe them instead of loading scenes.
    /// </summary>
    public class LevelHud : MonoBehaviour
    {
        public const string MenuSceneName = "MainMenu";

        public PauseController Pause;
        public PauseMenuView PauseMenu;
        public JournalView Journal;
        public KeypadView Keypad;
        public PlayerInventory Inventory;
        public Button JournalButton;
        public Button PauseButton;
        public GameObject EndCard;
        public Button EndMenuButton;
        public LevelBootstrap Level;

        ModalGate.Handle _endGate;

        /// <summary>Overridable for tests; defaults to SceneManager.LoadScene.</summary>
        public Action<string> SceneLoader = name => SceneManager.LoadScene(name);

        void Start()
        {
            // Runtime wiring: event subscriptions and models are not saved with the scene.
            Journal.Bind(Inventory);
            if (Level != null && Level.FinalLock != null) Keypad.Watch(Level.FinalLock);
            PauseMenu.Bind(Pause);
            PauseMenu.RestartRequested += RestartFromCheckpoint;
            PauseMenu.MainMenuRequested += GoToMenu;
            PauseMenu.QuitRequested += Quit;
            PauseButton.onClick.AddListener(() => { if (!ModalGate.IsOpen) Pause.Pause(); });
            JournalButton.onClick.AddListener(OpenJournal);
            EndMenuButton.onClick.AddListener(GoToMenu);
            EndCard.SetActive(false);
            if (Level != null) Level.LevelCompleted += ShowEndCard;
        }

        void OnDestroy()
        {
            _endGate?.Dispose();
            _endGate = null;
            if (Level != null) Level.LevelCompleted -= ShowEndCard;
        }

        public void OpenJournal()
        {
            // Never stack screens: a keypad, note or pause menu already owns the player's attention.
            if (ModalGate.IsOpen || Pause.IsPaused || Journal.IsOpen) return;
            Journal.Open();
        }

        public void ShowEndCard()
        {
            EndCard.SetActive(true);
            _endGate ??= ModalGate.Open();   // stops gameplay input behind the card
        }

        void RestartFromCheckpoint()
        {
            LevelLaunch.RequestContinue();
            SceneLoader(SceneManager.GetActiveScene().name);
        }

        void GoToMenu()
        {
            LevelLaunch.RequestNewGame();
            SceneLoader(MenuSceneName);
        }

        static void Quit() => Application.Quit();
    }
}
