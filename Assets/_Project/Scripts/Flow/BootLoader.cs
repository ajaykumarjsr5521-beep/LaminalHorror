using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NocturneAnnex.Flow
{
    /// <summary>Lives in the otherwise empty Boot scene and moves straight on to the main menu.</summary>
    public class BootLoader : MonoBehaviour
    {
        public const string MenuSceneName = "MainMenu";

        /// <summary>Overridable for tests; defaults to SceneManager.LoadScene.</summary>
        public Action<string> SceneLoader = name => SceneManager.LoadScene(name);

        void Start() => SceneLoader(MenuSceneName);
    }
}
