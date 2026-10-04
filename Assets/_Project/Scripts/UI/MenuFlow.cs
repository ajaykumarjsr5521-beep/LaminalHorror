using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using NocturneAnnex.Flow;

namespace NocturneAnnex.UI
{
    /// <summary>Sends the main menu's Continue and New Game choices to the level scene, and handles Quit.</summary>
    [RequireComponent(typeof(MenuBootstrap))]
    public class MenuFlow : MonoBehaviour
    {
        public const string LevelSceneName = "Level_B1";

        /// <summary>Overridable for tests; defaults to SceneManager.LoadScene.</summary>
        public Action<string> SceneLoader = name => SceneManager.LoadScene(name);

        MenuBootstrap _menu;

        void Awake()
        {
            _menu = GetComponent<MenuBootstrap>();
            _menu.ContinueRequested += OnContinue;
            _menu.NewGameRequested += OnNewGame;
            _menu.QuitRequested += OnQuit;
        }

        void OnDestroy()
        {
            if (_menu == null) return;
            _menu.ContinueRequested -= OnContinue;
            _menu.NewGameRequested -= OnNewGame;
            _menu.QuitRequested -= OnQuit;
        }

        void OnContinue()
        {
            LevelLaunch.RequestContinue();
            SceneLoader(LevelSceneName);
        }

        void OnNewGame()
        {
            LevelLaunch.RequestNewGame();
            SceneLoader(LevelSceneName);
        }

        static void OnQuit() => Application.Quit();
    }
}
