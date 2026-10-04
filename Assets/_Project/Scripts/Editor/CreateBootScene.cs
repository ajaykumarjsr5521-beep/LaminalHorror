using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NocturneAnnex.Flow;

namespace NocturneAnnex.Editor
{
    /// <summary>Creates the Boot scene (a BootLoader that opens the main menu) and registers it in Build Settings.</summary>
    public static class CreateBootScene
    {
        const string Path = "Assets/_Project/Scenes/Boot.unity";

        [MenuItem("Build/Create Boot Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("BootLoader").AddComponent<BootLoader>();
            EditorSceneManager.SaveScene(scene, Path);
            ApplyBuildOrder();
        }

        /// <summary>
        /// Build Settings order: Boot (first, so the game starts there), MainMenu, Level_B1. The Greybox test scene stays
        /// listed but disabled so it never ships. Scenes that do not exist yet are skipped.
        /// </summary>
        [MenuItem("Build/Apply Build Scene Order")]
        public static void ApplyBuildOrder()
        {
            var order = new (string path, bool enabled)[]
            {
                (Path, true),
                (MenuSceneBuilder.ScenePath, true),
                (LevelB1Builder.ScenePath, true),
                ("Assets/_Project/Scenes/Greybox.unity", false),
            };
            EditorBuildSettings.scenes = order
                .Where(o => System.IO.File.Exists(o.path))
                .Select(o => new EditorBuildSettingsScene(o.path, o.enabled))
                .ToArray();
        }
    }
}
